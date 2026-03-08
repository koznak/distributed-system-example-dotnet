# distributed-system-example-dotnet

Distributed system example in C#/.NET 10 with four services:

- `Gateway.Api`
- `Orders.Api`
- `Inventory.Api`
- `Payment.Api`

## Scenario

A checkout request enters through the gateway, then `Orders.Api` orchestrates:

1. Reserve stock from inventory.
2. Charge payment with retry attempts.
3. Compensate by releasing stock when payment fails.

This demonstrates orchestration, retries, and compensation patterns often used in distributed systems.

## Local run

```powershell
dotnet build distributed-system-example.slnx -c Release
dotnet test distributed-system-example.slnx -c Release

dotnet run --project src/Services/Inventory/Inventory.Api --urls http://localhost:5101
dotnet run --project src/Services/Payment/Payment.Api --urls http://localhost:5102
dotnet run --project src/Services/Orders/Orders.Api --urls http://localhost:5103
dotnet run --project src/Gateway/Gateway.Api --urls http://localhost:5100
```

## Docker run

```powershell
docker compose up --build
```

Gateway endpoint: `http://localhost:5100`

## Key endpoints

- `GET /api/distributed/stock`
- `POST /api/distributed/checkout`
- `GET /api/distributed/orders`

Example checkout payload:

```json
{
  "sku": "SKU-LAPTOP",
  "units": 1,
  "unitPrice": 1299.99,
  "currency": "USD",
  "customerEmail": "candidate@example.com"
}
```
"# distributed-system-example-dotnet" 
