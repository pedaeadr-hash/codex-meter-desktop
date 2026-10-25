$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $projectRoot 'artifacts'
$desktopOut = Join-Path $artifacts 'desktop'
$cliOut = Join-Path $artifacts 'cli'

dotnet publish (Join-Path $projectRoot 'src\CodexMeter.Desktop\CodexMeter.Desktop.csproj') `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -o $desktopOut
if ($LASTEXITCODE -ne 0) { throw 'Falha ao publicar o aplicativo desktop.' }

dotnet publish (Join-Path $projectRoot 'src\CodexMeter.Cli\CodexMeter.Cli.csproj') `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -o $cliOut
if ($LASTEXITCODE -ne 0) { throw 'Falha ao publicar o CLI.' }

$isccCandidates = @(
  "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
  "$env:ProgramFiles(x86)\Inno Setup 6\ISCC.exe",
  "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
)
$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $iscc) {
  throw 'Inno Setup 6 nao foi encontrado. Instale-o e execute o script novamente.'
}

& $iscc (Join-Path $projectRoot 'installer\CodexMeter.iss')
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o instalador.' }
Write-Host "Instalador criado em $artifacts\installer\CodexMeter-Setup.exe"

