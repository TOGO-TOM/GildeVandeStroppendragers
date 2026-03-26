@echo off
REM AdminMembers Application Launcher
REM This script starts the application and opens the web page in your browser

color 0A
title AdminMembers Application Launcher

echo.
echo ========================================
echo   AdminMembers Application Launcher
echo ========================================
echo.
echo Starting the application...
echo.

REM Change to the directory where this script is located
cd /d "%~dp0"

REM Start the application in a new window
start "AdminMembers Server" cmd /k "echo ====================================== & echo AdminMembers Server Running & echo ====================================== & echo. & echo Press Ctrl+C to stop the server & echo. & dotnet run"

REM Wait for the server to start (adjust time if needed)
echo Waiting for server to start...
timeout /t 8 /nobreak >nul

REM Open the default browser to the application
echo Opening application in browser...
start https://localhost:7223/index.html

REM Alternative URLs (uncomment if the first one doesn't work)
REM start http://localhost:5000/index.html

echo.
echo ========================================
echo   Application Launched Successfully!
echo ========================================
echo.
echo The application should open in your browser.
echo If not, manually navigate to:
echo   https://localhost:7223/index.html
echo   or
echo   http://localhost:5000/index.html
echo.
echo To stop the application:
echo   - Close the "AdminMembers Server" window
echo   - Or press Ctrl+C in that window
echo.
echo This window will close in 5 seconds...
timeout /t 5 >nul

exit
