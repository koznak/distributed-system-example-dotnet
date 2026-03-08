namespace Shared.Contracts;

public sealed record InventoryItem(string Sku, string Name, int AvailableUnits);

public sealed record ReserveInventoryRequest(string Sku, int Units, string OrderReference);

public sealed record ReserveInventoryResponse(Guid ReservationId, bool Reserved, string Message);

public sealed record ChargePaymentRequest(string OrderReference, decimal Amount, string Currency, string CustomerEmail);

public sealed record ChargePaymentResponse(Guid PaymentId, bool Approved, string Message);

public sealed record CheckoutRequest(string Sku, int Units, decimal UnitPrice, string Currency, string CustomerEmail);

public sealed record OrderResult(
	Guid OrderId,
	string OrderReference,
	string Sku,
	int Units,
	decimal UnitPrice,
	decimal Total,
	string Currency,
	string Status,
	DateTime CreatedUtc,
	string TraceId);