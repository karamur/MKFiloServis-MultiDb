@ECHO OFF
REM ============================================================
REM MKFiloServis 1.0.27 - Windows Installer (.exe) Builder
REM ============================================================
REM Bu batch dosyasi .exe installer olusturmak icin
REM gerekli adimlari otomatiklestirir.
REM ============================================================

cls
echo.
echo ╔══════════════════════════════════════════════════════════╗
echo ║   MKFiloServis 1.0.27 - Windows Installer Builder      ║
echo ╚══════════════════════════════════════════════════════════╝
echo.

REM Check for PowerShell
powershell -v >nul 2>&1
if %errorlevel% neq 0 (
  echo [HATA] PowerShell bulunamadi!
  pause
  exit /b 1
)

REM Get parameters
set METHOD=%1
if "%METHOD%"=="" set METHOD=auto

echo [INFO] Kurulum Metodu: %METHOD%
echo.

echo [1/3] Kurulum dosyalari hazirlaniyor...
cd /d "%~dp0"
cd ..

REM Publish dosyalarini hazirla
call pwsh -ExecutionPolicy Bypass -NoProfile -Command ^
  "try { & '.\setup\output\setup.ps1' -Configuration Release -Runtime win-x64 -Version '1.0.27' } catch { Write-Host 'HATA: $_'; exit 1 }"

if %errorlevel% neq 0 (
  echo [HATA] Setup hazirlama basarisiz
  pause
  exit /b 1
)

echo [OK] Kurulum dosyalari hazir
echo.
echo [2/3] Inno Setup kontrol ediliyor...

REM Check for Inno Setup
set INNO_SETUP=
if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" (
  set INNO_SETUP=C:\Program Files (x86)\Inno Setup 6\ISCC.exe
) else if exist "C:\Program Files\Inno Setup 6\ISCC.exe" (
  set INNO_SETUP=C:\Program Files\Inno Setup 6\ISCC.exe
)

if "%INNO_SETUP%"=="" (
  echo [UYARI] Inno Setup yüklenmiş değildir!
  echo.
  echo Inno Setup kurmak icin:
  echo  1. https://jrsoftware.org/isdl.php adresine gidin
  echo  2. "Inno Setup 6" indirin ve kurun
  echo  3. Bu scripti tekrar calistirin
  echo.
  pause
  exit /b 1
)

echo [OK] Inno Setup bulundu: %INNO_SETUP%
echo.
echo [3/3] Installer olusturuluyor...

REM Run Inno Setup
"%INNO_SETUP%" ".\setup\MKFiloServis-Setup.iss"

if %errorlevel% neq 0 (
  echo [HATA] Installer olusturma basarisiz
  pause
  exit /b 1
)

echo.
echo ╔══════════════════════════════════════════════════════════╗
echo ║           ✓ INSTALLER BASARILI SEKILDE OLUSTURULDU      ║
echo ╚══════════════════════════════════════════════════════════╝
echo.
echo Installer dosyasi:
echo   .\setup\output\v1.0.27\MKFiloServis-1.0.27-Setup.exe
echo.
echo Kurulum icin: MKFiloServis-1.0.27-Setup.exe
echo.
pause
