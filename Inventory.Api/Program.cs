using FluentValidation;
using Inventory.Application.Features.Adjustments.Commands;
using Inventory.Application.Features.Issues.Commands;
using Inventory.Application.Features.Pricing.Commands;
using Inventory.Application.Features.Pricing.Queries;
using Inventory.Application.Features.Receipts.Commands;
using Inventory.Application.Features.Stock.Commands; // ???????? ????
using Inventory.Application.Features.Shelves.Commands; // ???????? ????
using Inventory.Application.Features.Transfers.Commands;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inventory.Api;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Configuration (Inventory Only)
builder.Services.AddDbContext<InventoryDbContext>(opt =>
{
    // ????? ???? ?? appsettings.json ????? ?????? ??????? InventoryDb ?? ?????
    var cs = builder.Configuration.GetConnectionString("InventoryDb")
             ?? throw new InvalidOperationException("Connection string 'InventoryDb' is not configured.");
    opt.UseSqlServer(cs, sql =>
    {
        sql.MigrationsHistoryTable("__EFMigrationsHistory", InventoryDbContext.DefaultSchema);
        sql.EnableRetryOnFailure();
    });
});

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Inventory.Application.Markers.AssemblyMarker).Assembly);
});

builder.Services.AddValidatorsFromAssembly(typeof(Inventory.Application.Markers.AssemblyMarker).Assembly);

// Anti-Corruption Layer: Catalog API Gateway
var catalogApiUrl = builder.Configuration["CatalogApiUrl"]
    ?? throw new InvalidOperationException("CatalogApiUrl is not configured in appsettings.json");

// Service-to-Service authentication token for Catalog API
var catalogApiToken = builder.Configuration["CatalogApi:ServiceToken"]
    ?? builder.Configuration["CatalogApi:Password"] // Fallback to Password for backward compatibility
    ?? throw new InvalidOperationException("CatalogApi:ServiceToken or CatalogApi:Password is not configured in appsettings.json");

builder.Services.AddHttpClient("CatalogApi", client =>
{
    client.BaseAddress = new Uri(catalogApiUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    // Add Bearer token for service-to-service authentication
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", catalogApiToken);
});

// Register CatalogApiGateway with adapter pattern to match ICatalogGateway interface
builder.Services.AddScoped<Inventory.Application.Common.Interfaces.ICatalogGateway>(sp =>
{
    var gateway = sp.GetRequiredService<Inventory.Infrastructure.Gateways.CatalogApiGateway>();
    return new CatalogGatewayAdapter(gateway);
});
builder.Services.AddScoped<Inventory.Infrastructure.Gateways.CatalogApiGateway>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS: single admin policy; dev vs production
const string AdminCorsPolicy = "admin";
var adminOrigin = builder.Configuration["Cors:AdminOrigin"]; // e.g. https://admin.yourdomain.com
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

// 3. Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Run CORS before the admin gate so even 401 responses carry the headers
app.UseCors(AdminCorsPolicy);

// Simple shared-password gate for admin APIs (/api/inventory/*).
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
        try
        {
            // Allow CORS preflight without auth
            if (HttpMethods.IsOptions(ctx.Request.Method))
            {
                await next();
                return;
            }

            if (ctx.Request.Path.StartsWithSegments("/api/inventory"))
            {
                if (!ctx.Request.Headers.TryGetValue("Authorization", out var authHeader))
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    ctx.Response.ContentType = "text/plain";
                    await ctx.Response.WriteAsync("Unauthorized");
                    return;
                }

                const string prefix = "Bearer ";
                var auth = authHeader.ToString();
                if (!auth.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    ctx.Response.ContentType = "text/plain";
                    await ctx.Response.WriteAsync("Unauthorized");
                    return;
                }

                var token = auth[prefix.Length..].Trim();
                var ok = false;

                // Check admin password hash
                if (adminPasswordHash is not null)
                {
                    var bytes = Encoding.UTF8.GetBytes(token);
                    var hash = SHA256.HashData(bytes);
                    ok = CryptographicOperations.FixedTimeEquals(hash, adminPasswordHash);
                }
                // Fall back to plain admin password
                else if (!string.IsNullOrWhiteSpace(adminPassword))
                {
                    ok = string.Equals(token, adminPassword, StringComparison.Ordinal);
                }

                if (!ok)
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    ctx.Response.ContentType = "text/plain";
                    await ctx.Response.WriteAsync("Unauthorized");
                    return;
                }
            }

            await next();
        }
        catch (Exception ex)
        {
            var logger = ctx.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Error in authentication middleware for {Path}", ctx.Request.Path);
            ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.WriteAsync("Internal Server Error");
        }
    });
}

