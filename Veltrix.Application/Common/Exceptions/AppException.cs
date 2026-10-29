namespace Veltrix.Application.Common.Exceptions;

/// <summary>
/// Base class for "expected" errors (business rule broken, not found, etc.).
/// The global exception middleware turns these into ProblemDetails with the given status code.
/// Anything that is NOT an AppException is treated as an unexpected 500 error.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(int statusCode, string title, string message) : base(message)
    {
        StatusCode = statusCode;
        Title = title;
    }

    public int StatusCode { get; }
    public string Title { get; }
}

public sealed class BadRequestException : AppException
{
    public BadRequestException(string message) : base(400, "Request rejected", message) { }
}

public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string message) : base(401, "Unauthorized", message) { }
}

public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(403, "Forbidden", message) { }
}

public sealed class NotFoundException : AppException
{
    public NotFoundException(string message) : base(404, "Not found", message) { }
}

public sealed class ConflictException : AppException
{
    public ConflictException(string message) : base(409, "Conflict", message) { }
}

public sealed class AccountLockedException : AppException
{
    public AccountLockedException(DateTime lockedUntil)
        : base(423, "Account locked", $"Too many failed login attempts. Try again after {lockedUntil:yyyy-MM-dd HH:mm} UTC.")
    {
        LockedUntil = lockedUntil;
    }

    public DateTime LockedUntil { get; }
}
