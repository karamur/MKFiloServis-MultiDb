@echo off
REM ============================================================
REM  MKFiloServis-MultiDb - Setup EXE Uretici
REM ============================================================
setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
if "%SCRIPT_DIR:~-1%"=="\" set "SCRIPT_DIR=%SCRIPT_DIR:~0,-1%"

set "VERSION=%~1"
if "%VERSION%"=="" set "VERSION=1.0.22"

net session >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo [INFO] Admin yetkisi gerekiyor, yukseltiliyor...
    powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -ArgumentList '%VERSION%' -Verb RunAs"
    exit /b 0
)

echo ============================================================
echo  MKFiloServis-MultiDb Setup Builder
echo  Surum  : %VERSION%
echo  Klasor : %SCRIPT_DIR%
echo ============================================================
echo.

where dotnet >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo [HATA] .NET SDK bulunamadi. .NET 10 SDK kurun:
    echo        https://dotnet.microsoft.com/download/dotnet/10.0
    pause
    exit /b 1
)

set "ISCC="
for %%P in (
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    "C:\Program Files\Inno Setup 6\ISCC.exe"
    "%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"
) do (
    if exist %%P set "ISCC=%%P"
)

if "%ISCC%"=="" (
    echo [HATA] Inno Setup 6 (ISCC.exe) bulunamadi.
    echo        Kurmak icin: winget install JRSoftware.InnoSetup
    pause
    exit /b 1
)

echo [OK] dotnet ve ISCC bulundu.
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%\build.ps1" -Version %VERSION%
set "EXITCODE=%ERRORLEVEL%"

echo.
if %EXITCODE% EQU 0 (
    echo ============================================================
    echo  BASARILI! Cikti: %SCRIPT_DIR%\output\v%VERSION%
    echo ============================================================
    if exist "%SCRIPT_DIR%\output\v%VERSION%" start "" "%SCRIPT_DIR%\output\v%VERSION%"
) else (
    echo ============================================================
    echo  HATA! Cikis kodu: %EXITCODE%
    echo ============================================================
)

pause
exit /b %EXITCODE%
