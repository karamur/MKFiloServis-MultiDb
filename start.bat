@echo off
REM ============================================
REM Koa Filo Servis - Hızlı Başlatma
REM ============================================

echo.
echo  ========================================
echo   Koa Filo Servis Baslatiliyor...
echo  ========================================
echo.

REM Uygulama dizinine git
cd /d "%~dp0"

REM .NET Runtime kontrolü
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo HATA: .NET Runtime bulunamadi!
    echo Lutfen .NET 10 Runtime yukleyin.
    pause
    exit /b 1
)

REM Uygulamayı başlat
echo Uygulama baslatiliyor...
echo Web Adresi: http://localhost:5050
echo.
start http://localhost:5050
dotnet MKFiloServis.Web.dll --urls http://localhost:5050

pause
