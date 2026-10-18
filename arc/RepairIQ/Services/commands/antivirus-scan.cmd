:: Script to perform an antivirus scan using Windows Defender.
:: Exit codes:
:: 0 - Scan completed successfully with no unresolved threats.
:: 1 - Windows Defender not found.
:: 2 - Threats found that need manual action.
:: 3 - Scan failed with an error.
:: 4 - Virus scan skipped.
@echo off
setlocal
set "MP=%ProgramFiles%\Windows Defender\MpCmdRun.exe"
set "TYPE=%~1"
if not defined TYPE set "TYPE=1"
echo --------------------------------------------
echo Step 2: Antivirus Scan Script
echo --------------------------------------------

:: Step 1: Verify that Windows Defender is available.
if "%TYPE%"=="skip" (echo Virus scan skipped. & exit /b 4)
if not exist "%MP%" (echo Windows Defender not found. & exit /b 1)

echo Updating virus definitions...
"%MP%" -SignatureUpdate >nul || echo Warning: update failed, using existing definitions.

:: Step 2: Perform the antivirus scan.
echo Starting antivirus scan...
"%MP%" -Scan -ScanType %TYPE%
set "RC=%errorlevel%"

:: Step 3: Handle the scan results.
if "%RC%"=="0" (echo Antivirus scan completed. No unresolved threats. & exit /b 0)
if "%RC%"=="2" (echo Threats found that need manual action. & exit /b 2)
echo Scan failed with error code %RC%.
exit /b 3