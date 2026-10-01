; MK Filo Servis Windows MAUI istemcisi. Web sunucusuna baglanir.
#define MyAppName "MK Filo Servis Masaustu"
#ifndef MyAppVersion
#define MyAppVersion "1.0.37"
#endif
#ifndef OutputDir
#define OutputDir "output\v" + MyAppVersion
#endif

[Setup]
AppId=com.mkfiloservis.desktop
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher=MK Yazilim
DefaultDirName={localappdata}\Programs\MKFiloServis Desktop
DefaultGroupName={#MyAppName}
OutputDir={#OutputDir}
OutputBaseFilename=MKFiloServisMasaustu-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\MKFiloServis.Client.exe
CloseApplications=force

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Files]
Source: "payload\Desktop\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\MKFiloServis.Client.exe"; WorkingDir: "{app}"
Name: "{userdesktop}\{#MyAppName}"; Filename: "{app}\MKFiloServis.Client.exe"; WorkingDir: "{app}"

[Run]
Filename: "{app}\MKFiloServis.Client.exe"; Description: "{#MyAppName} uygulamasini ac"; Flags: nowait postinstall skipifsilent
