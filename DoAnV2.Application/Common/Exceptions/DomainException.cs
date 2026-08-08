namespace DoAnV2.Application.Common.Exceptions;

/// <summary>
/// Custom domain exception để Controller / ExceptionMiddleware map sang HTTP status.
/// </summary>
public class DomainException : Exception
{
    public int StatusCode { get; }

    public DomainException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }
}

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string message) : base(message, 404) { }
}

public sealed class ConflictException : DomainException
{
    public ConflictException(string message) : base(message, 409) { }
}

public sealed class ForbiddenException : DomainException
{
    public ForbiddenException(string message) : base(message, 403) { }
}

public sealed class UnauthorizedException : DomainException
{
    public UnauthorizedException(string message) : base(message, 401) { }
}

public sealed class ValidationException : DomainException
{
    public ValidationException(string message) : base(message, 422) { }
}
