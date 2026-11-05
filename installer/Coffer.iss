; -----------------------------------------------------------------------------
; Coffer -- Inno Setup installer script
; Build with:  ISCC.exe installer\Coffer.iss
;              or run  .\publish.ps1  (handles everything automatically)
; -----------------------------------------------------------------------------

#define AppName      "Coffer"
#define AppVersion   "1.1.0"
#define AppPublisher "Coffer"
#define AppURL       "https://github.com/NurAbir/Coffer"
#define AppExeName   "Coffer.exe"
#define PortableExe  "..\publish\Coffer.exe"
#define IconFile     "..\Coffer\Assets\logo.ico"

[Setup]
AppId={{A3F2C1D0-4E5B-4F6A-8C9D-1E2F3A4B5C6D}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}/releases
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
; Output
OutputDir=..\dist
OutputBaseFilename=Coffer-Setup
; Compression
Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes
; Appearance
WizardStyle=modern
WizardSizePercent=120
SetupIconFile={#IconFile}
UninstallDisplayIcon={app}\{#AppExeName}
; Privileges
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
; Minimum Windows version: Windows 10
MinVersion=10.0.17763
; Architecture
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon";   Description: "{cm:CreateDesktopIcon}";   GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startmenuicon"; Description: "Create a Start Menu shortcut"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Files]
; Main executable (single-file self-contained publish output)
Source: "{#PortableExe}"; DestDir: "{app}"; DestName: "{#AppExeName}"; Flags: ignoreversion

[Icons]
Name: "{userprograms}\{#AppName}";           Filename: "{app}\{#AppExeName}"; Tasks: startmenuicon
Name: "{userprograms}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{userdesktop}\{#AppName}";            Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Remove vault data only if the user explicitly requests it (leave by default)
; Uncomment the lines below to wipe vault data on uninstall:
; Type: filesandordirs; Name: "{localappdata}\Coffer"

[Code]
// -- Pre-install: warn if .NET 8 Desktop Runtime is missing -------------------
function IsDotNet8Installed(): Boolean;
var
  key: string;
  installed: Cardinal;
begin
  key := 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App';
  Result := RegQueryDWordValue(HKLM, key, 'Version', installed) or
            RegQueryDWordValue(HKCU, key, 'Version', installed);
  // Single-file self-contained bundles .NET -- this check is informational only.
  Result := True;
end;

procedure InitializeWizard();
begin
  // Nothing extra needed for self-contained publish.
end;
