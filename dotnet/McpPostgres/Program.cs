using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using McpPostgres.Services;
using McpPostgres.Tools;

var builder = Host.CreateEmptyApplicationBuilder(settings: null);

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddSingleton<McpConfig>();
builder.Services.AddSingleton<PgExecutor>();
builder.Services.AddSingleton<PgTools>();
builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInstructions =
            "Especialista em PostgreSQL (somente leitura). " +
            "Antes de consultar dados: 1) ListarChavesEstrangeiras e ListarIndices para entender JOINs, " +
            "2) ListarDependencias para impacto entre objetos, " +
            "3) ResumirBanco para visão geral, " +
            "4) ObterDocumentacaoObjeto para comentários do catálogo, " +
            "5) BuscarColuna/BuscarObjeto para descobrir nomes. " +
            "Use o parâmetro database para abrir outro banco no mesmo servidor. " +
            "O schema padrão das ferramentas é public.";
    })
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
