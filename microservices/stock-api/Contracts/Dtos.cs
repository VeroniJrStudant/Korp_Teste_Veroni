namespace Korp.Stock.Api.Contracts;

public record ProductResponse(Guid Id, string Code, string Description, decimal Balance, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public record CreateProductRequest(string Code, string Description, decimal Balance);

public record UpdateProductRequest(string Description, decimal Balance);

public record StockOperationRequest(decimal Quantity, string OperationId);

public record StockOperationResponse(Guid ProductId, string Code, decimal Balance, string OperationId, bool IdempotentReplay);

public record SuggestDescriptionRequest(string Code, string Name);

public record SuggestDescriptionResponse(string Description, string Source);

public record ChaosResponse(bool Enabled, string Message);
