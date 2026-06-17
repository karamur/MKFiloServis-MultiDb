#define MyAppName "KOAFiloServis"
#define MyAppPublisher "KOA Yazilim"
#define MyAppURL "https://github.com/karamur/KOAFiloServis-MultiDb"
#define MyAppExeName "KOAFiloServis.Web.exe"
#define MyInstallDir "C:\KOAFiloServis"
#define MyDataSyncExe "KOAFiloServis.DataSync.exe"

#ifndef MyAppVersion
#define MyAppVersion "1.0.25"
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

[Components]
Name: "web"; Description: "KOAFiloServis Web (IIS)"; Types: full; Flags: fixed
Name: "datasync"; Description: "Veri Aktarim Araci (PostgreSQL - SQLite)"; Types: full

[Tasks]
Name: "iisconfigure"; Description: "IIS Site ve AppPool'u otomatik yapilandir"; GroupDescription: "IIS:"; Flags: checkedonce
Name: "firewall"; Description: "Windows Guvenlik Duvarinda port ac (HTTP 5190)"; GroupDescription: "Firewall:"; Flags: checkedonce
Name: "browser"; Description: "Kurulum sonrasi tarayicida ac"; GroupDescription: "Son adim:"; Flags: unchecked

[Files]
Source: "payload\Web\*"; DestDir: "{app}"; Excludes: "dbsettings.json,appsettings.Production.json,*.db,logs\*,uploads\*"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: web
Source: "payload\Web\dbsettings.json"; DestDir: "{app}"; DestName: "dbsettings.json"; Flags: onlyifdoesntexist; Components: web
Source: "payload\DataSync\*"; DestDir: "{app}\DataSync"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: datasync
Source: "scripts\iis-configure.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "scripts\iis-remove.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "scripts\preinstall-check.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "scripts\backup-db.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion

[Dirs]
Name: "{app}\data"; Permissions: users-modify
Name: "{app}\uploads"; Permissions: users-modify
Name: "{app}\logs"; Permissions: users-modify
Name: "{app}\database"; Permissions: users-modify
Name: "{app}\Backups"; Permissions: users-modify

[Icons]
Name: "{group}\{#MyAppName} Web'i Ac"; Filename: "http://localhost:5190"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\Veri Aktarim (PG - SQLite)"; Filename: "{app}\DataSync\{#MyDataSyncExe}"; WorkingDir: "{app}\DataSync"; Components: datasync
Name: "{group}\Kurulum Klasorunu Ac"; Filename: "{app}"
Name: "{group}\Kaldir"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppName} Web"; Filename: "http://localhost:5190"; IconFilename: "{app}\{#MyAppExeName}"; Flags: createonlyiffileexists

[Run]
Filename: "powershell.exe"; \
    Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\scripts\iis-configure.ps1"" -InstallPath ""{app}"" -SiteName ""KOAFiloServis"" -Port 5190"; \
    StatusMsg: "IIS yapilandiriliyor..."; Flags: runhidden waituntilterminated; Tasks: iisconfigure

Filename: "netsh.exe"; \
    Parameters: "advfirewall firewall add rule name=""KOAFiloServis HTTP"" dir=in action=allow protocol=TCP localport=5190"; \
    StatusMsg: "Firewall kurali ekleniyor..."; Flags: runhidden waituntilterminated; Tasks: firewall

Filename: "http://localhost:5190"; Flags: shellexec nowait postinstall; Tasks: browser; Description: "Uygulamayi tarayicida ac"

[UninstallRun]
Filename: "powershell.exe"; \
    Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\scripts\iis-remove.ps1"" -SiteName ""KOAFiloServis"""; \
    Flags: runhidden waituntilterminated; RunOnceId: "RemoveIIS"

Filename: "netsh.exe"; \
    Parameters: "advfirewall firewall delete rule name=""KOAFiloServis HTTP"""; \
    Flags: runhidden waituntilterminated; RunOnceId: "RemoveFirewall"

[UninstallDelete]
Type: filesandordirs; Name: "{app}\wwwroot\_framework"
Type: dirifempty; Name: "{app}\scripts"

[Code]
function GetInstallPath(): String;
var sPrevPath: String;
begin
  if RegQueryStringValue(HKLM,'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#SetupSetting("AppId")}_is1','InstallLocation', sPrevPath) then
    Result := sPrevPath else Result := '';
end;
function IsUpgrade(): Boolean; begin Result := (GetInstallPath() <> ''); end;
function GetTimestamp(): String; begin Result := GetDateTimeString('yyyymmdd-hhnnss', #0, #0); end;

procedure BackupDatabase(InstallPath: String);
var DbFile,ShmFile,WalFile,BackupDir: String; ResultCode: Integer;
begin
  DbFile:=InstallPath+'\KOAFiloServis'; ShmFile:=InstallPath+'\KOAFiloServis-shm'; WalFile:=InstallPath+'\KOAFiloServis-wal';
  if not FileExists(DbFile) then Exit;
  BackupDir:=InstallPath+'\Backups\db-'+GetTimestamp();
  Exec('cmd.exe','/c mkdir "'+BackupDir+'"','',SW_HIDE,ewWaitUntilTerminated,ResultCode);
  Exec('cmd.exe','/c copy /Y "'+DbFile+'" "'+BackupDir+'\KOAFiloServis"','',SW_HIDE,ewWaitUntilTerminated,ResultCode);
  if FileExists(ShmFile) then Exec('cmd.exe','/c copy /Y "'+ShmFile+'" "'+BackupDir+'\KOAFiloServis-shm"','',SW_HIDE,ewWaitUntilTerminated,ResultCode);
  if FileExists(WalFile) then Exec('cmd.exe','/c copy /Y "'+WalFile+'" "'+BackupDir+'\KOAFiloServis-wal"','',SW_HIDE,ewWaitUntilTerminated,ResultCode);
end;

procedure StopIISSite(); var ResultCode: Integer;
begin Exec('cmd.exe','/c "%windir%\system32\inetsrv\appcmd.exe" stop site /site.name:"KOAFiloServis"','',SW_HIDE,ewWaitUntilTerminated,ResultCode); end;

procedure StartIISSite(); var ResultCode: Integer;
begin Exec('cmd.exe','/c "%windir%\system32\inetsrv\appcmd.exe" start site /site.name:"KOAFiloServis"','',SW_HIDE,ewWaitUntilTerminated,ResultCode); end;

procedure InitializeWizard();
begin if IsUpgrade() then WizardForm.Caption:='{#MyAppName} Guncelleme Sihirbazi' else WizardForm.Caption:='{#MyAppName} Kurulum Sihirbazi'; end;

function InitializeSetup(): Boolean;
var PrevPath,Msg: String;
begin
  Result:=True; PrevPath:=GetInstallPath();
  if PrevPath<>'' then begin
    Msg:='{#MyAppName} sistemde kurulu:'+#13#10+PrevPath+#13#10#13#10+'Bu islem mevcut kurulumu GUNCELLER.'+#13#10+'* Veritabani otomatik yedeklenecek.'+#13#10+'* Konfigurasyonlar KORUNUR.'+#13#10#13#10+'Devam etmek istiyor musunuz?';
    if MsgBox(Msg,mbConfirmation,MB_YESNO)=IDNO then begin Result:=False; Exit; end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var PrevPath: String;
begin
  PrevPath:=GetInstallPath();
  if CurStep=ssInstall then begin if PrevPath<>'' then begin BackupDatabase(PrevPath); StopIISSite(); end; end;
  if CurStep=ssPostInstall then begin if PrevPath<>'' then StartIISSite(); end;
end;
