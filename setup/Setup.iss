#define MyAppName "KOAFiloServis"
#define MyAppPublisher "KOA Yazilim"
#define MyAppURL "https://github.com/karamur/KOAFiloServis-MultiDb"
#define MyAppExeName "KOAFiloServis.Web.exe"
#define MyInstallDir "C:\KOAFiloServis"
#define MyDataDir "C:\KOAFiloServis\data"
#define MyLisansExe "KOAFiloServisLisans.exe"
#define MyDataSyncExe "KOAFiloServis.DataSync.exe"

#ifndef MyAppVersion
#define MyAppVersion "1.0.22"
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
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName} {#MyAppVersion}
ShowLanguageDialog=no
CloseApplications=force
RestartApplications=no
DisableProgramGroupPage=yes
AllowNoIcons=yes
SetupLogging=yes

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Types]
Name: "full"; Description: "Tam Kurulum"
Name: "compact"; Description: "Sadece Web (IIS)"

[Components]
Name: "web"; Description: "KOAFiloServis Web (IIS)"; Types: full compact; Flags: fixed
Name: "lisans"; Description: "Lisans Yonetim Araci"; Types: full
Name: "datasync"; Description: "Veri Aktarim Araci (PostgreSQL - SQLite)"; Types: full

[Tasks]
Name: "iisfeatures"; Description: "IIS rolu / .NET Hosting Bundle eksikse otomatik kur (Internet gerekir)"; GroupDescription: "On Hazirlik:"; Flags: unchecked
Name: "iisconfigure"; Description: "IIS Site ve AppPool'u otomatik yapilandir"; GroupDescription: "IIS:"; Flags: checkedonce
Name: "firewall"; Description: "Windows Guvenlik Duvarinda port ac (HTTP 5200)"; GroupDescription: "Firewall:"; Flags: checkedonce
Name: "browser"; Description: "Kurulum sonrasi tarayicida ac"; GroupDescription: "Son adim:"; Flags: unchecked

[Files]
; Web uygulamasi
Source: "payload\Web\*"; DestDir: "{app}"; Excludes: "dbsettings.json,appsettings.Production.json,*.db,logs\*,uploads\*"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: web

; Ilk kurulumda dbsettings.json ornegi (guncellemede DOKUNULMAZ)
Source: "payload\Web\dbsettings.json"; DestDir: "{app}"; DestName: "dbsettings.json"; Flags: onlyifdoesntexist; Components: web

; Lisans araci
Source: "payload\LisansDesktop\*"; DestDir: "{app}\Lisans"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: lisans

; DataSync
Source: "payload\DataSync\*"; DestDir: "{app}\DataSync"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: datasync

; IIS yapilandirma scriptleri
Source: "scripts\iis-configure.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "scripts\iis-remove.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "scripts\iis-install-features.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "scripts\preinstall-check.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "scripts\backup-db.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion

[Dirs]
Name: "{app}\data"; Permissions: users-modify
Name: "{app}\uploads"; Permissions: users-modify
Name: "{app}\logs"; Permissions: users-modify
Name: "{app}\database"; Permissions: users-modify
Name: "{app}\Backups"; Permissions: users-modify

[Icons]
Name: "{group}\{#MyAppName} Web'i Ac"; Filename: "http://localhost:5200"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\Lisans Yonetimi"; Filename: "{app}\Lisans\{#MyLisansExe}"; WorkingDir: "{app}\Lisans"; Components: lisans
Name: "{group}\Veri Aktarim (PG - SQLite)"; Filename: "{app}\DataSync\{#MyDataSyncExe}"; WorkingDir: "{app}\DataSync"; Components: datasync
Name: "{group}\Kurulum Klasorunu Ac"; Filename: "{app}"
Name: "{group}\Kaldir"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppName} Web"; Filename: "http://localhost:5200"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: ; Flags: createonlyiffileexists

[Run]
; IIS rolu / Hosting Bundle otomatik kurulum (secildiyse)
Filename: "powershell.exe"; \
    Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\scripts\iis-install-features.ps1"""; \
    StatusMsg: "IIS rolu / .NET Hosting Bundle yukleniyor (Internet gerekir)..."; \
    Flags: waituntilterminated; \
    Tasks: iisfeatures

; IIS yapilandirma
Filename: "powershell.exe"; \
    Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\scripts\iis-configure.ps1"" -InstallPath ""{app}"" -SiteName ""KOAFiloServis"" -Port 5200"; \
    StatusMsg: "IIS yapilandiriliyor..."; \
    Flags: runhidden waituntilterminated; \
    Tasks: iisconfigure

; Firewall kurali
Filename: "netsh.exe"; \
    Parameters: "advfirewall firewall add rule name=""KOAFiloServis HTTP"" dir=in action=allow protocol=TCP localport=5200"; \
    StatusMsg: "Firewall kurali ekleniyor..."; \
    Flags: runhidden waituntilterminated; \
    Tasks: firewall

; Tarayicida ac
Filename: "http://localhost:5200"; \
    Flags: shellexec nowait postinstall; \
    Tasks: browser; \
    Description: "Uygulamayi tarayicida ac"

[UninstallRun]
Filename: "powershell.exe"; \
    Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\scripts\iis-remove.ps1"" -SiteName ""KOAFiloServis"""; \
    Flags: runhidden waituntilterminated; \
    RunOnceId: "RemoveIIS"

