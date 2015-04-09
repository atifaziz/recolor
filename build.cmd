@echo off
pushd "%~dp0"
call :main %*
popd
goto :EOF

:main
setlocal
set MSBUILD=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe
if not exist "%MSBUILD%" (
    echo The .NET Framework 4.0 does not appear to be installed on this 
    echo machine, which is required to build the solution.
    exit /b 1
)
"%MSBUILD%" /p:Configuration=Debug   /v:m %* && ^
"%MSBUILD%" /p:Configuration=Release /v:m %*
goto :EOF
