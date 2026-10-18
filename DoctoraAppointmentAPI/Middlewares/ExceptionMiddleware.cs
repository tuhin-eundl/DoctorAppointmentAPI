using DoctorAppointment.Application.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await next(httpContext);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(httpContext, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, logLevel) = exception switch
        {
            ValidationException => (HttpStatusCode.BadRequest, LogLevel.Warning),
            ConflictException or UserAlreadyExistsException => (HttpStatusCode.Conflict, LogLevel.Warning),
            ForbiddenException => (HttpStatusCode.Forbidden, LogLevel.Warning),
            NotFoundException => (HttpStatusCode.NotFound, LogLevel.Warning),
            _ => (HttpStatusCode.InternalServerError, LogLevel.Error)
        };

        if (logLevel == LogLevel.Error)
        {
            logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);
        }
        else
        {
            logger.LogWarning(exception, "A business validation/conflict exception occurred: {Message}", exception.Message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var message = statusCode == HttpStatusCode.InternalServerError
            ? "An internal server error occurred."
            : exception.Message;

        var response = new ErrorResponse(message, exception.InnerException?.Message);

        string jsonResponse = JsonSerializer.Serialize(response, JsonOptions);
        await context.Response.WriteAsync(jsonResponse, context.RequestAborted);
    }
}

public record ErrorResponse(string Message, string? Details);