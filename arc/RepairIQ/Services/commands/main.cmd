:: Main script for the RepairIQ application. Executes a series of maintenance and diagnostic tasks.
:: Requires administrative privileges. Exits with the error code of the first failing task.
@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

:: Exit codes for the main script:
:: 0 - All tasks completed successfully.
:: 1 - Script requires administrative privileges.
:: 2 - One or more tasks failed.
:: 3 - Script failed due to an unexpected error.
net session >nul 2>&1 || (echo This script requires administrative privileges. & exit /b 1)
echo Administrative privileges confirmed.
echo.

:: Step 1: Verify administrative privileges.
for %%S in (
    hardware-test.cmd
    antivirus-scan.cmd
    repair-os.cmd
    disable-bitlocker.cmd
) do (
    echo.
    echo === %%S ===
    call "%~dp0%%S"
    set "RC=!errorlevel!"
    if not "!RC!"=="0" (
        echo *** %%S failed ^(code !RC!^). Stopping. ***
        exit /b !RC!
    )
)

:: Step 2: Execute maintenance and diagnostic tasks.
echo.
shutdown /r /t 5 /c "All tasks completed successfully. Restarting system..."