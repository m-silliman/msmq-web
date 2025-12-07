@echo off
REM Setup script to download required NSIS plugins for the installer

echo ========================================
echo NSIS Plugin Setup for MSMQ Manager
echo ========================================
echo.

REM Check if NSIS is installed
set "NSIS_PATH="
if exist "%ProgramFiles(x86)%\NSIS\makensis.exe" (
    set "NSIS_PATH=%ProgramFiles(x86)%\NSIS"
) else if exist "%ProgramFiles%\NSIS\makensis.exe" (
    set "NSIS_PATH=%ProgramFiles%\NSIS"
) else (
    echo ERROR: NSIS not found. Please install NSIS first from https://nsis.sourceforge.io/
    echo.
    pause
    exit /b 1
)

echo Found NSIS at: %NSIS_PATH%

REM Check for ServiceLib plugin
set PLUGIN_PATH=%NSIS_PATH%\Plugins\x86-unicode
if not exist "%PLUGIN_PATH%\ServiceLib.dll" (
    echo.
    echo WARNING: ServiceLib plugin not found at:
    echo %PLUGIN_PATH%\ServiceLib.dll
    echo.
    echo Please download and install ServiceLib plugin:
    echo 1. Visit: https://nsis.sourceforge.io/ServiceLib_plug-in
    echo 2. Download ServiceLib.zip
    echo 3. Extract ServiceLib.dll to: %PLUGIN_PATH%\
    echo 4. Extract ServiceLib.nsh to: %NSIS_PATH%\Include\
    echo.
    echo Opening download page in browser...
    start https://nsis.sourceforge.io/ServiceLib_plug-in
    echo.
) else (
    echo ServiceLib plugin found: OK
)

REM Check for other required files
if not exist "%NSIS_PATH%\Include\ServiceLib.nsh" (
    echo WARNING: ServiceLib.nsh not found in Include directory
) else (
    echo ServiceLib.nsh found: OK
)

echo.
echo Setup check complete.
echo Run build-installer.ps1 after installing any missing plugins.
echo.
pause