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

var builder = WebApplication.CreateBuilder(args);

// 1. Database Configuration (Inventory Only)
builder.Services.AddDbContext<InventoryDbContext>(opt =>
{
    // ????? ???? ?? appsettings.json ????? ?????? ??????? InventoryDb ?? ?????
    var cs = builder.Configuration.GetConnectionString("InventoryDb")
             ?? throw new InvalidOperationException("Connection string 'InventoryDb' is not configured.");
    opt.UseSqlServer(cs);
});

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Inventory.Application.Markers.AssemblyMarker).Assembly);
});

builder.Services.AddValidatorsFromAssembly(typeof(Inventory.Application.Markers.AssemblyMarker).Assembly);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// 3. Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// ==========================================
// INVENTORY ENDPOINTS
// ==========================================

// --- Receipts (???? ????) ---
var receipts = app.MapGroup("/api/inventory/receipts").DisableAntiforgery();

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


// --- Issues (???? ????) ---
var issues = app.MapGroup("/api/inventory/issues").DisableAntiforgery();

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


// --- Transfers (?????? ??? ?????) ---
var transfers = app.MapGroup("/api/inventory/transfers").DisableAntiforgery();

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


// --- Adjustments (???????????) ---
var adj = app.MapGroup("/api/inventory/adjustments").DisableAntiforgery();

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