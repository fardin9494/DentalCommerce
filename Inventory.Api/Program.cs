using FluentValidation;
using Inventory.Application.Features.Adjustments.Commands;
using Inventory.Application.Features.Issues.Commands;
using Inventory.Application.Features.Pricing.Commands;
using Inventory.Application.Features.Pricing.Queries;
using Inventory.Application.Features.Receipts.Commands;
using Inventory.Api.Contracts.Receipts;
using Inventory.Application.Features.Stock.Commands; // ???????? ????
using Inventory.Application.Features.Shelves.Commands; // ???????? ????
using Inventory.Application.Features.Transfers.Commands;
using Inventory.Application.Features.StockLedger.Queries;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inventory.Api;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
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

// Configure JSON serialization to use string enums
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
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

// List receipts with filters and pagination
receipts.MapGet("/", async (
    Guid? warehouseId,
    int? status,
    int? reason,
    DateTime? fromDate,
    DateTime? toDate,
    string? search,
    int? page,
    int? pageSize,
    IMediator m) =>
{
    var query = new Inventory.Application.Features.Receipts.Queries.GetReceiptsListQuery(
        WarehouseId: warehouseId,
        Status: status.HasValue ? (Inventory.Domain.Enums.ReceiptStatus)status.Value : null,
        Reason: reason.HasValue ? (Inventory.Domain.Enums.ReceiptReason)reason.Value : null,
        FromDate: fromDate,
        ToDate: toDate,
        Search: search,
        Page: page ?? 1,
        PageSize: pageSize ?? 20
    );
    var result = await m.Send(query);
    return Results.Ok(result);
});

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

receipts.MapPost("/{id:guid}/lines", async (Guid id, AddReceiptLineCommand body, IMediator m, ILogger<Program> logger) =>
{
    try
    {
        var lineId = await m.Send(body with { ReceiptId = id });
        return Results.Created($"/api/inventory/receipts/{id}/lines/{lineId}", new { id = lineId });
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "Failed to add receipt line for {ReceiptId}", id);
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error adding receipt line for {ReceiptId}", id);
        return Results.Problem(detail: ex.Message, title: "خطا در افزودن خط رسید");
    }
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
receipts.MapPost("/{id:guid}/receive", async (Guid id, [FromBody] DateTime? when, IMediator m, ILogger<Program> logger) =>
{
    try
    {
        await m.Send(new ReceiveReceiptCommand(id, when));
        return Results.NoContent();
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "Failed to receive receipt {ReceiptId}", id);
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error receiving receipt {ReceiptId}", id);
        return Results.Problem(detail: ex.Message, title: "خطا در دریافت رسید");
    }
});

// ??? ????: ????? ????? (??? ????? ???? ?????)
receipts.MapPost("/{id:guid}/approve", async (Guid id, IMediator m) =>
{
    await m.Send(new ApproveReceiptCommand { ReceiptId = id });
    return Results.NoContent();
});

// تایید جزئی یک خط از رسید
receipts.MapPost("/{id:guid}/lines/{lineId:guid}/approve-partial", async (Guid id, Guid lineId, [FromBody] ApproveReceiptLinePartialBody body, IMediator m, ILogger<Program> logger) =>
{
    try
    {
        await m.Send(new ApproveReceiptLinePartialCommand(id, lineId, body.Qty));
        return Results.NoContent();
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "Failed to approve receipt line {ReceiptId}/{LineId}", id, lineId);
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error approving receipt line {ReceiptId}/{LineId}", id, lineId);
        return Results.Problem(detail: ex.Message, title: "خطا در تایید خط رسید");
    }
});

// رد کردن یک خط از رسید
receipts.MapPost("/{id:guid}/lines/{lineId:guid}/reject", async (Guid id, Guid lineId, [FromBody] RejectReceiptLineBody body, IMediator m, ILogger<Program> logger) =>
{
    try
    {
        await m.Send(new RejectReceiptLineCommand(id, lineId, body.Qty, body.Reason));
        return Results.NoContent();
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "Failed to reject receipt line {ReceiptId}/{LineId}", id, lineId);
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error rejecting receipt line {ReceiptId}/{LineId}", id, lineId);
        return Results.Problem(detail: ex.Message, title: "خطا در رد خط رسید");
    }
});

