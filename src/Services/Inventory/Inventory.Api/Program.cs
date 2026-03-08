using System.Collections.Concurrent;
using Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var stock = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase)
{
    ["SKU-LAPTOP"] = 25,
    ["SKU-MONITOR"] = 18,
    ["SKU-KEYBOARD"] = 50
};
var reservations = new ConcurrentDictionary<Guid, (string Sku, int Units)>();

app.MapGet("/health", () => Results.Ok(new { service = "inventory", status = "healthy", timestampUtc = DateTime.UtcNow }));

app.MapGet("/api/inventory/items", () =>
{
    var items = stock.Select(x => new InventoryItem(x.Key, x.Key.Replace("SKU-", ""), x.Value)).OrderBy(x => x.Sku);
    return Results.Ok(items);
});

app.MapPost("/api/inventory/reserve", (ReserveInventoryRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Sku) || request.Units <= 0)
    {
        return Results.BadRequest(new { message = "SKU and units must be valid." });
    }

    if (!stock.TryGetValue(request.Sku, out var available) || available < request.Units)
    {
        return Results.Ok(new ReserveInventoryResponse(Guid.Empty, false, "Insufficient inventory."));
    }

    if (stock.TryUpdate(request.Sku, available - request.Units, available))
    {
        var reservationId = Guid.NewGuid();
        reservations[reservationId] = (request.Sku, request.Units);
        return Results.Ok(new ReserveInventoryResponse(reservationId, true, "Reserved."));
    }

    return Results.Ok(new ReserveInventoryResponse(Guid.Empty, false, "Inventory contention, retry."));
});

app.MapPost("/api/inventory/release/{reservationId:guid}", (Guid reservationId) =>
{
    if (!reservations.TryRemove(reservationId, out var reservation))
    {
        return Results.NotFound(new { message = "Reservation not found." });
    }

    stock.AddOrUpdate(reservation.Sku, reservation.Units, (_, current) => current + reservation.Units);
    return Results.Ok(new { released = true, reservationId });
});

app.Run();
