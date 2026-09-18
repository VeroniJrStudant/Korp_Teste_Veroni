namespace Korp.Stock.Api.Errors;

public class AppException(string code, string title, string detail, int statusCode) : Exception(detail)
{
    public string Code { get; } = code;
    public string Title { get; } = title;
    public int StatusCode { get; } = statusCode;
}

public static class StockErrors
{
    public static AppException NotFound(string detail) =>
        new("NOT_FOUND", "Recurso não encontrado", detail, StatusCodes.Status404NotFound);

    public static AppException Conflict(string detail) =>
        new("CONFLICT", "Conflito de dados", detail, StatusCodes.Status409Conflict);

    public static AppException InsufficientStock(string detail) =>
        new("INSUFFICIENT_STOCK", "Saldo insuficiente", detail, StatusCodes.Status409Conflict);

    public static AppException Validation(string detail) =>
        new("VALIDATION", "Dados inválidos", detail, StatusCodes.Status400BadRequest);
}
