; ============================================================
; MKFiloServis — Guncelleme Paketi (sadece dosyalar)
; Mevcut kurulum ZORUNLUDUR.
; ============================================================

#define MyAppName     "MKFiloServis"
#define MyAppExeName  "MKFiloServis.Web.exe"
#define MyInstallDir  "C:\MKFiloServis"
#define MyBackupDir  "C:\MKFiloServis_yedekleme"
#define MyIisPort     "5050"
#define MyDataSyncExe "MKFiloServis.DataSync.exe"

#ifndef MyAppVersion
#define MyAppVersion "1.0.47"
#endif

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName} Guncelleme
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher=MK Yazilim
DefaultDirName={code:GetInstallPath}
DisableDirPage=yes
DisableProgramGroupPage=yes
OutputBaseFilename=MKFiloServisGuncelle-{#MyAppVersion}
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
Name: "datasync"; Description: "Veri Aktarim Aracini da guncelle"; Types: full

[Types]
Name: "full";    Description: "Tam Guncelleme"
Name: "webonly"; Description: "Sadece Web"

[Files]
Source: "payload\Web\*"; DestDir: "{app}\app"; \
    Excludes: "dbsettings.json,appsettings.json,appsettings.*.json,portalsettings.json,backup_settings.json,cookies.txt,*.db,*.db-shm,*.db-wal,logs\*,uploads\*,Backups\*,keys\*"; \
    Flags: ignoreversion recursesubdirs createallsubdirs; Components: web
Source: "payload\DataSync\*"; DestDir: "{app}\tools\datasync"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: datasync
Source: "scripts\configure-iis-startup.ps1"; DestDir: "{app}\tools"; Flags: ignoreversion

[Code]
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
  Params: String;
begin
  Result := '';
  Params := '-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command "try { $listeners = @(Get-NetTCPConnection -State Listen -LocalPort {#MyIisPort} -ErrorAction Stop | Where-Object { $_.OwningProcess -ne 4 }); if ($listeners.Count -gt 0) { exit 32 }; exit 0 } catch { exit 64 }"';
  if not Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
      Params, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    ResultCode := 64;
  if ResultCode = 32 then
    Result := '5050 portu baska bir uygulama tarafindan kullaniliyor. Yerel MKFiloServis uygulamasini kapatin ve guncellemeyi yeniden baslatin.'
  else if ResultCode <> 0 then
    Result := '5050 portunun kullanilabilirligi denetlenemedi. Guncellemeye devam edilmedi.';
end;

function FindInstallPath(): String;
var MainPath, CustomerPath: String;
begin
  MainPath := '';
  CustomerPath := '';
  RegQueryStringValue(HKLM,
    'Software\Microsoft\Windows\CurrentVersion\Uninstall\A1B2C3D4-E5F6-7890-ABCD-EF1234567890-USTUN_is1',
    'InstallLocation', MainPath);
  RegQueryStringValue(HKLM,
    'Software\Microsoft\Windows\CurrentVersion\Uninstall\A1B2C3D4-E5F6-7890-ABCD-EF1234567890-MUSTERI-USTUN_is1',
    'InstallLocation', CustomerPath);
  if (MainPath <> '') and (CustomerPath <> '') then
    Result := ''
  else if MainPath <> '' then
    Result := MainPath
  else
    Result := CustomerPath;
end;

function GetInstallPath(Param: String): String;
begin
  Result := FindInstallPath();
end;

function InitializeSetup(): Boolean;
var PrevPath, AppVer, Msg: String;
begin
  Result := True;
  PrevPath := FindInstallPath();
  if PrevPath = '' then
  begin
    MsgBox('Tek bir desteklenen kurulum bulunamadi. Kurulum yoksa once ana veya musteri paketini calistirin; iki kurulum varsa guncelleme hedefini ayristirin.', mbError, MB_OK);
    Result := False; Exit;
  end;
  if not FileExists(AddBackslash(PrevPath) + 'app\{#MyAppExeName}') then
  begin
    MsgBox('Kurulu uygulama dosyasi hedef dizinde bulunamadi: ' + PrevPath, mbError, MB_OK);
    Result := False; Exit;
  end;
  AppVer := '';
  if not RegQueryStringValue(HKLM,
    'Software\Microsoft\Windows\CurrentVersion\Uninstall\A1B2C3D4-E5F6-7890-ABCD-EF1234567890-USTUN_is1',
    'DisplayVersion', AppVer) then
    RegQueryStringValue(HKLM,
      'Software\Microsoft\Windows\CurrentVersion\Uninstall\A1B2C3D4-E5F6-7890-ABCD-EF1234567890-MUSTERI-USTUN_is1',
      'DisplayVersion', AppVer);
  Msg := 'Mevcut kurulum: ' + PrevPath;
  if AppVer <> '' then Msg := Msg + #13#10 + 'Kurulu versiyon : ' + AppVer;
  Msg := Msg + #13#10 + 'Yeni versiyon   : {#MyAppVersion}' + #13#10#13#10 +
         'Guncelleme yapilacak:' + #13#10 +
         '  * Konfigurasyonlar KORUNACAK' + #13#10 +
         '  * Uygulama yeniden baslatilacak' + #13#10#13#10 +
         'Devam etmek istiyor musunuz?';
  { Sessiz kip yalnızca kullanıcı tarafından özellikle başlatılan güncellemelerde
    kullanılır; etkileşimli kurulumda mevcut onay penceresini göstermeye devam et. }
  if (not WizardSilent) and (MsgBox(Msg, mbConfirmation, MB_YESNO) = IDNO) then
    Result := False;
end;

procedure InitializeWizard();
begin WizardForm.Caption := '{#MyAppName} {#MyAppVersion} - Guncelleme Sihirbazi'; end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
  Params: String;
  ExtraInfo: String;
begin
  if CurStep = ssPostInstall then
  begin
    Params := ExpandConstant('-NoProfile -ExecutionPolicy Bypass -File "{app}\tools\configure-iis-startup.ps1" -AppPoolName "MKFiloServis" -SiteName "MKFiloServis" -Port {#MyIisPort} -AppPath "{app}\app" -StorageRoot "{#MyBackupDir}" -RestartAppPool');
    WizardForm.StatusLabel.Caption := 'IIS sitesi 5050 portunda yapilandiriliyor...';
    if not Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
      Params, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) or (ResultCode <> 0) then
    begin
      ExtraInfo := ExpandConstant('{app}\app\logs\configure-iis-startup.log');
      RaiseException(Format('IIS port ve baslangic ayarlari yapilandirilamadi (%d). Betik ayrintisi: %s', [ResultCode, ExtraInfo]));
    end;
  end;
end;
