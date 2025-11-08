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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<CatalogDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("CatalogDb")
             ?? "Server=DESKTOP-RAJN9B2\\FARDIN2019;Database=DentalCatalogDb;Integrated Security=True;TrustServerCertificate=True";
    opt.UseSqlServer(cs, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", CatalogDbContext.DefaultSchema));
});

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.Load("Catalog.Application")));

// FluentValidation (DI) + MediatR Pipeline
builder.Services.AddValidatorsFromAssembly(Assembly.Load("Catalog.Application"));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
// 1) Options برای پردازش تصویر
builder.Services.Configure<ImageProcessingOptions>(builder.Configuration.GetSection("Media:Image"));

// 2) Storage و Processor
builder.Services.AddScoped<IFileStorage, LocalFileStorage>();
builder.Services.AddScoped<IImageProcessor, ImageSharpProcessor>();
// سرویس خواندن دسته
builder.Services.AddScoped<ICategoryReadService, CategoryReadService>();
builder.Services.AddScoped<DbContext, CatalogDbContext>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.Use(async (ctx, next) =>
{
    try
    {
        await next();
    }
    // 400: FluentValidation
    catch (ValidationException ex)
    {
        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        await ctx.Response.WriteAsJsonAsync(new { message = "Validation failed", errors });
    }

    catch (InvalidOperationException ex)
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        await ctx.Response.WriteAsJsonAsync(new { message = ex.Message });
    }
    // 409: یکتایی/کلید تکراری SQL Server
    catch (DbUpdateException ex) when (
        ex.InnerException is Microsoft.Data.SqlClient.SqlException sql &&
        (sql.Number == 2601 || sql.Number == 2627)
    )
    {
        ctx.Response.StatusCode = StatusCodes.Status409Conflict;
        await ctx.Response.WriteAsJsonAsync(new { message = "Duplicate key", detail = sql.Message });
    }
    // 500: بقیه
    catch (Exception ex)
    {
        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await ctx.Response.WriteAsJsonAsync(new { message = "Internal error", detail = ex.Message });
    }
});
var mediaRoot = builder.Configuration["Media:Root"] ?? "./_media";

// مسیر رو کامل کن و اگر نبود بساز
var mediaRootPath = Path.GetFullPath(mediaRoot);
Directory.CreateDirectory(mediaRootPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.GetFullPath(mediaRoot)),
    RequestPath = "/media"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(mediaRootPath),
    RequestPath = "/media"
});
app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/api/catalog/products", async (CreateProductCommand cmd, IMediator mediator) =>
{
    var id = await mediator.Send(cmd);
    return Results.Created($"/api/catalog/products/{id}", new { id });
});
app.MapPost("/api/catalog/products/{id:guid}/activate", async (Guid id, IMediator mediator) =>
{
    await mediator.Send(new ActivateProductCommand(id));
    return Results.NoContent();
});

app.MapPost("/api/catalog/products/{id:guid}/images", async (Guid id, AddProductImageCommand body, IMediator mediator) =>
{
    // اطمینان: ProductId از route بره داخل Command
    var cmd = body with { ProductId = id };
    var imageId = await mediator.Send(cmd);
    return Results.Created($"/api/catalog/products/{id}/images/{imageId}", new { imageId });
});

app.MapPost("/api/catalog/products/{id:guid}/variants", async (Guid id, UpsertVariantCommand body, IMediator mediator) =>
{
    var cmd = body with { ProductId = id };
    var variantId = await mediator.Send(cmd);
    return Results.Created($"/api/catalog/products/{id}/variants/{variantId}", new { variantId });
});

app.MapDelete("/api/catalog/products/{id:guid}/variants/{variantId:guid}", async (Guid id, Guid variantId, IMediator mediator) =>
{
    await mediator.Send(new DeleteVariantCommand(id, variantId));
    return Results.NoContent();
});
app.MapPost("/api/catalog/products/{id:guid}/variation", async (Guid id, SetVariationCommand body, IMediator mediator) =>
{
    var cmd = body with { ProductId = id };
    await mediator.Send(cmd);
    return Results.NoContent();
});

