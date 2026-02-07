using Catalog.Api.Contracts;
using Catalog.Application.Brands;
using Catalog.Application.Categories;
using Catalog.Application.Common.Behaviors;
using Catalog.Application.Medias;
using Catalog.Application.Products;
using Catalog.Application.Stores;
using Catalog.Api.Permissions;
using Catalog.Domain.Brands;
using Catalog.Infrastructure;
using Catalog.Infrastructure.Categories;
using Catalog.Infrastructure.Media;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

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
const string AdminCorsPolicy = "admin";
var adminOrigin = builder.Configuration["Cors:AdminOrigin"]; // e.g. https://admin.yourdomain.com
builder.Services.AddCors(opt =>
{
    opt.AddPolicy(AdminCorsPolicy, p =>
    {
        if (builder.Environment.IsDevelopment())
        {
            p.WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173",
                "http://localhost:5174",
                "https://localhost:5174",
                "http://localhost:5175",
                "https://localhost:5175"
            )
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

var identityApiUrl = builder.Configuration["IdentityApiUrl"]
    ?? throw new InvalidOperationException("IdentityApiUrl is not configured in appsettings.json");

builder.Services.AddHttpClient("IdentityApi", client =>
{
    client.BaseAddress = new Uri(identityApiUrl);
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICatalogPermissionChecker, CatalogPermissionChecker>();

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is not configured in appsettings.json");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is not configured in appsettings.json");
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Jwt:SigningKey is not configured in appsettings.json");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey));
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromSeconds(15)
        };
    });

builder.Services.AddAuthorization();

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

// Run CORS before the admin gate so even 401 responses carry the headers
app.UseCors(AdminCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

var env = app.Services.GetRequiredService<IHostEnvironment>();
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("GlobalException");

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

var catalog = app.MapGroup("/api/catalog")
    .RequireAuthorization()
    .DisableAntiforgery();
catalog.MapGet("/auth/check", () => Results.NoContent());

catalog.MapPost("/products", async (CreateProductCommand cmd, IMediator mediator) =>
{
    var id = await mediator.Send(cmd);
    return Results.Created($"/api/catalog/products/{id}", new { id });
}).RequireCatalogPermission(CatalogPermissionKeys.ProductsCreate);
catalog.MapPost("/products/{id:guid}/activate", async (Guid id, IMediator mediator) =>
{
    await mediator.Send(new ActivateProductCommand(id));
    return Results.NoContent();
}).RequireCatalogPermission(CatalogPermissionKeys.ProductsActivate);

catalog.MapPost("/products/{id:guid}/hide", async (Guid id, IMediator mediator) =>
{
    await mediator.Send(new HideProductCommand(id));
    return Results.NoContent();
}).RequireCatalogPermission(CatalogPermissionKeys.ProductsHide);

catalog.MapPost("/products/{id:guid}/images", async (Guid id, AddProductImageCommand body, IMediator mediator) =>
{
    // ???????: ProductId ?? route ??? ???? Command
    var cmd = body with { ProductId = id };
    var imageId = await mediator.Send(cmd);
    return Results.Created($"/api/catalog/products/{id}/images/{imageId}", new { imageId });
}).RequireCatalogPermission(CatalogPermissionKeys.ProductsImagesUpload);

catalog.MapPost("/products/{id:guid}/variants", async (Guid id, UpsertVariantCommand body, IMediator mediator) =>
{
    var cmd = body with { ProductId = id };
    var variantId = await mediator.Send(cmd);
    return Results.Created($"/api/catalog/products/{id}/variants/{variantId}", new { variantId });
}).RequireCatalogPermission(CatalogPermissionKeys.ProductsVariantsCreate);

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
}).RequireCatalogPermission(CatalogPermissionKeys.ProductsVariantsEdit);

catalog.MapDelete("/products/{id:guid}/variants/{variantId:guid}", async (Guid id, Guid variantId, IMediator mediator) =>
{
    await mediator.Send(new DeleteVariantCommand(id, variantId));
    return Results.NoContent();
}).RequireCatalogPermission(CatalogPermissionKeys.ProductsVariantsDelete);
catalog.MapPost("/products/{id:guid}/variation", async (Guid id, SetVariationCommand body, IMediator mediator) =>
{
    var cmd = body with { ProductId = id };
    await mediator.Send(cmd);
    return Results.NoContent();
}).RequireCatalogPermission(CatalogPermissionKeys.ProductsVariationEdit);

catalog.MapPost("/products/{id:guid}/properties", async (Guid id, UpsertPropertyCommand body, IMediator mediator) =>
{
    var cmd = body with { ProductId = id };
    var propertyId = await mediator.Send(cmd);
    return Results.Created($"/api/catalog/products/{id}/properties/{propertyId}", new { propertyId });
}).RequireCatalogPermission(CatalogPermissionKeys.ProductsPropertiesCreate);
catalog.MapDelete("/products/{pid:guid}/properties/{propId:guid}", async (Guid pid, Guid propId, IMediator m) =>
{
    await m.Send(new DeleteProductPropertyCommand(pid, propId));
    return Results.NoContent();
}).RequireCatalogPermission(CatalogPermissionKeys.ProductsPropertiesDelete);

catalog.MapPost("/products/{id:guid}/stores", async (Guid id, UpsertProductStoreCommand body, IMediator mediator) =>
{
    var cmd = body with { ProductId = id };
    await mediator.Send(cmd);
    return Results.NoContent();
}).RequireCatalogPermission(CatalogPermissionKeys.ProductsStoresManage);

catalog.MapDelete("/products/{id:guid}/stores/{storeId:guid}", async (Guid id, Guid storeId, IMediator mediator) =>
{
    await mediator.Send(new DeleteProductStoreCommand(id, storeId));
    return Results.NoContent();
}).RequireCatalogPermission(CatalogPermissionKeys.ProductsStoresManage);

catalog.MapPost("/products/{id:guid}/stores/remove", async (Guid id, RemoveProductStoreDto body, IMediator mediator) =>
{
    await mediator.Send(new DeleteProductStoreCommand(id, body.StoreId));
    return Results.NoContent();
}).RequireCatalogPermission(CatalogPermissionKeys.ProductsStoresManage);

catalog.MapPost("/products/{id:guid}/basics", async (Guid id, UpdateProductBasicsCommand body, IMediator m) =>
{
    await m.Send(body with { ProductId = id });
    return Results.NoContent();
}).RequireCatalogPermission(CatalogPermissionKeys.ProductsBasicsEdit);

catalog.MapPost("/products/{id:guid}/categories", async (Guid id, SetProductCategoriesCommand body, IMediator m) =>
{
    await m.Send(body with { ProductId = id });
    return Results.NoContent();
}).RequireCatalogPermission(CatalogPermissionKeys.ProductsCategoriesEdit);

catalog.MapPost("/products/{id:guid}/categories/primary", async (Guid id, [FromBody] Guid categoryId, IMediator m) =>
{
    await m.Send(new SetPrimaryCategoryCommand(id, categoryId));
    return Results.NoContent();
}).RequireCatalogPermission(CatalogPermissionKeys.ProductsCategoriesPrimaryEdit);
catalog.MapPost("/products/{id:guid}/description",
    async (Guid id, SetProductDescriptionDto body, IMediator m) =>
    {
        await m.Send(new SetProductDescriptionCommand(id, body.ContentHtml));
        return Results.NoContent();
    }).RequireCatalogPermission(CatalogPermissionKeys.ProductsDescriptionEdit);
catalog.MapPost("/products/{id:guid}/seo", async (Guid id, UpsertProductSeoCommand body, IMediator mediator) =>
{
    var cmd = body with { ProductId = id };
    await mediator.Send(cmd);
    return Results.NoContent();
}).RequireCatalogPermission(CatalogPermissionKeys.ProductsSeoEdit);

catalog.MapGet("/products/{id:guid}", async (Guid id, IMediator mediator) =>
{
    var dto = await mediator.Send(new GetProductByIdQuery(id));
    return dto is null ? Results.NotFound() : Results.Ok(dto);
}).RequireCatalogPermission(CatalogPermissionKeys.ProductsView);

catalog.MapGet("/products/by-sku", async (string sku, IMediator mediator) =>
{
    var dto = await mediator.Send(new ResolveProductBySkuQuery(sku));
    return dto is null ? Results.NotFound() : Results.Ok(dto);
}).RequireCatalogPermission(CatalogPermissionKeys.ProductsResolveBySku);

// Validation endpoint for Inventory service
catalog.MapGet("/products/{id:guid}/validate", async (Guid id, Guid? variantId, IMediator mediator) =>
{
    var exists = await mediator.Send(new ValidateProductExistsQuery(id, variantId));
    return exists ? Results.Ok(new { exists = true }) : Results.NotFound(new { exists = false });
}).RequireCatalogPermission(CatalogPermissionKeys.ProductsValidate);
catalog.MapGet("/products", async (
    int page, int pageSize, string? search, Guid? brandId, Guid? categoryId, Guid? storeId, bool? visibleInStore, string? sort,
    IMediator mediator) =>
{
    page = page <= 0 ? 1 : page;
    pageSize = pageSize <= 0 ? 20 : pageSize;
    var result = await mediator.Send(new ListProductsQuery(page, pageSize, search, brandId, categoryId, storeId, visibleInStore, sort));
    return Results.Ok(result);
}).RequireCatalogPermission(CatalogPermissionKeys.ProductsView);
catalog.MapPost("/categories", async (CreateCategoryCommand cmd, IMediator mediator) =>
{
    var id = await mediator.Send(cmd);
    return Results.Created($"/api/catalog/categories/{id}", new { id });
}).RequireCatalogPermission(CatalogPermissionKeys.CategoriesCreate);
catalog.MapPost("/categories/{id:guid}/rename",
    async (Guid id, RenameCategoryCommand body, IMediator m) =>
    {
        var cmd = body with { CategoryId = id };
        await m.Send(cmd);
        return Results.NoContent();
    }).RequireCatalogPermission(CatalogPermissionKeys.CategoriesRename);
catalog.MapPost("/categories/{id:guid}/move",
    async (Guid id, MoveCategoryCommand body, IMediator m) =>
    {
        var cmd = body with { CategoryId = id };
        await m.Send(cmd);
        return Results.NoContent();
    }).RequireCatalogPermission(CatalogPermissionKeys.CategoriesMove);
catalog.MapGet("/categories/tree", async (IMediator m) =>
{
    var nodes = await m.Send(new GetCategoryTreeQuery());
    return Results.Ok(nodes.OrderBy(n => n.ParentId.HasValue).ThenBy(n => n.Name));
}).RequireCatalogPermission(CatalogPermissionKeys.CategoriesTreeView);

catalog.MapGet("/categories/leaves", async (IMediator m) =>
{
    var items = await m.Send(new ListLeafCategoriesQuery());
    return Results.Ok(items);
}).RequireCatalogPermission(CatalogPermissionKeys.CategoriesLeavesView);
catalog.MapGet("/categories/leaves/with-products", async (IMediator m) =>
{
    var items = await m.Send(new ListLeafCategoriesWithProductsQuery());
    return Results.Ok(items);
}).RequireCatalogPermission(CatalogPermissionKeys.CategoriesLeavesWithProductsView);

// Category flags: minimal payload to indicate product linkage per category
catalog.MapGet("/categories/flags", async (IMediator m) =>
{
    var flags = await m.Send(new ListCategoryFlagsQuery());
    return Results.Ok(flags);
}).RequireCatalogPermission(CatalogPermissionKeys.CategoriesFlagsView);
catalog.MapPost("/countries", async (CreateCountryCommand cmd, IMediator m) =>
{
    var code2 = await m.Send(cmd);
    return Results.Created($"/api/catalog/countries/{code2}", new { code2 });
}).RequireCatalogPermission(CatalogPermissionKeys.CountriesCreate);

// ???? ?????? (???????: ?????)
catalog.MapGet("/countries", async (string? search, IMediator m) =>
{
    var list = await m.Send(new ListCountriesQuery(search));
    return Results.Ok(list);
}).RequireCatalogPermission(CatalogPermissionKeys.CountriesView);

catalog.MapPost("/countries/{code2}", async (string code2, UpdateCountryCommand body, IMediator m) =>
{
    await m.Send(body with { Code2 = code2 });
    return Results.NoContent();
}).RequireCatalogPermission(CatalogPermissionKeys.CountriesEdit);

catalog.MapDelete("/countries/{code2}", async (string code2, IMediator m) =>
{
    await m.Send(new DeleteCountryCommand(code2));
    return Results.NoContent();
}).RequireCatalogPermission(CatalogPermissionKeys.CountriesDelete);
// ????? ????
catalog.MapPost("/brands", async (CreateBrandCommand cmd, IMediator m) =>
{
    var id = await m.Send(cmd);
    return Results.Created($"/api/catalog/brands/{id}", new { id });
}).RequireCatalogPermission(CatalogPermissionKeys.BrandsCreate);

// ???? ?????? ?? ?????
catalog.MapGet("/brands", async (string? search, BrandStatus? status, IMediator m) =>
{
    var list = await m.Send(new ListBrandsQuery(search, status));
    return Results.Ok(list);
}).RequireCatalogPermission(CatalogPermissionKeys.BrandsView);

// ?????? ????
catalog.MapGet("/brands/{id:guid}", async (Guid id, IMediator m) =>
{
    var dto = await m.Send(new GetBrandByIdQuery(id));
    return dto is null ? Results.NotFound() : Results.Ok(dto);
}).RequireCatalogPermission(CatalogPermissionKeys.BrandsDetailView);

catalog.MapPut("/brands/{id:guid}", async (Guid id, UpdateBrandCommand body, IMediator m) =>
{
    await m.Send(body with { BrandId = id });
    return Results.NoContent();
}).RequireCatalogPermission(CatalogPermissionKeys.BrandsEdit);

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
.WithName("UploadBrandLogo")
.RequireCatalogPermission(CatalogPermissionKeys.BrandsLogoUpload);

// ????? ??? ????
catalog.MapPost("/brands/{id:guid}/rename", async (Guid id, RenameBrandCommand body, IMediator m) =>
{
    await m.Send(body with { BrandId = id });
    return Results.NoContent();
}).RequireCatalogPermission(CatalogPermissionKeys.BrandsRename);

// ????? ??????? ???? (?????/???/????/??????)
catalog.MapPost("/brands/{id:guid}/profile", async (Guid id, SetBrandProfileCommand body, IMediator m) =>
{
    await m.Send(body with { BrandId = id });
    return Results.NoContent();
}).RequireCatalogPermission(CatalogPermissionKeys.BrandsProfileEdit);

// ????? ????? ???? (Active/Inactive/Deprecated)
catalog.MapPost("/brands/{id:guid}/status", async (Guid id, SetBrandStatusCommand body, IMediator m) =>
{
    await m.Send(body with { BrandId = id });
    return Results.NoContent();
}).RequireCatalogPermission(CatalogPermissionKeys.BrandsStatusEdit);

// ??? ???? (??? ?? ?????? ??????? ???? ????)
catalog.MapDelete("/brands/{id:guid}", async (Guid id, IMediator m) =>
{
    await m.Send(new DeleteBrandCommand(id));
    return Results.NoContent();
}).RequireCatalogPermission(CatalogPermissionKeys.BrandsDelete);
// ?????? ??? ??????
catalog.MapPost("/brands/{id:guid}/aliases", async (Guid id, AddBrandAliasCommand body, IMediator m) =>
{
    var aliasId = await m.Send(body with { BrandId = id });
    return Results.Created($"/api/catalog/brands/{id}/aliases/{aliasId}", new { id = aliasId });
}).RequireCatalogPermission(CatalogPermissionKeys.BrandsAliasesManage);

// ???? ??????? ?????? ????
catalog.MapGet("/brands/{id:guid}/aliases", async (Guid id, IMediator m) =>
{
    var list = await m.Send(new ListBrandAliasesQuery(id));
    return Results.Ok(list);
}).RequireCatalogPermission(CatalogPermissionKeys.BrandsAliasesView);

// ??? ??? ??????
catalog.MapDelete("/brands/aliases/{aliasId:guid}", async (Guid aliasId, IMediator m) =>
{
    await m.Send(new RemoveBrandAliasCommand(aliasId));
    return Results.NoContent();
}).RequireCatalogPermission(CatalogPermissionKeys.BrandsAliasesManage);


catalog.MapPost("/stores", async (CreateStoreCommand cmd, IMediator m) =>
{
    var id = await m.Send(cmd);
    return Results.Created($"/api/catalog/stores/{id}", new { id });
}).RequireCatalogPermission(CatalogPermissionKeys.StoresCreate);


catalog.MapGet("/stores", async (string? search, IMediator m) =>
{
    var list = await m.Send(new ListStoresQuery(search));
    return Results.Ok(list);
}).RequireCatalogPermission(CatalogPermissionKeys.StoresView);


catalog.MapGet("/stores/{id:guid}", async (Guid id, IMediator m) =>
{
    var dto = await m.Send(new GetStoreByIdQuery(id));
    return dto is null ? Results.NotFound() : Results.Ok(dto);
}).RequireCatalogPermission(CatalogPermissionKeys.StoresDetailView);


catalog.MapPost("/stores/{id:guid}/rename", async (Guid id, RenameStoreCommand body, IMediator m) =>
{
    await m.Send(body with { StoreId = id });
    return Results.NoContent();
}).RequireCatalogPermission(CatalogPermissionKeys.StoresRename);

catalog.MapPost("/stores/{id:guid}/domain", async (Guid id, SetStoreDomainCommand body, IMediator m) =>
{
    await m.Send(body with { StoreId = id });
    return Results.NoContent();
}).RequireCatalogPermission(CatalogPermissionKeys.StoresDomainEdit);
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
    .RequireCatalogPermission(CatalogPermissionKeys.ProductsImagesUpload)
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
}).RequireCatalogPermission(CatalogPermissionKeys.ProductsImagesSetMain);

