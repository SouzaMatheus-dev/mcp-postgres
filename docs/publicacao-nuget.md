# Publicação NuGet

Guia para publicar e consumir o pacote **McpPostgres** no NuGet.org, no mesmo fluxo do McpSqlServer.

## Pacote

| Campo | Valor |
|---|---|
| **PackageId** | `McpPostgres` |
| **Tipo** | .NET Global Tool (`DotnetTool`) |
| **Comando CLI** | `mcp-postgres` |
| **Target** | `net8.0` |
| **Perfil** | [matneves](https://www.nuget.org/profiles/matneves) |

## Consumir

```powershell
dotnet tool install --global McpPostgres
dotnet tool install --global McpPostgres --version 0.1.0
```

## Publicar pela tag (fluxo usado)

O workflow [`.github/workflows/publish-nuget.yml`](../.github/workflows/publish-nuget.yml) publica quando uma tag `v*` chega ao GitHub. A autenticação é Trusted Publisher do usuário **matneves** (`NuGet/login@v1`). O id do pacote `McpPostgres` precisa estar coberto pela política em nuget.org.

```powershell
git tag v0.1.0
git push origin v0.1.0
```

A versão publicada é a de `dotnet/McpPostgres/McpPostgres.csproj` (`<Version>`). A tag e a versão do csproj precisam ser a mesma.

## Publicar manualmente

```powershell
cd dotnet/McpPostgres
dotnet pack -c Release -o ../nupkg
dotnet nuget push ../nupkg/McpPostgres.0.1.0.nupkg `
  --api-key $env:NUGET_API_KEY `
  --source https://api.nuget.org/v3/index.json
```

Ou, na raiz do repositório:

```powershell
.\scripts\publish-nuget.ps1 -ApiKey $env:NUGET_API_KEY -Version 0.1.0
```

## Checklist

- [ ] `dotnet test` passou
- [ ] `<Version>` do csproj igual à tag (`v0.1.0` publica `0.1.0`)
- [ ] Política Trusted Publisher do perfil matneves inclui `McpPostgres`
- [ ] Tag `v*` enviada ao GitHub
