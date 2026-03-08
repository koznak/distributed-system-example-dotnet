using System.Collections.Concurrent;
using Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

builder.Services.AddHttpClient("inventory", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:InventoryUrl"] ?? "http://localhost:5101");
    client.Timeout = TimeSpan.FromSeconds(2);
});

builder.Services.AddHttpClient("payment", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:PaymentUrl"] ?? "http://localhost:5102");
    client.Timeout = TimeSpan.FromSeconds(2);
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var orders = new ConcurrentDictionary<Guid, OrderResult>();

app.MapGet("/health", () => Results.Ok(new { service = "orders", status = "healthy", timestampUtc = DateTime.UtcNow }));
app.MapGet("/api/orders", () => Results.Ok(orders.Values.OrderByDescending(x => x.CreatedUtc)));

app.MapPost("/api/orders/submit", async (CheckoutRequest request, IHttpClientFactory factory, HttpContext context, CancellationToken ct) =>
{
    if (request.Units <= 0 || request.UnitPrice <= 0 || string.IsNullOrWhiteSpace(request.Sku))
    {
        return Results.BadRequest(new { message = "Invalid checkout request." });
    }

    var traceId = context.TraceIdentifier;
    var orderReference = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";

    var inventoryClient = factory.CreateClient("inventory");
    var reserveResponse = await inventoryClient.PostAsJsonAsync("/api/inventory/reserve", new ReserveInventoryRequest(request.Sku, request.Units, orderReference), ct);
    var reserveResult = await reserveResponse.Content.ReadFromJsonAsync<ReserveInventoryResponse>(cancellationToken: ct);

    if (reserveResult is null || !reserveResult.Reserved)
    {
        return Results.Conflict(new { message = reserveResult?.Message ?? "Unable to reserve inventory.", traceId });
    }

    var total = decimal.Round(request.UnitPrice * request.Units, 2, MidpointRounding.AwayFromZero);
    var paymentRequest = new ChargePaymentRequest(orderReference, total, request.Currency, request.CustomerEmail);
    var paymentClient = factory.CreateClient("payment");

    ChargePaymentResponse? paymentResult = null;
    for (var attempt = 1; attempt <= 3; attempt++)
    {
        var payResponse = await paymentClient.PostAsJsonAsync("/api/payments/charge", paymentRequest, ct);
        paymentResult = await payResponse.Content.ReadFromJsonAsync<ChargePaymentResponse>(cancellationToken: ct);
        if (paymentResult?.Approved == true)
        {
            break;
        }

        await Task.Delay(TimeSpan.FromMilliseconds(150 * attempt), ct);
    }

    if (paymentResult?.Approved != true)
    {
        await inventoryClient.PostAsync($"/api/inventory/release/{reserveResult.ReservationId}", content: null, ct);
        return Results.Problem(
            title: "Checkout failed",
            detail: "Payment was not approved and inventory reservation was released.",
            statusCode: StatusCodes.Status502BadGateway,
            extensions: new Dictionary<string, object?> { ["traceId"] = traceId, ["orderReference"] = orderReference });
    }

    var order = new OrderResult(
        Guid.NewGuid(),
        orderReference,
        request.Sku,
        request.Units,
        request.UnitPrice,
        total,
        request.Currency,
        "confirmed",
        DateTime.UtcNow,
        traceId);

    orders[order.OrderId] = order;
    return Results.Created($"/api/orders/{order.OrderId}", order);
});

app.Run();
