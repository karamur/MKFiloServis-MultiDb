@echo off
setlocal

set CONFIG=Release
set RUNTIME=win-x64
set OUT=./artifacts/setup
set VERSION=1.0.27

if not "%~1"=="" set CONFIG=%~1
if not "%~2"=="" set RUNTIME=%~2
if not "%~3"=="" set OUT=%~3
if not "%~4"=="" set VERSION=%~4

echo [SETUP] PowerShell setup script calistiriliyor...
echo [INFO] Versiyon: %VERSION%
pwsh -ExecutionPolicy Bypass -File "%~dp0setup.ps1" -Configuration %CONFIG% -Runtime %RUNTIME% -OutputRoot %OUT% -Version %VERSION%

if %errorlevel% neq 0 (
  echo [HATA] Setup basarisiz.
  exit /b %errorlevel%
)

echo [OK] Setup tamamlandi.
endlocal
