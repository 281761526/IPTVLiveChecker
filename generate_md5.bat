@echo off
cd /d "%~dp0"
setlocal

echo ==================================================
echo         IPTVLiveChecker - Generate Encrypted MD5
echo ==================================================
echo.

set "EXE_PATH=bin\x64\Release\IPTVLiveChecker.exe"
set "TOOL_PATH=GenerateEncryptedMd5\bin\x64\Release\net472\GenerateEncryptedMd5.exe"

if not exist "%EXE_PATH%" (
    echo ERROR: Target file not found - %EXE_PATH%
    echo.
    echo Please run Release build first:
    echo   msbuild IPTVLiveChecker.sln /p:Configuration=Release /p:Platform=x64
    echo.
    pause
    exit /b 1
)

if not exist "%TOOL_PATH%" (
    echo Building generate tool...
    where dotnet >nul 2>&1
    if %errorlevel% neq 0 (
        echo ERROR: dotnet not found in PATH
        echo Please install .NET SDK or add it to PATH
        echo.
        pause
        exit /b 1
    )
    dotnet build GenerateEncryptedMd5\GenerateEncryptedMd5.csproj -c Release -p:Platform=x64
    if %errorlevel% neq 0 (
        echo ERROR: Failed to build generate tool
        echo.
        pause
        exit /b 1
    )
    echo.
)

echo Calculating and encrypting MD5...
echo.
"%TOOL_PATH%" "%EXE_PATH%" "%cd%"

echo.
echo ==================================================
pause
