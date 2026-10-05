@echo off
setlocal
set "MP=%ProgramFiles%\Windows Defender\MpCmdRun.exe"
set "TYPE=%~1"
if not defined TYPE set "TYPE=1"
echo --------------------------------------------
echo Step 2: Antivirus Scan Script
echo --------------------------------------------

if not exist "%MP%" (echo Windows Defender not found. & exit /b 1)

echo Updating virus definitions...
"%MP%" -SignatureUpdate >nul || echo Warning: update failed, using existing definitions.

echo Starting antivirus scan...
"%MP%" -Scan -ScanType %TYPE%
set "RC=%errorlevel%"

if "%RC%"=="0" (echo Antivirus scan completed. No unresolved threats. & exit /b 0)
if "%RC%"=="2" (echo Threats found that need manual action. & exit /b 2)
echo Scan failed with error code %RC%.
exit /b 3