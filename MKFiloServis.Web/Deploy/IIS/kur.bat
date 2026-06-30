@echo off
setlocal

set "TARGET_DIR=%~1"
if "%TARGET_DIR%"=="" set "TARGET_DIR=C:\KOAFiloServis\IIS"

set "BACKUP_ROOT=%~2"
if "%BACKUP_ROOT%"=="" set "BACKUP_ROOT=C:\KOAFiloServis_yedekleme\deploy"

set "SITE_NAME=%~3"

set "MODE=%~4"
if "%MODE%"=="" set "MODE=Update"

echo KOA Filo Servis IIS kurulum/guncelleme baslatiliyor...
echo Mod          : %MODE%
echo Hedef klasor : %TARGET_DIR%
echo Yedek klasoru: %BACKUP_ROOT%

pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0kur.ps1" -TargetDir "%TARGET_DIR%" -BackupRoot "%BACKUP_ROOT%" -SiteName "%SITE_NAME%" -Mode "%MODE%"
set "EXITCODE=%ERRORLEVEL%"

if not "%EXITCODE%"=="0" (
    echo Kurulum/guncelleme basarisiz oldu. Hata kodu: %EXITCODE%
    exit /b %EXITCODE%
)

echo Kurulum/guncelleme tamamlandi.
exit /b 0
