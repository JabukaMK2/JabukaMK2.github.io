@echo off
setlocal
set "P95DIR=%~dp0tools\prime95"
set "MIN=%~1"
if not defined MIN set "MIN=10"
set /a MIN=MIN
echo --------------------------------------------
echo Step 1: Hardware Test Script
echo --------------------------------------------

echo Checking disk health...
where powershell >nul 2>&1 || (echo PowerShell not found. & exit /b 1)
powershell -NoProfile -ExecutionPolicy Bypass -Command "$d = @(Get-PhysicalDisk | Where-Object BusType -ne 'USB'); if ($d.Count -eq 0) { exit 3 }; $d | Format-Table -AutoSize FriendlyName,MediaType,BusType,HealthStatus,OperationalStatus | Out-String | Write-Host; if ($d | Where-Object HealthStatus -ne 'Healthy') { exit 2 }; exit 0"
set "RC=%errorlevel%"
if "%RC%"=="2" (echo One or more disks are not healthy. & exit /b 2)
if not "%RC%"=="0" (echo Disk health check failed with error code %RC%. & exit /b 3)
echo Disks are healthy.
echo.

if %MIN% leq 0 (echo Stress test skipped. & exit /b 0)
if not exist "%P95DIR%\prime95.exe" (echo Prime95 not found in %P95DIR%. & exit /b 1)

findstr /x "StressTester=1" "%P95DIR%\prime.txt" >nul 2>&1 || >>"%P95DIR%\prime.txt" echo StressTester=1
findstr /x "UsePrimenet=0" "%P95DIR%\prime.txt" >nul 2>&1 || >>"%P95DIR%\prime.txt" echo UsePrimenet=0
del "%P95DIR%\results.txt" 2>nul

echo Starting %MIN% minute stress test...
start "" /min /d "%P95DIR%" "%P95DIR%\prime95.exe" -t

set /a LEFT=MIN
:wait
echo [%time:~0,8%] Stress test running - %LEFT% min left
rem ping instead of timeout: timeout fails when stdin is redirected
ping -n 61 127.0.0.1 >nul
tasklist /fi "imagename eq prime95.exe" | find /i "prime95.exe" >nul || (echo Prime95 exited unexpectedly. & exit /b 3)
findstr /i /c:"FATAL ERROR" /c:"Hardware failure" /c:"TORTURE TEST FAILED" "%P95DIR%\results.txt" >nul 2>&1 && goto failed
set /a LEFT-=1
if %LEFT% gtr 0 goto wait

taskkill /im prime95.exe /f >nul 2>&1
echo Stress test passed.
exit /b 0

:failed
taskkill /im prime95.exe /f >nul 2>&1
echo Stress test found errors:
findstr /i /c:"FATAL ERROR" /c:"Hardware failure" /c:"TORTURE TEST FAILED" "%P95DIR%\results.txt"
exit /b 2
