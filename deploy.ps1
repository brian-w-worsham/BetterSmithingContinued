<#
.SYNOPSIS
    Build and deploy BetterSmithingContinued mod to Bannerlord Modules folder.
.DESCRIPTION
    Builds the project and copies module files to the game's Modules directory.
    If a settings file already exists in the deployed module folder, it is
    preserved and any newly-introduced settings keys are merged in.
.PARAMETER GameFolder
    Path to your Bannerlord installation. Defaults to a common Steam location.
.PARAMETER Configuration
    Build configuration. Default: Release.
#>
param(
    [string]$GameFolder = "C:\Games\steamapps\common\Mount & Blade II Bannerlord",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$ModuleName = "BetterSmithingContinued"
$TargetModuleDir = Join-Path $GameFolder "Modules\$ModuleName"

Write-Host "=== Building $ModuleName ===" -ForegroundColor Cyan

dotnet build "src\BetterSmithingContinued\BetterSmithingContinued.csproj" `
    -c $Configuration `
    -p:GameFolder="$GameFolder"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed!"
    exit 1
}

Write-Host "=== Deploying to $TargetModuleDir ===" -ForegroundColor Cyan

$binDir = Join-Path $TargetModuleDir "bin\Win64_Shipping_Client"
New-Item -ItemType Directory -Path $binDir -Force | Out-Null

# SubModule.xml
Copy-Item "Module\SubModule.xml" -Destination $TargetModuleDir -Force

# Settings: preserve existing user customisations; merge in any new keys.
$settingsFile = "Module\BetterSmithingContinued.settings.xml"
$targetSettingsFile = Join-Path $TargetModuleDir "BetterSmithingContinued.settings.xml"
if (Test-Path $settingsFile) {
    if (Test-Path $targetSettingsFile) {
        try {
            [xml]$templateSettings = Get-Content $settingsFile
            [xml]$existingSettings = Get-Content $targetSettingsFile

            foreach ($templateNode in $templateSettings.DocumentElement.ChildNodes) {
                if ($templateNode.NodeType -ne [System.Xml.XmlNodeType]::Element) {
                    continue
                }

                if (-not $existingSettings.DocumentElement.SelectSingleNode($templateNode.Name)) {
                    $importedNode = $existingSettings.ImportNode($templateNode, $true)
                    [void]$existingSettings.DocumentElement.AppendChild($importedNode)
                }
            }

            $existingSettings.Save($targetSettingsFile)
        }
        catch {
            Write-Warning "Could not merge missing settings keys into $targetSettingsFile. Existing file left unchanged."
        }
    }
    else {
        Copy-Item $settingsFile -Destination $targetSettingsFile -Force
    }
}

# Built DLLs
$builtDll = "src\BetterSmithingContinued\bin\$Configuration\net472\BetterSmithingContinued.dll"
if (Test-Path $builtDll) {
    Copy-Item $builtDll -Destination $binDir -Force
}

$harmonyDll = "src\BetterSmithingContinued\bin\$Configuration\net472\0Harmony.dll"
if (Test-Path $harmonyDll) {
    Copy-Item $harmonyDll -Destination $binDir -Force
}

Write-Host "=== Done! ===" -ForegroundColor Green
Write-Host "Module deployed to: $TargetModuleDir"
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Launch Bannerlord"
Write-Host "  2. Open Mods, enable 'Better Smithing Continued', and start/load a campaign"
Write-Host "  3. (Optional) Edit Modules\BetterSmithingContinued\BetterSmithingContinued.settings.xml"
Write-Host "  4. Visit any town with a smithy and confirm the loaded message appears in green"