receipts.MapPost("/{id:guid}/cancel", async (Guid id, IMediator m) =>
{
    await m.Send(new CancelReceiptCommand(id));
    return Results.NoContent();
});


// --- Issues (خروج از انبار) ---
var issues = app.MapGroup("/api/inventory/issues").DisableAntiforgery();

// List issues with filters and pagination
issues.MapGet("/", async (
    Guid? warehouseId,
    int? status,
    DateTime? fromDate,
    DateTime? toDate,
    string? search,
    int? page,
    int? pageSize,
    IMediator m,
    ILogger<Program> logger) =>
{
    try
    {
        var query = new Inventory.Application.Features.Issues.Queries.GetIssuesListQuery(
            WarehouseId: warehouseId,
            Status: status.HasValue ? (Inventory.Domain.Enums.IssueStatus)status.Value : null,
            FromDate: fromDate,
            ToDate: toDate,
            Search: search,
            Page: page ?? 1,
            PageSize: pageSize ?? 20
        );
        var result = await m.Send(query);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error getting issues list");
        return Results.Problem(detail: ex.Message, title: "خطا در دریافت لیست خروجی‌ها");
    }
});

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

issues.MapPost("/{id:guid}/lines/{lineId:guid}/allocate-fefo", async (Guid id, Guid lineId, [FromBody] Guid? preferredWarehouseId, IMediator m, ILogger<Program> logger) =>
{
    try
    {
        var allocations = await m.Send(new AllocateIssueLineFefoCommand(id, lineId, preferredWarehouseId));
        return Results.Ok(allocations);
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "Failed to allocate issue line {LineId} with FEFO", lineId);
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error allocating issue line {LineId} with FEFO", lineId);
        return Results.Problem(detail: ex.Message, title: "خطا در تخصیص FEFO");
    }
});

issues.MapPost("/{id:guid}/lines/{lineId:guid}/allocate-fifo", async (Guid id, Guid lineId, [FromBody] Guid? preferredWarehouseId, IMediator m, ILogger<Program> logger) =>
{
    try
    {
        var allocations = await m.Send(new AllocateIssueLineFifoCommand(id, lineId, preferredWarehouseId));
        return Results.Ok(allocations);
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "Failed to allocate issue line {LineId} with FIFO", lineId);
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error allocating issue line {LineId} with FIFO", lineId);
        return Results.Problem(detail: ex.Message, title: "خطا در تخصیص FIFO");
    }
});

issues.MapPost("/{id:guid}/lines/{lineId:guid}/allocate-lifo", async (Guid id, Guid lineId, [FromBody] Guid? preferredWarehouseId, IMediator m, ILogger<Program> logger) =>
{
    try
    {
        var allocations = await m.Send(new AllocateIssueLineLifoCommand(id, lineId, preferredWarehouseId));
        return Results.Ok(allocations);
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "Failed to allocate issue line {LineId} with LIFO", lineId);
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error allocating issue line {LineId} with LIFO", lineId);
        return Results.Problem(detail: ex.Message, title: "خطا در تخصیص LIFO");
    }
});

issues.MapPost("/{id:guid}/post", async (Guid id, [FromBody] DateTime? when, IMediator m, ILogger<Program> logger) =>
{
    try
    {
        await m.Send(new PostIssueCommand(id, when));
        return Results.NoContent();
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "Failed to post issue {IssueId}", id);
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error posting issue {IssueId}", id);
        return Results.Problem(detail: ex.Message, title: "خطا در ثبت خروجی");
    }
});

issues.MapPost("/{id:guid}/cancel", async (Guid id, IMediator m) =>
{
    await m.Send(new CancelIssueCommand(id));
    return Results.NoContent();
});


// --- Transfers (انتقال بین انبارها) ---
var transfers = app.MapGroup("/api/inventory/transfers").DisableAntiforgery();