Filename: "netsh.exe"; \
    Parameters: "advfirewall firewall delete rule name=""KOAFiloServis HTTP"""; \
    Flags: runhidden waituntilterminated; \
    RunOnceId: "RemoveFirewall"

[UninstallDelete]
Type: filesandordirs; Name: "{app}\wwwroot\_framework"
Type: dirifempty; Name: "{app}\scripts"

[Code]
function GetInstallPath(): String;
var sPrevPath: String;
begin
  if RegQueryStringValue(HKLM,
        'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#SetupSetting("AppId")}_is1',
        'InstallLocation', sPrevPath) then
    Result := sPrevPath
  else
    Result := '';
end;

function IsUpgrade(): Boolean;
begin
  Result := (GetInstallPath() <> '');
end;

function GetTimestamp(): String;
begin
  Result := GetDateTimeString('yyyymmdd-hhnnss', #0, #0);
end;

procedure BackupDatabase(InstallPath: String);
var
  DbFile, ShmFile, WalFile: String;
  BackupDir: String;
  ResultCode: Integer;
begin
  DbFile  := InstallPath + '\KOAFiloServis';
  ShmFile := InstallPath + '\KOAFiloServis-shm';
  WalFile := InstallPath + '\KOAFiloServis-wal';
  if not FileExists(DbFile) then Exit;
  BackupDir := InstallPath + '\Backups\db-' + GetTimestamp();
  Exec('cmd.exe', '/c mkdir "' + BackupDir + '"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec('cmd.exe', '/c copy /Y "' + DbFile + '" "' + BackupDir + '\KOAFiloServis"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  if FileExists(ShmFile) then
    Exec('cmd.exe', '/c copy /Y "' + ShmFile + '" "' + BackupDir + '\KOAFiloServis-shm"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  if FileExists(WalFile) then
    Exec('cmd.exe', '/c copy /Y "' + WalFile + '" "' + BackupDir + '\KOAFiloServis-wal"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Log('DB yedegi alindi: ' + BackupDir);
end;

procedure StopIISSite();
var ResultCode: Integer;
begin
  Exec('cmd.exe', '/c "%windir%\system32\inetsrv\appcmd.exe" stop site /site.name:"KOAFiloServis"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Log('IIS site durduruldu');
end;

procedure StartIISSite();
var ResultCode: Integer;
begin
  Exec('cmd.exe', '/c "%windir%\system32\inetsrv\appcmd.exe" start site /site.name:"KOAFiloServis"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Log('IIS site baslatildi');
end;

procedure InitializeWizard();
begin
  if IsUpgrade() then
    WizardForm.Caption := '{#MyAppName} Guncelleme Sihirbazi'
  else
    WizardForm.Caption := '{#MyAppName} Kurulum Sihirbazi';
end;

function InitializeSetup(): Boolean;
var
  PrevPath, Msg: String;
  ResultCode: Integer;
  ScriptPath: String;
begin
  Result := True;
  ScriptPath := ExpandConstant('{src}\scripts\preinstall-check.ps1');
  if FileExists(ScriptPath) then
  begin
    Exec('powershell.exe', '-NoProfile -ExecutionPolicy Bypass -File "' + ScriptPath + '" -AllowMissingHostingBundle', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    if ResultCode <> 0 then
    begin
      Msg := 'Sistem gereksinimleri eksik gorunuyor (IIS / .NET 10 Hosting Bundle).' + #13#10#13#10 +
             'Sihirbaz devam ettiginde "IIS rolu/Hosting Bundle eksikse otomatik kur" secenegini isaretleyebilirsiniz.' + #13#10#13#10 +
             'Devam etmek istiyor musunuz?';
      if MsgBox(Msg, mbConfirmation, MB_YESNO) = IDNO then
      begin Result := False; Exit; end;
    end;
  end;
  PrevPath := GetInstallPath();
  if PrevPath <> '' then
  begin
    Msg := '{#MyAppName} sistemde kurulu:' + #13#10 + PrevPath + #13#10#13#10 +
           'Bu islem mevcut kurulumu GUNCELLER.' + #13#10 +
           '* Veritabani otomatik olarak yedeklenecek (Backups\db-<tarih>).' + #13#10 +
           '* Konfigurasyonunuz (dbsettings.json, appsettings.json) KORUNUR.' + #13#10#13#10 +
           'Devam etmek istiyor musunuz?';
    if MsgBox(Msg, mbConfirmation, MB_YESNO) = IDNO then
    begin Result := False; Exit; end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var PrevPath: String;
begin
  PrevPath := GetInstallPath();
  if CurStep = ssInstall then
  begin
    if PrevPath <> '' then
    begin
      WizardForm.StatusLabel.Caption := 'Veritabani yedekleniyor...';
      BackupDatabase(PrevPath);
      StopIISSite();
    end;
  end;
  if CurStep = ssPostInstall then
  begin
    if PrevPath <> '' then StartIISSite();
  end;
end;
