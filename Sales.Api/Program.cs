using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Sales.Api.Endpoints;
using Sales.Api.Infrastructure;
using Sales.Api.Permissions;
using Sales.Api.Services;
using Sales.Application.Abstractions;
using Sales.Application.Common.Behaviors;
using Sales.Application.Markers;
using Sales.Infrastructure.Gateways;
using Sales.Infrastructure.Persistence;
using Sales.Infrastructure.Transactions;
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
    ?? throw new InvalidOperationException("PricingApi:ServiceToken is not configured in appsettings.json");

builder.Services.AddHttpClient("PricingApi", client =>
{
    client.BaseAddress = new Uri(pricingApiUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", pricingApiToken);
}).AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>();

builder.Services.AddScoped<IPricingQuoteGateway, PricingApiGateway>();
builder.Services.AddScoped<IPaymentGateway, FakePaymentGateway>();

// Inventory API gateway (reservations)
var inventoryApiUrl = builder.Configuration["InventoryApiUrl"]
    ?? throw new InvalidOperationException("InventoryApiUrl is not configured in appsettings.json");
var inventoryApiToken = builder.Configuration["InventoryApi:ServiceToken"]
    ?? throw new InvalidOperationException("InventoryApi:ServiceToken is not configured in appsettings.json");

builder.Services.AddHttpClient("InventoryApi", client =>
{
    client.BaseAddress = new Uri(inventoryApiUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", inventoryApiToken);
}).AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>();

builder.Services.AddScoped<IInventoryReservationGateway, InventoryApiGateway>();

// Catalog API gateway (store lookup)
var catalogApiUrl = builder.Configuration["CatalogApiUrl"]
    ?? throw new InvalidOperationException("CatalogApiUrl is not configured in appsettings.json");
var catalogApiToken = builder.Configuration["CatalogApi:ServiceToken"]
    ?? throw new InvalidOperationException("CatalogApi:ServiceToken is not configured in appsettings.json");

builder.Services.AddHttpClient("CatalogApi", client =>
{
    client.BaseAddress = new Uri(catalogApiUrl);
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", catalogApiToken);
}).AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>();

builder.Services.AddScoped<IStoreLookupGateway, CatalogStoreGateway>();
builder.Services.AddHostedService<ReservationExpiryWorker>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ForwardAuthorizationHeaderHandler>();

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
builder.Services.AddScoped<ISalesPermissionChecker, SalesPermissionChecker>();

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

app.UseCors(AdminCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

var sales = app.MapGroup("/api/sales")
    .RequireAuthorization()
    .DisableAntiforgery();
sales.MapOrderEndpoints();
sales.MapReportEndpoints();

app.Run();