// List transfers with filters and pagination
transfers.MapGet("/", async (
    Guid? sourceWarehouseId,
    Guid? destinationWarehouseId,
    int? status,
    DateTime? fromDate,
    DateTime? toDate,
    string? search,
    int? page,
    int? pageSize,
    IMediator m,
    ILogger<Program> logger) =>
{
    try
    {
        var query = new Inventory.Application.Features.Transfers.Queries.GetTransfersListQuery(
            SourceWarehouseId: sourceWarehouseId,
            DestinationWarehouseId: destinationWarehouseId,
            Status: status.HasValue ? (Inventory.Domain.Enums.TransferStatus)status.Value : null,
            FromDate: fromDate,
            ToDate: toDate,
            Search: search,
            Page: page ?? 1,
            PageSize: pageSize ?? 20
        );
        var result = await m.Send(query);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error getting transfers list");
        return Results.Problem(detail: ex.Message, title: "خطا در دریافت لیست انتقالات");
    }
});

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

transfers.MapPost("/{id:guid}/lines/{lineId:guid}/allocate-fefo", async (Guid id, Guid lineId, IMediator m, ILogger<Program> logger) =>
{
    try
    {
        var res = await m.Send(new AllocateTransferLineFefoCommand(id, lineId));
        return Results.Ok(res);
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "Failed to allocate transfer line {LineId} for transfer {TransferId} (FEFO)", lineId, id);
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error allocating transfer line {LineId} for transfer {TransferId} (FEFO)", lineId, id);
        return Results.Problem(detail: ex.Message, title: "خطا در تخصیص موجودی (FEFO)");
    }
});

transfers.MapPost("/{id:guid}/lines/{lineId:guid}/allocate-fifo", async (Guid id, Guid lineId, IMediator m, ILogger<Program> logger) =>
{
    try
    {
        var res = await m.Send(new AllocateTransferLineFifoCommand(id, lineId));
        return Results.Ok(res);
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "Failed to allocate transfer line {LineId} for transfer {TransferId} (FIFO)", lineId, id);
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error allocating transfer line {LineId} for transfer {TransferId} (FIFO)", lineId, id);
        return Results.Problem(detail: ex.Message, title: "خطا در تخصیص موجودی (FIFO)");
    }
});

transfers.MapPost("/{id:guid}/lines/{lineId:guid}/allocate-lifo", async (Guid id, Guid lineId, IMediator m, ILogger<Program> logger) =>
{
    try
    {
        var res = await m.Send(new AllocateTransferLineLifoCommand(id, lineId));
        return Results.Ok(res);
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "Failed to allocate transfer line {LineId} for transfer {TransferId} (LIFO)", lineId, id);
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error allocating transfer line {LineId} for transfer {TransferId} (LIFO)", lineId, id);
        return Results.Problem(detail: ex.Message, title: "خطا در تخصیص موجودی (LIFO)");
    }
});

transfers.MapPost("/{id:guid}/ship", async (Guid id, IMediator m, ILogger<Program> logger) =>
{
    try
    {
        await m.Send(new ShipTransferCommand(id));
        return Results.NoContent();
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "Failed to ship transfer {TransferId}", id);
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        logger.LogWarning(ex, "Invalid argument for shipping transfer {TransferId}", id);
        return Results.BadRequest(new { error = ex.Message, param = ex.ParamName });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error shipping transfer {TransferId}", id);
        return Results.Problem(detail: ex.Message, title: "خطا در ارسال سند انتقال");
    }
});

transfers.MapPost("/{id:guid}/receive", async (Guid id, ReceiveTransferCommand body, IMediator m, ILogger<Program> logger) =>
{
    try
    {
        await m.Send(body with { TransferId = id });
        return Results.NoContent();
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "Failed to receive transfer {TransferId} segment {SegmentId}", id, body.SegmentId);
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        logger.LogWarning(ex, "Invalid argument for receiving transfer {TransferId} segment {SegmentId}", id, body.SegmentId);
        return Results.BadRequest(new { error = ex.Message, param = ex.ParamName });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error receiving transfer {TransferId} segment {SegmentId}", id, body.SegmentId);
        return Results.Problem(detail: ex.Message, title: "خطا در ثبت دریافت انتقال");
    }
});

