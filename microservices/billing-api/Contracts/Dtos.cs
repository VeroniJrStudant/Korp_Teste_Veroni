namespace Korp.Billing.Api.Contracts;

public record InvoiceItemRequest(Guid ProductId, decimal Quantity);

public record CreateInvoiceRequest(IReadOnlyList<InvoiceItemRequest> Items);

public record UpdateInvoiceRequest(IReadOnlyList<InvoiceItemRequest> Items);

public record InvoiceItemResponse(Guid Id, Guid ProductId, string ProductCode, string ProductDescription, decimal Quantity);

public record InvoiceResponse(
    Guid Id,
    int Number,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ClosedAt,
    IReadOnlyList<InvoiceItemResponse> Items);

public record PrintInvoiceResponse(InvoiceResponse Invoice, bool IdempotentReplay, string Message);

public record InsightResponse(string Title, string Detail, string Severity);

public record AssistantRequest(string Question);

public record AssistantResponse(string Answer, IReadOnlyList<string> Highlights);
