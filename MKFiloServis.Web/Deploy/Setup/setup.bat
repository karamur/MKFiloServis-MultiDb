@echo off
setlocal

set CONFIG=Release
set RUNTIME=win-x64
set OUT=./artifacts/setup

if not "%~1"=="" set CONFIG=%~1
if not "%~2"=="" set RUNTIME=%~2
if not "%~3"=="" set OUT=%~3

echo [SETUP] PowerShell setup script calistiriliyor...
pwsh -ExecutionPolicy Bypass -File "%~dp0setup.ps1" -Configuration %CONFIG% -Runtime %RUNTIME% -OutputRoot %OUT%

if %errorlevel% neq 0 (
  echo [HATA] Setup basarisiz.
  exit /b %errorlevel%
)

echo [OK] Setup tamamlandi.
endlocal
