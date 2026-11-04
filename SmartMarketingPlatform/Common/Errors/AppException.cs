namespace SmartMarketingPlatform.Common.Errors;

// Services throw these; GlobalExceptionHandler turns them into ProblemDetails responses.
// This keeps controllers thin: they never decide error status codes themselves.
public abstract class AppException(int statusCode, string title, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
}

// 400: the request is well-formed JSON but breaks a business rule (e.g. an invalid URL).
public sealed class BadRequestException(string message)
    : AppException(StatusCodes.Status400BadRequest, "Bad request", message);

// 401: who you are could not be proven (wrong email/password, token without the needed claims).
public sealed class UnauthorizedException(string message)
    : AppException(StatusCodes.Status401Unauthorized, "Unauthorized", message);

// 403: the user is known but is not allowed to do this.
public sealed class ForbiddenException(string message)
    : AppException(StatusCodes.Status403Forbidden, "Forbidden", message);

// 404: the resource does not exist (or belongs to another tenant: we never reveal which).
public sealed class NotFoundException(string message)
    : AppException(StatusCodes.Status404NotFound, "Not found", message);

// 409: the request conflicts with the current state (duplicate, wrong status for this action).
public sealed class ConflictException(string message)
    : AppException(StatusCodes.Status409Conflict, "Conflict", message);

// 413: the uploaded file is bigger than allowed for its kind.
public sealed class PayloadTooLargeException(string message)
    : AppException(StatusCodes.Status413PayloadTooLarge, "Payload too large", message);

// 415: the uploaded file's real type (detected from its bytes) is not accepted.
public sealed class UnsupportedMediaTypeException(string message)
    : AppException(StatusCodes.Status415UnsupportedMediaType, "Unsupported media type", message);

// 502: a service we depend on (the AI provider) failed. The failure is recorded in AiExecutions.
public sealed class BadGatewayException(string message)
    : AppException(StatusCodes.Status502BadGateway, "Bad gateway", message);