// Reorder
catalog.MapPost("/products/{pid:guid}/images/reorder", async (Guid pid, Guid[] orderedIds, IMediator m) =>
{
    await m.Send(new ReorderProductImagesCommand(pid, orderedIds));
    return Results.NoContent();
}).RequireCatalogPermission(CatalogPermissionKeys.ProductsImagesReorder);

// Delete
catalog.MapDelete("/products/{pid:guid}/images/{imgId:guid}", async (Guid pid, Guid imgId, IMediator m) =>
{
    await m.Send(new DeleteProductImageCommand(pid, imgId));
    return Results.NoContent();
}).RequireCatalogPermission(CatalogPermissionKeys.ProductsImagesDelete);

catalog.MapGet("/products/properties/keys", async (int? top, IMediator m) =>
{
    var list = await m.Send(new ListPropertyKeysQuery(top ?? 20));
    return Results.Ok(list);
}).RequireCatalogPermission(CatalogPermissionKeys.MetadataPropertyKeysView);

catalog.MapGet("/products/properties/{key}/values", async (string key, int? top, IMediator m) =>
{
    var list = await m.Send(new ListPropertyValuesQuery(key, top ?? 20));
    return Results.Ok(list);
}).RequireCatalogPermission(CatalogPermissionKeys.MetadataPropertyValuesView);

catalog.MapGet("/products/variants/values", async (int? top, IMediator m) =>
{
    var list = await m.Send(new ListVariantValuesQuery(top ?? 20));
    return Results.Ok(list);
}).RequireCatalogPermission(CatalogPermissionKeys.MetadataVariantValuesView);

catalog.MapGet("/products/variants/recent", async (int? top, IMediator m) =>
{
    var dto = await m.Send(new ListRecentVariantsQuery(top ?? 10));
    return Results.Ok(dto);
}).RequireCatalogPermission(CatalogPermissionKeys.MetadataVariantRecentView);

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
}).RequireCatalogPermission(CatalogPermissionKeys.ProductsView);


app.Run();
