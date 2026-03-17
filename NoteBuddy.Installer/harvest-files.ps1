<#
.SYNOPSIS
    Generates WiX component fragment files from the published Server and Tray output directories.
    Run this after 'dotnet publish' to regenerate the file lists for the installer.

.DESCRIPTION
    Scans the publish/Server and publish/Tray directories and produces two WiX fragment files
    (ServerComponents.wxs and TrayComponents.wxs) containing Component and File elements
    for every published file. These fragments are referenced by Package.wxs.
#>
param(
    [string]$PublishDir = (Join-Path $PSScriptRoot "..\publish"),
    [string]$OutputDir = $PSScriptRoot
)

function New-WixFragment {
    param(
        [string]$SourceDir,
        [string]$ComponentGroupId,
        [string]$DirectoryRef,
        [string]$OutputFile
    )

    if (-not (Test-Path $SourceDir)) {
        Write-Error "Publish directory not found: $SourceDir"
        return
    }

    $sourceAbsolute = (Resolve-Path $SourceDir).Path.TrimEnd('\')
    $files = Get-ChildItem -Path $sourceAbsolute -File -Recurse
    $xml = @"
<?xml version="1.0" encoding="UTF-8"?>

<!--
    Auto-generated WiX component fragment for $ComponentGroupId.
    Do not edit manually — regenerate with harvest-files.ps1 after publishing.
-->
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Fragment>
    <ComponentGroup Id="$ComponentGroupId" Directory="$DirectoryRef">

"@

    $counter = 0
    foreach ($file in $files) {
        $counter++
        $relativePath = $file.FullName.Substring($sourceAbsolute.Length + 1)
        $componentId = "${ComponentGroupId}_$counter"
        $fileId = "${ComponentGroupId}_File_$counter"
        $leafDir = Split-Path $sourceAbsolute -Leaf
        $source = "`$(sys.SOURCEFILEDIR)..\publish\$leafDir\$relativePath"

        # Handle files in subdirectories
        $subDir = Split-Path $relativePath -Parent
        if ($subDir) {
            # Create directory reference for subdirectories
            $dirId = "${DirectoryRef}_" + ($subDir -replace '[\\/ .]', '_')
            $xml += @"
      <Component Id="$componentId" Subdirectory="$subDir">
        <File Id="$fileId" Source="$source" />
      </Component>

"@
        } else {
            $xml += @"
      <Component Id="$componentId">
        <File Id="$fileId" Source="$source" />
      </Component>

"@
        }
    }

    $xml += @"
    </ComponentGroup>
  </Fragment>
</Wix>
"@

    $xml | Out-File -FilePath $OutputFile -Encoding UTF8
    Write-Host "Generated $OutputFile with $counter components."
}

# Generate fragments for both the Server and Tray publish outputs
New-WixFragment `
    -SourceDir (Join-Path $PublishDir "Server") `
    -ComponentGroupId "ServerComponents" `
    -DirectoryRef "ServerFolder" `
    -OutputFile (Join-Path $OutputDir "ServerComponents.wxs")

New-WixFragment `
    -SourceDir (Join-Path $PublishDir "Tray") `
    -ComponentGroupId "TrayComponents" `
    -DirectoryRef "TrayFolder" `
    -OutputFile (Join-Path $OutputDir "TrayComponents.wxs")

Write-Host "`nHarvest complete. WiX fragment files written to $OutputDir"
