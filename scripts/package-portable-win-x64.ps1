[CmdletBinding()]
param(
    [string]$OutputDirectory = "artifacts/packages"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot "src/VideoToMp3.App/VideoToMp3.App.csproj"
[xml]$project = Get-Content -LiteralPath $projectPath
$properties = $project.Project.PropertyGroup | Where-Object { $_.VersionPrefix } | Select-Object -First 1
$version = [string]$properties.VersionPrefix
if ($properties.VersionSuffix) {
    $version = "$version-$($properties.VersionSuffix)"
}
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "The application version is not configured."
}

$packageName = "VideoToMp3-$version-win-x64-portable"
$workRoot = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot "artifacts/package-work"))
$stagingDirectory = [System.IO.Path]::GetFullPath((Join-Path $workRoot $packageName))
$packageDirectory = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputDirectory))
$zipPath = Join-Path $packageDirectory "$packageName.zip"
$checksumPath = "$zipPath.sha256"

$expectedPrefix = $workRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
if (-not $stagingDirectory.StartsWith($expectedPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Unsafe package staging directory: $stagingDirectory"
}

if (Test-Path -LiteralPath $stagingDirectory) {
    Remove-Item -LiteralPath $stagingDirectory -Recurse -Force
}
New-Item -ItemType Directory -Path $stagingDirectory -Force | Out-Null

$relativeStaging = "artifacts/package-work/$packageName"
& (Join-Path $PSScriptRoot "publish-win-x64.ps1") -OutputDirectory $relativeStaging
if ($LASTEXITCODE -ne 0) {
    throw "Portable publish failed with exit code $LASTEXITCODE."
}

Copy-Item -LiteralPath (Join-Path $repositoryRoot "LICENSE") -Destination $stagingDirectory
Copy-Item -LiteralPath (Join-Path $repositoryRoot "README.md") -Destination $stagingDirectory
Copy-Item -LiteralPath (Join-Path $repositoryRoot "THIRD_PARTY_NOTICES.md") -Destination $stagingDirectory
Set-Content -LiteralPath (Join-Path $stagingDirectory "VERSION.txt") -Value $version -Encoding ascii

New-Item -ItemType Directory -Path $packageDirectory -Force | Out-Null
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}
Compress-Archive -LiteralPath $stagingDirectory -DestinationPath $zipPath -CompressionLevel Optimal

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $entryNames = $archive.Entries.FullName | ForEach-Object { $_.Replace('\', '/') }
    $requiredEntries = @(
        "$packageName/VideoToMp3.exe",
        "$packageName/LICENSE",
        "$packageName/README.md",
        "$packageName/THIRD_PARTY_NOTICES.md",
        "$packageName/VERSION.txt",
        "$packageName/tools/ffmpeg/README.txt",
        "$packageName/tools/yt-dlp/README.txt"
    )
    foreach ($entry in $requiredEntries) {
        if ($entry -notin $entryNames) {
            throw "Portable ZIP is missing: $entry"
        }
    }
}
finally {
    $archive.Dispose()
}

$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath $checksumPath -Value "$hash  $([System.IO.Path]::GetFileName($zipPath))" -Encoding ascii

Write-Host "Portable package created: $zipPath"
Write-Host "SHA256: $hash"
