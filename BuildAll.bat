@echo off
setlocal

rem ================================================================
rem BuildAll.bat - full Release build + self test + artifact staging.
rem Run from anywhere; all paths resolve relative to this script.
rem Exit code: 0 = success, 1 = failure (CI friendly, no pause).
rem ================================================================

rem This machine has a polluted "Platform" environment variable that
rem breaks MSBuild property resolution. Clear it and always pass
rem /p:Platform explicitly.
set Platform=

set "ROOT=%~dp0"

set "SOLUTION=%ROOT%03. Src\TextCompare\TextCompare.sln"
set "CONFIG=Release"
set "PLATFORM=Any CPU"
set "PLATFORM_DIR=AnyCPU"
set "SELFTEST=%ROOT%build\%PLATFORM_DIR%%CONFIG%\TextCompare.SelfTest.exe"

echo *****************************
echo ** Locate MSBuild (vswhere)
echo *****************************
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%VSWHERE%" (
    echo ERROR: vswhere.exe not found at "%VSWHERE%"
    goto Failure
)
set "MSBUILD="
for /f "usebackq delims=" %%i in (`"%VSWHERE%" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do set "MSBUILD=%%i"
if not defined MSBUILD (
    echo ERROR: MSBuild.exe not found by vswhere
    goto Failure
)
echo Using MSBuild: "%MSBUILD%"

echo *****************************
echo ** Compile and Link - %PLATFORM_DIR%%CONFIG%
echo *****************************
"%MSBUILD%" "%SOLUTION%" /m /t:Rebuild /p:Configuration=%CONFIG% /p:Platform="%PLATFORM%"
if errorlevel 1 goto Failure

echo *****************************
echo ** Self Test
echo *****************************
if not exist "%SELFTEST%" (
    echo ERROR: self test binary not found: "%SELFTEST%"
    goto Failure
)
"%SELFTEST%"
if errorlevel 1 goto Failure

echo *****************************
echo ** Git history
echo *****************************
if not exist "%ROOT%artifact" mkdir "%ROOT%artifact"
if exist "%ROOT%artifact\history.TextCompare.txt" del /f /q "%ROOT%artifact\history.TextCompare.txt"
git -C "%ROOT%." log > "%ROOT%artifact\history.TextCompare.txt"

echo *****************************
echo ** Successfully finished.
echo *****************************
exit /b 0

:Failure
echo *****************************
echo ** Build failed.
echo *****************************
exit /b 1