transfers.MapPost("/{id:guid}/complete", async (Guid id, IMediator m, ILogger<Program> logger) =>
{
    try
    {
        await m.Send(new CompleteTransferCommand(id));
        return Results.Ok(new { message = "انتقال با موفقیت تایید شد." });
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "خطا در تایید نهایی انتقال {TransferId}", id);
        return Results.BadRequest(new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            title = "خطا در تایید نهایی انتقال",
            status = 400,
            detail = ex.Message
        });
    }
    catch (ArgumentException ex)
    {
        logger.LogWarning(ex, "خطای اعتبارسنجی در تایید نهایی انتقال {TransferId}", id);
        return Results.BadRequest(new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            title = "خطا در تایید نهایی انتقال",
            status = 400,
            detail = ex.Message
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "خطای غیرمنتظره در تایید نهایی انتقال {TransferId}", id);
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "خطا در تایید نهایی انتقال"
        );
    }
});

transfers.MapPost("/{id:guid}/cancel", async (Guid id, IMediator m) =>
{
    await m.Send(new CancelTransferCommand(id));
    return Results.NoContent();
});


// --- Adjustments (اصلاح موجودی / انبارگردانی) ---
var adj = app.MapGroup("/api/inventory/adjustments").DisableAntiforgery();

adj.MapGet("/", async (
    Guid? warehouseId,
    int? status,
    int? reason,
    DateTime? fromDate,
    DateTime? toDate,
    string? search,
    int? page,
    int? pageSize,
    IMediator m,
    ILogger<Program> logger) =>
{
    try
    {
        var query = new Inventory.Application.Features.Adjustments.Queries.GetAdjustmentsListQuery(
            WarehouseId: warehouseId,
            Status: status.HasValue ? (Inventory.Domain.Enums.AdjustmentStatus)status.Value : null,
            Reason: reason.HasValue ? (Inventory.Domain.Enums.AdjustmentReason)reason.Value : null,
            FromDate: fromDate,
            ToDate: toDate,
            Search: search,
            Page: page ?? 1,
            PageSize: pageSize ?? 20
        );
        var result = await m.Send(query);
        return Results.Ok(result);
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "خطا در دریافت لیست اصلاحات موجودی");
        return Results.BadRequest(new { error = ex.Message, type = "InvalidOperation" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "خطا در دریافت لیست اصلاحات موجودی");
        return Results.Problem(
            detail: ex.Message,
            title: "خطا در دریافت لیست اصلاحات موجودی",
            statusCode: StatusCodes.Status500InternalServerError
        );
    }
});

adj.MapGet("/{id:guid}", async (Guid id, IMediator m, ILogger<Program> logger) =>
{
    try
    {
        var dto = await m.Send(new Inventory.Application.Features.Adjustments.Queries.AdjustmentDetailsQuery(id));
        return dto is null ? Results.NotFound(new { error = "اصلاح موجودی یافت نشد", id }) : Results.Ok(dto);
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "خطا در دریافت جزئیات اصلاح موجودی {AdjustmentId}", id);
        return Results.BadRequest(new { error = ex.Message, type = "InvalidOperation", id });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "خطا در دریافت جزئیات اصلاح موجودی {AdjustmentId}", id);
        return Results.Problem(
            detail: ex.Message,
            title: "خطا در دریافت جزئیات اصلاح موجودی",
            statusCode: StatusCodes.Status500InternalServerError
        );
    }
});

adj.MapPost("/", async (CreateAdjustmentDraftCommand cmd, IMediator m, ILogger<Program> logger) =>
{
    try
    {
        var id = await m.Send(cmd);
        return Results.Created($"/api/inventory/adjustments/{id}", new { id });
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "خطا در ایجاد اصلاح موجودی جدید");
        return Results.BadRequest(new { error = ex.Message, type = "InvalidOperation" });
    }
    catch (ArgumentException ex)
    {
        logger.LogWarning(ex, "خطا در اعتبارسنجی داده‌های ایجاد اصلاح موجودی");
        return Results.BadRequest(new { error = ex.Message, type = "Validation", paramName = ex.ParamName });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "خطا در ایجاد اصلاح موجودی جدید");
        return Results.Problem(
            detail: ex.Message,
            title: "خطا در ایجاد اصلاح موجودی",
            statusCode: StatusCodes.Status500InternalServerError
        );
    }
});

