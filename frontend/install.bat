@echo off
echo Installing Motor Valley Dashboard dependencies...
call npm install
if %ERRORLEVEL% EQU 0 (
    echo.
    echo Done. Run "npm run dev" to start the dashboard.
) else (
    echo.
    echo Error: npm not found. Install Node.js from https://nodejs.org and try again.
    pause
)
