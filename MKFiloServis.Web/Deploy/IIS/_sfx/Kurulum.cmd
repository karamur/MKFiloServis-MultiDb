@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "SCRIPT_DIR=%~dp0"
set "MODE=Update"
set "VERSION="

if exist "%SCRIPT_DIR%MODE.txt" (
    set /p MODE=<"%SCRIPT_DIR%MODE.txt"
)
if /I "%MODE%"=="Install" (
    set "MODE=Install"
) else (
    set "MODE=Update"
)

if exist "%SCRIPT_DIR%VERSION.txt" (
    set /p VERSION=<"%SCRIPT_DIR%VERSION.txt"
)

echo ================================================
echo KOA Filo Servis SFX paket aciliyor...
echo Mod      : %MODE%
echo Versiyon : %VERSION%
echo Konum    : %SCRIPT_DIR%
echo ================================================
echo.

if not exist "%SCRIPT_DIR%App.zip" (
    echo [HATA] App.zip bulunamadi.
    pause
    exit /b 1
)

where pwsh >nul 2>&1
if errorlevel 1 (
    echo [HATA] PowerShell (pwsh) bulunamadi. Kurulum durduruldu.
    pause
    exit /b 1
)

set "WORK_DIR=%TEMP%\KOAFiloServisSFX_%RANDOM%%RANDOM%"
mkdir "%WORK_DIR%" >nul 2>&1
if errorlevel 1 (
    echo [HATA] Gecici klasor olusturulamadi: %WORK_DIR%
    pause
    exit /b 1
)

echo [1/3] Paket gecici klasore aciliyor...
pwsh -NoProfile -ExecutionPolicy Bypass -Command "Expand-Archive -LiteralPath '%SCRIPT_DIR%App.zip' -DestinationPath '%WORK_DIR%' -Force"
if errorlevel 1 (
    echo [HATA] Paket acilamadi.
    rmdir /s /q "%WORK_DIR%" >nul 2>&1
    pause
    exit /b 1
)

if not exist "%WORK_DIR%\kur.bat" (
    echo [HATA] kur.bat bulunamadi. Paket icerigi gecersiz.
    rmdir /s /q "%WORK_DIR%" >nul 2>&1
    pause
    exit /b 1
)

echo [2/3] Kurulum/guncelleme komutu calistiriliyor...
pushd "%WORK_DIR%"
call "%WORK_DIR%\kur.bat" "" "" "" "%MODE%"
set "RC=%ERRORLEVEL%"
popd

echo [3/3] Gecici dosyalar temizleniyor...
rmdir /s /q "%WORK_DIR%" >nul 2>&1

if not "%RC%"=="0" (
    echo.
    echo [HATA] Kurulum/guncelleme basarisiz oldu. Hata kodu: %RC%
    pause
    exit /b %RC%
)

echo.
echo [OK] Kurulum/guncelleme basariyla tamamlandi.
pause
exit /b 0
