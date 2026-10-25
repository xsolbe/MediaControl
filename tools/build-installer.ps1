param(
    [string]$InnoSetupCompiler
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectPath = Join-Path $repoRoot 'MediaControl.csproj'
$scriptPath = Join-Path $repoRoot 'installer\MediaControl.iss'
$project = [xml](Get-Content -LiteralPath $projectPath -Raw)
$version = [string]$project.Project.PropertyGroup.Version

if ([string]::IsNullOrWhiteSpace($version)) {
    throw 'MediaControl.csproj is missing a Version value.'
}

if (-not $InnoSetupCompiler) {
    $knownLocations = @(
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe')
    )
    $InnoSetupCompiler = $knownLocations | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}

if (-not $InnoSetupCompiler -or -not (Test-Path -LiteralPath $InnoSetupCompiler)) {
    throw 'Inno Setup 6 is required. Install it with: winget install --id JRSoftware.InnoSetup --exact'
}

$artifactRoot = Join-Path $repoRoot 'artifacts'
$payloadDir = Join-Path $artifactRoot ('installer-payload-' + [guid]::NewGuid().ToString('N'))
$outputDir = Join-Path $artifactRoot 'installer'
New-Item -ItemType Directory -Path $payloadDir, $outputDir -Force | Out-Null

try {
    & dotnet publish $projectPath -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o $payloadDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }

    & $InnoSetupCompiler "-dAppVersion=$version" "-dPayloadDir=$payloadDir" "-o$outputDir" $scriptPath
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed with exit code $LASTEXITCODE." }
}
finally {
    $resolvedArtifactRoot = [IO.Path]::GetFullPath($artifactRoot).TrimEnd('\')
    $resolvedPayloadDir = [IO.Path]::GetFullPath($payloadDir)
    if (-not $resolvedPayloadDir.StartsWith($resolvedArtifactRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to delete a directory outside artifacts: $resolvedPayloadDir"
    }
    Remove-Item -LiteralPath $resolvedPayloadDir -Recurse -Force
}

$setupName = "MediaControl-Setup-v$version-win-x64.exe"
$setupPath = Join-Path $outputDir $setupName
if (-not (Test-Path -LiteralPath $setupPath)) { throw "Installer was not created: $setupPath" }

$checksumPath = "$setupPath.sha256"
$hash = (Get-FileHash -LiteralPath $setupPath -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath $checksumPath -Value "$hash  $setupName" -Encoding ascii
Write-Output "Installer: $setupPath"
Write-Output "SHA-256:   $hash"
