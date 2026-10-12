:: Hardware Test Script
:: Performs a hardware stress test using Prime95 and checks disk health.
:: Echo set to off to avoid displaying commands in the console.
@echo off
setlocal

:: Set the directory for Prime95 and the duration of the stress test.
set "P95DIR=%~dp0..\tools\prime95"
set "MIN=%~1"

:: Check if the duration of the stress test is defined, if not set a default value.
if not defined MIN set "MIN=10"
set /a MIN=MIN

:: Display the header for the hardware test script.
echo --------------------------------------------
echo Step 1: Hardware Test Script
echo --------------------------------------------

:: Step 1: Check disk health.
:: Exit codes for disk health check:
:: 0 - All disks are healthy.
:: 2 - One or more disks are not healthy.
:: 3 - Disk health check failed due to an error.
echo Checking disk health...
where powershell >nul 2>&1 || (echo PowerShell not found. & exit /b 1)
powershell -NoProfile -ExecutionPolicy Bypass -Command "$d = @(Get-PhysicalDisk | Where-Object BusType -ne 'USB'); if ($d.Count -eq 0) { exit 3 }; $d | Format-Table -AutoSize FriendlyName,MediaType,BusType,HealthStatus,OperationalStatus | Out-String | Write-Host; if ($d | Where-Object HealthStatus -ne 'Healthy') { exit 2 }; exit 0"
set "RC=%errorlevel%"
if "%RC%"=="2" (echo One or more disks are not healthy. & exit /b 2)
if not "%RC%"=="0" (echo Disk health check failed with error code %RC%. & exit /b 3)
echo Disks are healthy.
echo.

:: Step 2: Run the hardware stress test using Prime95.
:: Exit codes for the stress test:
:: 0 - Stress test passed.
:: 2 - Stress test found errors.
:: 3 - Stress test failed due to an unexpected error.
if %MIN% leq 0 (echo Stress test skipped. & exit /b 0)
if not exist "%P95DIR%\prime95.exe" (echo Prime95 not found in %P95DIR%. & exit /b 1)

:: Configure Prime95 settings for the stress test.
findstr /x "StressTester=1" "%P95DIR%\prime.txt" >nul 2>&1 || >>"%P95DIR%\prime.txt" echo StressTester=1
findstr /x "UsePrimenet=0" "%P95DIR%\prime.txt" >nul 2>&1 || >>"%P95DIR%\prime.txt" echo UsePrimenet=0
del "%P95DIR%\results.txt" 2>nul

:: Start the stress test and monitor its progress.
echo Starting %MIN% minute stress test...
start "" /min /d "%P95DIR%" "%P95DIR%\prime95.exe" -t

:: Initialize the remaining time for the stress test.
set /a LEFT=MIN
:wait
echo [%time:~0,8%] Stress test running - %LEFT% min left
rem ping instead of timeout: timeout fails when stdin is redirected
ping -n 61 127.0.0.1 >nul
tasklist /fi "imagename eq prime95.exe" | find /i "prime95.exe" >nul || (echo Prime95 exited unexpectedly. & exit /b 3)
findstr /i /c:"FATAL ERROR" /c:"Hardware failure" /c:"TORTURE TEST FAILED" "%P95DIR%\results.txt" >nul 2>&1 && goto failed
set /a LEFT-=1
if %LEFT% gtr 0 goto wait

:: Step 3: Conclude the stress test and report results.
:: Exit codes for concluding the stress test:
:: 0 - Stress test passed.
:: 2 - Stress test found errors.
:: 3 - Stress test failed due to an unexpected error.
taskkill /im prime95.exe /f >nul 2>&1
echo Stress test passed.
exit /b 0

:: Step 4: Handle stress test failure.
:: Exit codes for handling stress test failure:
:: 2 - Stress test found errors.
:: 3 - Stress test failed due to an unexpected error.
:failed
taskkill /im prime95.exe /f >nul 2>&1
echo Stress test found errors:
findstr /i /c:"FATAL ERROR" /c:"Hardware failure" /c:"TORTURE TEST FAILED" "%P95DIR%\results.txt"
exit /b 2
