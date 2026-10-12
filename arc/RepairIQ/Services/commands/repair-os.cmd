:: Script to repair the operating system using built-in Windows tools.
:: Exit codes:
:: 0 - OS repair process completed successfully.
:: 1 - Failed to delete temporary files.
:: 2 - DISM cleanup or repair failed.
:: 3 - System file check failed.
:: 4 - Disk check scheduling failed.
@echo off
echo --------------------------------------------
echo Step 4: Operating System Repair Script
echo --------------------------------------------
echo Starting OS repair process...
echo.

:: Step 1: Delete temporary files.
echo Deleting temporary files...
echo Deleting temporary files...
del /q /f /s "%TEMP%\*" 2>nul || (echo Failed to delete temporary files. & exit /b 1)
echo Done!
echo.
    
:: Step 2: Perform DISM cleanup and repair.
echo DISM cleanup and repair process starting...
dism /online /cleanup-image /startcomponentcleanup || (echo DISM cleanup failed. & exit /b 2)
dism /online /cleanup-image /restorehealth || (echo DISM repair failed. & exit /b 2)
echo Done!
echo.

:: Step 3: Perform system file check and repair.
echo System file check and repair process starting...
sfc /scannow || (echo System file check failed. & exit /b 3)
echo Done!
echo.

:: Step 4: Schedule disk check on next restart.
echo Scheduling disk check on next restart...
echo Y | chkdsk %SystemDrive% /f >nul || (echo Disk check scheduling failed. & exit /b 4)
exit /b 0
