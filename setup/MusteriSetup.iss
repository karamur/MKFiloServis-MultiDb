; ============================================================
; MKFiloServis — Musteri Kurulumu (Web + DataSync, Lisans Yok)
; ============================================================

#define MyAppName        "MKFiloServis"
#define MyAppPublisher   "MK Yazilim"
#define MyAppURL         "https://github.com/karamur/MKFiloServis-MultiDb"
#define MyAppExeName     "MKFiloServis.Web.exe"
#ifndef MyInstallDirBase
#define MyInstallDirBase "C:\MKFiloServis_Musteri"
#endif
#define MyDataSyncExe    "MKFiloServis.DataSync.exe"

#ifndef MyAppVersion
#define MyAppVersion "1.0.37"
#endif

#define MyVersionToken StringChange(MyAppVersion, ".", "_")
#define MyInstallDir MyInstallDirBase
#define MyBackupDir "C:\MKFiloServis_yedekleme"
#define MyAppId "A1B2C3D4-E5F6-7890-ABCD-EF1234567890-MUSTERI-USTUN"
#define MyShortcutName MyAppName + " Musteri Ustun"

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
OutputBaseFilename=MKFiloServisKurulumMusteri-{#MyAppVersion}
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
Name: "web"; Description: "MKFiloServis Web"; Types: full; Flags: fixed
Name: "datasync"; Description: "Veri Aktarim Araci"; Types: full

[Files]
Source: "payload\Web\*"; DestDir: "{app}\app"; Excludes: "dbsettings.json,portalsettings.json,backup_settings.json,appsettings.*.json,cookies.txt,*.db,*.db-shm,*.db-wal,logs\*,uploads\*,Backups\*,keys\*"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: web
Source: "payload\DataSync\*"; DestDir: "{app}\tools\datasync"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: datasync
Source: "payload\redist\dotnet-hosting-win.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Dirs]
Name: "{app}\data"; Permissions: users-modify
Name: "{app}\uploads"; Permissions: users-modify
Name: "{app}\logs"; Permissions: users-modify
Name: "{app}\Backups"; Permissions: users-modify
Name: "{#MyBackupDir}"; Permissions: users-modify

[Icons]
Name: "{group}\{#MyShortcutName}"; Filename: "{app}\app\{#MyAppExeName}"; WorkingDir: "{app}\app"
Name: "{group}\{#MyShortcutName} - Veri Aktarim"; Filename: "{app}\tools\datasync\{#MyDataSyncExe}"; WorkingDir: "{app}\tools\datasync"; Components: datasync
Name: "{group}\{#MyShortcutName} - Kurulum Klasorunu Ac"; Filename: "{app}"
Name: "{group}\{#MyShortcutName} - Kaldir"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyShortcutName}"; Filename: "{app}\app\{#MyAppExeName}"; WorkingDir: "{app}\app"

[Run]
Filename: "{app}\app\{#MyAppExeName}"; Description: "Uygulamayi Baslat"; Flags: nowait postinstall skipifsilent; WorkingDir: "{app}\app"; Check: CanLaunchApp

[Code]
var
  DbProviderPage: TInputOptionWizardPage;
  DbConnectionPage: TInputQueryWizardPage;
  HostingRestartRequired: Boolean;

function CanLaunchApp(): Boolean;
begin
  Result := not HostingRestartRequired;
end;

function NeedRestart(): Boolean;
begin
  Result := HostingRestartRequired;
end;

function InitializeSetup(): Boolean;
var Msg: String;
begin
  Result := True;
  if FileExists('{#MyInstallDir}\app\dbsettings.json') or
     FileExists('{#MyInstallDir}\app\{#MyAppExeName}') then
  begin
    MsgBox('Bu dizinde mevcut kurulum var. Ayarlari ve veriyi korumak icin guncelleme paketini kullanin.', mbError, MB_OK);
    Result := False; Exit;
  end;
  Msg := '{#MyAppName} Musteri {#MyAppVersion} ayri bir klasore kurulacaktir:' + #13#10 +
         '{#MyInstallDir}' + #13#10#13#10 +
         'Mevcut kurulumlarin dizinine kurulmaz.' + #13#10 +
         'Devam etmek istiyor musunuz?';
  if MsgBox(Msg, mbConfirmation, MB_YESNO) = IDNO then begin Result := False; Exit; end;
end;

procedure InitializeWizard();
begin
  WizardForm.Caption := '{#MyAppName} Musteri {#MyAppVersion} Kurulum Sihirbazi';
  DbProviderPage := CreateInputOptionPage(wpSelectDir,
    'Veritabani Secimi', 'Uygulamanin kullanacagi veritabanini secin',
    'Bu surumde PostgreSQL ve SQLite desteklenir.',
    True, False);
  DbProviderPage.Add('PostgreSQL');
  DbProviderPage.Add('SQLite');
  DbProviderPage.SelectedValueIndex := 0;
  DbConnectionPage := CreateInputQueryPage(DbProviderPage.ID,
    'Veritabani Baglantisi', 'Secilen veritabani icin baglanti bilgilerini girin',
    'PostgreSQL icin sunucu/parola; SQLite icin veritabani dosya yolu kullanilir.');
  DbConnectionPage.Add('Host (PostgreSQL; SQLite icin kullanilmaz)', False);
  DbConnectionPage.Add('Port (PostgreSQL; SQLite icin kullanilmaz)', False);
  DbConnectionPage.Add('Veritabani adi (PostgreSQL) / DB dosya yolu (SQLite)', False);
  DbConnectionPage.Add('Kullanici adi', False);
  DbConnectionPage.Add('Parola', True);
  DbConnectionPage.Values[0] := 'localhost';
  DbConnectionPage.Values[1] := '5432';
  DbConnectionPage.Values[2] := 'MKFiloServis';
  DbConnectionPage.Values[3] := 'postgres';
end;

function JsonEscape(Value: String): String;
begin
  StringChangeEx(Value, '\', '\\', True);
  StringChangeEx(Value, '"', '\"', True);
  StringChangeEx(Value, #13, '\r', True);
  StringChangeEx(Value, #10, '\n', True);
  Result := Value;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = DbProviderPage.ID then
  begin
    if DbProviderPage.SelectedValueIndex = 1 then
    begin
      DbConnectionPage.Values[2] := 'App_Data/MKFiloServis.db';
    end
    else
    begin
      DbConnectionPage.Values[0] := 'localhost';
      DbConnectionPage.Values[1] := '5432';
      DbConnectionPage.Values[2] := 'MKFiloServis';
      DbConnectionPage.Values[3] := 'postgres';
    end;
  end
  else if CurPageID = DbConnectionPage.ID then
  begin
    if DbProviderPage.SelectedValueIndex = 0 then
    begin
      if (Trim(DbConnectionPage.Values[0]) = '') or
         (Trim(DbConnectionPage.Values[2]) = '') or
         (Trim(DbConnectionPage.Values[3]) = '') or
         (StrToIntDef(DbConnectionPage.Values[1], 0) < 1) or
         (StrToIntDef(DbConnectionPage.Values[1], 0) > 65535) then
      begin
        MsgBox('PostgreSQL sunucu, port, veritabani adi ve kullanici adi gecerli olmalidir.', mbError, MB_OK);
        Result := False;
      end;
    end
    else if Trim(DbConnectionPage.Values[2]) = '' then
    begin
      MsgBox('SQLite veritabani dosya yolu bos olamaz.', mbError, MB_OK);
      Result := False;
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ProviderValue, HostValue, PortValue, NameValue, UserValue, PasswordValue: String;
  SettingsPath, JsonText: String;
  ResultCode: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    if not Exec(ExpandConstant('{tmp}\dotnet-hosting-win.exe'), '/quiet /norestart', '',
      SW_HIDE, ewWaitUntilTerminated, ResultCode) then
      RaiseException('.NET Hosting Bundle baslatilamadi.');
    if (ResultCode <> 0) and (ResultCode <> 3010) then
      RaiseException('.NET Hosting Bundle kurulumu basarisiz: ' + IntToStr(ResultCode));
    HostingRestartRequired := ResultCode = 3010;
    if DbProviderPage.SelectedValueIndex = 0 then
    begin
      ProviderValue := '2';
      HostValue := JsonEscape(Trim(DbConnectionPage.Values[0]));
      PortValue := IntToStr(StrToIntDef(DbConnectionPage.Values[1], 5432));
      NameValue := JsonEscape(Trim(DbConnectionPage.Values[2]));
      UserValue := JsonEscape(Trim(DbConnectionPage.Values[3]));
      PasswordValue := JsonEscape(DbConnectionPage.Values[4]);
    end
    else
    begin
      ProviderValue := '1';
      HostValue := '';
      PortValue := '0';
      NameValue := JsonEscape(Trim(DbConnectionPage.Values[2]));
      UserValue := '';
      PasswordValue := '';
    end;
    JsonText := '{' + #13#10 +
      '  "Id": 0,' + #13#10 +
      '  "Provider": ' + ProviderValue + ',' + #13#10 +
      '  "CanonicalProvider": 2,' + #13#10 +
      '  "Host": "' + HostValue + '",' + #13#10 +
      '  "Port": ' + PortValue + ',' + #13#10 +
      '  "DatabaseName": "' + NameValue + '",' + #13#10 +
      '  "Username": "' + UserValue + '",' + #13#10 +
      '  "Password": "' + PasswordValue + '",' + #13#10 +
      '  "UseIntegratedSecurity": false,' + #13#10 +
      '  "LastUpdated": "' + GetDateTimeString('yyyy-mm-dd"T"hh:nn:ss', '-', ':') + 'Z"' + #13#10 +
      '}';
    SettingsPath := ExpandConstant('{app}\app\dbsettings.json');
    if not SaveStringToFile(SettingsPath, UTF8Encode(JsonText), False) then
      RaiseException('dbsettings.json dosyasi yazilamadi: ' + SettingsPath);
    if DbProviderPage.SelectedValueIndex = 1 then
      ForceDirectories(ExpandConstant('{app}\app\App_Data'));
  end;
end;
