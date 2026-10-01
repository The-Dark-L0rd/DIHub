; ═══════════════════════════════════════════════════════════
;  DI Hub — Inno Setup Script (MSIX-based)
;  Version: 3.12.3
;  Publisher: DARK L0RD
; ═══════════════════════════════════════════════════════════

#define MyAppName "DI Hub"
#define MyAppVersion "3.12.3"
#define MyAppPublisher "DARK L0RD"
#define MyAppURL "https://github.com/The-Dark-L0rd"
#define MyAppId "{{E4B8A5F1-2C7D-4E3A-9B1F-8D6C4A2E7F9B}}"
#define MyAumid "DIHub.App_hpkrjsv7q7j3a!App"

; Paths — using the latest 3.12.3.0 build
#define MsixPath "C:\Users\M.D.S\Desktop\DIHub\Publish\MSIX\DIHub.APP_3.12.3.0_x64_Test\DIHub.APP_3.12.3.0_x64.msix"
#define CerPath "C:\Users\M.D.S\Desktop\DIHub\Publish\MSIX\DIHub.APP_3.12.3.0_x64_Test\DIHub.APP_3.12.3.0_x64.cer"
#define IconPath "C:\Users\M.D.S\Desktop\DIHub\DIHub.APP\Assets\DIHub.ico"
#define ShortcutScript "C:\Users\M.D.S\Desktop\DIHub\Installer\create-shortcut.ps1"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

DefaultDirName={autopf}\{#MyAppName}
DisableDirPage=yes
DisableProgramGroupPage=yes
AllowNoIcons=yes

; Output
OutputDir=C:\Users\M.D.S\Desktop\DIHub\Installer\Output
OutputBaseFilename=DIHub-Setup-{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName={#MyAppName}

; Icon for Setup.exe
SetupIconFile={#IconPath}
UninstallDisplayIcon={#IconPath}

; Architecture
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible

; No admin required
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

; Minimum Windows version
MinVersion=10.0.17763

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "{#MsixPath}"; DestDir: "{tmp}"; DestName: "DIHub.APP.msix"; Flags: deleteafterinstall
Source: "{#CerPath}"; DestDir: "{tmp}"; DestName: "DIHub.APP.cer"; Flags: deleteafterinstall
Source: "{#ShortcutScript}"; DestDir: "{tmp}"; DestName: "create-shortcut.ps1"; Flags: deleteafterinstall
Source: "{#IconPath}"; DestDir: "{app}"; DestName: "DIHub.ico"; Flags: ignoreversion

[Run]
; Step 1: Install certificate
Filename: "powershell.exe"; \
  Parameters: "-ExecutionPolicy Bypass -WindowStyle Hidden -Command ""Import-Certificate -FilePath '{tmp}\DIHub.APP.cer' -CertStoreLocation 'Cert:\CurrentUser\TrustedPeople' -ErrorAction SilentlyContinue"""; \
  Flags: runhidden waituntilterminated; \
  StatusMsg: "Installing certificate..."

; Step 2: Install MSIX
Filename: "powershell.exe"; \
  Parameters: "-ExecutionPolicy Bypass -WindowStyle Hidden -Command ""Add-AppxPackage -Path '{tmp}\DIHub.APP.msix' -ForceApplicationShutdown"""; \
  Flags: runhidden waituntilterminated; \
  StatusMsg: "Installing DI Hub..."

; Step 3: Create shortcuts (via external PS1)
Filename: "powershell.exe"; \
  Parameters: "-ExecutionPolicy Bypass -WindowStyle Hidden -File ""{tmp}\create-shortcut.ps1"" -Aumid ""{#MyAumid}"""; \
  Flags: runhidden waituntilterminated; \
  StatusMsg: "Creating Desktop shortcut..."

; Step 4: Launch the app
Filename: "{win}\explorer.exe"; \
  Parameters: "shell:AppsFolder\{#MyAumid}"; \
  Flags: nowait postinstall skipifsilent; \
  Description: "Launch DI Hub"

[UninstallRun]
; Remove MSIX
Filename: "powershell.exe"; \
  Parameters: "-ExecutionPolicy Bypass -WindowStyle Hidden -Command ""Get-AppxPackage DIHub.App | Remove-AppxPackage"""; \
  Flags: runhidden waituntilterminated; \
  RunOnceId: "RemoveAppxPackage"

; Remove Desktop shortcut
Filename: "powershell.exe"; \
  Parameters: "-ExecutionPolicy Bypass -WindowStyle Hidden -Command ""Remove-Item -LiteralPath (Join-Path ([Environment]::GetFolderPath('Desktop')) 'DI Hub.lnk') -ErrorAction SilentlyContinue"""; \
  Flags: runhidden waituntilterminated; \
  RunOnceId: "RemoveDesktopShortcut"

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
  if not IsWin64 then
  begin
    MsgBox('DI Hub requires a 64-bit version of Windows.', mbError, MB_OK);
    Result := False;
  end;
end;