@echo off
setlocal
cd /d "%~dp0"

rem Usage: Build.bat [single] [nosetup] [nobump]
rem   (no argument)  self-contained, ReadyToRun publish as a folder (recommended)
rem   single         self-contained, ReadyToRun publish as a single file (slower start, native libs are extracted)
rem   nosetup        skip the Inno Setup step
rem   nobump         do not raise the minor version after a successful build
rem Requires the cstidy global tool for the cleanup step.
rem The setup step needs Inno Setup 6 (ISCC.exe on PATH or in the default install folder).

set "APP_PROJECT=App\App.csproj"
set "CONFIGURATION=Release"
set "RUNTIME_ID=win-x64"
set "OUTPUT_DIR=%CD%\App\bin\Publish"
set "SETUP_DIR=_Setup"
set "SETUP_SCRIPT=Additionals\Build\SportDJ.iss"
set "SINGLE_FILE_ARGS="
set "BUILD_SETUP=1"
set "NO_BUMP="
set "BUILD_COMPLETE="
set "VERSION_FILE=App\App.csproj"
set "BUMP_SCRIPT=Additionals\Build\BumpVersion.ps1"

for %%A in (%*) do (
    if /i "%%~A"=="single" set "SINGLE_FILE_ARGS=-p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true"
    if /i "%%~A"=="nosetup" set "BUILD_SETUP="
    if /i "%%~A"=="nobump" set "NO_BUMP=1"
)

where dotnet >nul 2>&1
if errorlevel 1 (
    echo ERROR: dotnet not found on PATH.
    set "EXIT_CODE=1"
    goto :end
)

if not exist "%APP_PROJECT%" (
    echo ERROR: %APP_PROJECT% not found.
    set "EXIT_CODE=1"
    goto :end
)

echo.
echo === SportDJ build ===
echo.

echo [1/4] Cleaning build and test artifacts
where cstidy >nul 2>&1
if errorlevel 1 (
    echo WARNING: cstidy not found on PATH, skipping cleanup.
) else (
    cstidy clean . --yes
    if errorlevel 1 echo WARNING: cleanup reported errors, continuing.
)

echo.
echo [2/4] Cleaning %OUTPUT_DIR% and %SETUP_DIR%
if exist "%OUTPUT_DIR%" rd /s /q "%OUTPUT_DIR%"
if exist "%SETUP_DIR%" rd /s /q "%SETUP_DIR%"

echo.
echo [3/4] Publishing %APP_PROJECT% (%CONFIGURATION%, %RUNTIME_ID%, ReadyToRun)
dotnet publish "%APP_PROJECT%" ^
    --configuration %CONFIGURATION% ^
    --runtime %RUNTIME_ID% ^
    --self-contained ^
    --output "%OUTPUT_DIR%" ^
    -p:PublishReadyToRun=true ^
    -p:DebugType=portable ^
    -p:DebugSymbols=true ^
    %SINGLE_FILE_ARGS%
set "EXIT_CODE=%errorlevel%"

if not "%EXIT_CODE%"=="0" (
    echo ERROR: dotnet publish failed with exit code %EXIT_CODE%.
    goto :end
)

echo.
echo [4/4] Building setup and portable setup
if not defined BUILD_SETUP (
    echo Skipped.
    set "BUILD_COMPLETE=1"
    goto :done
)

set "ISCC="
where iscc >nul 2>&1
if not errorlevel 1 set "ISCC=iscc"
if not defined ISCC if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not defined ISCC if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"

if not defined ISCC (
    echo WARNING: Inno Setup not found, skipping setup.
    goto :done
)

echo.
echo Installer
"%ISCC%" /Qp "/DPublishDir=%OUTPUT_DIR%" "%SETUP_SCRIPT%"
if errorlevel 1 (
    echo ERROR: installer build failed.
    set "EXIT_CODE=1"
    goto :end
)

echo.
echo Portable setup
"%ISCC%" /Qp /DPortable "/DPublishDir=%OUTPUT_DIR%" "%SETUP_SCRIPT%"
if errorlevel 1 (
    echo ERROR: portable setup build failed.
    set "EXIT_CODE=1"
    goto :end
)

goto :complete

:complete
set "BUILD_COMPLETE=1"

:done
rem The bump comes last, so a failed build never changes the version
rem The build above carries the old version, the next build gets the new one
if defined BUILD_COMPLETE if not defined NO_BUMP (
    echo.
    echo Raising minor version in %VERSION_FILE%
    powershell -NoProfile -ExecutionPolicy Bypass -File "%BUMP_SCRIPT%" -Path "%VERSION_FILE%"
    if errorlevel 1 echo WARNING: version bump failed.
)

echo.
echo Done. Output: %OUTPUT_DIR%
if not defined SINGLE_FILE_ARGS if defined BUILD_SETUP echo Setups: %SETUP_DIR%

:end
echo.
if not defined CI pause
endlocal & exit /b %EXIT_CODE%
