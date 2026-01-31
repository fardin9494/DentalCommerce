using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Sales.Api.Endpoints;
using Sales.Api.Services;
using Sales.Application.Abstractions;
using Sales.Application.Common.Behaviors;
using Sales.Application.Markers;
using Sales.Infrastructure.Gateways;
using Sales.Infrastructure.Persistence;
using Sales.Infrastructure.Transactions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<SalesDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("SalesDb")
             ?? throw new InvalidOperationException("Connection string 'SalesDb' is not configured.");
    opt.UseSqlServer(cs, sql =>
    {
        sql.MigrationsHistoryTable("__EFMigrationsHistory", SalesDbContext.DefaultSchema);
        sql.EnableRetryOnFailure();
    });
});

builder.Services.AddScoped<ISalesDbContext>(sp => sp.GetRequiredService<SalesDbContext>());
builder.Services.AddScoped<ITransactionRunner, EfTransactionRunner>();

// MediatR + Validation
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Pricing API gateway
var pricingApiUrl = builder.Configuration["PricingApiUrl"]
    ?? throw new InvalidOperationException("PricingApiUrl is not configured in appsettings.json");
var pricingApiToken = builder.Configuration["PricingApi:ServiceToken"]
    ?? builder.Configuration["PricingApi:Password"]
    ?? throw new InvalidOperationException("PricingApi:ServiceToken or PricingApi:Password is not configured in appsettings.json");

builder.Services.AddHttpClient("PricingApi", client =>
{
    client.BaseAddress = new Uri(pricingApiUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", pricingApiToken);
});

builder.Services.AddScoped<IPricingQuoteGateway, PricingApiGateway>();
builder.Services.AddScoped<IPaymentGateway, FakePaymentGateway>();

// Inventory API gateway (reservations)
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
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", inventoryApiToken);
});

builder.Services.AddScoped<IInventoryReservationGateway, InventoryApiGateway>();

// Catalog API gateway (store lookup)
var catalogApiUrl = builder.Configuration["CatalogApiUrl"]
    ?? throw new InvalidOperationException("CatalogApiUrl is not configured in appsettings.json");
var catalogApiToken = builder.Configuration["CatalogApi:ServiceToken"]
    ?? builder.Configuration["CatalogApi:Password"]
    ?? throw new InvalidOperationException("CatalogApi:ServiceToken or CatalogApi:Password is not configured in appsettings.json");

builder.Services.AddHttpClient("CatalogApi", client =>
{
    client.BaseAddress = new Uri(catalogApiUrl);
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", catalogApiToken);
});

builder.Services.AddScoped<IStoreLookupGateway, CatalogStoreGateway>();
builder.Services.AddHostedService<ReservationExpiryWorker>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
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

app.UseCors(AdminCorsPolicy);

// Simple shared-password gate for admin APIs (/api/sales/*).
// Service:Token allows internal services to call Sales API.
var adminPassword = app.Configuration["Admin:Password"];
var adminPasswordHashHex = app.Configuration["Admin:PasswordHash"];
var serviceToken = app.Configuration["Service:Token"];
byte[]? adminPasswordHash = null;
if (!string.IsNullOrWhiteSpace(adminPasswordHashHex))
{
    adminPasswordHash = Convert.FromHexString(adminPasswordHashHex);
}

if (!string.IsNullOrWhiteSpace(adminPassword) || adminPasswordHash is not null || !string.IsNullOrWhiteSpace(serviceToken))
{
    app.Use(async (ctx, next) =>
    {
        if (HttpMethods.IsOptions(ctx.Request.Method))
        {
            await next();
            return;
        }

        if (ctx.Request.Path.StartsWithSegments("/api/sales"))
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

            var token = auth.Substring(prefix.Length).Trim();

            // Allow service-to-service token without hashing
            if (!string.IsNullOrWhiteSpace(serviceToken) && token == serviceToken)
            {
                await next();
                return;
            }

            if (adminPasswordHash is not null)
            {
                var bytes = Encoding.UTF8.GetBytes(token);
                var hash = SHA256.HashData(bytes);
                if (!hash.SequenceEqual(adminPasswordHash))
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await ctx.Response.WriteAsync("Unauthorized");
                    return;
                }
            }
            else if (!string.IsNullOrWhiteSpace(adminPassword))
            {
                if (!string.Equals(token, adminPassword, StringComparison.Ordinal))
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await ctx.Response.WriteAsync("Unauthorized");
                    return;
                }
            }
        }

        await next();
    });
}

var sales = app.MapGroup("/api/sales").DisableAntiforgery();
sales.MapGet("/auth/check", () => Results.NoContent());
sales.MapOrderEndpoints();
sales.MapReportEndpoints();

app.Run();
