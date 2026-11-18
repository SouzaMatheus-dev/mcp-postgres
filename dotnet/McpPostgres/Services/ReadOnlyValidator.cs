using System.Text.RegularExpressions;

namespace McpPostgres.Services;

public static partial class ReadOnlyValidator
{
    public static string? Validate(string sql)
    {
        var normalized = Normalize(sql);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "Consulta vazia não é permitida.";
        }

        if (!AllowedStart().IsMatch(normalized))
        {
            return "Bloqueado: em modo somente leitura só são permitidas consultas que começam com SELECT ou WITH.";
        }

        var keyword = BlockedKeyword().Match(normalized);
        if (keyword.Success)
        {
            return $"Bloqueado: palavra-chave não permitida em modo leitura: {keyword.Value.ToUpperInvariant()}.";
        }

        if (DangerousFunction().IsMatch(normalized))
        {
            return "Bloqueado: função não permitida em modo leitura.";
        }

        if (RowLock().IsMatch(normalized))
        {
            return "Bloqueado: padrão SQL não permitido em modo leitura (FOR UPDATE/SHARE).";
        }

        if (SelectInto().IsMatch(normalized))
        {
            return "Bloqueado: padrão SQL não permitido em modo leitura (INTO).";
        }

        if (MultipleStatements().IsMatch(normalized))
        {
            return "Bloqueado: padrão SQL não permitido em modo leitura (;).";
        }

        return null;
    }

    private static string Normalize(string sql)
    {
        var withoutBlockComments = BlockComment().Replace(sql, " ");
        var withoutLineComments = LineComment().Replace(withoutBlockComments, " ");
        return Whitespace().Replace(withoutLineComments, " ").Trim();
    }

    [GeneratedRegex(
        @"^(SELECT|WITH)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AllowedStart();

    [GeneratedRegex(
        @"\b(INSERT|UPDATE|DELETE|MERGE|TRUNCATE|DROP|CREATE|ALTER|GRANT|REVOKE|COPY|CALL|DO|VACUUM|REINDEX|CLUSTER|REFRESH|LISTEN|NOTIFY|LOAD|DISCARD|CHECKPOINT|REASSIGN|LOCK|PREPARE|DEALLOCATE|EXECUTE)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BlockedKeyword();

    [GeneratedRegex(
        @"\b(pg_read_file|pg_read_binary_file|pg_ls_dir|pg_stat_file|lo_import|lo_export|lo_unlink|pg_terminate_backend|pg_cancel_backend|dblink_exec|pg_write_file)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DangerousFunction();

    [GeneratedRegex(
        @"\bFOR\s+(UPDATE|SHARE|NO\s+KEY\s+UPDATE|KEY\s+SHARE)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RowLock();

    [GeneratedRegex(@"\bINTO\s+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SelectInto();

    [GeneratedRegex(@";\s*\S", RegexOptions.CultureInvariant)]
    private static partial Regex MultipleStatements();

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex BlockComment();

    [GeneratedRegex(@"--[^\n\r]*", RegexOptions.CultureInvariant)]
    private static partial Regex LineComment();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex Whitespace();
}
