using System.Net;
using System.Text.Json;
using AlDawahPharma.Application.Common;
using AlDawahPharma.Application.Exceptions;
using Oracle.ManagedDataAccess.Client;

namespace AlDawahPharma.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        var message = "An unexpected server error occurred.";
        IDictionary<string, string[]>? validationErrors = null;

        switch (exception)
        {
            case NotFoundException notFoundEx:
                statusCode = HttpStatusCode.NotFound;
                message = notFoundEx.Message;
                _logger.LogWarning("Resource not found: {Message}", notFoundEx.Message);
                break;

            case ValidationException valEx:
                statusCode = HttpStatusCode.BadRequest;
                message = valEx.Message;
                validationErrors = valEx.Errors;
                _logger.LogWarning("Validation failure: {Message}", valEx.Message);
                break;

            case BusinessRuleException bizEx:
                statusCode = HttpStatusCode.BadRequest;
                message = bizEx.Message;
                _logger.LogWarning("Business rule violation: {Message}", bizEx.Message);
                break;

            case UnauthorizedException unauthEx:
                statusCode = HttpStatusCode.Unauthorized;
                message = unauthEx.Message;
                _logger.LogWarning("Unauthorized access: {Message}", unauthEx.Message);
                break;

            case ForbiddenException forbEx:
                statusCode = HttpStatusCode.Forbidden;
                message = forbEx.Message;
                _logger.LogWarning("Forbidden operation: {Message}", forbEx.Message);
                break;

            case OracleException oraEx:
                statusCode = HttpStatusCode.BadRequest;
                message = ParseOracleError(oraEx);
                _logger.LogError(oraEx, "Oracle Database Error [{ErrorNumber}]: {OracleMessage}", oraEx.Number, oraEx.Message);
                break;

            default:
                _logger.LogError(exception, "Unhandled Exception: {Message}", exception.Message);
                message = "An internal server error occurred. Please contact the administrator.";
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse.Fail(message, validationErrors);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private static string ParseOracleError(OracleException ex)
    {
        return ex.Number switch
        {
            20001 => CleanOracleMessage(ex.Message), // Insufficient stock
            20010 or 20011 => CleanOracleMessage(ex.Message), // Category duplicate
            >= 20020 and <= 20027 => CleanOracleMessage(ex.Message), // Medicine rules
            >= 20030 and <= 20035 => CleanOracleMessage(ex.Message), // Purchase rules
            >= 20040 and <= 20045 => CleanOracleMessage(ex.Message), // Sale rules
            1 => "A record with this unique identifier or code already exists.",
            2291 => "Foreign key violation: The referenced record does not exist.",
            2292 => "Cannot perform operation because dependent related records exist.",
            _ => "A database constraint or validation rule was violated."
        };
    }

    private static string CleanOracleMessage(string rawMessage)
    {
        // Extracts the clean ORA-20xxx message without line numbers
        var lines = rawMessage.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length > 0)
        {
            var first = lines[0];
            var colonIdx = first.IndexOf(':');
            if (colonIdx > 0 && colonIdx < first.Length - 1)
            {
                return first[(colonIdx + 1)..].Trim();
            }
            return first.Trim();
        }
        return rawMessage;
    }
}
