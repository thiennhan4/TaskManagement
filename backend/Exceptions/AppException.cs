namespace TaskHub.backend.Exceptions;

/// <summary>
/// Base exception for all application-level errors.
/// Handled by GlobalExceptionMiddleware — maps to HTTP status codes.
/// </summary>
public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(string message, int statusCode = 400)
        : base(message)
    {
        StatusCode = statusCode;
    }
}

/// <summary>
/// 404 — Resource not found.
/// Usage: throw new NotFoundException("Task", taskId);
/// </summary>
public class NotFoundException : AppException
{
    public NotFoundException(string resource, object id)
        : base($"{resource} with id '{id}' was not found.", 404) { }

    public NotFoundException(string message)
        : base(message, 404) { }
}

/// <summary>
/// 403 — User authenticated but not authorized.
/// Usage: throw new ForbiddenException();
/// </summary>
public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "You do not have permission to perform this action.")
        : base(message, 403) { }
}

/// <summary>
/// 401 — Not authenticated or invalid credentials.
/// Usage: throw new UnauthorizedException("Invalid email or password.");
/// </summary>
public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Unauthorized.")
        : base(message, 401) { }
}

/// <summary>
/// 409 — Duplicate resource conflict.
/// Usage: throw new ConflictException("Email already exists.");
/// </summary>
public class ConflictException : AppException
{
    public ConflictException(string message)
        : base(message, 409) { }
}

/// <summary>
/// 422 — Business rule validation error.
/// Usage: throw new ValidationException("Due date must be in the future.");
/// </summary>
public class BusinessValidationException : AppException
{
    public BusinessValidationException(string message)
        : base(message, 422) { }
}

/// <summary>
/// 400 — Bad request or invalid input.
/// </summary>
public class BadRequestException : AppException
{
    public BadRequestException(string message)
        : base(message, 400) { }
}
