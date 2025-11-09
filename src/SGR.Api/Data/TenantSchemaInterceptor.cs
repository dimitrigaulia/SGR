using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace SGR.Api.Data;

/// <summary>
/// Interceptor simples que define o search_path do PostgreSQL antes de cada comando
/// Isso permite que o EF Core use o schema do tenant sem modificar o SQL gerado
/// </summary>
public class TenantSchemaInterceptor : DbCommandInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantSchemaInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        SetSearchPath(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        SetSearchPath(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        SetSearchPath(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        SetSearchPath(command);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    private void SetSearchPath(DbCommand command)
    {
        // Obter o schema do HttpContext (definido pelo middleware)
        var schema = _httpContextAccessor.HttpContext?.Items["TenantSchema"] as string;
        if (!string.IsNullOrEmpty(schema))
        {
            // SET LOCAL define o search_path apenas para a transação atual
            // Isso garante que todas as queries neste comando usem o schema do tenant
            command.CommandText = $"SET LOCAL search_path TO \"{schema}\", public; {command.CommandText}";
        }
    }
}

