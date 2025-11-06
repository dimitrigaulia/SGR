using System.Net;
using System.Text.Json;
using SGR.Api.Exceptions;

namespace SGR.Api.Middleware;

/// <summary>
/// Middleware para tratamento global de exceções
/// </summary>
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
            _logger.LogError(ex, "Erro não tratado: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = HttpStatusCode.InternalServerError;
        var message = "Ocorreu um erro interno no servidor.";

        switch (exception)
        {
            case BusinessException businessEx:
                code = HttpStatusCode.Conflict;
                message = businessEx.Message;
                break;

            case NotFoundException notFoundEx:
                code = HttpStatusCode.NotFound;
                message = notFoundEx.Message;
                break;

            case InvalidOperationException invalidOpEx:
                code = HttpStatusCode.BadRequest;
                message = invalidOpEx.Message;
                break;

            case UnauthorizedAccessException:
                code = HttpStatusCode.Unauthorized;
                message = "Acesso não autorizado.";
                break;
        }

        var result = JsonSerializer.Serialize(new { message, statusCode = (int)code });
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;

        return context.Response.WriteAsync(result);
    }
}

