@echo off
echo --------------------------------------------
echo Step 4: Operating System Repair Script
echo --------------------------------------------
echo Starting OS repair process...
echo.

echo Deleting temporary files...
del /q /f /s "%TEMP%\*" 2>nul
echo Done!
echo.

echo DISM cleanup and repair process starting...
dism /online /cleanup-image /startcomponentcleanup
dism /online /cleanup-image /restorehealth
echo Done!
echo.

echo System file check and repair process starting...
sfc /scannow
echo Done!
echo.

echo Scheduling disk check on next restart...
echo Y | chkdsk %SystemDrive% /f >nul
exit /b 0
