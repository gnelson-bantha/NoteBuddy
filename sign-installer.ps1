<#
.SYNOPSIS
    Signs the NoteBuddy EXEs and MSI using Azure Trusted Signing.

.DESCRIPTION
    Uses signtool.exe with the Azure Trusted Signing dlib to sign the published
    executables and the final MSI installer. Requires Azure Trusted Signing to be
    configured (see signing-metadata.json).

    If signing-metadata.json is not found, the script will print setup instructions and exit.

.PARAMETER MsiPath
    Path to the MSI file to sign. Defaults to ./release/NoteBuddy-Setup.msi.

.PARAMETER MetadataFile
    Path to the Azure Trusted Signing metadata JSON file. Defaults to ./signing-metadata.json.
#>
param(
    [string]$MsiPath = (Join-Path $PSScriptRoot "release\NoteBuddy-Setup.msi"),
    [string]$MetadataFile = (Join-Path $PSScriptRoot "signing-metadata.json")
)

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot
$PublishDir = Join-Path $Root "publish"

# Check for signing metadata
if (-not (Test-Path $MetadataFile)) {
    Write-Host "=== Azure Trusted Signing Not Configured ===" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "signing-metadata.json not found. To set up code signing:" -ForegroundColor White
    Write-Host ""
    Write-Host "1. Create an Azure Trusted Signing account at https://portal.azure.com" -ForegroundColor Gray
    Write-Host "   - Search 'Trusted Signing' -> Create" -ForegroundColor Gray
    Write-Host "   - Basic tier: `$9.99/month" -ForegroundColor Gray
    Write-Host ""
    Write-Host "2. Complete identity validation (photo ID + selfie, 1-3 days)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "3. Create a certificate profile (type: Public Trust)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "4. Create signing-metadata.json in the repo root:" -ForegroundColor Gray
    Write-Host '   {' -ForegroundColor Cyan
    Write-Host '     "Endpoint": "https://eus.codesigning.azure.net",' -ForegroundColor Cyan
    Write-Host '     "CodeSigningAccountName": "<your-account-name>",' -ForegroundColor Cyan
    Write-Host '     "CertificateProfileName": "<your-profile-name>"' -ForegroundColor Cyan
    Write-Host '   }' -ForegroundColor Cyan
    Write-Host ""
    Write-Host "5. Install the Trusted Signing client:" -ForegroundColor Gray
    Write-Host "   dotnet tool install --global Azure.CodeSigning.Dlib" -ForegroundColor Gray
    Write-Host ""
    Write-Host "6. Log in to Azure CLI:" -ForegroundColor Gray
    Write-Host "   az login" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Then re-run this script." -ForegroundColor White
    exit 1
}

# Locate signtool.exe
$signtool = Get-ChildItem "C:\Program Files (x86)\Windows Kits\10\bin\*\x64\signtool.exe" -ErrorAction SilentlyContinue |
    Sort-Object { [version]($_.Directory.Parent.Name) } -Descending |
    Select-Object -First 1 -ExpandProperty FullName

if (-not $signtool) {
    Write-Error "signtool.exe not found. Install the Windows SDK: winget install Microsoft.WindowsSDK"
}

# Locate the Trusted Signing dlib
$dlib = Get-ChildItem "$env:USERPROFILE\.dotnet\tools\.store\azure.codesigning.dlib\*\azure.codesigning.dlib\*\tools\net8.0\any\Azure.CodeSigning.Dlib.dll" -ErrorAction SilentlyContinue |
    Select-Object -First 1 -ExpandProperty FullName

if (-not $dlib) {
    # Try NuGet global packages fallback
    $dlib = Get-ChildItem "$env:USERPROFILE\.nuget\packages\microsoft.trusted.signing.client\*\bin\x64\Azure.CodeSigning.Dlib.dll" -ErrorAction SilentlyContinue |
        Select-Object -First 1 -ExpandProperty FullName
}

if (-not $dlib) {
    Write-Error "Azure.CodeSigning.Dlib not found. Install with: dotnet tool install --global Azure.CodeSigning.Dlib"
}

Write-Host "=== NoteBuddy Code Signing ===" -ForegroundColor Cyan
Write-Host "signtool: $signtool"
Write-Host "dlib:     $dlib"
Write-Host "metadata: $MetadataFile"
Write-Host ""

function Sign-File {
    param([string]$FilePath, [string]$Description)

    if (-not (Test-Path $FilePath)) {
        Write-Warning "File not found, skipping: $FilePath"
        return
    }

    Write-Host "Signing: $FilePath" -ForegroundColor Yellow
    & $signtool sign /v /fd SHA256 /tr "http://timestamp.digicert.com" /td SHA256 `
        /dlib "$dlib" /dmdf "$MetadataFile" `
        /d "$Description" `
        "$FilePath"

    if ($LASTEXITCODE -ne 0) { throw "Failed to sign: $FilePath" }
    Write-Host "  Signed successfully." -ForegroundColor Green
}

# Sign EXEs first (they get bundled into the MSI)
Sign-File -FilePath (Join-Path $PublishDir "Server\NoteBuddy.exe") -Description "NoteBuddy Server"
Sign-File -FilePath (Join-Path $PublishDir "Tray\NoteBuddy.Tray.exe") -Description "NoteBuddy Tray Launcher"

# Sign the MSI
Sign-File -FilePath $MsiPath -Description "NoteBuddy Installer"

Write-Host ""
Write-Host "=== Signing Complete ===" -ForegroundColor Green
Write-Host "All artifacts signed successfully."
