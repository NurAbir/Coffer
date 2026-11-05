@echo off
setlocal
cd /d "%~dp0"

echo ==========================================
echo   Coffer - Installer build
echo ==========================================
echo.
echo This builds the portable EXE, then wraps it
echo in a Windows installer using Inno Setup 6.
echo.

where dotnet >nul 2>&1
if errorlevel 1 (
    echo ERROR: dotnet was not found.
    echo Install the .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8
    echo.
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0publish.ps1"
if errorlevel 1 (
    echo.
    echo The build failed. See the messages above.
    echo.
    pause
    exit /b 1
)

if not exist "%~dp0dist\Coffer-Setup.exe" (
    echo.
    echo The portable EXE was built, but the installer was NOT created.
    echo Install Inno Setup 6, then run this script again:
    echo   https://jrsoftware.org/isdl.php
    echo.
    pause
    exit /b 1
)

echo.
echo Done. Installer build is here:
echo   %~dp0dist\Coffer-Setup.exe
echo.
echo The portable EXE is also in dist\Coffer-Portable.exe
echo.
pause
endlocal