// ==========================================
// INVENTORY ENDPOINTS
// ==========================================

// Authentication check endpoint
var auth = app.MapGroup("/api/inventory").DisableAntiforgery();
auth.MapGet("/auth/check", () => Results.NoContent());

// --- Receipts (ورود به انبار) ---
var receipts = app.MapGroup("/api/inventory/receipts").DisableAntiforgery();

receipts.MapGet("/{id:guid}", async (Guid id, IMediator m) =>
{
    var dto = await m.Send(new Inventory.Application.Features.Receipts.Queries.ReceiptDetailsQuery(id));
    return dto is null ? Results.NotFound() : Results.Ok(dto);
});

receipts.MapPost("/", async (CreateReceiptDraftCommand cmd, IMediator m) =>
{
    var id = await m.Send(cmd);
    return Results.Created($"/api/inventory/receipts/{id}", new { id });
});

receipts.MapPost("/{id:guid}/lines", async (Guid id, AddReceiptLineCommand body, IMediator m) =>
{
    var lineId = await m.Send(body with { ReceiptId = id });
    return Results.Created($"/api/inventory/receipts/{id}/lines/{lineId}", new { id = lineId });
});

receipts.MapDelete("/{id:guid}/lines/{lineId:guid}", async (Guid id, Guid lineId, IMediator m) =>
{
    await m.Send(new RemoveReceiptLineCommand(id, lineId));
    return Results.NoContent();
});

receipts.MapPut("/{id:guid}", async (Guid id, UpdateReceiptHeaderCommand body, IMediator m) =>
{
    await m.Send(body with { ReceiptId = id });
    return Results.NoContent();
});

receipts.MapPut("/{id:guid}/lines/{lineId:guid}", async (Guid id, Guid lineId, UpdateReceiptLineCommand body, IMediator m) =>
{
    await m.Send(body with { ReceiptId = id, LineId = lineId });
    return Results.NoContent();
});

// ????? ????: ??? Receive ??????? Post ??
receipts.MapPost("/{id:guid}/receive", async (Guid id, [FromBody] DateTime? when, IMediator m) =>
{
    // ??????? ?? Constructor ?? ??? Object Initializer
    await m.Send(new ReceiveReceiptCommand(id, when));
    return Results.NoContent();
});

// ??? ????: ????? ????? (??? ????? ???? ?????)
receipts.MapPost("/{id:guid}/approve", async (Guid id, IMediator m) =>
{
    await m.Send(new ApproveReceiptCommand { ReceiptId = id });
    return Results.NoContent();
});

receipts.MapPost("/{id:guid}/cancel", async (Guid id, IMediator m) =>
{
    await m.Send(new CancelReceiptCommand(id));
    return Results.NoContent();
});


// --- Issues (خروج از انبار) ---
var issues = app.MapGroup("/api/inventory/issues").DisableAntiforgery();

issues.MapGet("/{id:guid}", async (Guid id, IMediator m) =>
{
    var dto = await m.Send(new Inventory.Application.Features.Issues.Queries.IssueDetailsQuery(id));
    return dto is null ? Results.NotFound() : Results.Ok(dto);
});

issues.MapPost("/", async (CreateIssueDraftCommand cmd, IMediator m) =>
{
    var id = await m.Send(cmd);
    return Results.Created($"/api/inventory/issues/{id}", new { id });
});

