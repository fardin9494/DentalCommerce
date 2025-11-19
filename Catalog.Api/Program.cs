using Catalog.Api.Contracts;
using Catalog.Application.Brands;
using Catalog.Application.Categories;
using Catalog.Application.Common.Behaviors;
using Catalog.Application.Medias;
using Catalog.Application.Products;
using Catalog.Application.Stores;
using Catalog.Domain.Brands;
using Catalog.Infrastructure;
using Catalog.Infrastructure.Categories;
using Catalog.Infrastructure.Media;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<CatalogDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("CatalogDb")
             ?? throw new InvalidOperationException("Connection string 'CatalogDb' is not configured.");
    opt.UseSqlServer(cs, sql =>
    {
        sql.MigrationsHistoryTable("__EFMigrationsHistory", CatalogDbContext.DefaultSchema);
        sql.EnableRetryOnFailure();
    });
});

// CORS: single admin policy; dev vs production
var adminOrigin = builder.Configuration["Cors:AdminOrigin"]; // e.g. https://admin.yourdomain.com
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("admin", p =>
    {
        if (builder.Environment.IsDevelopment())
        {
            p.WithOrigins("http://localhost:5173")
             .AllowAnyHeader()
             .AllowAnyMethod()
             .AllowCredentials();
        }
        else if (!string.IsNullOrWhiteSpace(adminOrigin))
        {
            p.WithOrigins(adminOrigin)
             .AllowAnyHeader()
             .AllowAnyMethod()
             .AllowCredentials();
        }
    });
});

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.Load("Catalog.Application")));

// FluentValidation (DI) + MediatR Pipeline
builder.Services.AddValidatorsFromAssembly(Assembly.Load("Catalog.Application"));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
// 1) Options ???? ?????? ?????
builder.Services.Configure<ImageProcessingOptions>(builder.Configuration.GetSection("Media:Image"));

