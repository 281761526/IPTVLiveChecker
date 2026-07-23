@echo off
setlocal enabledelayedexpansion
for /f "delims=" %%f in ('git ls-files') do (
    echo %%f | findstr /r "\.exe$ \.dll$ \.pdb$ \.zip$" >nul && goto :skip
    echo %%f | findstr "^publish/" >nul && goto :skip
    for %%d in ("srcpkg\%%~dpf") do if not exist "%%d" mkdir "%%d"
    copy /y "%%f" "srcpkg\%%f" >nul 2>&1
    :skip
)
echo Done copying
