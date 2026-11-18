using System.Text;
using Npgsql;

namespace McpPostgres.Services;

public sealed class PgExecutor(McpConfig config)
{
    public string Execute(string sql, string? database = null, params NpgsqlParameter[] parameters)
    {
        try
        {
            using var connection = new NpgsqlConnection(config.GetConnectionString(database));
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = config.CommandTimeoutSeconds;
            command.Parameters.AddRange(parameters);

            using var reader = command.ExecuteReader();
            return ResultFormatter.Format(reader, config.MaxRows);
        }
        catch (PostgresException ex)
        {
            return PgErrorFormatter.Format(ex);
        }
        catch (NpgsqlException ex)
        {
            return $"Erro de conexão PostgreSQL: {ex.Message}";
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message;
        }
    }

    public string ExecuteExplain(string sql, string? database = null, bool analyze = false)
    {
        var options = analyze ? "ANALYZE, BUFFERS, FORMAT TEXT" : "FORMAT TEXT";
        return Execute($"EXPLAIN ({options}) {sql}", database);
    }
}

internal static class ResultFormatter
{
    public static string Format(NpgsqlDataReader reader, int maxRows)
    {
        var columns = Enumerable.Range(0, reader.FieldCount)
            .Select(reader.GetName)
            .ToArray();

        var header = string.Join(" | ", columns);
        var output = new StringBuilder();
        output.AppendLine(header);
        output.AppendLine(new string('-', Math.Max(header.Length, 1)));

        var count = 0;
        while (count < maxRows && reader.Read())
        {
            var values = Enumerable.Range(0, reader.FieldCount)
                .Select(index => reader.IsDBNull(index) ? "NULL" : reader.GetValue(index)?.ToString() ?? string.Empty);
            output.AppendLine(string.Join(" | ", values));
            count++;
        }

        var truncated = reader.Read();
        if (truncated)
        {
            output.AppendLine($"... resultado truncado em {maxRows} linhas ...");
        }

        output.AppendLine($"({count} linha(s) exibida(s))");
        return output.ToString().TrimEnd();
    }
}