// 2) Storage ? Processor
builder.Services.AddScoped<IFileStorage, LocalFileStorage>();
builder.Services.AddScoped<IImageProcessor, ImageSharpProcessor>();
// ????? ?????? ????
builder.Services.AddScoped<ICategoryReadService, CategoryReadService>();
builder.Services.AddScoped<DbContext, CatalogDbContext>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Simple shared-password gate for admin APIs (/api/catalog/*).
// If Admin:PasswordHash is configured, the incoming bearer token
// is hashed with SHA-256 and compared to that hash. Otherwise we
// fall back to plain Admin:Password comparison. This is temporary
// until full auth/roles are implemented.
var adminPassword = app.Configuration["Admin:Password"];
var adminPasswordHashHex = app.Configuration["Admin:PasswordHash"];
byte[]? adminPasswordHash = null;
if (!string.IsNullOrWhiteSpace(adminPasswordHashHex))
{
    adminPasswordHash = Convert.FromHexString(adminPasswordHashHex);
}

if (!string.IsNullOrWhiteSpace(adminPassword) || adminPasswordHash is not null)
{
    app.Use(async (ctx, next) =>
    {
        // Allow CORS preflight without auth
        if (HttpMethods.IsOptions(ctx.Request.Method))
        {
            await next();
            return;
        }

        if (ctx.Request.Path.StartsWithSegments("/api/catalog"))
        {
            if (!ctx.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsync("Unauthorized");
                return;
            }

            const string prefix = "Bearer ";
            var auth = authHeader.ToString();
            if (!auth.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsync("Unauthorized");
                return;
            }

            var token = auth[prefix.Length..].Trim();
            var ok = false;

            if (adminPasswordHash is not null)
            {
                var bytes = Encoding.UTF8.GetBytes(token);
                var hash = SHA256.HashData(bytes);
                ok = CryptographicOperations.FixedTimeEquals(hash, adminPasswordHash);
            }
            else if (!string.IsNullOrWhiteSpace(adminPassword))
            {
                ok = string.Equals(token, adminPassword, StringComparison.Ordinal);
            }

            if (!ok)
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsync("Unauthorized");
                return;
            }
        }

        await next();
    });
}

var env = app.Services.GetRequiredService<IHostEnvironment>();
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("GlobalException");

app.UseCors("admin");
app.Use(async (ctx, next) =>
{
    try
    {
        await next();
    }
    // 400: FluentValidation
    catch (ValidationException ex)
    {
        logger.LogWarning(ex, "Validation failed for request {Path}", ctx.Request.Path);

        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        await Results.ValidationProblem(errors, statusCode: StatusCodes.Status400BadRequest)
            .ExecuteAsync(ctx);
    }

    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "Bad request (InvalidOperation) for {Path}", ctx.Request.Path);
        var detail = env.IsDevelopment() ? ex.Message : null;

        await Results.Problem(title: "Bad Request", detail: detail, statusCode: StatusCodes.Status400BadRequest)
            .ExecuteAsync(ctx);
    }
    catch (SixLabors.ImageSharp.UnknownImageFormatException ex)
    {
        logger.LogWarning(ex, "Unsupported image format for {Path}", ctx.Request.Path);
        var detail = env.IsDevelopment() ? ex.Message : null;

        await Results.Problem(title: "Unsupported image format", detail: detail, statusCode: StatusCodes.Status400BadRequest)
            .ExecuteAsync(ctx);
    }
    catch (DbUpdateException ex) when (
        ex.InnerException is Microsoft.Data.SqlClient.SqlException sql &&
        (sql.Number == 2601 || sql.Number == 2627)
    )
    {
        var sqllog = (Microsoft.Data.SqlClient.SqlException)ex.InnerException!;
        logger.LogWarning(ex, "Duplicate key error ({SqlNumber}) for {Path}", sqllog.Number, ctx.Request.Path);
        var detail = env.IsDevelopment() ? sqllog.Message : null;

        await Results.Problem(title: "Duplicate key", detail: detail, statusCode: StatusCodes.Status409Conflict)
            .ExecuteAsync(ctx);
    }
    // 500: ????
    catch (Exception ex)
    {
        logger.LogError(ex, "Unhandled exception for request {Path}", ctx.Request.Path);
        var detail = env.IsDevelopment() ? ex.Message : null;

        await Results.Problem(title: "Internal Server Error", detail: detail, statusCode: StatusCodes.Status500InternalServerError)
            .ExecuteAsync(ctx);
    }
});
var mediaRoot = builder.Configuration["Media:Root"] ?? "./_media";

var mediaRootPath = Path.GetFullPath(mediaRoot);
Directory.CreateDirectory(mediaRootPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(mediaRootPath),
    RequestPath = "/media"
});
app.UseSwagger();
app.UseSwaggerUI();

// Enable CORS for development UI
app.UseCors("dev");

var catalog = app.MapGroup("/api/catalog");
catalog.MapGet("/auth/check", () => Results.NoContent());

catalog.MapPost("/products", async (CreateProductCommand cmd, IMediator mediator) =>
{
    var id = await mediator.Send(cmd);
    return Results.Created($"/api/catalog/products/{id}", new { id });
});
catalog.MapPost("/products/{id:guid}/activate", async (Guid id, IMediator mediator) =>
{
    await mediator.Send(new ActivateProductCommand(id));
    return Results.NoContent();
});

catalog.MapPost("/products/{id:guid}/hide", async (Guid id, IMediator mediator) =>
{
    await mediator.Send(new HideProductCommand(id));
    return Results.NoContent();
});

catalog.MapPost("/products/{id:guid}/images", async (Guid id, AddProductImageCommand body, IMediator mediator) =>
{
    // ???????: ProductId ?? route ??? ???? Command
    var cmd = body with { ProductId = id };
    var imageId = await mediator.Send(cmd);
    return Results.Created($"/api/catalog/products/{id}/images/{imageId}", new { imageId });
});

catalog.MapPost("/products/{id:guid}/variants", async (Guid id, UpsertVariantCommand body, IMediator mediator) =>
{
    var cmd = body with { ProductId = id };
    var variantId = await mediator.Send(cmd);
    return Results.Created($"/api/catalog/products/{id}/variants/{variantId}", new { variantId });
});

// Update variant by id
catalog.MapPost("/products/{id:guid}/variants/{variantId:guid}", async (Guid id, Guid variantId, UpdateVariantBody body, IMediator mediator) =>
{
    await mediator.Send(new UpdateVariantCommand(
        ProductId: id,
        VariantId: variantId,
        VariantValue: body.VariantValue,
        Sku: body.Sku,
        IsActive: body.IsActive
    ));
    return Results.NoContent();
});

app.MapDelete("/api/catalog/products/{id:guid}/variants/{variantId:guid}", async (Guid id, Guid variantId, IMediator mediator) =>
{
    await mediator.Send(new DeleteVariantCommand(id, variantId));
    return Results.NoContent();
});
catalog.MapPost("/products/{id:guid}/variation", async (Guid id, SetVariationCommand body, IMediator mediator) =>
{
    var cmd = body with { ProductId = id };
    await mediator.Send(cmd);
    return Results.NoContent();
});

catalog.MapPost("/products/{id:guid}/properties", async (Guid id, UpsertPropertyCommand body, IMediator mediator) =>
{
    var cmd = body with { ProductId = id };
    var propertyId = await mediator.Send(cmd);
    return Results.Created($"/api/catalog/products/{id}/properties/{propertyId}", new { propertyId });
});
catalog.MapDelete("/products/{pid:guid}/properties/{propId:guid}", async (Guid pid, Guid propId, IMediator m) =>
{
    await m.Send(new DeleteProductPropertyCommand(pid, propId));
    return Results.NoContent();
});

catalog.MapPost("/products/{id:guid}/stores", async (Guid id, UpsertProductStoreCommand body, IMediator mediator) =>
{
    var cmd = body with { ProductId = id };
    await mediator.Send(cmd);
    return Results.NoContent();
});

catalog.MapDelete("/products/{id:guid}/stores/{storeId:guid}", async (Guid id, Guid storeId, IMediator mediator) =>
{
    await mediator.Send(new DeleteProductStoreCommand(id, storeId));
    return Results.NoContent();
});

catalog.MapPost("/products/{id:guid}/stores/remove", async (Guid id, RemoveProductStoreDto body, IMediator mediator) =>
{
    await mediator.Send(new DeleteProductStoreCommand(id, body.StoreId));
    return Results.NoContent();
});

catalog.MapPost("/products/{id:guid}/basics", async (Guid id, UpdateProductBasicsCommand body, IMediator m) =>
{
    await m.Send(body with { ProductId = id });
    return Results.NoContent();
});

catalog.MapPost("/products/{id:guid}/categories", async (Guid id, SetProductCategoriesCommand body, IMediator m) =>
{
    await m.Send(body with { ProductId = id });
    return Results.NoContent();
});

catalog.MapPost("/products/{id:guid}/categories/primary", async (Guid id, [FromBody] Guid categoryId, IMediator m) =>
{
    await m.Send(new SetPrimaryCategoryCommand(id, categoryId));
    return Results.NoContent();
});
catalog.MapPost("/products/{id:guid}/description",
	async (Guid id, SetProductDescriptionDto body, IMediator m) =>
	{
		await m.Send(new SetProductDescriptionCommand(id, body.ContentHtml));
		return Results.NoContent();
	});
catalog.MapPost("/products/{id:guid}/seo", async (Guid id, UpsertProductSeoCommand body, IMediator mediator) =>
{
    var cmd = body with { ProductId = id };
    await mediator.Send(cmd);
    return Results.NoContent();
});

app.MapGet("/api/catalog/products/{id:guid}", async (Guid id, IMediator mediator) =>
{
    var dto = await mediator.Send(new GetProductByIdQuery(id));
    return dto is null ? Results.NotFound() : Results.Ok(dto);
});
catalog.MapGet("/products", async (
    int page, int pageSize, string? search, Guid? brandId, Guid? categoryId, Guid? storeId, bool? visibleInStore, string? sort,
    IMediator mediator) =>
{
    page = page <= 0 ? 1 : page;
    pageSize = pageSize <= 0 ? 20 : pageSize;
    var result = await mediator.Send(new ListProductsQuery(page, pageSize, search, brandId, categoryId, storeId, visibleInStore, sort));
    return Results.Ok(result);
});
catalog.MapPost("/categories", async (CreateCategoryCommand cmd, IMediator mediator) =>
{
    var id = await mediator.Send(cmd);
    return Results.Created($"/api/catalog/categories/{id}", new { id });
});
catalog.MapPost("/categories/{id:guid}/rename",
    async (Guid id, RenameCategoryCommand body, IMediator m) =>
    {
        var cmd = body with { CategoryId = id };
        await m.Send(cmd);
        return Results.NoContent();
    });
catalog.MapPost("/categories/{id:guid}/move",
    async (Guid id, MoveCategoryCommand body, IMediator m) =>
    {
        var cmd = body with { CategoryId = id };
        await m.Send(cmd);
        return Results.NoContent();
    });
catalog.MapGet("/categories/tree", async (IMediator m) =>
{
    var nodes = await m.Send(new GetCategoryTreeQuery());
    return Results.Ok(nodes.OrderBy(n => n.ParentId.HasValue).ThenBy(n => n.Name));
});

catalog.MapGet("/categories/leaves", async (IMediator m) =>
{
    var items = await m.Send(new ListLeafCategoriesQuery());
    return Results.Ok(items);
});
catalog.MapGet("/categories/leaves/with-products", async (IMediator m) =>
{
    var items = await m.Send(new ListLeafCategoriesWithProductsQuery());
    return Results.Ok(items);
});

// Category flags: minimal payload to indicate product linkage per category
catalog.MapGet("/categories/flags", async (IMediator m) =>
{
    var flags = await m.Send(new ListCategoryFlagsQuery());
    return Results.Ok(flags);
});
catalog.MapPost("/countries", async (CreateCountryCommand cmd, IMediator m) =>
{
    var code2 = await m.Send(cmd);
    return Results.Created($"/api/catalog/countries/{code2}", new { code2 });
});

// ???? ?????? (???????: ?????)
catalog.MapGet("/countries", async (string? search, IMediator m) =>
{
    var list = await m.Send(new ListCountriesQuery(search));
    return Results.Ok(list);
});

catalog.MapPost("/countries/{code2}", async (string code2, UpdateCountryCommand body, IMediator m) =>
{
    await m.Send(body with { Code2 = code2 });
    return Results.NoContent();
});

catalog.MapDelete("/countries/{code2}", async (string code2, IMediator m) =>
{
    await m.Send(new DeleteCountryCommand(code2));
    return Results.NoContent();
});
// ????? ????
catalog.MapPost("/brands", async (CreateBrandCommand cmd, IMediator m) =>
{
    var id = await m.Send(cmd);
    return Results.Created($"/api/catalog/brands/{id}", new { id });
});

// ???? ?????? ?? ?????
catalog.MapGet("/brands", async (string? search, BrandStatus? status, IMediator m) =>
{
    var list = await m.Send(new ListBrandsQuery(search, status));
    return Results.Ok(list);
});

// ?????? ????
catalog.MapGet("/brands/{id:guid}", async (Guid id, IMediator m) =>
{
    var dto = await m.Send(new GetBrandByIdQuery(id));
    return dto is null ? Results.NotFound() : Results.Ok(dto);
});

catalog.MapPut("/brands/{id:guid}", async (Guid id, UpdateBrandCommand body, IMediator m) =>
{
    await m.Send(body with { BrandId = id });
    return Results.NoContent();
});

catalog.MapPost("/brands/{id:guid}/logo", async (Guid id, [FromForm] UploadBrandLogoForm form, IMediator m) =>
{
    if (form.file is null || form.file.Length == 0)
        return Results.BadRequest("file is required.");

    await using var stream = form.file.OpenReadStream();
    var dto = await m.Send(new UploadBrandLogoCommand(
        BrandId: id,
        FileName: form.file.FileName,
        ContentType: form.file.ContentType ?? "application/octet-stream",
        Content: stream
    ));

    return Results.Ok(dto);
})
.Accepts<UploadBrandLogoForm>("multipart/form-data")
.DisableAntiforgery()
.Produces<BrandLogoDto>(StatusCodes.Status200OK)
.WithName("UploadBrandLogo");

// ????? ??? ????
catalog.MapPost("/brands/{id:guid}/rename", async (Guid id, RenameBrandCommand body, IMediator m) =>
{
    await m.Send(body with { BrandId = id });
    return Results.NoContent();
});

// ????? ??????? ???? (?????/???/????/??????)
catalog.MapPost("/brands/{id:guid}/profile", async (Guid id, SetBrandProfileCommand body, IMediator m) =>
{
    await m.Send(body with { BrandId = id });
    return Results.NoContent();
});

// ????? ????? ???? (Active/Inactive/Deprecated)
catalog.MapPost("/brands/{id:guid}/status", async (Guid id, SetBrandStatusCommand body, IMediator m) =>
{
    await m.Send(body with { BrandId = id });
    return Results.NoContent();
});

// ??? ???? (??? ?? ?????? ??????? ???? ????)
catalog.MapDelete("/brands/{id:guid}", async (Guid id, IMediator m) =>
{
    await m.Send(new DeleteBrandCommand(id));
    return Results.NoContent();
});
// ?????? ??? ??????
catalog.MapPost("/brands/{id:guid}/aliases", async (Guid id, AddBrandAliasCommand body, IMediator m) =>
{
    var aliasId = await m.Send(body with { BrandId = id });
    return Results.Created($"/api/catalog/brands/{id}/aliases/{aliasId}", new { id = aliasId });
});

// ???? ??????? ?????? ????
catalog.MapGet("/brands/{id:guid}/aliases", async (Guid id, IMediator m) =>
{
    var list = await m.Send(new ListBrandAliasesQuery(id));
    return Results.Ok(list);
});

// ??? ??? ??????
catalog.MapDelete("/brands/aliases/{aliasId:guid}", async (Guid aliasId, IMediator m) =>
{
    await m.Send(new RemoveBrandAliasCommand(aliasId));
    return Results.NoContent();
});


catalog.MapPost("/stores", async (CreateStoreCommand cmd, IMediator m) =>
{
    var id = await m.Send(cmd);
    return Results.Created($"/api/catalog/stores/{id}", new { id });
});


catalog.MapGet("/stores", async (string? search, IMediator m) =>
{
    var list = await m.Send(new ListStoresQuery(search));
    return Results.Ok(list);
});


catalog.MapGet("/stores/{id:guid}", async (Guid id, IMediator m) =>
{
    var dto = await m.Send(new GetStoreByIdQuery(id));
    return dto is null ? Results.NotFound() : Results.Ok(dto);
});


catalog.MapPost("/stores/{id:guid}/rename", async (Guid id, RenameStoreCommand body, IMediator m) =>
{
    await m.Send(body with { StoreId = id });
    return Results.NoContent();
});

catalog.MapPost("/stores/{id:guid}/domain", async (Guid id, SetStoreDomainCommand body, IMediator m) =>
{
    await m.Send(body with { StoreId = id });
    return Results.NoContent();
});
catalog.MapPost("/products/{id:guid}/images/upload",
        async (Guid id, [FromForm] UploadProductImageForm form, IMediator m) =>
        {
            if (form.file is null || form.file.Length == 0)
                return Results.BadRequest("file is required.");

            await using var s = form.file.OpenReadStream();
            var imageId = await m.Send(new UploadProductImageCommand(
                ProductId: id,
                FileName: form.file.FileName,
                ContentType: form.file.ContentType ?? "application/octet-stream",
                Content: s,
                Alt: form.alt
            ));

            return Results.Created($"/api/catalog/products/{id}/images/{imageId}", new { id = imageId });
        })
// ??? ?????? ?????????? ??? Swagger ?? ??????? ??????:
    .Accepts<UploadProductImageForm>("multipart/form-data")
    .WithMetadata(new RequestSizeLimitAttribute(10 * 1024 * 1024))
    .Produces(StatusCodes.Status201Created)
    .DisableAntiforgery()
    .WithName("UploadProductImage")
    .WithOpenApi(op =>
    {
        op.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content =
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties =
                        {
                            ["file"] = new OpenApiSchema { Type = "string", Format = "binary" },
                            ["alt"]  = new OpenApiSchema { Type = "string" }
                        },
                        Required = new HashSet<string> { "file" }
                    }
                }
            }
        };
        return op;
    });
// Set main
catalog.MapPost("/products/{pid:guid}/images/{imgId:guid}/main", async (Guid pid, Guid imgId, IMediator m) =>
{
    await m.Send(new SetMainImageCommand(pid, imgId));
    return Results.NoContent();
});

// Reorder
catalog.MapPost("/products/{pid:guid}/images/reorder", async (Guid pid, Guid[] orderedIds, IMediator m) =>
{
    await m.Send(new ReorderProductImagesCommand(pid, orderedIds));
    return Results.NoContent();
});

// Delete
catalog.MapDelete("/products/{pid:guid}/images/{imgId:guid}", async (Guid pid, Guid imgId, IMediator m) =>
{
    await m.Send(new DeleteProductImageCommand(pid, imgId));
    return Results.NoContent();
});

catalog.MapGet("/products/properties/keys", async (int? top, IMediator m) =>
{
    var list = await m.Send(new ListPropertyKeysQuery(top ?? 20));
    return Results.Ok(list);
});

catalog.MapGet("/products/properties/{key}/values", async (string key, int? top, IMediator m) =>
{
    var list = await m.Send(new ListPropertyValuesQuery(key, top ?? 20));
    return Results.Ok(list);
});

catalog.MapGet("/products/variants/values", async (int? top, IMediator m) =>
{
    var list = await m.Send(new ListVariantValuesQuery(top ?? 20));
    return Results.Ok(list);
});

catalog.MapGet("/products/variants/recent", async (int? top, IMediator m) =>
{
    var dto = await m.Send(new ListRecentVariantsQuery(top ?? 10));
    return Results.Ok(dto);
});

catalog.MapGet("/admin/products", async (
    HttpResponse response,
    int page,
    int pageSize,
    string? search,
    Guid? brandId,
    Guid? categoryId,
    Guid? storeId,
    bool? visibleInStore,
    string? sort,
    IMediator m) =>
{
    var res = await m.Send(new ListProductsQuery(
        Page: page == 0 ? 1 : page,
        PageSize: pageSize == 0 ? 20 : pageSize,
        Search: search,
        BrandId: brandId,
        CategoryId: categoryId,
        StoreId: storeId,
        VisibleInStore: visibleInStore,
        Sort: sort
    ));

    // ????? ?? ??? response ?? ??
    response.Headers.Append("X-Total", res.Total.ToString());
    response.Headers.Append("X-Page", res.Page.ToString());
    response.Headers.Append("X-Page-Size", res.PageSize.ToString());
    response.Headers.Append("X-Total-Pages",
        Math.Ceiling((double)res.Total / Math.Max(1, res.PageSize)).ToString());

    return Results.Ok(res); // ?? TypedResults.Ok(res)
});


app.Run();

