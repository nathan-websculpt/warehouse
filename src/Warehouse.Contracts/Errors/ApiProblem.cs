namespace Warehouse.Contracts.Errors;

public sealed record ApiProblem(
    string? Title = null,
    string? Detail = null,
    string? TraceId = null,
    Dictionary<string, string[]>? Errors = null,
    string? Code = null,
    string? Type = null,
    int? Status = null);

// codes are API identifiers
public static class ErrorCodes
{
    public const string Validation = "VALIDATION_ERROR";
    public const string NotFound = "NOT_FOUND";
    public const string Unexpected = "UNEXPECTED_ERROR";
    public const string InvalidCursor = "INVALID_CURSOR";
    public const string InvalidThreshold = "INVALID_STOCK_THRESHOLD";
    public const string CompletionUnavailable = "TRANSFER_COMPLETION_NOT_AVAILABLE";
    public const string VersionConflict = "TRANSFER_VERSION_CONFLICT";
    public const string StatusConflict = "TRANSFER_STATUS_CONFLICT";
    public const string Immutable = "TRANSFER_IMMUTABLE";
}
