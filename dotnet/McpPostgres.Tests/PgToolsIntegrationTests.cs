using McpPostgres.Services;
using McpPostgres.Tools;
using Npgsql;
using Xunit;

namespace McpPostgres.Tests;

public class PgToolsIntegrationTests
{
    [IntegrationFact]
    public void Ferramentas_de_catalogo_leem_o_schema_de_teste()
    {
        var connectionString = Environment.GetEnvironmentVariable("MCP_POSTGRES_TEST_CONNECTION_STRING")!;
        Seed(connectionString);

        var config = new McpConfig(name => name == "MCP_POSTGRES_CONNECTION_STRING" ? connectionString : null);
        var tools = new PgTools(config, new PgExecutor(config));

        var usuario = tools.UsuarioConectado();
        Assert.Contains("somente_leitura", usuario);
        Assert.Contains("mcp_test", usuario);

        var tabelas = tools.ListarTabelas(schema: "mcp_demo");
        Assert.Contains("clientes", tabelas);
        Assert.Contains("pedidos", tabelas);

        var colunas = tools.DescreverTabela("clientes", schema: "mcp_demo");
        Assert.Contains("nome", colunas);
        Assert.Contains("PK", colunas);

        var amostra = tools.AmostrarTabela("clientes", schema: "mcp_demo", limite: 5);
        Assert.Contains("Ana", amostra);

        var bloqueio = tools.ExecutarConsulta("INSERT INTO mcp_demo.clientes (id, nome) VALUES (2, 'Bruno')");
        Assert.StartsWith("Bloqueado:", bloqueio);

        var fks = tools.ListarChavesEstrangeiras(schema: "mcp_demo", tabela: "pedidos");
        Assert.Contains("cliente_id", fks);
        Assert.Contains("clientes", fks);

        var semIndice = tools.ListarFksSemIndice(schema: "mcp_demo");
        Assert.Contains("cliente_id", semIndice);

        var coluna = tools.BuscarColuna("cliente");
        Assert.Contains("cliente_id", coluna);

        var docs = tools.ObterDocumentacaoObjeto("clientes", schema: "mcp_demo");
        Assert.Contains("Clientes de teste", docs);

        var plano = tools.EstimarPlanoConsulta("SELECT * FROM mcp_demo.pedidos WHERE status = 'aberto'");
        Assert.Contains("Seq Scan", plano);

        var sugestoes = tools.ExtrairSugestoesPlano("SELECT * FROM mcp_demo.pedidos WHERE status = 'aberto'");
        Assert.Contains("pedidos", sugestoes);

        AssertNoError(tools.ListarBancos());
        AssertNoError(tools.ListarSchemas());
        AssertNoError(tools.ListarViews(schema: "mcp_demo"));
        AssertNoError(tools.ListarProcedures(schema: "mcp_demo"));
        AssertNoError(tools.ListarIndices(schema: "mcp_demo", tabela: "clientes"));
        AssertNoError(tools.ResumirBanco());
        AssertNoError(tools.BuscarObjeto("cliente"));
        AssertNoError(tools.PerfilColuna("pedidos", "id", schema: "mcp_demo"));
        AssertNoError(tools.MedirConsulta("SELECT * FROM mcp_demo.pedidos WHERE status = 'aberto'"));
        AssertNoError(tools.ListarColunasCandidatasIndice(schema: "mcp_demo", minLinhas: 1));
        AssertNoError(tools.CompararIndicesRedundantes(schema: "mcp_demo"));
        AssertNoError(tools.AnalisarCoberturaIndice("clientes", "id", schema: "mcp_demo"));
        Assert.Contains("COMPLETA", tools.AnalisarCoberturaIndice("clientes", "id", schema: "mcp_demo"));
        Assert.StartsWith("Bloqueado:", tools.ConsultasLentas());
        Assert.StartsWith("Bloqueado:", tools.IndicesNaoUtilizados());

        var definicao = tools.ObterDefinicaoSql("clientes_por_status", schema: "mcp_demo", tipo: "VIEW");
        Assert.Contains("pedidos", definicao);
        AssertNoError(tools.ConsultarView("clientes_por_status", schema: "mcp_demo", limite: 5));
        AssertNoError(tools.DescreverView("clientes_por_status", schema: "mcp_demo"));
        AssertNoError(tools.DescreverProcedure("total_pedidos", schema: "mcp_demo"));
        AssertNoError(tools.BuscarTextoSql("aberto", schema: "mcp_demo"));

        var dependencias = tools.ListarDependencias("pedidos", schema: "mcp_demo", direcao: "referenciado_por");
        Assert.Contains("clientes_por_status", dependencias);
    }

    private static void AssertNoError(string result)
    {
        Assert.DoesNotContain("Erro PostgreSQL", result);
        Assert.DoesNotContain("Erro de conexão", result);
    }

    private static void Seed(string connectionString)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            DROP SCHEMA IF EXISTS mcp_demo CASCADE;
            CREATE SCHEMA mcp_demo;
            CREATE TABLE mcp_demo.clientes (
                id integer PRIMARY KEY,
                nome text NOT NULL
            );
            CREATE TABLE mcp_demo.pedidos (
                id integer PRIMARY KEY,
                cliente_id integer NOT NULL REFERENCES mcp_demo.clientes (id),
                status text
            );
            INSERT INTO mcp_demo.clientes (id, nome) VALUES (1, 'Ana');
            INSERT INTO mcp_demo.pedidos (id, cliente_id, status) VALUES (10, 1, 'aberto');
            COMMENT ON TABLE mcp_demo.clientes IS 'Clientes de teste';
            CREATE VIEW mcp_demo.clientes_por_status AS
                SELECT c.nome, p.status
                FROM mcp_demo.clientes c
                JOIN mcp_demo.pedidos p ON p.cliente_id = c.id;
            CREATE FUNCTION mcp_demo.total_pedidos()
            RETURNS bigint
            LANGUAGE sql
            AS $$ SELECT count(*) FROM mcp_demo.pedidos $$;
            """;
        command.ExecuteNonQuery();
    }
}