app.MapPost("/api/catalog/products/{id:guid}/properties", async (Guid id, UpsertPropertyCommand body, IMediator mediator) =>
{
    var cmd = body with { ProductId = id };
    var propertyId = await mediator.Send(cmd);
    return Results.Created($"/api/catalog/products/{id}/properties/{propertyId}", new { propertyId });
});

app.MapPost("/api/catalog/products/{id:guid}/stores", async (Guid id, UpsertProductStoreCommand body, IMediator mediator) =>
{
    var cmd = body with { ProductId = id };
    await mediator.Send(cmd);
    return Results.NoContent();
});
app.MapPost("/api/catalog/products/{id:guid}/seo", async (Guid id, UpsertProductSeoCommand body, IMediator mediator) =>
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
app.MapGet("/api/catalog/products", async (
    int page, int pageSize, string? search, Guid? brandId, Guid? categoryId, Guid? storeId, bool? visibleInStore, string? sort,
    IMediator mediator) =>
{
    page = page <= 0 ? 1 : page;
    pageSize = pageSize <= 0 ? 20 : pageSize;
    var result = await mediator.Send(new ListProductsQuery(page, pageSize, search, brandId, categoryId, storeId, visibleInStore, sort));
    return Results.Ok(result);
});
app.MapPost("/api/catalog/categories", async (CreateCategoryCommand cmd, IMediator mediator) =>
{
    var id = await mediator.Send(cmd);
    return Results.Created($"/api/catalog/categories/{id}", new { id });
});
app.MapPost("/api/catalog/categories/{id:guid}/rename",
    async (Guid id, RenameCategoryCommand body, IMediator m) =>
    {
        var cmd = body with { CategoryId = id };
        await m.Send(cmd);
        return Results.NoContent();
    });
app.MapPost("/api/catalog/categories/{id:guid}/move",
    async (Guid id, MoveCategoryCommand body, IMediator m) =>
    {
        var cmd = body with { CategoryId = id };
        await m.Send(cmd);
        return Results.NoContent();
    });
app.MapGet("/api/catalog/categories/tree", async (IMediator m) =>
{
    var nodes = await m.Send(new GetCategoryTreeQuery());
    return Results.Ok(nodes.OrderBy(n => n.ParentId.HasValue).ThenBy(n => n.Name));
});
app.MapPost("/api/catalog/countries", async (CreateCountryCommand cmd, IMediator m) =>
{
    var code2 = await m.Send(cmd);
    return Results.Created($"/api/catalog/countries/{code2}", new { code2 });
});

// لیست کشورها (اختیاری: جستجو)
app.MapGet("/api/catalog/countries", async (string? search, IMediator m) =>
{
    var list = await m.Send(new ListCountriesQuery(search));
    return Results.Ok(list);
});
// ایجاد برند
app.MapPost("/api/catalog/brands", async (CreateBrandCommand cmd, IMediator m) =>
{
    var id = await m.Send(cmd);
    return Results.Created($"/api/catalog/brands/{id}", new { id });
});

// لیست برندها با فیلتر
app.MapGet("/api/catalog/brands", async (string? search, string? countryCode, BrandStatus? status, IMediator m) =>
{
    var list = await m.Send(new ListBrandsQuery(search, countryCode, status));
    return Results.Ok(list);
});

// جزییات برند
app.MapGet("/api/catalog/brands/{id:guid}", async (Guid id, IMediator m) =>
{
    var dto = await m.Send(new GetBrandByIdQuery(id));
    return dto is null ? Results.NotFound() : Results.Ok(dto);
});

// تغییر نام برند
app.MapPost("/api/catalog/brands/{id:guid}/rename", async (Guid id, RenameBrandCommand body, IMediator m) =>
{
    await m.Send(body with { BrandId = id });
    return Results.NoContent();
});

// تنظیم پروفایل برند (توضیح/سال/لوگو/وبسایت)
app.MapPost("/api/catalog/brands/{id:guid}/profile", async (Guid id, SetBrandProfileCommand body, IMediator m) =>
{
    await m.Send(body with { BrandId = id });
    return Results.NoContent();
});

// تغییر وضعیت برند (Active/Inactive/Deprecated)
app.MapPost("/api/catalog/brands/{id:guid}/status", async (Guid id, SetBrandStatusCommand body, IMediator m) =>
{
    await m.Send(body with { BrandId = id });
    return Results.NoContent();
});

// حذف برند (اگر در محصولی استفاده نشده باشد)
app.MapDelete("/api/catalog/brands/{id:guid}", async (Guid id, IMediator m) =>
{
    await m.Send(new DeleteBrandCommand(id));
    return Results.NoContent();
});
// افزودن نام مستعار
app.MapPost("/api/catalog/brands/{id:guid}/aliases", async (Guid id, AddBrandAliasCommand body, IMediator m) =>
{
    var aliasId = await m.Send(body with { BrandId = id });
    return Results.Created($"/api/catalog/brands/{id}/aliases/{aliasId}", new { id = aliasId });
});

// لیست نام‌های مستعار برند
app.MapGet("/api/catalog/brands/{id:guid}/aliases", async (Guid id, IMediator m) =>
{
    var list = await m.Send(new ListBrandAliasesQuery(id));
    return Results.Ok(list);
});

// حذف نام مستعار
app.MapDelete("/api/catalog/brands/aliases/{aliasId:guid}", async (Guid aliasId, IMediator m) =>
{
    await m.Send(new RemoveBrandAliasCommand(aliasId));
    return Results.NoContent();
});


app.MapPost("/api/catalog/stores", async (CreateStoreCommand cmd, IMediator m) =>
{
    var id = await m.Send(cmd);
    return Results.Created($"/api/catalog/stores/{id}", new { id });
});


app.MapGet("/api/catalog/stores", async (string? search, IMediator m) =>
{
    var list = await m.Send(new ListStoresQuery(search));
    return Results.Ok(list);
});


app.MapGet("/api/catalog/stores/{id:guid}", async (Guid id, IMediator m) =>
{
    var dto = await m.Send(new GetStoreByIdQuery(id));
    return dto is null ? Results.NotFound() : Results.Ok(dto);
});


app.MapPost("/api/catalog/stores/{id:guid}/rename", async (Guid id, RenameStoreCommand body, IMediator m) =>
{
    await m.Send(body with { StoreId = id });
    return Results.NoContent();
});

app.MapPost("/api/catalog/stores/{id:guid}/domain", async (Guid id, SetStoreDomainCommand body, IMediator m) =>
{
    await m.Send(body with { StoreId = id });
    return Results.NoContent();
});
app.MapPost("/api/catalog/products/{id:guid}/images/upload",
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
// این بخش‌ها اختیاری‌ان ولی Swagger رو قشنگ‌تر می‌کنن:
    .Accepts<UploadProductImageForm>("multipart/form-data")
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
app.MapPost("/api/catalog/products/{pid:guid}/images/{imgId:guid}/main", async (Guid pid, Guid imgId, IMediator m) =>
{
    await m.Send(new SetMainImageCommand(pid, imgId));
    return Results.NoContent();
});

// Reorder
app.MapPost("/api/catalog/products/{pid:guid}/images/reorder", async (Guid pid, Guid[] orderedIds, IMediator m) =>
{
    await m.Send(new ReorderProductImagesCommand(pid, orderedIds));
    return Results.NoContent();
});

// Delete
app.MapDelete("/api/catalog/products/{pid:guid}/images/{imgId:guid}", async (Guid pid, Guid imgId, IMediator m) =>
{
    await m.Send(new DeleteProductImageCommand(pid, imgId));
    return Results.NoContent();
});
app.MapGet("/api/catalog/admin/products", async (
    HttpResponse response,                // 👈 این را اضافه کن
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

    // هدرها را روی response ست کن
    response.Headers.Append("X-Total", res.Total.ToString());
    response.Headers.Append("X-Page", res.Page.ToString());
    response.Headers.Append("X-Page-Size", res.PageSize.ToString());
    response.Headers.Append("X-Total-Pages",
        Math.Ceiling((double)res.Total / Math.Max(1, res.PageSize)).ToString());

    return Results.Ok(res); // یا TypedResults.Ok(res)
});


app.Run();