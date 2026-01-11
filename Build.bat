@echo off
setlocal
cd /d "%~dp0"

set APP_PROJECT=.\App\App.csproj
set CONFIGURATION=Release
set RUNTIME_ID=win-x64
set FRAMEWORK=net8.0-windows
set OUTPUT_DIR=_Publish
set CLEANUP_SCRIPT_PATH=.\Additionals\Scripts\CleanContents.bat

echo.
echo ###########################
echo # Starting Build Process  #
echo ###########################
echo.

echo [1/3] Executing cleanup script: %CLEANUP_SCRIPT_PATH%

if exist %CLEANUP_SCRIPT_PATH% (
    pushd .\Additionals\Scripts
    call CleanContents.bat
    popd
    echo Cleanup completed successfully.
) else (
    echo WARNING: Cleanup script not found at %CLEANUP_SCRIPT_PATH%. Skipping cleanup.
)

echo.
echo --------------------------------------
echo.

echo [2/3] Cleaning Output Directory: %OUTPUT_DIR%...

if exist "%OUTPUT_DIR%" (
    rd /s /q "%OUTPUT_DIR%"
    echo Old output directory deleted.
)

echo.
echo --------------------------------------
echo.

echo [3/3] Publishing %APP_PROJECT% in %CONFIGURATION% config for %RUNTIME_ID%

dotnet publish %APP_PROJECT% ^
    -p:DebugType=None ^
    -p:DebugSymbols=false ^
    -p:PublishReadyToRun=true ^
    -p:PublishSingleFile=false ^
    --self-contained ^
    --configuration %CONFIGURATION% ^
    --framework %FRAMEWORK% ^
    --runtime %RUNTIME_ID% ^
    --output %OUTPUT_DIR%
	
REM dotnet publish %APP_PROJECT% ^
    REM -p:DebugType=None ^
    REM -p:DebugSymbols=false ^
    REM -p:IncludeNativeLibrariesForSelfExtract=true ^
    REM -p:PublishReadyToRun=true ^
    REM -p:PublishSingleFile=true ^
    REM --self-contained ^
    REM --configuration %CONFIGURATION% ^
	REM --framework %FRAMEWORK% ^
    REM --runtime %RUNTIME_ID% ^
    REM --output %OUTPUT_DIR%

:: Check the exit code of the last command (dotnet publish)
if %errorlevel% neq 0 (
    echo.
    echo ERROR: The dotnet publish command failed!
    echo.
    goto :end
)

echo.
echo ######################################
echo # Build Process finished successfully! #
echo # Output is located in: %OUTPUT_DIR% #
echo ######################################

:end
pause
endlocal