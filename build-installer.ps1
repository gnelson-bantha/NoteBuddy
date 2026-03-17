<#
.SYNOPSIS
    Builds the NoteBuddy MSI installer from source.

.DESCRIPTION
    Publishes both the Blazor server and tray launcher as self-contained win-x64 deployments,
    harvests the published files into WiX component fragments, and builds the MSI package.

.PARAMETER Configuration
    Build configuration (Debug or Release). Defaults to Release.

.PARAMETER OutputDir
    Directory for the final MSI output. Defaults to ./release.
#>
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$OutputDir = (Join-Path $PSScriptRoot "release")
)

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot
$PublishDir = Join-Path $Root "publish"
$InstallerDir = Join-Path $Root "NoteBuddy.Installer"

Write-Host "=== NoteBuddy Installer Build ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Clean previous output
Write-Host "[1/5] Cleaning previous build output..." -ForegroundColor Yellow
if (Test-Path $PublishDir) { Remove-Item -Recurse -Force $PublishDir }
New-Item -ItemType Directory -Path $PublishDir -Force | Out-Null
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

# Step 2: Publish both projects
Write-Host "[2/5] Publishing NoteBuddy server (self-contained win-x64)..." -ForegroundColor Yellow
dotnet publish "$Root\NoteBuddy" -c $Configuration -r win-x64 --self-contained -o "$PublishDir\Server" --source https://api.nuget.org/v3/index.json
if ($LASTEXITCODE -ne 0) { throw "Server publish failed." }

Write-Host "[2/5] Publishing NoteBuddy.Tray (self-contained win-x64)..." -ForegroundColor Yellow
dotnet publish "$Root\NoteBuddy.Tray" -c $Configuration -r win-x64 --self-contained -o "$PublishDir\Tray" --source https://api.nuget.org/v3/index.json
if ($LASTEXITCODE -ne 0) { throw "Tray publish failed." }

# Step 3: Harvest published files into WiX fragments
Write-Host "[3/5] Harvesting published files for WiX..." -ForegroundColor Yellow
& "$InstallerDir\harvest-files.ps1" -PublishDir $PublishDir -OutputDir $InstallerDir
if ($LASTEXITCODE -ne 0) { throw "File harvest failed." }

# Step 4: Build the MSI
Write-Host "[4/5] Building MSI installer..." -ForegroundColor Yellow
dotnet build "$InstallerDir" -c $Configuration --source https://api.nuget.org/v3/index.json
if ($LASTEXITCODE -ne 0) { throw "WiX build failed." }

# Step 5: Copy MSI to output directory
Write-Host "[5/5] Copying MSI to output directory..." -ForegroundColor Yellow
$msiSource = Join-Path $InstallerDir "bin\$Configuration\NoteBuddy.Installer.msi"
$msiDest = Join-Path $OutputDir "NoteBuddy-Setup.msi"
Copy-Item $msiSource $msiDest -Force

$msiSize = [math]::Round((Get-Item $msiDest).Length / 1MB, 1)
Write-Host ""
Write-Host "=== Build Complete ===" -ForegroundColor Green
Write-Host "MSI: $msiDest ($msiSize MB)"
Write-Host ""
Write-Host "To sign the installer, run:" -ForegroundColor Gray
Write-Host "  .\sign-installer.ps1 -MsiPath `"$msiDest`"" -ForegroundColor Gray
