namespace Korp.Billing.Api.Errors;

public class AppException(string code, string title, string detail, int statusCode) : Exception(detail)
{
    public string Code { get; } = code;
    public string Title { get; } = title;
    public int StatusCode { get; } = statusCode;
}

public static class BillingErrors
{
    public static AppException NotFound(string detail) =>
        new("NOT_FOUND", "Recurso não encontrado", detail, StatusCodes.Status404NotFound);

    public static AppException Validation(string detail) =>
        new("VALIDATION", "Dados inválidos", detail, StatusCodes.Status400BadRequest);

    public static AppException Conflict(string detail) =>
        new("CONFLICT", "Operação não permitida", detail, StatusCodes.Status409Conflict);

    public static AppException StockUnavailable(string detail) =>
        new("STOCK_UNAVAILABLE", "Serviço de estoque indisponível", detail, StatusCodes.Status503ServiceUnavailable);

    public static AppException InsufficientStock(string detail) =>
        new("INSUFFICIENT_STOCK", "Saldo insuficiente", detail, StatusCodes.Status409Conflict);
}
