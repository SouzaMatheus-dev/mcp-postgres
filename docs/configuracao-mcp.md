# Configuração MCP

A connection string fica no `env` do servidor MCP. Não coloque senha no repositório.

## Gemini — global tool

`%USERPROFILE%\.gemini\settings.json`

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

Exemplo pronto: [`examples/gemini-global-tool.json`](examples/gemini-global-tool.json)

## Vários ambientes

Uma entrada por servidor. O parâmetro `database` escolhe o banco.

```json
{
  "mcpServers": {
    "postgres-dev": {
      "command": "mcp-postgres",
      "env": {
        "MCP_POSTGRES_CONNECTION_STRING": "Host=pg-dev;Username=leitura;Password=SUA_SENHA;Database=postgres;SSL Mode=Prefer",
        "PG_READONLY": "true"
      }
    },
    "postgres-hml": {
      "command": "mcp-postgres",
      "env": {
        "MCP_POSTGRES_CONNECTION_STRING": "Host=pg-hml;Username=leitura;Password=SUA_SENHA;Database=postgres;SSL Mode=Require",
        "PG_READONLY": "true"
      }
    },
    "postgres-prod": {
      "command": "mcp-postgres",
      "env": {
        "MCP_POSTGRES_CONNECTION_STRING": "Host=pg-prod;Username=leitura;Password=SUA_SENHA;Database=postgres;SSL Mode=VerifyFull",
        "PG_READONLY": "true"
      }
    }
  }
}
```

Exemplo pronto: [`examples/gemini-multi-ambiente.json`](examples/gemini-multi-ambiente.json)

## Cursor

`.cursor/mcp.json`

```json
{
  "mcpServers": {
    "postgres-hml": {
      "command": "mcp-postgres",
      "env": {
        "MCP_POSTGRES_CONNECTION_STRING": "Host=pg-hml;Username=leitura;Password=SUA_SENHA;Database=postgres;SSL Mode=Require",
        "PG_READONLY": "true"
      }
    }
  }
}
```

Exemplo pronto: [`examples/cursor-postgres-hml.json`](examples/cursor-postgres-hml.json)

## Claude Desktop

```json
{
  "mcpServers": {
    "postgres-hml": {
      "command": "mcp-postgres",
      "env": {
        "MCP_POSTGRES_CONNECTION_STRING": "Host=pg-hml;Username=leitura;Password=SUA_SENHA;Database=postgres;SSL Mode=Require",
        "PG_READONLY": "true"
      }
    }
  }
}
```

Exemplo pronto: [`examples/claude-desktop-hml.json`](examples/claude-desktop-hml.json)

## Sem connection string pronta

`PGHOST` e `PGUSER` são obrigatórios nesse modo. `PGPASSWORD` entra quando o servidor exige senha.

| Variável | Exemplo |
|---|---|
| `PGHOST` | `pg-hml` |
| `PGPORT` | `5432` |
| `PGUSER` | `leitura` |
| `PGPASSWORD` | senha do usuário |
| `PGDATABASE` | `postgres` |
| `PGSSLMODE` | `Require` |
