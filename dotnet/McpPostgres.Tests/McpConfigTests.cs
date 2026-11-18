using McpPostgres.Services;
using Npgsql;
using Xunit;

namespace McpPostgres.Tests;

public class McpConfigTests
{
    [Fact]
    public void GetConnectionString_exige_host_e_usuario_quando_nao_ha_connection_string()
    {
        var config = new McpConfig(_ => null);

        var error = Assert.Throws<InvalidOperationException>(() => config.GetConnectionString());

        Assert.Contains("MCP_POSTGRES_CONNECTION_STRING", error.Message);
    }

    [Fact]
    public void GetConnectionString_monta_a_partir_de_PGHOST_e_troca_o_banco()
    {
        var config = new McpConfig(name => name switch
        {
            "PGHOST" => "pg.exemplo",
            "PGPORT" => "5433",
            "PGUSER" => "leitura",
            "PGPASSWORD" => "segredo",
            "PGDATABASE" => "postgres",
            "PGSSLMODE" => "Require",
            "PG_READONLY" => "true",
            _ => null,
        });

        var builder = new NpgsqlConnectionStringBuilder(config.GetConnectionString("vendas"));

        Assert.Equal("pg.exemplo", builder.Host);
        Assert.Equal(5433, builder.Port);
        Assert.Equal("leitura", builder.Username);
        Assert.Equal("segredo", builder.Password);
        Assert.Equal("vendas", builder.Database);
        Assert.Equal(SslMode.Require, builder.SslMode);
        Assert.True(config.ReadOnly);
    }

    [Fact]
    public void GetConnectionString_prefere_a_connection_string_completa()
    {
        const string connectionString = "Host=direto;Username=app;Password=x;Database=appdb";
        var config = new McpConfig(name => name == "MCP_POSTGRES_CONNECTION_STRING" ? connectionString : null);

        var builder = new NpgsqlConnectionStringBuilder(config.GetConnectionString());

        Assert.Equal("direto", builder.Host);
        Assert.Equal("appdb", builder.Database);
    }
}
