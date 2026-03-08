using Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { service = "payment", status = "healthy", timestampUtc = DateTime.UtcNow }));

app.MapPost("/api/payments/charge", (ChargePaymentRequest request, HttpContext context) =>
{
    if (request.Amount <= 0 || string.IsNullOrWhiteSpace(request.CustomerEmail))
    {
        return Results.BadRequest(new { message = "Payment amount and customer email are required." });
    }

    var chaosHeader = context.Request.Headers["x-chaos-mode"].ToString();
    var rejectByChaos = string.Equals(chaosHeader, "fail", StringComparison.OrdinalIgnoreCase);
    var rejectByRule = request.Amount > 3000m;

    if (rejectByChaos || rejectByRule)
    {
        return Results.Ok(new ChargePaymentResponse(Guid.Empty, false, "Payment rejected by risk controls."));
    }

    return Results.Ok(new ChargePaymentResponse(Guid.NewGuid(), true, "Payment approved."));
});

app.Run();
