:: Script to disable Bitlocker on a specified drive. If no drive is specified, defaults to the system drive.
:: Exit codes:
:: 0 - Bitlocker successfully disabled.
:: 1 - Failed to start decryption.
:: 2 - Bitlocker decryption interrupted or failed unexpectedly.
@echo off
setlocal enabledelayedexpansion
set "DRV=%~1"
if not defined DRV set "DRV=%SystemDrive%"
echo --------------------------------------------
echo Step 3: Disable Bitlocker Script
echo --------------------------------------------

:: Step 1: Disable Bitlocker on the specified drive.
manage-bde -status %DRV% | find "Fully Decrypted" >nul && (echo %DRV% is fully decrypted. & exit /b 0)
echo Starting Bitlocker disable process...
manage-bde -off %DRV% || (echo Failed to start decryption on %DRV%. & exit /b 1)
echo.

:: Loop to monitor the decryption progress.
:loop
manage-bde -status %DRV% | find "Fully Decrypted" >nul && goto done
for /f "tokens=2 delims=:" %%A in ('manage-bde -status %DRV% ^| find "Percentage Encrypted"') do set "PCT=%%A"
echo [%time:~0,8%] Decrypting %DRV% - !PCT: =! still encrypted
timeout /t 10 /nobreak >nul
goto loop

:: Step 2: Conclude the Bitlocker disable process.
:done
echo Bitlocker is fully disabled on %DRV%.
exit /b 0