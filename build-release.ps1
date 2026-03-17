<#
.SYNOPSIS
    Full release pipeline: publish, build MSI, and optionally sign everything.

.DESCRIPTION
    Orchestrates the complete NoteBuddy release process:
    1. Publishes both projects as self-contained win-x64
    2. Harvests files and builds the MSI installer
    3. Optionally signs EXEs and MSI (if Azure Trusted Signing is configured)
    4. Outputs the final NoteBuddy-Setup.msi to the release directory

.PARAMETER Configuration
    Build configuration. Defaults to Release.

.PARAMETER SkipSigning
    Skip the signing step even if signing-metadata.json exists.

.PARAMETER Version
    Version number for the installer (e.g., "1.0.0"). Updates the WiX package version.
#>
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$SkipSigning,
    [string]$Version
)

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot

Write-Host ""
Write-Host "  _   _       _       ____            _     _       " -ForegroundColor Cyan
Write-Host " | \ | | ___ | |_ ___| __ ) _   _  __| | __| |_   _" -ForegroundColor Cyan
Write-Host " |  \| |/ _ \| __/ _ \  _ \| | | |/ _`` |/ _`` | | | |" -ForegroundColor Cyan
Write-Host " | |\  | (_) | ||  __/ |_) | |_| | (_| | (_| | |_| |" -ForegroundColor Cyan
Write-Host " |_| \_|\___/ \__\___|____/ \__,_|\__,_|\__,_|\__, |" -ForegroundColor Cyan
Write-Host "                                              |___/ " -ForegroundColor Cyan
Write-Host "  Release Builder" -ForegroundColor Gray
Write-Host ""

# Step 1: Build the installer (publish + harvest + WiX)
Write-Host "Phase 1: Building installer..." -ForegroundColor Magenta
& "$Root\build-installer.ps1" -Configuration $Configuration -OutputDir "$Root\release"

# Step 2: Sign (if configured and not skipped)
$metadataFile = Join-Path $Root "signing-metadata.json"
$msiPath = Join-Path $Root "release\NoteBuddy-Setup.msi"

if ($SkipSigning) {
    Write-Host ""
    Write-Host "Phase 2: Signing skipped (--SkipSigning flag)." -ForegroundColor Yellow
} elseif (Test-Path $metadataFile) {
    Write-Host ""
    Write-Host "Phase 2: Signing artifacts..." -ForegroundColor Magenta

    # Rebuild MSI after signing EXEs (signed EXEs need to be in the MSI)
    Write-Host "Signing EXEs before MSI rebuild..." -ForegroundColor Yellow
    & "$Root\sign-installer.ps1" -MsiPath $msiPath -MetadataFile $metadataFile
} else {
    Write-Host ""
    Write-Host "Phase 2: Signing skipped (signing-metadata.json not found)." -ForegroundColor Yellow
    Write-Host "  The MSI will work but show 'Unknown Publisher' in SmartScreen." -ForegroundColor Gray
    Write-Host "  Run sign-installer.ps1 later to add signing." -ForegroundColor Gray
}

# Done
Write-Host ""
Write-Host "===============================================" -ForegroundColor Green
Write-Host "  Release build complete!" -ForegroundColor Green
Write-Host "  MSI: $msiPath" -ForegroundColor White
$size = [math]::Round((Get-Item $msiPath).Length / 1MB, 1)
Write-Host "  Size: $size MB" -ForegroundColor White
Write-Host "===============================================" -ForegroundColor Green