issues.MapPost("/{id:guid}/lines", async (Guid id, AddIssueLineCommand body, IMediator m) =>
{
    var lineId = await m.Send(body with { IssueId = id });
    return Results.Created($"/api/inventory/issues/{id}/lines/{lineId}", new { id = lineId });
});

issues.MapDelete("/{id:guid}/lines/{lineId:guid}", async (Guid id, Guid lineId, IMediator m) =>
{
    await m.Send(new RemoveIssueLineCommand(id, lineId));
    return Results.NoContent();
});

issues.MapPut("/{id:guid}", async (Guid id, UpdateIssueHeaderCommand body, IMediator m) =>
{
    await m.Send(body with { IssueId = id });
    return Results.NoContent();
});

issues.MapPut("/{id:guid}/lines/{lineId:guid}", async (Guid id, Guid lineId, UpdateIssueLineCommand body, IMediator m) =>
{
    await m.Send(body with { IssueId = id, LineId = lineId });
    return Results.NoContent();
});

issues.MapPost("/{id:guid}/lines/{lineId:guid}/allocate-fefo", async (Guid id, Guid lineId, IMediator m) =>
{
    // ????? ????: ??? ????? ???? ???????????
    var allocations = await m.Send(new AllocateIssueLineFefoCommand(id, lineId));
    return Results.Ok(allocations);
});

issues.MapPost("/{id:guid}/post", async (Guid id, [FromBody] DateTime? when, IMediator m) =>
{
    await m.Send(new PostIssueCommand(id, when));
    return Results.NoContent();
});

issues.MapPost("/{id:guid}/cancel", async (Guid id, IMediator m) =>
{
    await m.Send(new CancelIssueCommand(id));
    return Results.NoContent();
});


// --- Transfers (انتقال بین انبارها) ---
var transfers = app.MapGroup("/api/inventory/transfers").DisableAntiforgery();

transfers.MapGet("/{id:guid}", async (Guid id, IMediator m) =>
{
    var dto = await m.Send(new Inventory.Application.Features.Transfers.Queries.TransferDetailsQuery(id));
    return dto is null ? Results.NotFound() : Results.Ok(dto);
});

transfers.MapPost("/", async (CreateTransferDraftCommand cmd, IMediator m) =>
{
    var id = await m.Send(cmd);
    return Results.Created($"/api/inventory/transfers/{id}", new { id });
});

transfers.MapPost("/{id:guid}/lines", async (Guid id, AddTransferLineCommand body, IMediator m) =>
{
    var lineId = await m.Send(body with { TransferId = id });
    return Results.Created($"/api/inventory/transfers/{id}/lines/{lineId}", new { id = lineId });
});

transfers.MapDelete("/{id:guid}/lines/{lineId:guid}", async (Guid id, Guid lineId, IMediator m) =>
{
    await m.Send(new RemoveTransferLineCommand(id, lineId));
    return Results.NoContent();
});

transfers.MapPut("/{id:guid}", async (Guid id, UpdateTransferHeaderCommand body, IMediator m) =>
{
    await m.Send(body with { TransferId = id });
    return Results.NoContent();
});

transfers.MapPut("/{id:guid}/lines/{lineId:guid}", async (Guid id, Guid lineId, UpdateTransferLineCommand body, IMediator m) =>
{
    await m.Send(body with { TransferId = id, LineId = lineId });
    return Results.NoContent();
});

transfers.MapPost("/{id:guid}/lines/{lineId:guid}/allocate-fefo", async (Guid id, Guid lineId, IMediator m) =>
{
    var res = await m.Send(new AllocateTransferLineFefoCommand(id, lineId));
    return Results.Ok(res);
});

transfers.MapPost("/{id:guid}/ship", async (Guid id, IMediator m) =>
{
    await m.Send(new ShipTransferCommand(id));
    return Results.NoContent();
});

transfers.MapPost("/{id:guid}/receive", async (Guid id, ReceiveTransferCommand body, IMediator m) =>
{
    await m.Send(body with { TransferId = id });
    return Results.NoContent();
});

