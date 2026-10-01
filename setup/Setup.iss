; ============================================================
; MKFiloServis — Tam Kurulum (IIS + Hosting Bundle gomulu)
; Harici indirme/kurulum gerektirmez; IIS otomatik etkinlestirilir.
; ============================================================

#define MyAppName        "MKFiloServis"
#define MyAppPublisher   "MK Yazilim"
#define MyAppURL         "https://github.com/karamur/MKFiloServis-MultiDb"
#define MyAppExeName     "MKFiloServis.Web.exe"
#define MyInstallDirBase "C:\MKFiloServis"
#define MyBackupDirBase  "C:\MKFiloServis_yedekleme"
#define MyLisansExe      "MKFiloServisLisans.exe"
#define MyDataSyncExe    "MKFiloServis.DataSync.exe"
#define MyIisSiteName    "MKFiloServis"
#define MyIisAppPool     "MKFiloServis"
#define MyIisPort        "5050"

#ifndef MyAppVersion
#define MyAppVersion "1.0.37"
#endif

#define MyVersionToken StringChange(MyAppVersion, ".", "_")
#define MyInstallDir MyInstallDirBase
#define MyBackupDir MyBackupDirBase
#define MyAppId "A1B2C3D4-E5F6-7890-ABCD-EF1234567890-USTUN"
#define MyShortcutName MyAppName + " Ustun"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={#MyInstallDir}
DisableDirPage=yes
DefaultGroupName={#MyAppName}
OutputBaseFilename=MKFiloServisKurulum-{#MyAppVersion}
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

; ASP.NET Core Hosting Bundle (gomulu; hedef makinede internet gerekmez)
Source: "payload\redist\dotnet-hosting-win.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

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

function IsIISInstalled(): Boolean;
begin
  Result := FileExists(ExpandConstant('{sys}\inetsrv\appcmd.exe'));
end;

function IsHostingBundleInstalled(): Boolean;
var
  FindRec: TFindRec;
begin
  // .NET 10 ASP.NET Core runtime kurulu mu?
  Result := FindFirst(ExpandConstant('{commonpf64}\dotnet\shared\Microsoft.AspNetCore.App\10.*'), FindRec);
  if Result then
    FindClose(FindRec);
end;

procedure RunHidden(const FileName, Params, StatusMsg: String);
var
  ResultCode: Integer;
begin
  WizardForm.StatusLabel.Caption := StatusMsg;
  Exec(FileName, Params, '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Log(Format('RunHidden: %s %s -> %d', [FileName, Params, ResultCode]));
end;

procedure ConfigureIIS();
var
  AppCmd: String;
begin
  // 1) IIS ozelliklerini etkinlestir (kuruluysa hizli gecer)
  if not IsIISInstalled() then
    RunHidden(ExpandConstant('{sys}\dism.exe'),
      '/online /enable-feature /featurename:IIS-WebServerRole /featurename:IIS-WebServer' +
      ' /featurename:IIS-CommonHttpFeatures /featurename:IIS-StaticContent /featurename:IIS-DefaultDocument' +
      ' /featurename:IIS-HttpErrors /featurename:IIS-HttpLogging /featurename:IIS-RequestFiltering' +
      ' /featurename:IIS-ApplicationInit /featurename:IIS-WebSockets /featurename:IIS-ManagementConsole' +
      ' /all /norestart',
      'IIS etkinlestiriliyor (birkac dakika surebilir)...');

  // 2) Hosting Bundle'i gomulu paketten sessiz kur (kuruluysa atla)
  if not IsHostingBundleInstalled() then
    RunHidden(ExpandConstant('{tmp}\dotnet-hosting-win.exe'),
      '/install /quiet /norestart',
      '.NET Hosting Bundle kuruluyor...');

  // 3) AppPool + Site olustur/guncelle (varsa hatalar yoksayilir)
  AppCmd := ExpandConstant('{sys}\inetsrv\appcmd.exe');
  if FileExists(AppCmd) then
  begin
    RunHidden(AppCmd, 'add apppool /name:"{#MyIisAppPool}" /managedRuntimeVersion:"" /startMode:AlwaysRunning',
      'IIS uygulama havuzu olusturuluyor...');
    RunHidden(AppCmd, ExpandConstant('add site /name:"{#MyIisSiteName}" /physicalPath:"{app}\app" /bindings:http/*:{#MyIisPort}:'),
      'IIS sitesi olusturuluyor...');
    RunHidden(AppCmd, 'set site /site.name:"{#MyIisSiteName}" /[path=''/''].applicationPool:"{#MyIisAppPool}"',
      'IIS sitesi yapilandiriliyor...');
    RunHidden(AppCmd, ExpandConstant('set vdir "{#MyIisSiteName}/" /physicalPath:"{app}\app"'),
      'IIS fiziksel yol guncelleniyor...');
    RunHidden(AppCmd, 'start apppool /apppool.name:"{#MyIisAppPool}"', 'Uygulama havuzu baslatiliyor...');
    RunHidden(AppCmd, 'start site /site.name:"{#MyIisSiteName}"', 'Site baslatiliyor...');
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    ConfigureIIS();
end;

function InitializeSetup(): Boolean;
var
  Msg: String;
begin
  Result := True;
  Msg := '{#MyAppName} {#MyAppVersion} ayri bir klasore kurulacaktir:' + #13#10 +
         '{#MyInstallDir}' + #13#10#13#10 +
         'IIS ve .NET Hosting Bundle otomatik kurulur; harici indirme gerekmez.' + #13#10 +
         'Bu kurulum mevcut versiyonlara dokunmaz ve yan yana calisabilir.' + #13#10 +
         'Devam etmek istiyor musunuz?';
  if MsgBox(Msg, mbConfirmation, MB_YESNO) = IDNO then
  begin Result := False; Exit; end;
end;
