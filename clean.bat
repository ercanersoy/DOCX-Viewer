@echo off
rem Copyright (c) 2024-2026 Ercan Ersoy
rem Written with Claude Code Opus 5.5.
rem Usage: clean.bat [all]
rem   (no argument) removes build output: bin, obj, .vs
rem   all           also removes the NuGet packages downloaded by build.bat

setlocal EnableExtensions
cd /d "%~dp0"

set "ALL="
if /i "%~1"=="all" set "ALL=1"
if not "%~1"=="" if not defined ALL (
    echo Usage: clean.bat [all]
    exit /b 1
)

call :remove "DOCX Viewer\bin"
call :remove "DOCX Viewer\obj"
call :remove ".vs"
if defined ALL call :remove "packages"

echo.
echo Clean finished.
exit /b 0

rem ---------------------------------------------------------------
rem :remove <directory>  -> deletes the directory if it exists
:remove
if not exist "%~1" exit /b 0
echo Removing: %~1
rmdir /s /q "%~1"
if exist "%~1" (
    echo WARNING: Could not remove "%~1". Is a file in use?
    exit /b 1
)
exit /b 0
