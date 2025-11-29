@echo off
setlocal

:: Define variables
set APP_PROJECT=App
set CONFIGURATION=Release
set RUNTIME_ID=win-x64
set OUTPUT_DIR=_Publish
set CLEANUP_SCRIPT_PATH=.\Additionals\Scripts\CleanContents.bat

echo.
echo ###########################
echo # Starting Build Process  #
echo ###########################
echo.

:: 1. Clean up previous build artifacts
echo [1/2] Executing cleanup script...
echo Target: %CLEANUP_SCRIPT_PATH%

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

:: 2. Build the self-contained application using ReadyToRun
echo [2/2] Publishing %APP_PROJECT% for %RUNTIME_ID%...
echo Configuration: %CONFIGURATION%
echo Output Directory: %OUTPUT_DIR%\

dotnet publish %APP_PROJECT% ^
    -p:DebugType=None ^
    -p:DebugSymbols=false ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:PublishReadyToRun=true ^
    -p:PublishSingleFile=true ^
    --self-contained ^
    --configuration %CONFIGURATION% ^
    --runtime %RUNTIME_ID% ^
    --output %OUTPUT_DIR%

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