transfers.MapPost("/{id:guid}/cancel", async (Guid id, IMediator m) =>
{
    await m.Send(new CancelTransferCommand(id));
    return Results.NoContent();
});


// --- Adjustments (اصلاح موجودی) ---
var adj = app.MapGroup("/api/inventory/adjustments").DisableAntiforgery();

adj.MapGet("/{id:guid}", async (Guid id, IMediator m) =>
{
    var dto = await m.Send(new Inventory.Application.Features.Adjustments.Queries.AdjustmentDetailsQuery(id));
    return dto is null ? Results.NotFound() : Results.Ok(dto);
});

adj.MapPost("/", async (CreateAdjustmentDraftCommand cmd, IMediator m) =>
{
    var id = await m.Send(cmd);
    return Results.Created($"/api/inventory/adjustments/{id}", new { id });
});

adj.MapPost("/{id:guid}/lines", async (Guid id, AddAdjustmentLineCommand body, IMediator m) =>
{
    var lineId = await m.Send(body with { AdjustmentId = id });
    return Results.Created($"/api/inventory/adjustments/{id}/lines/{lineId}", new { id = lineId });
});

adj.MapDelete("/{id:guid}/lines/{lineId:guid}", async (Guid id, Guid lineId, IMediator m) =>
{
    await m.Send(new RemoveAdjustmentLineCommand(id, lineId));
    return Results.NoContent();
});

adj.MapPut("/{id:guid}", async (Guid id, UpdateAdjustmentHeaderCommand body, IMediator m) =>
{
    await m.Send(body with { AdjustmentId = id });
    return Results.NoContent();
});

adj.MapPut("/{id:guid}/lines/{lineId:guid}", async (Guid id, Guid lineId, UpdateAdjustmentLineCommand body, IMediator m) =>
{
    await m.Send(body with { AdjustmentId = id, LineId = lineId });
    return Results.NoContent();
});

adj.MapPost("/{id:guid}/post", async (Guid id, IMediator m) =>
{
    await m.Send(new PostAdjustmentCommand(id));
    return Results.NoContent();
});

adj.MapPost("/{id:guid}/cancel", async (Guid id, IMediator m) =>
{
    await m.Send(new CancelAdjustmentCommand(id));
    return Results.NoContent();
});


// --- Costs & Pricing (??? ????) ---
var costs = app.MapGroup("/api/inventory/costs").DisableAntiforgery();

// ??? ????? ???? ???? ?? ?? (??????? SetStockItemPrice)
costs.MapPost("/", async (SetInventoryCostCommand cmd, IMediator m) =>
{
    var id = await m.Send(cmd);
    return Results.Ok(new { id });
});

// ?????? ????? ???? ?? ???? ???
costs.MapGet("/stock-items/{id:guid}", async (Guid id, IMediator m) =>
{
    var dto = await m.Send(new GetInventoryCostQuery { StockItemId = id });
    return Results.Ok(dto);
});

// ?????? ??????? ????? ????? (???? ??????? ?????)
costs.MapGet("/products/{pid:guid}", async (Guid pid, Guid? variantId, Guid? warehouseId, IMediator m) =>
{
    var dto = await m.Send(new GetAvailableStockCostQuery(pid, variantId, warehouseId));
    return Results.Ok(dto);
});


// --- Shelves & Operations (??? ????: ?????? ?????) ---
var ops = app.MapGroup("/api/inventory/operations").DisableAntiforgery();

// ????? ??? ????
ops.MapPost("/shelves", async (CreateStockShelfCommand cmd, IMediator m) =>
{
    var id = await m.Send(cmd);
    return Results.Created($"/api/inventory/operations/shelves/{id}", new { id });
});

// ??????? ???? (Put-away / Internal Move)
ops.MapPost("/move-stock", async (MoveStockItemCommand cmd, IMediator m) =>
{
    await m.Send(cmd);
    return Results.NoContent();
});

app.Run();
