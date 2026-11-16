@echo off
setlocal
cd /d "%~dp0"

rem Usage: Build.bat [single]
rem   (no argument)  self-contained publish as a folder
rem   single         self-contained publish as a single file

set "APP_PROJECT=App\App.csproj"
set "CONFIGURATION=Release"
set "RUNTIME_ID=win-x64"
set "OUTPUT_DIR=_Publish"
set "CLEANUP_DIR=Additionals\Scripts"
set "CLEANUP_SCRIPT=CleanContents.bat"
set "SINGLE_FILE_ARGS="

if /i "%~1"=="single" set "SINGLE_FILE_ARGS=-p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true"

echo.
echo === SportDJ build ===
echo.

echo [1/3] Running cleanup script
if exist "%CLEANUP_DIR%\%CLEANUP_SCRIPT%" (
    pushd "%CLEANUP_DIR%"
    call "%CLEANUP_SCRIPT%"
    popd
) else (
    echo WARNING: %CLEANUP_DIR%\%CLEANUP_SCRIPT% not found, skipping.
)

echo.
echo [2/3] Cleaning %OUTPUT_DIR%
if exist "%OUTPUT_DIR%" rd /s /q "%OUTPUT_DIR%"

echo.
echo [3/3] Publishing %APP_PROJECT% (%CONFIGURATION%, %RUNTIME_ID%)
dotnet publish "%APP_PROJECT%" ^
    --configuration %CONFIGURATION% ^
    --runtime %RUNTIME_ID% ^
    --self-contained ^
    --output "%OUTPUT_DIR%" ^
    -p:DebugType=None ^
    -p:DebugSymbols=false ^
    %SINGLE_FILE_ARGS%
set "EXIT_CODE=%errorlevel%"

echo.
if not "%EXIT_CODE%"=="0" (
    echo ERROR: dotnet publish failed with exit code %EXIT_CODE%.
) else (
    echo Done. Output: %OUTPUT_DIR%
)

echo.
if not defined CI pause
endlocal & exit /b %EXIT_CODE%
