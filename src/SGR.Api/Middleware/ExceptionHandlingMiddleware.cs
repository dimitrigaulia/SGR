using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SGR.Api.Exceptions;
using SGR.Api.Helpers;

namespace SGR.Api.Middleware;

/// <summary>
/// Middleware para tratamento global de exceÃ§Ãµes
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
            _logger.LogError(ex, "Erro nÃ£o tratado: {Message}", ex.Message);
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
                message = "Acesso nÃ£o autorizado.";
                break;

            case DbUpdateConcurrencyException concurrencyEx:
                code = HttpStatusCode.Conflict;
                message = "O registro foi modificado por outro usuÃ¡rio. Por favor, atualize a pÃ¡gina e tente novamente.";
                break;

            case DbUpdateException dbUpdateEx:
                code = HttpStatusCode.Conflict;
                message = PostgreSqlErrorHelper.ExtractErrorMessage(dbUpdateEx);
                break;
        }

        var result = JsonSerializer.Serialize(new { message, statusCode = (int)code });
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;

        return context.Response.WriteAsync(result);
    }
}

