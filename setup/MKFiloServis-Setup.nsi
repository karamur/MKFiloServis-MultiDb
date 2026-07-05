; MKFiloServis NSIS Installer Script
; Version: 1.0.27
; NSIS version required: 3.0 or higher

;--------------------------------
; Include necessary header files
!include "MUI2.nsh"
!include "x64.nsh"
!include "FileFunc.nsh"

;--------------------------------
; General Settings
Name "MKFiloServis 1.0.27"
OutFile "..\setup\output\v1.0.27\MKFiloServis-1.0.27-Setup.exe"
InstallDir "$PROGRAMFILES64\MKFiloServis"
InstallDirRegKey HKCU "Software\MKFiloServis" "InstallPath"
RequestExecutionLevel admin

;--------------------------------
; Version Information
VIProductVersion "1.0.27.0"
VIAddVersionKey "ProductName" "MKFiloServis"
VIAddVersionKey "ProductVersion" "1.0.27"
VIAddVersionKey "CompanyName" "MKFiloServis"
VIAddVersionKey "FileVersion" "1.0.27"
VIAddVersionKey "FileDescription" "MKFiloServis Setup Installer"
VIAddVersionKey "LegalCopyright" "Copyright (C) 2024 MKFiloServis"

;--------------------------------
; MUI Settings
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_LANGUAGE "Turkish"
!insertmacro MUI_LANGUAGE "English"

;--------------------------------
; Installer Sections

Section "MKFiloServis Application" SEC_APP
  SectionIn RO
  SetOutPath "$INSTDIR"

  ; Copy application files
  File /r "..\artifacts\setup\package\*.*"

  ; Create directories
  CreateDirectory "$INSTDIR\Logs"
  CreateDirectory "$INSTDIR\Data"
  CreateDirectory "$INSTDIR\Config"
  CreateDirectory "$INSTDIR\Tools"
  CreateDirectory "$INSTDIR\Documentation"

  ; Write installation info to registry
  WriteRegStr HKCU "Software\MKFiloServis" "InstallPath" "$INSTDIR"
  WriteRegStr HKCU "Software\MKFiloServis" "Version" "1.0.27"
  WriteRegStr HKCU "Software\MKFiloServis" "InstallDate" "$INSTDIR"

  ; Create uninstaller
  WriteUninstaller "$INSTDIR\Uninstall.exe"

  ; Create shortcuts
  CreateShortcut "$SMPROGRAMS\MKFiloServis.lnk" "$INSTDIR\MKFiloServis.Web.exe"

SectionEnd

Section "IIS Integration Tools" SEC_IIS
  SetOutPath "$INSTDIR\Tools"
  File "..\MKFiloServis.Web\Deploy\IIS\kur.ps1"
  File "..\MKFiloServis.Web\Deploy\IIS\kur.bat"
SectionEnd

Section "Documentation" SEC_DOCS
  SetOutPath "$INSTDIR\Documentation"
  File "..\setup\output\README.md"
  File "..\setup\output\INSTALL.md"
  File "..\setup\output\QUICKSTART.md"
  File "..\setup\output\.deploy\DEPLOY.md"
  File "..\setup\output\.deploy\DOCKER.md"
  File "..\setup\output\.deploy\CONFIG.md"
SectionEnd

;--------------------------------
; Uninstaller
Section "Uninstall"
  RMDir /r "$INSTDIR"
  DeleteRegKey HKCU "Software\MKFiloServis"
  Delete "$SMPROGRAMS\MKFiloServis.lnk"
SectionEnd

;--------------------------------
; Language Strings
LangString DESC_APP ${LANG_TURKISH} "MKFiloServis uygulaması dosyaları"
LangString DESC_IIS ${LANG_TURKISH} "IIS entegrasyon araçları"
LangString DESC_DOCS ${LANG_TURKISH} "Kullanım dokümantasyonu"

LangString DESC_APP ${LANG_ENGLISH} "MKFiloServis application files"
LangString DESC_IIS ${LANG_ENGLISH} "IIS integration tools"
LangString DESC_DOCS ${LANG_ENGLISH} "Documentation files"

!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${SEC_APP} $(DESC_APP)
  !insertmacro MUI_DESCRIPTION_TEXT ${SEC_IIS} $(DESC_IIS)
  !insertmacro MUI_DESCRIPTION_TEXT ${SEC_DOCS} $(DESC_DOCS)
!insertmacro MUI_FUNCTION_DESCRIPTION_END
