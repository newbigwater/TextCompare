@echo off
setlocal

rem ================================================================
rem BuildInfo.bat - capture build artifact and version info.
rem Called from the TextCompare Release PostBuildEvent.
rem
rem Usage:
rem   BuildInfo.bat <Platform> <Config> <TargetDir> <TargetName> <TargetExt> <TargetFullPath>
rem   e.g. BuildInfo.bat AnyCPU Release ..\..\build\AnyCPURelease\ TextCompare .exe "C:\...\build\AnyCPURelease\TextCompare.exe"
rem ================================================================

set "PLATFORM=%~1"
set "CONFIG=%~2"
set "TARGETDIR=%~3"
set "TARGETNAME=%~4"
set "TARGETEXT=%~5"
set "TARGETPATH=%~6"

echo [BuildInfo] ------------------------------------------------
echo Platform         : %PLATFORM%
echo Configuration    : %CONFIG%
echo Target Dir       : %TARGETDIR%
echo Target Name      : %TARGETNAME%
echo Target Ext       : %TARGETEXT%
echo Target Full Path : %TARGETPATH%
echo ----------------------------------------------------------------

if "%TARGETPATH%"=="" (
    echo ERROR: missing arguments.
    echo Usage: BuildInfo.bat Platform Config TargetDir TargetName TargetExt TargetFullPath
    exit /b 1
)

if not exist "%TARGETPATH%" (
    echo ERROR: target binary not found: "%TARGETPATH%"
    exit /b 1
)

rem --- Read FileVersion of the built binary (wildcard AssemblyVersion result) ---
set "VERSION="
for /f "usebackq delims=" %%v in (`powershell -NoProfile -Command "(Get-Item '%TARGETPATH%').VersionInfo.FileVersion"`) do set "VERSION=%%v"
if not defined VERSION (
    echo ERROR: could not read FileVersion from "%TARGETPATH%"
    exit /b 1
)

rem --- Artifact directory: relative to this script (repo root), independent of caller cwd ---
set "ARTIFACTDIR=%~dp0artifact\%PLATFORM%%CONFIG%"
set "ARTIFACTINFO=%ARTIFACTDIR%\%TARGETNAME%.buildInfo.txt"

echo Target Version   : %VERSION%
echo Artifact Dir     : %ARTIFACTDIR%
echo Artifact Info    : %ARTIFACTINFO%

if not exist "%ARTIFACTDIR%" mkdir "%ARTIFACTDIR%"

rem --- Copy binary (required), app config and xml doc (optional) ---
copy /y "%TARGETPATH%" "%ARTIFACTDIR%\" >nul
if errorlevel 1 (
    echo ERROR: failed to copy "%TARGETPATH%" to "%ARTIFACTDIR%"
    exit /b 1
)
if exist "%TARGETDIR%%TARGETNAME%%TARGETEXT%.config" copy /y "%TARGETDIR%%TARGETNAME%%TARGETEXT%.config" "%ARTIFACTDIR%\" >nul
if exist "%TARGETDIR%%TARGETNAME%.xml" copy /y "%TARGETDIR%%TARGETNAME%.xml" "%ARTIFACTDIR%\" >nul

rem --- Write build info manifest ---
if exist "%ARTIFACTINFO%" del /f /q "%ARTIFACTINFO%"
(
    echo Date Time              : %DATE% %TIME%
    echo Artifact Name          : %TARGETNAME%%TARGETEXT%
    echo Artifact Version       : %VERSION%
    echo Artifact Platform      : %PLATFORM%
    echo Artifact Configuration : %CONFIG%
) > "%ARTIFACTINFO%"

echo [BuildInfo] %TARGETNAME%%TARGETEXT% %VERSION% -^> "%ARTIFACTDIR%"
exit /b 0
