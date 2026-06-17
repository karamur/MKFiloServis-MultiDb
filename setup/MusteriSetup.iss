; ============================================================
; KOAFiloServis — Musteri Kurulumu (Web + DataSync, Lisans Yok)
; ============================================================

#define MyAppName        "KOAFiloServis"
#define MyAppPublisher   "KOA Yazilim"
#define MyAppURL         "https://github.com/karamur/KOAFiloServis-MultiDb"
#define MyAppExeName     "KOAFiloServis.Web.exe"
#define MyInstallDir     "C:\KOAFiloServis"
#define MyDataSyncExe    "KOAFiloServis.DataSync.exe"

#ifndef MyAppVersion
#define MyAppVersion "1.0.26"
#endif

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={#MyInstallDir}
DisableDirPage=yes
DefaultGroupName={#MyAppName}
OutputBaseFilename=KOAFiloServisKurulumMusteri-{#MyAppVersion}
#ifdef OutputDir
OutputDir={#OutputDir}
#else
OutputDir=output\v{#MyAppVersion}
#endif
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\app\{#MyAppExeName}
UninstallDisplayName={#MyAppName} {#MyAppVersion}
ShowLanguageDialog=no
CloseApplications=force
DisableProgramGroupPage=yes
AllowNoIcons=yes
SetupLogging=yes

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Types]
Name: "full"; Description: "Tam Kurulum"

[Components]
Name: "web"; Description: "KOAFiloServis Web"; Types: full; Flags: fixed
Name: "datasync"; Description: "Veri Aktarim Araci"; Types: full

[Files]
Source: "payload\Web\*"; DestDir: "{app}\app"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: web
Source: "payload\DataSync\*"; DestDir: "{app}\tools\datasync"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: datasync

[Dirs]
Name: "{app}\data"; Permissions: users-modify
Name: "{app}\uploads"; Permissions: users-modify
Name: "{app}\logs"; Permissions: users-modify
Name: "{app}\Backups"; Permissions: users-modify
Name: "C:\KOAFiloServis_yedekleme"; Permissions: users-modify

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\app\{#MyAppExeName}"; WorkingDir: "{app}\app"
Name: "{group}\Veri Aktarim"; Filename: "{app}\tools\datasync\{#MyDataSyncExe}"; WorkingDir: "{app}\tools\datasync"; Components: datasync
Name: "{group}\Kurulum Klasorunu Ac"; Filename: "{app}"
Name: "{group}\Kaldir"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\app\{#MyAppExeName}"; WorkingDir: "{app}\app"

[Run]
Filename: "{app}\app\{#MyAppExeName}"; Description: "Uygulamayi Baslat"; Flags: nowait postinstall skipifsilent; WorkingDir: "{app}\app"

[Code]
function GetInstallPath(): String;
var sPrevPath: String;
begin
  if RegQueryStringValue(HKLM,'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#SetupSetting("AppId")}_is1','InstallLocation', sPrevPath) then
    Result := sPrevPath else Result := '';
end;

function IsUpgrade(): Boolean;
begin Result := (GetInstallPath() <> ''); end;

function InitializeSetup(): Boolean;
var PrevPath, Msg: String;
begin
  Result := True; PrevPath := GetInstallPath();
  if PrevPath <> '' then
  begin
    Msg := '{#MyAppName} sistemde kurulu.' + #13#10 + PrevPath + #13#10#13#10 +
           'Bu islem mevcut kurulumu GUNCELLER.' + #13#10 +
           '* Konfigurasyonlar KORUNUR.' + #13#10#13#10 + 'Devam etmek istiyor musunuz?';
    if MsgBox(Msg, mbConfirmation, MB_YESNO) = IDNO then begin Result := False; Exit; end;
  end;
end;

procedure InitializeWizard();
begin
  if IsUpgrade() then WizardForm.Caption := '{#MyAppName} Guncelleme Sihirbazi'
  else WizardForm.Caption := '{#MyAppName} Kurulum Sihirbazi';
end;
