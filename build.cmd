@echo off
pushd "%~dp0"
call :main %*
popd
goto :EOF

:main
setlocal
if "%PROCESSOR_ARCHITECTURE%"=="x86" set MSBUILD=%ProgramFiles%
if defined ProgramFiles(x86) set MSBUILD=%ProgramFiles(x86)%
set MSBUILD=%MSBUILD%\MSBuild\14.0\bin\msbuild
if not exist "%MSBUILD%" (
    echo Microsoft Build Tools 2015 does not appear to be installed on this
    echo machine, which is required to build the solution. You can install
    echo it from the URL below and then try building again:
    echo https://www.microsoft.com/en-us/download/details.aspx?id=48159
    exit /b 1
)
     "%MSBUILD%" /p:Configuration=Debug   /v:m %* ^
  && "%MSBUILD%" /p:Configuration=Release /v:m %*
goto :EOF
