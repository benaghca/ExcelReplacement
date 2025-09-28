@echo off
echo Starting Excel Replacement Application...

:: Compile TypeScript
echo Compiling TypeScript...
call npm run compile:ts

:: Check if TypeScript compilation was successful
if %errorlevel% neq 0 (
    echo TypeScript compilation failed.
    exit /b %errorlevel%
)

echo TypeScript compilation successful.

:: Start the C# backend
start /B dotnet run --project ExcelReplacement.csproj

:: Wait a moment for the backend to start
timeout /t 2 /nobreak > nul

:: Start the Electron frontend
start /B npm start

echo Application started! 