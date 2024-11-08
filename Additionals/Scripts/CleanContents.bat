@ECHO off

SET APPDATAFOLDER=%LOCALAPPDATA%\SportDJ

ECHO.
ECHO Clean app data folder %APPDATAFOLDER%
ECHO.

del /q "%APPDATAFOLDER%\*.*"

pushd %~dp0..\..

ECHO.
ECHO Clean contents in %cd%
ECHO.

FOR /F "tokens=*" %%G IN ('DIR /B /AD /S bin') DO RMDIR /S /Q "%%G"
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S obj') DO RMDIR /S /Q "%%G"

for /f "usebackq delims=" %%d in (`"dir /ad/b/s | sort /R"`) do rd "%%d"

popd