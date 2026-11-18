# McpPostgres

[![NuGet](https://img.shields.io/nuget/v/McpPostgres?logo=nuget&logoColor=white&label=NuGet&color=004880)](https://www.nuget.org/packages/McpPostgres)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![C#](https://img.shields.io/badge/C%23-12-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![MCP](https://img.shields.io/badge/MCP-1.4-111111)](https://modelcontextprotocol.io)
[![License](https://img.shields.io/github/license/SouzaMatheus-dev/mcp-postgres?color=blue)](https://github.com/SouzaMatheus-dev/mcp-postgres/blob/main/LICENSE)

![PostgreSQL](https://img.shields.io/badge/PostgreSQL-leitura-4169E1?logo=postgresql&logoColor=white)
![Somente leitura](https://img.shields.io/badge/modo-somente%20leitura-44403c)
![Npgsql](https://img.shields.io/badge/driver-Npgsql-512BD4?logo=dotnet&logoColor=white)

Servidor **MCP para PostgreSQL** com **modo somente leitura** por padrão.

Conecte **Gemini**, **Cursor**, **Claude Desktop** ou **VS Code** ao Postgres com uma connection string.

Repositório: https://github.com/SouzaMatheus-dev/mcp-postgres

## Instalação

### Global tool (.NET 8+)

```powershell
dotnet tool install --global McpPostgres
dotnet tool update --global McpPostgres
```

### dotnet dnx (requer .NET 10 SDK)

```powershell
dotnet dnx McpPostgres --yes --source https://api.nuget.org/v3/index.json
```

> Em máquinas com .NET 8/9, prefira a **global tool** (`mcp-postgres`).

## Configuração MCP

### Global tool — Gemini / Cursor

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

### dotnet dnx — Gemini

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

Reinicie o cliente MCP e valide com `usuario_conectado`.

## Um servidor, vários bancos

Configure **uma entrada por ambiente** (DEV, HML, PROD). Use o parâmetro `database` nas ferramentas:

```
listar_bancos()
listar_tabelas(database="vendas")
executar_consulta(sql="SELECT * FROM public.pedidos LIMIT 10", database="financeiro")
```

O schema padrão das ferramentas que recebem `schema` é `public`.

## Ferramentas

| Ferramenta | Descrição |
|---|---|
| `usuario_conectado` | Usuário, banco atual e modo MCP |
| `listar_bancos` | Bancos do cluster |
| `listar_schemas` | Schemas de usuário |
| `listar_tabelas` | Tabelas base |
| `listar_views` | Views e materialized views |
| `listar_procedures` | Functions e procedures |
| `descrever_tabela` | Colunas e chave primária |
| `descrever_view` | Colunas da view |
| `descrever_procedure` | Parâmetros |
| `obter_definicao_sql` | `pg_get_viewdef` / `pg_get_functiondef` |
| `consultar_view` | `SELECT` com `LIMIT` |
| `executar_consulta` | `SELECT`/`WITH` validado |
| `listar_chaves_estrangeiras` | Foreign keys |
| `buscar_coluna` | Busca colunas pelo nome |
| `buscar_objeto` | Busca tabelas, views e routines |
| `obter_documentacao_objeto` | Comentários do catálogo |
| `listar_indices` | Índices da tabela |
| `listar_dependencias` | Dependências de views e functions |
| `resumir_banco` | Contagens e maiores tabelas |
| `amostrar_tabela` | Amostra com `LIMIT` |
| `perfil_coluna` | Nulos, distintos, min/max |
| `buscar_texto_sql` | Texto em views e routines |
| `listar_fks_sem_indice` | FK sem índice na primeira coluna |
| `analisar_cobertura_indice` | Cobertura de um conjunto de colunas |
| `comparar_indices_redundantes` | Índices duplicados ou prefixo |
| `listar_colunas_candidatas_indice` | Colunas sem índice em tabelas grandes |
| `medir_consulta` | `EXPLAIN (ANALYZE, BUFFERS)` |
| `estimar_plano_consulta` | `EXPLAIN` |
| `extrair_sugestoes_plano` | Seq Scan do plano |
| `consultas_lentas` | `pg_stat_statements` (opt-in) |
| `indices_nao_utilizados` | `idx_scan = 0` (opt-in) |

## Variáveis de ambiente

| Variável | Padrão | Descrição |
|---|---|---|
| `MCP_POSTGRES_CONNECTION_STRING` | — | Connection string completa |
| `PGHOST` / `PGPORT` / `PGUSER` / `PGPASSWORD` / `PGDATABASE` | porta `5432`, banco `postgres` | Alternativa à connection string |
| `PGSSLMODE` | `Prefer` | Modo SSL do Npgsql |
| `PG_READONLY` | `true` | Bloqueia DDL, DML e `CALL` |
| `PG_MAX_ROWS` | `200` | Máximo de linhas |
| `PG_CONNECTION_TIMEOUT` | `15` | Timeout de conexão (segundos) |
| `PG_COMMAND_TIMEOUT` | `60` | Timeout de comando (segundos) |
| `PG_ENABLE_PERFORMANCE_STATS` | `false` | Libera estatísticas de runtime |

`consultas_lentas` lê `pg_stat_statements`. A extensão precisa existir no banco (`CREATE EXTENSION pg_stat_statements`).

## Segurança

- Autenticação pela connection string ou por `PGUSER`/`PGPASSWORD`
- Validador SQL bloqueia comandos de escrita
- Permissões reais vêm do PostgreSQL
- Mantenha `PG_READONLY=true` em produção

## Links

- Instalação: https://github.com/SouzaMatheus-dev/mcp-postgres/blob/main/docs/instalacao.md
- Configuração MCP: https://github.com/SouzaMatheus-dev/mcp-postgres/blob/main/docs/configuracao-mcp.md
- Exemplos de uso: https://github.com/SouzaMatheus-dev/mcp-postgres/blob/main/docs/exemplos-uso.md
