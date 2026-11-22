<#
.SYNOPSIS
    Prepara e corrige o cliente OblivionCoClienteFull (v5915) removendo arquivos Debug/quebrados,
    mesclando arquivos faltantes do Scorpions (v5900) e instalando o ConquerHook + PlayLauncher.
.EXAMPLE
    .\FixClient.ps1 -ServerHost "203.0.113.10" -AuthPort 9958 -OutputDir "C:\ConquerOblivion"
#>
param(
    [string]$DownloadsDir = "C:\Users\User\Downloads\conquer",
    [string]$OutputDir    = "C:\ConquerOblivionClient",
    [string]$ServerHost   = "127.0.0.1",
    [int]$AuthPort        = 9958,
    [int]$GamePort        = 5816,
    [string]$UnrarExe     = "C:\Program Files\WinRAR\UnRAR.exe"
)

$ErrorActionPreference = 'Stop'
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$DistClient = Join-Path (Split-Path -Parent (Split-Path -Parent $ScriptDir)) "dist\client"

$OblivionRar = Join-Path $DownloadsDir "OblivionCoClienteFull.rar"
$ScorpionsExe = Join-Path $DownloadsDir "Conquer v6000 Scorpions-co fixed sem bugs.exe"

if (-not (Test-Path $UnrarExe)) {
    throw "UnRAR.exe nao encontrado em $UnrarExe. Instale o WinRAR ou informe -UnrarExe."
}
if (-not (Test-Path $OblivionRar)) {
    throw "Cliente base nao encontrado: $OblivionRar"
}

Write-Host "[1/5] Extraindo OblivionCoClienteFull (v5915) para $OutputDir ..."
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
& $UnrarExe x -o+ -inul $OblivionRar "$OutputDir\"

# Se extraiu dentro de subpasta OblivionCoClienteFull\, mover tudo para a raiz do OutputDir
$nested = Join-Path $OutputDir "OblivionCoClienteFull"
if (Test-Path $nested) {
    Write-Host "[2/5] Organizando estrutura de pastas do cliente..."
    Get-ChildItem $nested -Force | ForEach-Object {
        $dest = Join-Path $OutputDir $_.Name
        if (Test-Path $dest) { Remove-Item -Recurse -Force $dest }
        Move-Item -Force $_.FullName $dest
    }
    Remove-Item -Recurse -Force $nested
}

Write-Host "[3/5] Removendo arquivos Debug quebrados, atalhos e AutoPatchers antigos (2014)..."
$junk = @(
    "play.exe",
    "CSV3Hook.dll",
    "CSV3Hook.dll.lnk",
    "Ntyr.dll",
    "SeW.dll",
    "OblivionCo.exe",
    "AutoPatch.exe",
    "AutoPatchRestart.exe",
    "AutoPatch",
    "AutoPatch1",
    "ConquistandopatchFULL"
)
foreach ($item in $junk) {
    $p = Join-Path $OutputDir $item
    if (Test-Path $p) {
        Remove-Item -Recurse -Force $p
        Write-Host "  Removido: $item"
    }
}

# Complementar arquivo ini\cq.fcs do Scorpions v5900 caso exista
if (Test-Path $ScorpionsExe) {
    Write-Host "[4/5] Verificando arquivos complementares no Scorpions v5900..."
    $iniDir = Join-Path $OutputDir "ini"
    if (-not (Test-Path (Join-Path $iniDir "cq.fcs"))) {
        & $UnrarExe e -o+ -inul $ScorpionsExe '*\ini\cq.fcs' "$iniDir\"
        if (Test-Path (Join-Path $iniDir "cq.fcs")) {
            Write-Host "  Adicionado: ini\cq.fcs (do Scorpions v5900)"
        }
    }
}

Write-Host "[5/5] Instalando configuracao ConquerHook (host=$ServerHost auth_port=$AuthPort game_port=$GamePort)..."
$hookIni = @"
[config]
host=$ServerHost
auth_port=$AuthPort
game_port=$GamePort
redirect_game_port=0
redirect_all_remote=1
multiclient=1
bypass_antidebug=1
block_autopatch=1
log=0
"@
Set-Content -Path (Join-Path $OutputDir "conquer_hook.ini") -Value $hookIni -Encoding ASCII
# Mantem csv3config.ini sincronizado por compatibilidade
$legacyIni = @"
[config]
host=$ServerHost
port=$AuthPort
"@
Set-Content -Path (Join-Path $OutputDir "csv3config.ini") -Value $legacyIni -Encoding ASCII

foreach ($bin in @("ConquerHook.dll", "PlayLauncher.exe")) {
    $srcBin = Join-Path $DistClient $bin
    if (Test-Path $srcBin) {
        Copy-Item -Force $srcBin (Join-Path $OutputDir $bin)
        Write-Host "  Instalado: $bin"
    } else {
        Write-Warning "  $bin ainda nao compilado em $DistClient. Compile com build_mingw_linux.sh (na VPS) ou ConquerHook.sln (Visual Studio) e copie para $OutputDir."
    }
}

Write-Host ""
Write-Host "Cliente corrigido e pronto em: $OutputDir" -ForegroundColor Green
Write-Host "Para jogar, execute: $OutputDir\PlayLauncher.exe"
