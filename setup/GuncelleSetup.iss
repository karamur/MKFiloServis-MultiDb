; ============================================================
; KOAFiloServis-MultiDb — Guncelleme Paketi
; SADECE uygulama dosyalarini gunceller.
; Mevcut kurulum ZORUNLUDUR.
; ============================================================

#define MyAppName    "KOAFiloServis"
#define MyAppExeName "KOAFiloServis.Web.exe"
#define MyInstallDir "C:\KOAFiloServis"
#define MyLisansExe  "KOAFiloServisLisans.exe"
#define MyDataSyncExe "KOAFiloServis.DataSync.exe"

#ifndef MyAppVersion
#define MyAppVersion "1.0.22"
#endif

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName} Guncelleme
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher=KOA Yazilim
DefaultDirName={#MyInstallDir}
DisableDirPage=yes
DisableProgramGroupPage=yes
OutputBaseFilename=KOAFiloServisGuncelle-{#MyAppVersion}
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
ShowLanguageDialog=no
CloseApplications=force
RestartApplications=no
CreateUninstallRegKey=no
UpdateUninstallLogAppName=no
AllowNoIcons=yes
SetupLogging=yes

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Components]
Name: "web";      Description: "Web Uygulamasi (zorunlu)"; Flags: fixed; Types: full
Name: "lisans";   Description: "Lisans Yonetim Aracini da guncelle"; Types: full
Name: "datasync"; Description: "Veri Aktarim Aracini da guncelle"; Types: full

[Types]
Name: "full";    Description: "Tam Guncelleme (Web + Lisans + DataSync)"
Name: "webonly"; Description: "Sadece Web Uygulamasi"

[Files]
Source: "payload\Web\*"; \
    DestDir: "{app}"; \
    Excludes: "dbsettings.json,appsettings.Production.json,appsettings.json,portalsettings.json,backup_settings.json,*.db,*.db-shm,*.db-wal,logs\*,uploads\*,Backups\*,keys\*"; \
    Flags: ignoreversion recursesubdirs createallsubdirs; \
    Components: web

Source: "payload\LisansDesktop\*"; DestDir: "{app}\Lisans"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: lisans
Source: "payload\DataSync\*"; DestDir: "{app}\DataSync"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: datasync

Source: "scripts\backup-db.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "scripts\iis-configure.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "scripts\preinstall-check.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion

[Code]
function GetInstallPath(): String;
var sPrevPath: String;
begin
  if RegQueryStringValue(HKLM,
        'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#SetupSetting("AppId")}_is1',
        'InstallLocation', sPrevPath) then
    Result := sPrevPath
  else Result := '';
end;

function GetTimestamp(): String;
begin Result := GetDateTimeString('yyyymmdd-hhnnss', #0, #0); end;

procedure BackupDatabase(InstallPath: String);
var DbFile, BackupDir: String; ResultCode: Integer;
begin
  DbFile := InstallPath + '\KOAFiloServis';
  if not FileExists(DbFile) then Exit;
  BackupDir := InstallPath + '\Backups\db-' + GetTimestamp();
  Exec('cmd.exe', '/c mkdir "' + BackupDir + '"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec('cmd.exe', '/c copy /Y "' + DbFile + '" "' + BackupDir + '\KOAFiloServis"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  if FileExists(InstallPath + '\KOAFiloServis-shm') then
    Exec('cmd.exe', '/c copy /Y "' + InstallPath + '\KOAFiloServis-shm" "' + BackupDir + '\KOAFiloServis-shm"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  if FileExists(InstallPath + '\KOAFiloServis-wal') then
    Exec('cmd.exe', '/c copy /Y "' + InstallPath + '\KOAFiloServis-wal" "' + BackupDir + '\KOAFiloServis-wal"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

procedure StopIISSite();
var ResultCode: Integer;
begin
  Exec('cmd.exe', '/c "%windir%\system32\inetsrv\appcmd.exe" stop site /site.name:"KOAFiloServis"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

procedure StartIISSite();
var ResultCode: Integer;
begin
  Exec('cmd.exe', '/c "%windir%\system32\inetsrv\appcmd.exe" start site /site.name:"KOAFiloServis"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

function InitializeSetup(): Boolean;
var PrevPath, AppVer, Msg: String;
begin
  Result := True;
  PrevPath := GetInstallPath();
  if PrevPath = '' then
  begin
    MsgBox('{#MyAppName} sistemde kurulu degil.' + #13#10#13#10 +
           'Bu paket GUNCELLEME icindir. Once ana kurulum paketini calistirin.', mbError, MB_OK);
    Result := False; Exit;
  end;
  RegQueryStringValue(HKLM,
    'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#SetupSetting("AppId")}_is1',
    'DisplayVersion', AppVer);
  Msg := 'Mevcut kurulum: ' + PrevPath;
  if AppVer <> '' then Msg := Msg + #13#10 + 'Kurulu versiyon : ' + AppVer;
  Msg := Msg + #13#10 + 'Yeni versiyon   : {#MyAppVersion}' + #13#10#13#10 +
         'Guncelleme yapilacak:' + #13#10 +
         '  * Veritabani otomatik yedeklenecek' + #13#10 +
         '  * Konfigurasyonlar KORUNACAK' + #13#10 +
         '  * IIS sitesi yeniden baslatilacak' + #13#10#13#10 +
         'Devam etmek istiyor musunuz?';
  if MsgBox(Msg, mbConfirmation, MB_YESNO) = IDNO then Result := False;
end;

procedure InitializeWizard();
begin WizardForm.Caption := '{#MyAppName} {#MyAppVersion} - Guncelleme Sihirbazi'; end;

procedure CurStepChanged(CurStep: TSetupStep);
var PrevPath: String;
begin
  PrevPath := GetInstallPath();
  if PrevPath = '' then Exit;
  if CurStep = ssInstall then
  begin
    WizardForm.StatusLabel.Caption := 'Veritabani yedekleniyor...';
    BackupDatabase(PrevPath);
    WizardForm.StatusLabel.Caption := 'IIS sitesi durduruluyor...';
    StopIISSite();
  end;
  if CurStep = ssPostInstall then
  begin
    WizardForm.StatusLabel.Caption := 'IIS sitesi baslatiliyor...';
    StartIISSite();
  end;
end;
