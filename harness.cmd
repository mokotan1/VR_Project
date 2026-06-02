@echo off
setlocal
set "CENTRAL=C:\Users\user\Documents\Codex\project-harness\harness.cmd"

if "%~1"=="" (
    call "%CENTRAL%" help
    exit /b %ERRORLEVEL%
)

if /I "%~1"=="help" goto passthrough
if /I "%~1"=="list" goto passthrough
if not "%~2"=="" goto passthrough

call "%CENTRAL%" %1 VR_Project
exit /b %ERRORLEVEL%

:passthrough
call "%CENTRAL%" %*
exit /b %ERRORLEVEL%
