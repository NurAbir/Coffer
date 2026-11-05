@echo off
setlocal
cd /d "%~dp0"

echo ==========================================
echo   Coffer - Portable build
echo ==========================================
echo.

where dotnet >nul 2>&1
if errorlevel 1 (
    echo ERROR: dotnet was not found.
    echo Install the .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8
    echo.
    pause
    exit /b 1
)

echo Building the single-file portable EXE...
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0publish.ps1" -SkipInstaller
if errorlevel 1 (
    echo.
    echo The portable build failed. See the messages above.
    echo.
    pause
    exit /b 1
)

if not exist "%~dp0dist\Coffer-Portable.exe" (
    echo.
    echo ERROR: dist\Coffer-Portable.exe was not created.
    echo.
    pause
    exit /b 1
)

echo.
echo Done. Portable build is here:
echo   %~dp0dist\Coffer-Portable.exe
echo.
pause
endlocal