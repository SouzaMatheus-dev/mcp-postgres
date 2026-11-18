using Npgsql;

namespace McpPostgres.Services;

internal static class PgErrorFormatter
{
    public static string Format(PostgresException exception)
    {
        var message = exception.MessageText.Trim();
        var hint = exception.SqlState switch
        {
            "42501" => " Dica: verifique GRANT/permissões no PostgreSQL ou use ferramentas de catálogo.",
            "28P01" or "28000" => " Dica: confira usuário e senha na connection string.",
            "3D000" => " Dica: o banco informado no parâmetro database não existe ou não aceita conexão.",
            "42P01" when message.Contains("pg_stat_statements", StringComparison.OrdinalIgnoreCase) =>
                " Dica: crie a extensão com CREATE EXTENSION pg_stat_statements no banco (exige permissão de administrador).",
            _ => string.Empty,
        };

        return $"Erro PostgreSQL ({exception.SqlState}): {message}{hint}";
    }
}