adj.MapPost("/{id:guid}/lines", async (Guid id, AddAdjustmentLineCommand body, IMediator m, ILogger<Program> logger) =>
{
    try
    {
        var lineId = await m.Send(body with { AdjustmentId = id });
        return Results.Created($"/api/inventory/adjustments/{id}/lines/{lineId}", new { id = lineId });
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "خطا در افزودن خط به اصلاح موجودی {AdjustmentId}", id);
        return Results.BadRequest(new { error = ex.Message, type = "InvalidOperation", adjustmentId = id });
    }
    catch (ArgumentException ex)
    {
        logger.LogWarning(ex, "خطا در اعتبارسنجی داده‌های خط اصلاح موجودی");
        return Results.BadRequest(new { error = ex.Message, type = "Validation", paramName = ex.ParamName });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "خطا در افزودن خط به اصلاح موجودی {AdjustmentId}", id);
        return Results.Problem(
            detail: ex.Message,
            title: "خطا در افزودن خط اصلاح موجودی",
            statusCode: StatusCodes.Status500InternalServerError
        );
    }
});

adj.MapDelete("/{id:guid}/lines/{lineId:guid}", async (Guid id, Guid lineId, IMediator m, ILogger<Program> logger) =>
{
    try
    {
        await m.Send(new RemoveAdjustmentLineCommand(id, lineId));
        return Results.NoContent();
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "خطا در حذف خط از اصلاح موجودی {AdjustmentId}, LineId: {LineId}", id, lineId);
        return Results.BadRequest(new { error = ex.Message, type = "InvalidOperation", adjustmentId = id, lineId });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "خطا در حذف خط از اصلاح موجودی {AdjustmentId}, LineId: {LineId}", id, lineId);
        return Results.Problem(
            detail: ex.Message,
            title: "خطا در حذف خط اصلاح موجودی",
            statusCode: StatusCodes.Status500InternalServerError
        );
    }
});

adj.MapPut("/{id:guid}", async (Guid id, UpdateAdjustmentHeaderCommand body, IMediator m, ILogger<Program> logger) =>
{
    try
    {
        await m.Send(body with { AdjustmentId = id });
        return Results.NoContent();
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "خطا در به‌روزرسانی هدر اصلاح موجودی {AdjustmentId}", id);
        return Results.BadRequest(new { error = ex.Message, type = "InvalidOperation", adjustmentId = id });
    }
    catch (ArgumentException ex)
    {
        logger.LogWarning(ex, "خطا در اعتبارسنجی داده‌های به‌روزرسانی هدر اصلاح موجودی");
        return Results.BadRequest(new { error = ex.Message, type = "Validation", paramName = ex.ParamName });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "خطا در به‌روزرسانی هدر اصلاح موجودی {AdjustmentId}", id);
        return Results.Problem(
            detail: ex.Message,
            title: "خطا در به‌روزرسانی هدر اصلاح موجودی",
            statusCode: StatusCodes.Status500InternalServerError
        );
    }
});

adj.MapPut("/{id:guid}/lines/{lineId:guid}", async (Guid id, Guid lineId, UpdateAdjustmentLineCommand body, IMediator m, ILogger<Program> logger) =>
{
    try
    {
        await m.Send(body with { AdjustmentId = id, LineId = lineId });
        return Results.NoContent();
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "خطا در به‌روزرسانی خط اصلاح موجودی {AdjustmentId}, LineId: {LineId}", id, lineId);
        return Results.BadRequest(new { error = ex.Message, type = "InvalidOperation", adjustmentId = id, lineId });
    }
    catch (ArgumentException ex)
    {
        logger.LogWarning(ex, "خطا در اعتبارسنجی داده‌های به‌روزرسانی خط اصلاح موجودی");
        return Results.BadRequest(new { error = ex.Message, type = "Validation", paramName = ex.ParamName });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "خطا در به‌روزرسانی خط اصلاح موجودی {AdjustmentId}, LineId: {LineId}", id, lineId);
        return Results.Problem(
            detail: ex.Message,
            title: "خطا در به‌روزرسانی خط اصلاح موجودی",
            statusCode: StatusCodes.Status500InternalServerError
        );
    }
});

