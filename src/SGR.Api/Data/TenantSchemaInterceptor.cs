using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace SGR.Api.Data;

/// <summary>
/// Interceptor que configura o search_path do PostgreSQL ao abrir a conexão
/// Isso permite que o EF Core use o schema do tenant sem modificar o SQL gerado
/// e evita problemas de concorrência causados pela modificação do CommandText
/// </summary>
public class TenantSchemaInterceptor : DbConnectionInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantSchemaInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        SetSearchPath(connection);
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await SetSearchPathAsync(connection, cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private void SetSearchPath(DbConnection connection)
    {
        var schema = _httpContextAccessor.HttpContext?.Items["TenantSchema"] as string;
        if (string.IsNullOrEmpty(schema))
            return;

        using var command = connection.CreateCommand();
        command.CommandText = $"SET search_path TO \"{schema}\", public;";
        command.ExecuteNonQuery();
    }

    private async Task SetSearchPathAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        var schema = _httpContextAccessor.HttpContext?.Items["TenantSchema"] as string;
        if (string.IsNullOrEmpty(schema))
            return;

        await using var command = connection.CreateCommand();
        command.CommandText = $"SET search_path TO \"{schema}\", public;";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

