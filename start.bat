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

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\start.ps1" %*
set "RESULT=%ERRORLEVEL%"
pause
exit /b %RESULT%
