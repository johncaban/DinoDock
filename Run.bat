@echo off
rem Builds and starts DinoDock. Double-click this file.
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo.
    echo The .NET SDK is not installed.
    echo Install the .NET 10 SDK from https://dotnet.microsoft.com/download/dotnet/10.0
    echo then double-click Run.bat again.
    echo.
    start "" "https://dotnet.microsoft.com/download/dotnet/10.0"
    pause
    exit /b 1
)

echo Building DinoDock (the first build downloads packages and takes a minute)...
dotnet build DinoDock\DinoDock.csproj -c Release -nologo -v quiet
if errorlevel 1 (
    echo.
    echo Build failed - see the messages above.
    pause
    exit /b 1
)

start "" "DinoDock\bin\Release\net10.0-windows\DinoDock.exe"
