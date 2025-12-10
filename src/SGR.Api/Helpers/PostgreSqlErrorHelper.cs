using Microsoft.EntityFrameworkCore;

namespace SGR.Api.Helpers;

/// <summary>
/// Helper para extrair mensagens de erro do PostgreSQL de forma amigável
/// </summary>
public static class PostgreSqlErrorHelper
{
    /// <summary>
    /// Extrai uma mensagem de erro amigável de uma exceção DbUpdateException
    /// </summary>
    public static string ExtractErrorMessage(DbUpdateException ex)
    {
        if (ex.InnerException == null)
        {
            return "Ocorreu um erro ao atualizar o banco de dados.";
        }

        var innerException = ex.InnerException;
        var errorMessage = innerException.Message;

        // Verificar se é um erro do PostgreSQL
        if (errorMessage.Contains("23505")) // Violação de chave única
        {
            return "Já existe um registro com esses dados. Verifique os campos únicos.";
        }

        if (errorMessage.Contains("23503")) // Violação de chave estrangeira
        {
            if (errorMessage.Contains("DELETE"))
            {
                return "Não é possível excluir este registro pois ele está sendo utilizado em outros lugares.";
            }
            return "O registro está relacionado a outros dados e não pode ser modificado.";
        }

        if (errorMessage.Contains("23502")) // Violação de NOT NULL
        {
            return "Alguns campos obrigatórios não foram preenchidos.";
        }

        if (errorMessage.Contains("23514")) // Violação de CHECK constraint
        {
            return "Os dados fornecidos não atendem às regras de validação.";
        }

        if (errorMessage.Contains("duplicate key"))
        {
            return "Já existe um registro com esses dados.";
        }

        if (errorMessage.Contains("foreign key"))
        {
            return "Não é possível realizar esta operação pois o registro está relacionado a outros dados.";
        }

        // Retornar mensagem genérica se não conseguir identificar o tipo de erro
        return "Ocorreu um erro ao processar a operação no banco de dados. Tente novamente.";
    }
}


