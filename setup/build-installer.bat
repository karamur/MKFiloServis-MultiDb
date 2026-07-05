@echo off
REM MKFiloServis Setup Installer Generator - Batch Wrapper
REM Version: 1.0.27

setlocal enabledelayedexpansion

echo.
echo ========================================
echo MKFiloServis Setup Installer Generator
echo Version: 1.0.27
echo ========================================
echo.

REM Check if PowerShell is available
powershell -v >nul 2>&1
if %errorlevel% neq 0 (
  echo [HATA] PowerShell bulunamadi!
  pause
  exit /b 1
)

REM Get installer type from argument
set INSTALLER=inno
if not "%~1"=="" set INSTALLER=%~1

REM Build flag
set BUILD=
if not "%~2"=="" (
  if "%~2"=="build" set BUILD=-BuildBefore
)

echo [INFO] Installer Tipi: %INSTALLER%
echo [INFO] Derleme: %BUILD%
echo.

REM Run PowerShell script
echo [1/1] PowerShell scripti calistiriliyor...
powsh -ExecutionPolicy Bypass -File "%~dp0build-installer.ps1" -InstallerType %INSTALLER% %BUILD%

if %errorlevel% neq 0 (
  echo [HATA] Installer olusturma basarisiz!
  pause
  exit /b %errorlevel%
)

echo.
echo [BASARILI] Installer olusturma tamamlandi!
echo.
pause
endlocal
