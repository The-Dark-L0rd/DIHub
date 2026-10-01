# ═══════════════════════════════════════════════════════════
#  DI Hub — Post-install Shortcut Creator
#  Copies the Start Menu shortcut (created by Windows for MSIX)
#  to the Desktop.
# ═══════════════════════════════════════════════════════════

param(
    [Parameter(Mandatory=$true)]
    [string]$Aumid
)

$ErrorActionPreference = 'SilentlyContinue'

Write-Host "DI Hub: Creating shortcuts for AUMID = $Aumid"

# Wait for Windows to register the MSIX and create the Start Menu shortcut
Start-Sleep -Seconds 4

# Search locations for the auto-created Start Menu shortcut
$searchPaths = @(
    (Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs'),
    (Join-Path $env:ProgramData 'Microsoft\Windows\Start Menu\Programs')
)

$foundShortcut = $null

foreach ($p in $searchPaths) {
    if (-not (Test-Path $p)) { continue }

    $match = Get-ChildItem -Path $p -Filter '*DI Hub*.lnk' -Recurse -ErrorAction SilentlyContinue |
             Select-Object -First 1

    if ($match) {
        $foundShortcut = $match
        Write-Host "Found Start Menu shortcut: $($match.FullName)"
        break
    }
}

$desktopPath = Join-Path ([Environment]::GetFolderPath('Desktop')) 'DI Hub.lnk'

if ($foundShortcut) {
    # Copy the working Start Menu shortcut to the Desktop
    Copy-Item -Path $foundShortcut.FullName -Destination $desktopPath -Force
    Write-Host "Desktop shortcut created: $desktopPath"
}
else {
    # Fallback: create a shortcut manually using shell:AppsFolder
    Write-Host "Start Menu shortcut not found. Creating manually..."

    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($desktopPath)
    $shortcut.TargetPath = Join-Path $env:WINDIR 'explorer.exe'
    $shortcut.Arguments = "shell:AppsFolder\$Aumid"
    $shortcut.Description = 'DI Hub - Unified AI Workspace'
    $shortcut.Save()

    Write-Host "Fallback Desktop shortcut created: $desktopPath"
}

Write-Host "DI Hub: Shortcut creation complete."
exit 0