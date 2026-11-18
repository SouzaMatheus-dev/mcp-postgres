using Npgsql;

namespace McpPostgres.Services;

public sealed class McpConfig
{
    private readonly Func<string, string?> _environment;
    private readonly Lazy<string> _baseConnectionString;

    public McpConfig()
        : this(static name => Environment.GetEnvironmentVariable(name))
    {
    }

    public McpConfig(Func<string, string?> environment)
    {
        _environment = environment;
        _baseConnectionString = new Lazy<string>(() => BuildBaseConnectionString(_environment));
    }

    public bool ReadOnly => GetBool("PG_READONLY", defaultValue: true);

    public bool PerformanceStatsEnabled =>
        string.Equals(_environment("PG_ENABLE_PERFORMANCE_STATS"), "true", StringComparison.OrdinalIgnoreCase);

    public int MaxRows => GetInt("PG_MAX_ROWS", defaultValue: 200);

    public int ConnectionTimeoutSeconds => GetInt("PG_CONNECTION_TIMEOUT", defaultValue: 15);

    public int CommandTimeoutSeconds => GetInt("PG_COMMAND_TIMEOUT", defaultValue: 60);

    public string GetConnectionString(string? database = null)
    {
        var builder = new NpgsqlConnectionStringBuilder(_baseConnectionString.Value)
        {
            Timeout = ConnectionTimeoutSeconds,
            IncludeErrorDetail = true,
        };

        if (!string.IsNullOrWhiteSpace(database))
        {
            builder.Database = database;
        }

        return builder.ConnectionString;
    }

    private static string BuildBaseConnectionString(Func<string, string?> environment)
    {
        var connectionString = FirstNonEmpty(
            environment("MCP_POSTGRES_CONNECTION_STRING"),
            environment("PG_CONNECTION_STRING"));

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        var host = environment("PGHOST");
        var username = environment("PGUSER");
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException(
                "Defina MCP_POSTGRES_CONNECTION_STRING ou PGHOST e PGUSER no ambiente.");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = ParsePort(environment("PGPORT")),
            Username = username,
            Database = environment("PGDATABASE") ?? "postgres",
            SslMode = ParseSslMode(environment("PGSSLMODE")),
            IncludeErrorDetail = true,
        };

        var password = environment("PGPASSWORD");
        if (!string.IsNullOrWhiteSpace(password))
        {
            builder.Password = password;
        }

        return builder.ConnectionString;
    }

    private bool GetBool(string name, bool defaultValue)
    {
        var value = _environment(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
    }

    private int GetInt(string name, int defaultValue)
    {
        var value = _environment(name);
        return int.TryParse(value, out var parsed) && parsed > 0 ? parsed : defaultValue;
    }

    private static int ParsePort(string? value) =>
        int.TryParse(value, out var port) && port is > 0 and <= 65535 ? port : 5432;

    private static SslMode ParseSslMode(string? value) =>
        Enum.TryParse<SslMode>(value, ignoreCase: true, out var mode) ? mode : SslMode.Prefer;

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
