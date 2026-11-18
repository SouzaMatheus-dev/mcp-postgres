using System.Text;

namespace McpPostgres.Services;

public static class ExplainParser
{
    public static string ExtractSeqScanSuggestions(string plan)
    {
        if (string.IsNullOrWhiteSpace(plan))
        {
            return "Plano vazio. Nenhuma sugestão.";
        }

        if (plan.StartsWith("Erro", StringComparison.Ordinal) || plan.StartsWith("Bloqueado", StringComparison.Ordinal))
        {
            return plan;
        }

        var tables = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in plan.Split('\n'))
        {
            var line = rawLine.Trim();
            const string marker = "Seq Scan on ";
            var index = line.IndexOf(marker, StringComparison.Ordinal);
            if (index < 0)
            {
                continue;
            }

            var table = ReadRelationName(line[(index + marker.Length)..]);
            if (table.Length > 0 && seen.Add(table))
            {
                tables.Add(table);
            }
        }

        if (tables.Count == 0)
        {
            return "Nenhum Seq Scan encontrado no plano estimado. A consulta já parece usar índice, ou as tabelas são pequenas demais para o planejador evitar varredura.";
        }

        var output = new StringBuilder();
        output.AppendLine("Varreduras sequenciais (candidatas a índice ou a revisão de filtro):");
        foreach (var table in tables)
        {
            output.AppendLine($"- {table}");
        }

        output.AppendLine();
        output.AppendLine("Sugestão: confira listar_indices e listar_fks_sem_indice antes de propor um índice.");
        return output.ToString().TrimEnd();
    }

    private static string ReadRelationName(string rest)
    {
        if (rest.StartsWith('"'))
        {
            var end = rest.IndexOf('"', 1);
            return end > 1 ? rest[1..end] : string.Empty;
        }

        var space = rest.IndexOf(' ');
        var name = space < 0 ? rest : rest[..space];
        return name.Trim();
    }
}
