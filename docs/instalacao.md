# Instalação

## Requisitos

- .NET SDK **8** ou superior para a global tool
- .NET SDK **10** se for usar `dotnet dnx`
- Um usuário PostgreSQL com permissão de `CONNECT` e `SELECT`
- Rede até o host do banco

## Global tool

```powershell
dotnet tool install --global McpPostgres
mcp-postgres --version
where.exe mcp-postgres
```

Atualização:

```powershell
dotnet tool update --global McpPostgres
```

## dotnet dnx

```powershell
dotnet --version   # precisa ser 10.0.x
dotnet dnx McpPostgres --yes --source https://api.nuget.org/v3/index.json
```

Exemplo pronto: [`examples/gemini-nuget-dnx.json`](examples/gemini-nuget-dnx.json)

## Conferir a conexão

Depois de configurar o cliente MCP, chame `usuario_conectado`. A resposta traz o usuário, o banco atual e `somente_leitura`.

## Desenvolvimento a partir do código

```powershell
git clone https://github.com/SouzaMatheus-dev/mcp-postgres.git
cd mcp-postgres
dotnet test dotnet/McpPostgres.Tests/McpPostgres.Tests.csproj -c Release --filter "FullyQualifiedName!~IntegrationTests"
```
