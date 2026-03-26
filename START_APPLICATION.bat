@echo off
echo =====================================
echo AdminMembers Quick Start
echo =====================================
echo.

REM Run the PowerShell deployment script
powershell -ExecutionPolicy Bypass -File "%~dp0deploy-and-run.ps1"

pause
