# Publica o pacote McpPostgres no NuGet.org (perfil matneves)
#
# Uso:
#   1. Crie a API Key em https://www.nuget.org/account/apikeys
#   2. Execute: .\scripts\publish-nuget.ps1 -ApiKey "sua-key"
#
param(
    [Parameter(Mandatory = $true)]
    [string]$ApiKey,

    [string]$Version = "0.1.0",
    [string]$Project = "dotnet/McpPostgres/McpPostgres.csproj",
    [string]$Output = "dotnet/nupkg"
)

$ErrorActionPreference = "Stop"

Write-Host ">> Build e pack v$Version..." -ForegroundColor Cyan
dotnet pack $Project -c Release -o $Output /p:Version=$Version

$nupkg = Join-Path $Output "McpPostgres.$Version.nupkg"
if (-not (Test-Path $nupkg)) {
    throw "Pacote nao encontrado: $nupkg"
}

Write-Host ">> Publicando no NuGet.org..." -ForegroundColor Cyan
dotnet nuget push $nupkg `
    --source https://api.nuget.org/v3/index.json `
    --api-key $ApiKey `
    --skip-duplicate

Write-Host ""
Write-Host "Publicado com sucesso!" -ForegroundColor Green
Write-Host "Pacote: https://www.nuget.org/packages/McpPostgres/$Version" -ForegroundColor Green
Write-Host ""
Write-Host "Teste no cliente MCP:" -ForegroundColor Yellow
Write-Host "  Reinicie o cliente e use a ferramenta usuario_conectado" -ForegroundColor Yellow
