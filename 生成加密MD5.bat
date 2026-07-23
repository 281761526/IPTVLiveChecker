@echo off
chcp 65001 >nul
cd /d "%~dp0"
setlocal enabledelayedexpansion

echo ==================================================
echo         IPTVLiveChecker - 生成加密 MD5
echo ==================================================
echo.

set "TOOL_PATH=GenerateEncryptedMd5\bin\x64\Release\net472\GenerateEncryptedMd5.exe"

if not exist "%TOOL_PATH%" (
    echo [信息] 正在编译加密工具...
    where dotnet >nul 2>&1
    if %errorlevel% neq 0 (
        echo [错误] 未找到 dotnet 命令，请安装 .NET SDK 并添加到 PATH
        echo.
        pause
        exit /b 1
    )
    dotnet build GenerateEncryptedMd5\GenerateEncryptedMd5.csproj -c Release -p:Platform=x64
    if %errorlevel% neq 0 (
        echo [错误] 编译加密工具失败
        echo.
        pause
        exit /b 1
    )
    echo.
)

set "FOUND=0"

rem 依次处理每个存在的 EXE，分别生成加密 MD5 并更新同目录 config
for %%P in ("bin\x64\Release\IPTVLiveChecker.exe" "bin\Release\IPTVLiveChecker.exe" "release\IPTVLiveChecker.exe" "publish\IPTVLiveChecker.exe") do (
    if exist "%%~P" (
        echo [信息] 处理: %%~P
        "%TOOL_PATH%" "%%~P" "%cd%" < nul
        echo.
        set "FOUND=1"
    )
)

if "%FOUND%"=="0" (
    echo [错误] 未找到任何 IPTVLiveChecker.exe
    echo.
    echo 请先编译 Release 版本:
    echo   dotnet build IPTVLiveChecker.csproj -c Release -p:Platform=x64
    echo.
    pause
    exit /b 1
)

echo ==================================================
echo 所有 EXE 的加密 MD5 已生成并更新对应 config
echo 注意: App.config 会被最后一次处理的 EXE 覆盖
echo ==================================================
pause