adj.MapPost("/{id:guid}/post", async (Guid id, IMediator m, ILogger<Program> logger) =>
{
    try
    {
        await m.Send(new PostAdjustmentCommand(id));
        return Results.NoContent();
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "خطا در ثبت اصلاح موجودی {AdjustmentId}", id);
        return Results.BadRequest(new { error = ex.Message, type = "InvalidOperation", adjustmentId = id });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "خطا در ثبت اصلاح موجودی {AdjustmentId}", id);
        return Results.Problem(
            detail: ex.Message,
            title: "خطا در ثبت اصلاح موجودی",
            statusCode: StatusCodes.Status500InternalServerError
        );
    }
});

adj.MapPost("/{id:guid}/cancel", async (Guid id, IMediator m, ILogger<Program> logger) =>
{
    try
    {
        await m.Send(new CancelAdjustmentCommand(id));
        return Results.NoContent();
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "خطا در لغو اصلاح موجودی {AdjustmentId}", id);
        return Results.BadRequest(new { error = ex.Message, type = "InvalidOperation", adjustmentId = id });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "خطا در لغو اصلاح موجودی {AdjustmentId}", id);
        return Results.Problem(
            detail: ex.Message,
            title: "خطا در لغو اصلاح موجودی",
            statusCode: StatusCodes.Status500InternalServerError
        );
    }
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

// Shelves endpoints
var shelves = app.MapGroup("/api/inventory/shelves").DisableAntiforgery();

shelves.MapGet("/", async (Guid? warehouseId, bool? isActive, IMediator m) =>
{
    var result = await m.Send(new Inventory.Application.Features.Shelves.Queries.GetShelvesListQuery(warehouseId, isActive));
    return Results.Ok(result);
});

shelves.MapPost("/", async (CreateStockShelfCommand cmd, IMediator m) =>
{
    var id = await m.Send(cmd);
    return Results.Created($"/api/inventory/shelves/{id}", new { id });
});

shelves.MapPost("/batch", async (Inventory.Application.Features.Shelves.Commands.CreateStockShelvesBatchCommand cmd, IMediator m, ILogger<Program> logger) =>
{
    try
    {
        var created = await m.Send(cmd);
        return Results.Ok(new { created });
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "Failed to batch create shelves for warehouse {WarehouseId}", cmd.WarehouseId);
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error batch creating shelves for warehouse {WarehouseId}", cmd.WarehouseId);
        return Results.Problem(detail: ex.Message, title: "خطا در ایجاد گروهی قفسه‌ها");
    }
});

shelves.MapPut("/{id:guid}", async (Guid id, Inventory.Application.Features.Shelves.Commands.UpdateStockShelfCommand body, IMediator m) =>
{
    await m.Send(body with { Id = id });
    return Results.NoContent();
});

shelves.MapDelete("/{id:guid}", async (Guid id, IMediator m, ILogger<Program> logger) =>
{
    try
    {
        await m.Send(new Inventory.Application.Features.Shelves.Commands.DeleteStockShelfCommand(id));
        return Results.NoContent();
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "Failed to delete shelf {ShelfId}", id);
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error deleting shelf {ShelfId}", id);
        return Results.Problem(detail: ex.Message, title: "خطا در حذف قفسه");
    }
});

shelves.MapPost("/{id:guid}/activate", async (Guid id, IMediator m) =>
{
    await m.Send(new Inventory.Application.Features.Shelves.Commands.ActivateStockShelfCommand(id));
    return Results.NoContent();
});

