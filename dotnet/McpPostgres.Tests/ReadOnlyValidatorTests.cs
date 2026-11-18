using McpPostgres.Services;
using Xunit;

namespace McpPostgres.Tests;

public class ReadOnlyValidatorTests
{
    [Theory]
    [InlineData("SELECT 1")]
    [InlineData("WITH cte AS (SELECT 1 AS n) SELECT * FROM cte")]
    [InlineData("SELECT * FROM public.clientes WHERE id = 1")]
    public void Validate_permite_consultas_leitura(string sql)
    {
        Assert.Null(ReadOnlyValidator.Validate(sql));
    }

    [Theory]
    [InlineData("DELETE FROM tabela")]
    [InlineData("INSERT INTO tabela VALUES (1)")]
    [InlineData("SELECT * INTO backup FROM tabela")]
    [InlineData("COPY clientes TO STDOUT")]
    [InlineData("SELECT * FROM clientes FOR UPDATE")]
    [InlineData("SELECT pg_read_file('/etc/passwd')")]
    [InlineData("SELECT 1; DROP TABLE t")]
    public void Validate_bloqueia_comandos_perigosos(string sql)
    {
        Assert.NotNull(ReadOnlyValidator.Validate(sql));
    }

    [Fact]
    public void Validate_bloqueia_bypass_por_comentario()
    {
        Assert.NotNull(ReadOnlyValidator.Validate("/* ok */ SELECT 1; DELETE FROM t"));
    }
}
