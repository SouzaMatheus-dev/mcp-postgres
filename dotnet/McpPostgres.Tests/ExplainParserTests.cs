using McpPostgres.Services;
using Xunit;

namespace McpPostgres.Tests;

public class ExplainParserTests
{
    [Fact]
    public void ExtractSeqScanSuggestions_lista_tabelas_sem_repetir()
    {
        const string plan = """
            Seq Scan on pedidos p  (cost=0.00..35.50 rows=10 width=4)
              Filter: (status = 'aberto'::text)
            Parallel Seq Scan on pedidos p
            Index Scan using clientes_pkey on clientes c
            Seq Scan on "Pedido Item" pi
            """;

        var result = ExplainParser.ExtractSeqScanSuggestions(plan);

        Assert.Contains("- pedidos", result);
        Assert.Contains("- Pedido Item", result);
        Assert.DoesNotContain("clientes", result);
        Assert.Equal(1, CountOccurrences(result, "- pedidos"));
    }

    [Fact]
    public void ExtractSeqScanSuggestions_sem_seq_scan_explica_ausencia()
    {
        var result = ExplainParser.ExtractSeqScanSuggestions("Index Scan using clientes_pkey on clientes");

        Assert.Contains("Nenhum Seq Scan", result);
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