shelves.MapPost("/{id:guid}/deactivate", async (Guid id, IMediator m) =>
{
    await m.Send(new Inventory.Application.Features.Shelves.Commands.DeactivateStockShelfCommand(id));
    return Results.NoContent();
});

// ????? ??? ????
ops.MapPost("/shelves", async (CreateStockShelfCommand cmd, IMediator m) =>
{
    var id = await m.Send(cmd);
    return Results.Created($"/api/inventory/operations/shelves/{id}", new { id });
});

// ??????? ???? (Put-away / Internal Move)
ops.MapPost("/move-stock", async (MoveStockItemCommand cmd, IMediator m, ILogger<Program> logger) =>
{
    try
    {
        await m.Send(cmd);
        return Results.NoContent();
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "Failed to move stock to shelf");
        return Results.Problem(
            title: "خطا در انتقال کالا",
            detail: ex.Message,
            statusCode: StatusCodes.Status400BadRequest);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error moving stock to shelf");
        return Results.Problem(
            title: "خطا در انتقال کالا",
            detail: ex.Message,
            statusCode: StatusCodes.Status500InternalServerError);
    }
});


// --- Warehouses (انبارها) ---
var warehouses = app.MapGroup("/api/inventory/warehouses").DisableAntiforgery();

warehouses.MapGet("/", async (bool? isActive, IMediator m) =>
{
    var result = await m.Send(new Inventory.Application.Features.Warehouses.Queries.GetWarehousesListQuery(isActive));
    return Results.Ok(result);
});

warehouses.MapPost("/", async (Inventory.Application.Features.Warehouses.Commands.CreateWarehouseCommand cmd, IMediator m) =>
{
    var id = await m.Send(cmd);
    return Results.Created($"/api/inventory/warehouses/{id}", new { id });
});

warehouses.MapPut("/{id:guid}", async (Guid id, Inventory.Application.Features.Warehouses.Commands.UpdateWarehouseCommand body, IMediator m) =>
{
    await m.Send(body with { Id = id });
    return Results.NoContent();
});

warehouses.MapPost("/{id:guid}/activate", async (Guid id, IMediator m) =>
{
    await m.Send(new Inventory.Application.Features.Warehouses.Commands.ActivateWarehouseCommand(id));
    return Results.NoContent();
});

warehouses.MapPost("/{id:guid}/deactivate", async (Guid id, IMediator m) =>
{
    await m.Send(new Inventory.Application.Features.Warehouses.Commands.DeactivateWarehouseCommand(id));
    return Results.NoContent();
});


// --- Stock Ledger (کاردکس انبار) ---
var stockLedger = app.MapGroup("/api/inventory/stock-ledger").DisableAntiforgery();

stockLedger.MapGet("/{id:guid}", async (Guid id, IMediator m) =>
{
    var query = new GetStockLedgerEntryDetailsQuery(id);
    var result = await m.Send(query);
    if (result is null) return Results.NotFound();
    return Results.Ok(result);
});

stockLedger.MapGet("/", async (
    Guid? warehouseId,
    Guid? productId,
    Guid? variantId,
    int? movementType,
    string? refDocType,
    Guid? refDocId,
    DateTime? fromDate,
    DateTime? toDate,
    string? sortBy,
    string? sortDirection,
    int page = 1,
    int pageSize = 50,
    IMediator m = null!) =>
{
    var parsedSortField = Enum.TryParse<StockLedgerSortField>(sortBy ?? string.Empty, ignoreCase: true, out var sortField)
        ? sortField
        : StockLedgerSortField.Timestamp;

    var parsedSortDirection = Enum.TryParse<SortDirection>(sortDirection ?? string.Empty, ignoreCase: true, out var directionValue)
        ? directionValue
        : SortDirection.Desc;

    var query = new GetStockLedgerQuery(
        WarehouseId: warehouseId,
        ProductId: productId,
        VariantId: variantId,
        MovementType: movementType.HasValue ? (StockMovementType?)movementType.Value : null,
        RefDocType: refDocType,
        RefDocId: refDocId,
        FromDate: fromDate,
        ToDate: toDate,
        Page: page,
        PageSize: pageSize,
        SortBy: parsedSortField,
        SortDirection: parsedSortDirection
    );
    return await m.Send(query);
});

