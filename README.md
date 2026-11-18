# MCP-Postgres

[![NuGet](https://img.shields.io/nuget/v/McpPostgres?logo=nuget&logoColor=white&label=NuGet&color=004880)](https://www.nuget.org/packages/McpPostgres)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![C#](https://img.shields.io/badge/C%23-12-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![MCP](https://img.shields.io/badge/MCP-1.4-111111)](https://modelcontextprotocol.io)
[![License](https://img.shields.io/github/license/SouzaMatheus-dev/mcp-postgres?color=blue)](https://github.com/SouzaMatheus-dev/mcp-postgres/blob/main/LICENSE)

![PostgreSQL](https://img.shields.io/badge/PostgreSQL-leitura-4169E1?logo=postgresql&logoColor=white)
![Somente leitura](https://img.shields.io/badge/modo-somente%20leitura-44403c)
![Npgsql](https://img.shields.io/badge/driver-Npgsql-512BD4?logo=dotnet&logoColor=white)

Servidor MCP para **PostgreSQL** com **modo somente leitura** por padrão.

Conecte Gemini, Cursor, Claude Desktop ou VS Code a um Postgres usando uma connection string.
O pacote segue o mesmo modelo do [McpSqlServer](https://www.nuget.org/packages/McpSqlServer): catálogo, dependências, índices e `SELECT` validado.

## Pacote NuGet

| Instalação | Comando | SDK necessário |
|---|---|---|
| **Global tool** | `dotnet tool install --global McpPostgres` | .NET **8+** |
| **dotnet dnx** | `dotnet dnx McpPostgres --yes` | .NET **10+** |

> **`dotnet dnx` exige SDK 10.** Em máquinas com .NET 8/9, use a **global tool** (`mcp-postgres`).

## Início rápido (global tool, .NET 8+)

```powershell
dotnet tool install --global McpPostgres
```

Configure no Gemini (`%USERPROFILE%\.gemini\settings.json`):

```json
{
  "mcpServers": {
    "postgres-hml": {
      "command": "mcp-postgres",
      "env": {
        "MCP_POSTGRES_CONNECTION_STRING": "Host=pg-hml;Port=5432;Username=leitura;Password=SUA_SENHA;Database=postgres;SSL Mode=Require",
        "PG_READONLY": "true"
      }
    }
  }
}
```

Reinicie o cliente MCP e valide com a ferramenta `usuario_conectado`.

## Início rápido (dotnet dnx, requer .NET 10 SDK)

```json
{
  "mcpServers": {
    "postgres-hml": {
      "command": "dotnet",
      "args": ["dnx", "McpPostgres", "--yes", "--source", "https://api.nuget.org/v3/index.json"],
      "env": {
        "MCP_POSTGRES_CONNECTION_STRING": "Host=pg-hml;Port=5432;Username=leitura;Password=SUA_SENHA;Database=postgres;SSL Mode=Require",
        "PG_READONLY": "true"
      }
    }
  }
}
```

## Um servidor, vários bancos

Uma entrada por **servidor/ambiente** (DEV, HML, PROD) basta. O parâmetro `database` abre outra conexão no mesmo host:

```
listar_bancos()
listar_tabelas(database="vendas", schema="public")
executar_consulta(sql="SELECT * FROM public.pedidos LIMIT 10", database="financeiro")
```

## Documentação

| Guia | Conteúdo |
|---|---|
| [Instalação](docs/instalacao.md) | NuGet, requisitos e verificação |
| [Configuração MCP](docs/configuracao-mcp.md) | Gemini, Cursor, Claude Desktop |
| [Exemplos de uso](docs/exemplos-uso.md) | Ferramentas e multi-banco |
| [Publicação NuGet](docs/publicacao-nuget.md) | Build, pack e publish do pacote .NET |

## Exemplos prontos

Arquivos JSON em [`docs/examples/`](docs/examples/):

- `gemini-nuget-dnx.json` — `dotnet dnx` (**requer .NET 10 SDK**)
- `gemini-global-tool.json` — global tool (**requer .NET 8+ SDK**)
- `gemini-multi-ambiente.json` — DEV + HML + PROD
- `cursor-postgres-hml.json` — Cursor com servidor HML
- `claude-desktop-hml.json` — Claude Desktop

## Ferramentas MCP

| Ferramenta | Descrição |
|---|---|
| `usuario_conectado` | Usuário, banco atual e modo MCP |
| `listar_bancos` | Bancos do cluster |
| `listar_schemas` | Schemas de usuário |
| `listar_tabelas` | Tabelas base |
| `listar_views` | Views e materialized views |
| `listar_procedures` | Functions e procedures (metadados) |
| `descrever_tabela` | Colunas de tabela |
| `descrever_view` | Colunas de view |
| `descrever_procedure` | Parâmetros de function/procedure |
| `obter_definicao_sql` | Definição SQL do objeto |
| `consultar_view` | `SELECT` com `LIMIT` em view |
| `executar_consulta` | `SELECT`/`WITH` validado |
| `listar_chaves_estrangeiras` | Foreign keys |
| `buscar_coluna` | Busca colunas pelo nome |
| `buscar_objeto` | Busca tabelas, views e routines |
| `obter_documentacao_objeto` | `COMMENT ON` do catálogo |
| `listar_indices` | Índices, unique e PK |
| `listar_dependencias` | Quem referencia / é referenciado |
| `resumir_banco` | Contagens e TOP 20 maiores tabelas |
| `amostrar_tabela` | Amostra linhas (`LIMIT`) |
| `perfil_coluna` | Nulos, distintos, min/max |
| `buscar_texto_sql` | Busca texto em views e routines |
| `listar_fks_sem_indice` | FKs sem índice na primeira coluna |
| `analisar_cobertura_indice` | Cobertura de colunas por índices |
| `comparar_indices_redundantes` | Índices duplicados ou com prefixo |
| `listar_colunas_candidatas_indice` | Colunas sem índice em tabelas grandes |
| `medir_consulta` | `EXPLAIN (ANALYZE, BUFFERS)` |
| `estimar_plano_consulta` | `EXPLAIN` sem executar a consulta |
| `extrair_sugestoes_plano` | Seq Scan do plano estimado |
| `consultas_lentas` | `pg_stat_statements` (opt-in) |
| `indices_nao_utilizados` | Índices com `idx_scan = 0` (opt-in) |

> Procedures **não são executadas** (`CALL` bloqueado). O foco é leitura.

## Variáveis de ambiente

| Variável | Padrão | Descrição |
|---|---|---|
| `MCP_POSTGRES_CONNECTION_STRING` | — | Connection string completa (**recomendado**) |
| `PG_CONNECTION_STRING` | — | Alias da connection string |
| `PGHOST` | — | Host, quando não houver connection string |
| `PGPORT` | `5432` | Porta |
| `PGUSER` | — | Usuário |
| `PGPASSWORD` | — | Senha |
| `PGDATABASE` | `postgres` | Banco padrão |
| `PGSSLMODE` | `Prefer` | `Disable`, `Allow`, `Prefer`, `Require`, `VerifyCA`, `VerifyFull` |
| `PG_READONLY` | `true` | Bloqueia DDL/DML/`CALL` |
| `PG_MAX_ROWS` | `200` | Limite de linhas retornadas |
| `PG_CONNECTION_TIMEOUT` | `15` | Timeout de conexão (segundos) |
| `PG_COMMAND_TIMEOUT` | `60` | Timeout de comando (segundos) |
| `PG_ENABLE_PERFORMANCE_STATS` | `false` | Habilita `consultas_lentas` e `indices_nao_utilizados` |

## Segurança

- O modo padrão só aceita SQL que começa com `SELECT` ou `WITH`
- O validador bloqueia escrita, `COPY`, `CALL`, `SELECT INTO`, locks `FOR UPDATE` e funções de arquivo
- Permissões reais vêm do PostgreSQL — o MCP não eleva privilégios
- Mantenha `PG_READONLY=true` em produção
- Prefira um usuário de banco com `GRANT SELECT`

## Desenvolvimento

```powershell
git clone https://github.com/SouzaMatheus-dev/mcp-postgres.git
cd mcp-postgres
dotnet test dotnet/McpPostgres.Tests/McpPostgres.Tests.csproj -c Release --filter "FullyQualifiedName!~IntegrationTests"
```

### Testes de integração (PostgreSQL)

```powershell
$env:MCP_POSTGRES_TEST_CONNECTION_STRING = "Host=localhost;Port=5432;Username=postgres;Password=SUA_SENHA;Database=mcp_test;Ssl Mode=Disable"
dotnet test dotnet/McpPostgres.Tests/McpPostgres.Tests.csproj -c Release --filter "FullyQualifiedName~IntegrationTests"
```

O CI executa esses testes em pull requests com PostgreSQL 16 em container.

## Licença

MIT — veja [LICENSE](LICENSE).
