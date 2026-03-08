using Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddHttpClient("orders", client =>
{
	client.BaseAddress = new Uri(builder.Configuration["Services:OrdersUrl"] ?? "http://localhost:5103");
});
builder.Services.AddHttpClient("inventory", client =>
{
	client.BaseAddress = new Uri(builder.Configuration["Services:InventoryUrl"] ?? "http://localhost:5101");
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { service = "gateway", status = "healthy", timestampUtc = DateTime.UtcNow }));

app.MapGet("/api/distributed/stock", async (IHttpClientFactory factory, CancellationToken ct) =>
{
	var client = factory.CreateClient("inventory");
	var items = await client.GetFromJsonAsync<List<InventoryItem>>("/api/inventory/items", ct);
	return Results.Ok(items ?? new List<InventoryItem>());
});

app.MapGet("/api/distributed/orders", async (IHttpClientFactory factory, CancellationToken ct) =>
{
	var client = factory.CreateClient("orders");
	var orders = await client.GetFromJsonAsync<List<OrderResult>>("/api/orders", ct);
	return Results.Ok(orders ?? new List<OrderResult>());
});

app.MapPost("/api/distributed/checkout", async (CheckoutRequest request, IHttpClientFactory factory, CancellationToken ct) =>
{
	var client = factory.CreateClient("orders");
	var response = await client.PostAsJsonAsync("/api/orders/submit", request, ct);
	var content = await response.Content.ReadAsStringAsync(ct);
	return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
});

app.MapGet("/", () => Results.Redirect("/api/distributed/stock"));

app.Run();
