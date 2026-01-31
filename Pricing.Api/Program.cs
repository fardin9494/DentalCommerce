using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Pricing.Api.Endpoints;
using Pricing.Application.Abstractions;
using Pricing.Application.Common.Behaviors;
using Pricing.Application.Markers;
using Pricing.Application.Services;
using Pricing.Infrastructure.Gateways;
using Pricing.Infrastructure.Persistence;
using Pricing.Infrastructure.Persistence.Converters;
using Pricing.Infrastructure.Transactions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Pricing.Api;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<PricingDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("PricingDb")
             ?? throw new InvalidOperationException("Connection string 'PricingDb' is not configured.");
    opt.UseSqlServer(cs, sql =>
    {
        sql.MigrationsHistoryTable("__EFMigrationsHistory", PricingDbContext.DefaultSchema);
        sql.EnableRetryOnFailure();
    });
});

builder.Services.AddScoped<IPricingDbContext>(sp => sp.GetRequiredService<PricingDbContext>());
builder.Services.AddScoped<ITransactionRunner, EfTransactionRunner>();

// MediatR + Validation
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Pricing services
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<EligibilityEvaluator>();
builder.Services.AddSingleton<BenefitApplier>();
builder.Services.AddSingleton<GuardrailEnforcer>();
builder.Services.AddScoped<PromotionSelector>();
builder.Services.AddScoped<PricingEngine>();

// Catalog API gateway
var catalogApiUrl = builder.Configuration["CatalogApiUrl"]
    ?? throw new InvalidOperationException("CatalogApiUrl is not configured in appsettings.json");
var catalogApiToken = builder.Configuration["CatalogApi:ServiceToken"]
    ?? builder.Configuration["CatalogApi:Password"]
    ?? throw new InvalidOperationException("CatalogApi:ServiceToken or CatalogApi:Password is not configured in appsettings.json");

builder.Services.AddHttpClient("CatalogApi", client =>
{
    client.BaseAddress = new Uri(catalogApiUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", catalogApiToken);
});

builder.Services.AddScoped<ICatalogPricingGateway, CatalogApiGateway>();

// Inventory API gateway
var inventoryApiUrl = builder.Configuration["InventoryApiUrl"]
    ?? throw new InvalidOperationException("InventoryApiUrl is not configured in appsettings.json");
var inventoryApiToken = builder.Configuration["InventoryApi:ServiceToken"]
    ?? builder.Configuration["InventoryApi:Password"]
    ?? throw new InvalidOperationException("InventoryApi:ServiceToken or InventoryApi:Password is not configured in appsettings.json");

builder.Services.AddHttpClient("InventoryApi", client =>
{
    client.BaseAddress = new Uri(inventoryApiUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", inventoryApiToken);
});

builder.Services.AddScoped<IInventoryBatchInfoGateway, InventoryApiGateway>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.Converters.Add(new EligibilityDefinitionJsonConverter());
    options.SerializerOptions.Converters.Add(new BenefitDefinitionJsonConverter());
});

// CORS: single admin policy; dev vs production
const string AdminCorsPolicy = "admin";
var adminOrigin = builder.Configuration["Cors:AdminOrigin"];
builder.Services.AddCors(opt =>
{
    opt.AddPolicy(AdminCorsPolicy, p =>
    {
        if (builder.Environment.IsDevelopment())
        {
            p.WithOrigins("http://localhost:5173", "https://localhost:5173")
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Run CORS before the admin gate so even 401 responses carry the headers
app.UseCors(AdminCorsPolicy);

// Simple shared-password gate for admin APIs (/api/pricing/*).
// Service:Token allows internal services to call Pricing API.
var adminPassword = app.Configuration["Admin:Password"];
var adminPasswordHashHex = app.Configuration["Admin:PasswordHash"];
var serviceToken = app.Configuration["Service:Token"]; // Service-to-service token
byte[]? adminPasswordHash = null;
if (!string.IsNullOrWhiteSpace(adminPasswordHashHex))
{
    adminPasswordHash = Convert.FromHexString(adminPasswordHashHex);
}

if (!string.IsNullOrWhiteSpace(adminPassword) || adminPasswordHash is not null || !string.IsNullOrWhiteSpace(serviceToken))
{
    app.Use(async (ctx, next) =>
    {
        // Allow CORS preflight without auth
        if (HttpMethods.IsOptions(ctx.Request.Method))
        {
            await next();
            return;
        }

        if (ctx.Request.Path.StartsWithSegments("/api/pricing"))
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

            if (!string.IsNullOrWhiteSpace(serviceToken) && string.Equals(token, serviceToken, StringComparison.Ordinal))
            {
                ok = true;
            }
            else if (adminPasswordHash is not null)
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

app.Use(async (ctx, next) =>
{
    try
    {
        await next();
    }
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
    catch (Exception ex)
    {
        logger.LogError(ex, "Unhandled exception for request {Path}", ctx.Request.Path);
        var detail = env.IsDevelopment() ? ex.Message : null;

        await Results.Problem(title: "Internal Server Error", detail: detail, statusCode: StatusCodes.Status500InternalServerError)
            .ExecuteAsync(ctx);
    }
});

var pricing = app.MapGroup("/api/pricing").DisableAntiforgery();
pricing.MapGet("/auth/check", () => Results.NoContent());

pricing.MapQuoteEndpoints();
pricing.MapPriceListEndpoints();
pricing.MapOverrideEndpoints();
pricing.MapCampaignEndpoints();
pricing.MapCouponEndpoints();
pricing.MapBulkEndpoints();
pricing.MapPricingPolicyEndpoints();

app.Run();
