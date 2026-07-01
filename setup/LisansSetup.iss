; ============================================================
; MKFiloServis - Lisans Yonetim Araci (Bagimsiz Installer)
; ============================================================

#define LisansAppName   "MK Lisans Yonetimi"
#define LisansPublisher "MK Yazilim"
#define LisansURL       "https://github.com/karamur/MKFiloServis-MultiDb"
#define LisansExe       "MKFiloServisLisans.exe"
#define LisansInstallDir "C:\MKLisans"

#ifndef LisansAppVersion
#define LisansAppVersion "1.0.26"
#endif

[Setup]
AppId={{B2C3D4E5-F6A7-8901-BCDE-F12345678901}
AppName={#LisansAppName}
AppVersion={#LisansAppVersion}
AppVerName={#LisansAppName} {#LisansAppVersion}
AppPublisher={#LisansPublisher}
AppPublisherURL={#LisansURL}
DefaultDirName={#LisansInstallDir}
DisableDirPage=no
DefaultGroupName={#LisansAppName}
OutputBaseFilename=MKLisansArac-{#LisansAppVersion}
#ifdef OutputDir
OutputDir={#OutputDir}
#else
OutputDir=output\v{#LisansAppVersion}
#endif
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#LisansExe}
UninstallDisplayName={#LisansAppName} {#LisansAppVersion}
ShowLanguageDialog=no
CloseApplications=force
DisableProgramGroupPage=yes
AllowNoIcons=yes
SetupLogging=yes

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Files]
Source: "payload\LisansDesktop\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
Name: "{app}\data"

[Icons]
Name: "{group}\{#LisansAppName}"; Filename: "{app}\{#LisansExe}"; WorkingDir: "{app}"
Name: "{group}\Kaldir"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#LisansAppName}"; Filename: "{app}\{#LisansExe}"; WorkingDir: "{app}"

[Run]
Filename: "{app}\{#LisansExe}"; Description: "Lisans aracini hemen ac"; Flags: postinstall nowait skipifsilent shellexec

[Code]
function IsUpgrade(): Boolean;
var sPrevPath: String;
begin
  Result := RegQueryStringValue(HKCU,'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#SetupSetting("AppId")}_is1','InstallLocation', sPrevPath) and (sPrevPath <> '');
end;

procedure InitializeWizard();
begin
  if IsUpgrade() then WizardForm.Caption := '{#LisansAppName} Guncelleme'
  else WizardForm.Caption := '{#LisansAppName} Kurulum';
end;
