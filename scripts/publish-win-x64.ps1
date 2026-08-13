[CmdletBinding()]
param(
    [string]$OutputDirectory = "artifacts/publish/win-x64"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot "src/VideoToMp3.App/VideoToMp3.App.csproj"
$publishDirectory = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputDirectory))

dotnet publish $projectPath `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --property:PublishProfile=win-x64 `
    --output $publishDirectory

if ($LASTEXITCODE -ne 0) {
    throw "Windows x64 publish failed with exit code $LASTEXITCODE."
}

Get-ChildItem -LiteralPath $publishDirectory -Filter "*.pdb" -File |
    Remove-Item -Force

$toolDocumentation = @{
    "tools/ffmpeg/README.txt" = Join-Path $repositoryRoot "src/VideoToMp3.App/tools/ffmpeg/README.txt"
    "tools/yt-dlp/README.txt" = Join-Path $repositoryRoot "src/VideoToMp3.App/tools/yt-dlp/README.txt"
}
foreach ($entry in $toolDocumentation.GetEnumerator()) {
    $destination = Join-Path $publishDirectory $entry.Key
    New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
    Copy-Item -LiteralPath $entry.Value -Destination $destination -Force
}

$requiredPaths = @(
    "VideoToMp3.App.exe",
    "tools/ffmpeg/README.txt",
    "tools/yt-dlp/README.txt"
)

foreach ($relativePath in $requiredPaths) {
    $path = Join-Path $publishDirectory $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Publish output is missing: $relativePath"
    }
}

$executablePath = Join-Path $publishDirectory "VideoToMp3.App.exe"
$executableText = [System.Text.Encoding]::UTF8.GetString(
    [System.IO.File]::ReadAllBytes($executablePath))
if ($executableText.IndexOf($repositoryRoot, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
    throw "Publish output contains the developer repository path."
}

Write-Host "Windows x64 release published to $publishDirectory"
