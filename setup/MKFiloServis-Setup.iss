; MKFiloServis Setup Installer - Inno Setup Script
; Version: 1.0.27
; Language: Turkish
; This script creates a Windows installer for MKFiloServis

[Setup]
AppName=MKFiloServis
AppVersion=1.0.27
AppPublisher=MKFiloServis
AppPublisherURL=https://github.com/karamur/MKFiloServis-MultiDb
AppSupportURL=https://github.com/karamur/MKFiloServis-MultiDb/issues
AppUpdatesURL=https://github.com/karamur/MKFiloServis-MultiDb/releases
AppCopyright=Copyright (C) 2024 MKFiloServis
DefaultDirName={pf}\MKFiloServis
DefaultGroupName=MKFiloServis
AllowNoIcons=yes
OutputDir=.\setup\output\v1.0.27
OutputBaseFilename=MKFiloServis-1.0.27-Setup
SetupIconFile={src}\..\..\..\..\setup\icons\app.ico
UninstallIconFile={src}\..\..\..\..\setup\icons\app.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
VersionInfoVersion=1.0.27.0
VersionInfoProductVersion=1.0.27.0
VersionInfoProductName=MKFiloServis
VersionInfoOriginalFilename=MKFiloServis-1.0.27-Setup.exe
CloseApplications=yes
RestartApplications=yes
PrivilegesRequired=admin
ArchitecturesInstalled=x64
ArchitecturesAllowed=x64
MinVersion=10.0
LanguageDetectionMethod=uilanguage
ShowLanguageDialog=yes

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
turkish.WelcomeLabel1=MKFiloServis Kurulum Sihirbazına Hoşgeldiniz
turkish.WelcomeLabel2=Bu sihirbaz, MKFiloServis sürüm 1.0.27'yi bilgisayarınıza yükleyecektir.%n%nYüklemeye başlamadan önce tüm uygulamaları kapatmanız tavsiye edilir.
turkish.WizardSelectComponents=Bileşenleri Seç
turkish.SelectComponentsText1=Kurmak istediğiniz bileşenleri seçiniz:%n%nMevcut alana göre kurulacak:
turkish.SelectComponentsText2=MB alanı gereklidir.

english.WelcomeLabel1=Welcome to MKFiloServis Setup Wizard
english.WelcomeLabel2=This wizard will install MKFiloServis version 1.0.27 on your computer.%n%nPlease close all other applications before continuing.
english.WizardSelectComponents=Select Components
english.SelectComponentsText1=Check the components you want to install:%n%nRequired disk space:
english.SelectComponentsText2=MB.

[Types]
Name: "custom"; Description: "Custom Installation"; Flags: iscustom

[Components]
Name: "application"; Description: "MKFiloServis Application"; Types: custom; Flags: fixed
Name: "iishelper"; Description: "IIS Integration Tools"; Types: custom
Name: "documentation"; Description: "Documentation & Help Files"; Types: custom
Name: "examples"; Description: "Example Configuration Files"; Types: custom

[Files]
; Application files
Source: "{src}\artifacts\setup\package\*"; DestDir: "{app}"; Components: application; Flags: ignoreversion recursesubdirs createallsubdirs
; IIS Helper scripts
Source: "{src}\MKFiloServis.Web\Deploy\IIS\kur.ps1"; DestDir: "{app}\Tools"; Components: iishelper; DestName: "Deploy-IIS.ps1"
Source: "{src}\MKFiloServis.Web\Deploy\IIS\kur.bat"; DestDir: "{app}\Tools"; Components: iishelper; DestName: "Deploy-IIS.bat"
; Documentation
Source: "{src}\setup\output\README.md"; DestDir: "{app}\Documentation"; Components: documentation
Source: "{src}\setup\output\INSTALL.md"; DestDir: "{app}\Documentation"; Components: documentation
Source: "{src}\setup\output\QUICKSTART.md"; DestDir: "{app}\Documentation"; Components: documentation
Source: "{src}\setup\output\.deploy\DEPLOY.md"; DestDir: "{app}\Documentation"; Components: documentation
Source: "{src}\setup\output\.deploy\DOCKER.md"; DestDir: "{app}\Documentation"; Components: documentation
Source: "{src}\setup\output\.deploy\CONFIG.md"; DestDir: "{app}\Documentation"; Components: documentation
; Configuration examples
Source: "{src}\MKFiloServis.Web\appsettings.json"; DestDir: "{app}\Config"; Components: examples
Source: "{src}\MKFiloServis.Web\appsettings.Development.json"; DestDir: "{app}\Config"; Components: examples; Name: "appsettings.Development.json.example"
; Setup scripts
Source: "{src}\setup\output\setup.ps1"; DestDir: "{app}\Setup"; Components: application
Source: "{src}\setup\output\setup.bat"; DestDir: "{app}\Setup"; Components: application
Source: "{src}\setup\output\version.txt"; DestDir: "{app}"; Components: application

[Dirs]
Name: "{app}\Logs"
Name: "{app}\Data"
Name: "{app}\Config"
Name: "{app}\Tools"
Name: "{app}\Documentation"

[Icons]
Name: "{group}\MKFiloServis"; Filename: "{app}\MKFiloServis.Web.exe"; IconIndex: 0; Comment: "Filo Servis Management System"
Name: "{group}\IIS Deploy Tools"; Filename: "{app}\Tools\Deploy-IIS.bat"; IconIndex: 0; WorkingDir: "{app}\Tools"; Comment: "Deploy to IIS"
Name: "{group}\Documentation"; Filename: "{app}\Documentation\README.md"; Comment: "Help and Documentation"
Name: "{group}\{cm:UninstallProgram,MKFiloServis}"; Filename: "{uninstallexe}"; Comment: "Uninstall MKFiloServis"
Name: "{commondesktop}\MKFiloServis"; Filename: "{app}\MKFiloServis.Web.exe"; IconIndex: 0; WorkingDir: "{app}"; Comment: "MKFiloServis - Filo Servis Management"

[Run]
Filename: "{app}\Setup\setup.ps1"; Parameters: "-Configuration Release -Version 1.0.27"; Flags: runhidden; Components: application; StatusMsg: "Finalizing installation..."
Filename: "{app}\MKFiloServis.Web.exe"; Description: "Start MKFiloServis"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: dirifempty; Name: "{app}"
Type: dirifempty; Name: "{app}\Logs"
Type: dirifempty; Name: "{app}\Data"
Type: dirifempty; Name: "{app}\Tools"
Type: dirifempty; Name: "{app}\Documentation"

[InstallDelete]
Type: files; Name: "{app}\*.log"

[Registry]
Root: HKCU; Subkey: "Software\MKFiloServis"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: createvalueifdoesntexist
Root: HKCU; Subkey: "Software\MKFiloServis"; ValueType: string; ValueName: "Version"; ValueData: "1.0.27"; Flags: createvalueifdoesntexist
Root: HKCU; Subkey: "Software\MKFiloServis"; ValueType: string; ValueName: "InstallDate"; ValueData: "{code:GetInstallDate}"; Flags: createvalueifdoesntexist

[Code]
function GetInstallDate(Param: string): string;
begin
  Result := GetDateTimeString('yyyy-mm-dd hh:mm:ss', '-', ':');
end;

procedure InitializeWizard();
begin
  Log('MKFiloServis Setup v1.0.27 Starting...');
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpFinished then
    Log('MKFiloServis Setup Completed Successfully');
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
end;
