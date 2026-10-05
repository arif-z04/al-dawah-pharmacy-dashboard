namespace AlDawahPharma.Application.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string entityName, object key) : base($"{entityName} with ID '{key}' was not found.") { }
}

public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IDictionary<string, string[]> errors) 
        : base("One or more validation failures have occurred.")
    {
        Errors = errors;
    }
}

public class BusinessRuleException : Exception
{
    public int? OracleErrorCode { get; }

    public BusinessRuleException(string message, int? oracleErrorCode = null) : base(message)
    {
        OracleErrorCode = oracleErrorCode;
    }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "Invalid credentials or token expired.") : base(message) { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "You do not have permission to perform this action.") : base(message) { }
}
