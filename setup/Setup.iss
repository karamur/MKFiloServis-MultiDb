; ============================================================
; KOAFiloServis — Simplified Direct-EXE Installer
; No IIS, no firewall, just run the self-contained EXE.
; ============================================================

#define MyAppName        "KOAFiloServis"
#define MyAppPublisher   "KOA Yazilim"
#define MyAppURL         "https://github.com/karamur/KOAFiloServis-MultiDb"
#define MyAppExeName     "KOAFiloServis.Web.exe"
#define MyInstallDirBase "C:\KOAFiloServis_ustun"
#define MyBackupDirBase  "C:\KOAFiloServis_yedekleme_ustun"
#define MyLisansExe      "KOAFiloServisLisans.exe"
#define MyDataSyncExe    "KOAFiloServis.DataSync.exe"

#ifndef MyAppVersion
#define MyAppVersion "1.0.26"
#endif

#define MyVersionToken StringChange(MyAppVersion, ".", "_")
#define MyInstallDir MyInstallDirBase
#define MyBackupDir MyBackupDirBase
#define MyAppId "A1B2C3D4-E5F6-7890-ABCD-EF1234567890-USTUN"
#define MyShortcutName MyAppName + " Ustun"

[Setup]
AppId={{#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={#MyInstallDir}
DisableDirPage=yes
DefaultGroupName={#MyAppName}
OutputBaseFilename=KOAFiloServisKurulum-{#MyAppVersion}
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
RestartApplications=no
DisableProgramGroupPage=yes
AllowNoIcons=yes
SetupLogging=yes

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Files]
; Web uygulamasi (self-contained, Kestrel ile calisir)
Source: "payload\Web\*"; DestDir: "{app}\app"; Flags: ignoreversion recursesubdirs createallsubdirs

; Lisans Yonetim Araci
Source: "payload\LisansDesktop\*"; DestDir: "{app}\tools\lisans"; Flags: ignoreversion recursesubdirs createallsubdirs

; Veri Aktarim Araci
Source: "payload\DataSync\*"; DestDir: "{app}\tools\datasync"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
Name: "{app}\data"; Permissions: users-modify
Name: "{app}\uploads"; Permissions: users-modify
Name: "{app}\logs"; Permissions: users-modify
Name: "{app}\database"; Permissions: users-modify
Name: "{app}\Backups"; Permissions: users-modify
Name: "{#MyBackupDir}"; Permissions: users-modify

[Icons]
Name: "{group}\{#MyShortcutName}"; Filename: "{app}\app\{#MyAppExeName}"; WorkingDir: "{app}\app"
Name: "{group}\{#MyShortcutName} - Lisans Yonetimi"; Filename: "{app}\tools\lisans\{#MyLisansExe}"; WorkingDir: "{app}\tools\lisans"
Name: "{group}\{#MyShortcutName} - Veri Aktarim"; Filename: "{app}\tools\datasync\{#MyDataSyncExe}"; WorkingDir: "{app}\tools\datasync"
Name: "{group}\{#MyShortcutName} - Kurulum Klasorunu Ac"; Filename: "{app}"
Name: "{group}\{#MyShortcutName} - Kaldir"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyShortcutName}"; Filename: "{app}\app\{#MyAppExeName}"; WorkingDir: "{app}\app"

[Run]
Filename: "{app}\app\{#MyAppExeName}"; Description: "Uygulamayi Baslat"; Flags: nowait postinstall skipifsilent; WorkingDir: "{app}\app"

[UninstallDelete]
Type: filesandordirs; Name: "{app}\app\wwwroot\_framework"
Type: dirifempty; Name: "{app}\app"

[Code]
procedure InitializeWizard();
begin
  WizardForm.Caption := '{#MyAppName} {#MyAppVersion} Kurulum Sihirbazi';
end;

function InitializeSetup(): Boolean;
var
  Msg: String;
begin
  Result := True;
  Msg := '{#MyAppName} {#MyAppVersion} ayri bir klasore kurulacaktir:' + #13#10 +
         '{#MyInstallDir}' + #13#10#13#10 +
         'Bu kurulum mevcut versiyonlara dokunmaz ve yan yana calisabilir.' + #13#10 +
         'Devam etmek istiyor musunuz?';
  if MsgBox(Msg, mbConfirmation, MB_YESNO) = IDNO then
  begin Result := False; Exit; end;
end;
