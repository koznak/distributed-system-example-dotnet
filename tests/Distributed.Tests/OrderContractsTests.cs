using Shared.Contracts;

namespace Distributed.Tests;

public class OrderContractsTests
{
    [Fact]
    public void CheckoutRequest_ComputesExpectedTotal()
    {
        var request = new CheckoutRequest("SKU-LAPTOP", 2, 199.99m, "USD", "candidate@example.com");
        var expected = decimal.Round(request.Units * request.UnitPrice, 2, MidpointRounding.AwayFromZero);

        Assert.Equal(399.98m, expected);
    }

    [Fact]
    public void OrderResult_StoresTraceId()
    {
        var result = new OrderResult(
            Guid.NewGuid(),
            "ORD-1",
            "SKU-LAPTOP",
            1,
            99m,
            99m,
            "USD",
            "confirmed",
            DateTime.UtcNow,
            "trace-123");

        Assert.Equal("trace-123", result.TraceId);
    }
}
