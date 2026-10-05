@echo off
setlocal enabledelayedexpansion
set "DRV=%~1"
if not defined DRV set "DRV=%SystemDrive%"
echo --------------------------------------------
echo Step 3: Disable Bitlocker Script
echo --------------------------------------------

manage-bde -status %DRV% | find "Fully Decrypted" >nul && (echo %DRV% is fully decrypted. & exit /b 0)
echo Starting Bitlocker disable process...
manage-bde -off %DRV% || (echo Failed to start decryption on %DRV%. & exit /b 1)
echo.

:loop
manage-bde -status %DRV% | find "Fully Decrypted" >nul && goto done
for /f "tokens=2 delims=:" %%A in ('manage-bde -status %DRV% ^| find "Percentage Encrypted"') do set "PCT=%%A"
echo [%time:~0,8%] Decrypting %DRV% - !PCT: =! still encrypted
timeout /t 10 /nobreak >nul
goto loop

:done
echo Bitlocker is fully disabled on %DRV%.
exit /b 0