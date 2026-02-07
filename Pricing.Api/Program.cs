using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pricing.Api.Endpoints;
using Pricing.Api.Infrastructure;
using Pricing.Api.Permissions;
using Pricing.Application.Abstractions;
using Pricing.Application.Common.Behaviors;
using Pricing.Application.Markers;
using Pricing.Application.Services;
using Pricing.Infrastructure.Gateways;
using Pricing.Infrastructure.Persistence;
using Pricing.Infrastructure.Persistence.Converters;
using Pricing.Infrastructure.Transactions;
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
    ?? throw new InvalidOperationException("CatalogApi:ServiceToken is not configured in appsettings.json");

builder.Services.AddHttpClient("CatalogApi", client =>
{
    client.BaseAddress = new Uri(catalogApiUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", catalogApiToken);
}).AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>();

builder.Services.AddScoped<ICatalogPricingGateway, CatalogApiGateway>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ForwardAuthorizationHeaderHandler>();

// Inventory API gateway
var inventoryApiUrl = builder.Configuration["InventoryApiUrl"]
    ?? throw new InvalidOperationException("InventoryApiUrl is not configured in appsettings.json");
var inventoryApiToken = builder.Configuration["InventoryApi:ServiceToken"]
    ?? throw new InvalidOperationException("InventoryApi:ServiceToken is not configured in appsettings.json");

builder.Services.AddHttpClient("InventoryApi", client =>
{
    client.BaseAddress = new Uri(inventoryApiUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", inventoryApiToken);
}).AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>();

builder.Services.AddScoped<IInventoryBatchInfoGateway, InventoryApiGateway>();

// Identity API + permissions
var identityApiUrl = builder.Configuration["IdentityApiUrl"]
    ?? throw new InvalidOperationException("IdentityApiUrl is not configured in appsettings.json");

builder.Services.AddHttpClient("IdentityApi", client =>
{
    client.BaseAddress = new Uri(identityApiUrl);
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddMemoryCache();
builder.Services.AddScoped<IPricingPermissionChecker, PricingPermissionChecker>();

// JWT auth (issued by Identity)
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
            p.WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173",
                "http://localhost:5174",
                "https://localhost:5174",
                "http://localhost:5175",
                "https://localhost:5175",
                "http://localhost:5176",
                "https://localhost:5176"
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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

var pricing = app.MapGroup("/api/pricing")
    .RequireAuthorization()
    .DisableAntiforgery();

pricing.MapQuoteEndpoints();
pricing.MapPriceListEndpoints();
pricing.MapOverrideEndpoints();
pricing.MapCampaignEndpoints();
pricing.MapCouponEndpoints();
pricing.MapBulkEndpoints();
pricing.MapPricingPolicyEndpoints();

app.Run();