// --- Stock Items (موجودی‌های انبار) ---
var stockItems = app.MapGroup("/api/inventory/stock-items").DisableAntiforgery();

stockItems.MapGet("/", async (
    Guid? warehouseId,
    Guid? productId,
    Guid? variantId,
    Guid? shelfId,
    string? search,
    bool? hasStock,
    bool? shelvedOnly,
    int? page,
    int? pageSize,
    IMediator m) =>
{
    var query = new Inventory.Application.Features.Stock.Queries.GetStockItemsListQuery(
        WarehouseId: warehouseId,
        ProductId: productId,
        VariantId: variantId,
        ShelfId: shelfId,
        Search: search,
        HasStock: hasStock,
        ShelvedOnly: shelvedOnly,
        Page: page ?? 1,
        PageSize: pageSize ?? 20
    );
    var result = await m.Send(query);
    return Results.Ok(result);
});

stockItems.MapGet("/unassigned", async (
    Guid? warehouseId,
    Guid? productId,
    string? search,
    int? page,
    int? pageSize,
    IMediator m) =>
{
    var query = new Inventory.Application.Features.Stock.Queries.GetUnassignedStockItemsQuery(
        WarehouseId: warehouseId,
        ProductId: productId,
        Search: search,
        Page: page ?? 1,
        PageSize: pageSize ?? 20
    );
    var result = await m.Send(query);
    return Results.Ok(result);
});

stockItems.MapGet("/products", async (
    Guid warehouseId,
    string? search,
    int? page,
    int? pageSize,
    IMediator m) =>
{
    var query = new Inventory.Application.Features.Stock.Queries.GetStockProductsQuery(
        WarehouseId: warehouseId,
        Search: search,
        Page: page ?? 1,
        PageSize: pageSize ?? 50
    );
    var result = await m.Send(query);
    return Results.Ok(result);
});


// --- Catalog Proxy (برای جستجوی محصولات از کاتالوگ) ---
var catalogProxy = app.MapGroup("/api/inventory/catalog").DisableAntiforgery();

// Search products from Catalog API
catalogProxy.MapGet("/products", async (
    string? search,
    int? page,
    int? pageSize,
    IHttpClientFactory httpClientFactory,
    ILogger<Program> logger) =>
{
    try
    {
        var client = httpClientFactory.CreateClient("CatalogApi");
        var queryParams = new List<string>();
        
        queryParams.Add($"page={page ?? 1}");
        queryParams.Add($"pageSize={pageSize ?? 20}");
        if (!string.IsNullOrWhiteSpace(search))
            queryParams.Add($"search={Uri.EscapeDataString(search)}");
        
        var url = $"/api/catalog/products?{string.Join("&", queryParams)}";
        var response = await client.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Catalog API returned {StatusCode} for products search", response.StatusCode);
            return Results.StatusCode((int)response.StatusCode);
        }
        
        var json = await response.Content.ReadAsStringAsync();
        return Results.Content(json, "application/json");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error fetching products from Catalog API");
        return Results.Problem("خطا در دریافت لیست محصولات از کاتالوگ");
    }
});

// Get product detail with variants
catalogProxy.MapGet("/products/{id:guid}", async (
    Guid id,
    IHttpClientFactory httpClientFactory,
    ILogger<Program> logger) =>
{
    try
    {
        var client = httpClientFactory.CreateClient("CatalogApi");
        var response = await client.GetAsync($"/api/catalog/products/{id}");
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return Results.NotFound();
        
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Catalog API returned {StatusCode} for product {ProductId}", response.StatusCode, id);
            return Results.StatusCode((int)response.StatusCode);
        }
        
        var json = await response.Content.ReadAsStringAsync();
        return Results.Content(json, "application/json");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error fetching product {ProductId} from Catalog API", id);
        return Results.Problem("خطا در دریافت اطلاعات محصول از کاتالوگ");
    }
});

app.Run();
