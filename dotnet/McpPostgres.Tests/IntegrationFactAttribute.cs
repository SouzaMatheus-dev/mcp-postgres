using Xunit;

namespace McpPostgres.Tests;

[AttributeUsage(AttributeTargets.Method)]
public sealed class IntegrationFactAttribute : FactAttribute
{
    public IntegrationFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MCP_POSTGRES_TEST_CONNECTION_STRING")))
        {
            Skip = "Defina MCP_POSTGRES_TEST_CONNECTION_STRING para executar testes de integração.";
        }
    }
}
