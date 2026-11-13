param(
    [string]$Version
)

$ErrorActionPreference = 'Stop'

$projectRoot = $PSScriptRoot
$projectFile = Join-Path $projectRoot 'ClickerApp.csproj'
$project = [xml](Get-Content -Raw $projectFile)
$propertyGroup = $project.Project.PropertyGroup | Select-Object -First 1
$projectVersion = [string]$propertyGroup.Version
if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = $projectVersion
}

if ($Version -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$') {
    throw "Version '$Version' is not a valid semantic version."
}

$coreVersion = ($Version -split '[+-]', 2)[0]
$versionParts = $coreVersion.Split('.')
$assemblyVersion = "$($versionParts[0]).$($versionParts[1]).$($versionParts[2]).0"
$publishDirectory = Join-Path $projectRoot "obj\release-publish\$Version"
$releaseDirectory = Join-Path $projectRoot 'releases'
$publishedExecutable = Join-Path $publishDirectory 'SabriAbi.exe'
$releaseExecutable = Join-Path $releaseDirectory "SabriAbi-v$Version-win-x64.exe"

dotnet publish $projectFile -c Release -o $publishDirectory `
    "-p:Version=$Version" `
    "-p:AssemblyVersion=$assemblyVersion" `
    "-p:FileVersion=$assemblyVersion" `
    "-p:InformationalVersion=$Version"
if ($LASTEXITCODE -ne 0) {
    throw "Publishing Sabri Abi v$Version failed."
}

if (-not (Test-Path $publishedExecutable)) {
    throw 'The self-contained SabriAbi.exe was not created.'
}

New-Item -ItemType Directory -Force -Path $releaseDirectory | Out-Null
Copy-Item -LiteralPath $publishedExecutable -Destination $releaseExecutable -Force

$hash = (Get-FileHash -LiteralPath $releaseExecutable -Algorithm SHA256).Hash
Write-Output "Version: $Version"
Write-Output "GitHub Release asset: $releaseExecutable"
Write-Output "Size: $((Get-Item -LiteralPath $releaseExecutable).Length) bytes"
Write-Output "SHA-256: $hash"
