# McpPostgres (.NET)

Pacote NuGet publicável em [nuget.org/profiles/matneves](https://www.nuget.org/profiles/matneves).

## Build e publish

```powershell
cd dotnet/McpPostgres
dotnet pack -c Release -o ../nupkg

# Obtenha a API Key em https://www.nuget.org/account/apikeys
$env:NUGET_API_KEY = "sua-api-key-aqui"
dotnet nuget push ../nupkg/McpPostgres.0.1.0.nupkg `
  --source https://api.nuget.org/v3/index.json `
  --api-key $env:NUGET_API_KEY `
  --skip-duplicate
```

O fluxo usado no GitHub é o mesmo do SQL Server: tag `v*` dispara `.github/workflows/publish-nuget.yml`, que autentica o perfil **matneves** como Trusted Publisher e envia o pacote.

## Testar localmente (antes de publicar)

```powershell
dotnet pack -c Release -o ../nupkg

$env:MCP_POSTGRES_CONNECTION_STRING = "Host=localhost;Username=leitura;Password=SUA_SENHA;Database=postgres"
dotnet dnx McpPostgres --yes --source "C:\caminho\mcp-postgres\dotnet\nupkg"
```

Veja também: [docs/instalacao.md](../docs/instalacao.md)
