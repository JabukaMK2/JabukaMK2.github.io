@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

net session >nul 2>&1 || (echo This script requires administrative privileges. & exit /b 1)
echo Administrative privileges confirmed.
echo.

for %%S in (
    hardware-test.cmd
    antivirus-scan.cmd
    disable-bitlocker.cmd
    repair-os.cmd
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

echo.
shutdown /r /t 5 /c "All tasks completed successfully. Restarting system..."