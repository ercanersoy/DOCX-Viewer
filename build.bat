@echo off
rem Copyright (c) 2024-2026 Ercan Ersoy
rem Written with Claude Code Opus 5.5.
rem Usage: build.bat [Release|Debug]   (default: Release)

setlocal EnableExtensions
cd /d "%~dp0"

set "CONFIG=%~1"
if "%CONFIG%"=="" set "CONFIG=Release"

rem --- Find MSBuild (shipped with .NET Framework 4.x) ---
set "MSBUILD=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
if not exist "%MSBUILD%" set "MSBUILD=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
if not exist "%MSBUILD%" (
    echo ERROR: .NET Framework 4.x MSBuild not found.
    exit /b 1
)
echo MSBuild: %MSBUILD%

rem --- Download NuGet packages (Framework MSBuild cannot restore packages) ---
call :restore DocumentFormat.OpenXml 3.0.2 || exit /b 1
call :restore DocumentFormat.OpenXml.Framework 3.0.2 || exit /b 1

rem --- Build ---
rem Without the 4.0 targeting pack, references resolve from the GAC; the
rem resulting architecture mismatch warning (MSB3270) is harmless and suppressed.
"%MSBUILD%" "DOCX Viewer\DOCX Viewer.csproj" /nologo /v:minimal /tv:4.0 /t:Build /p:Configuration=%CONFIG% /p:ResolveAssemblyWarnOrErrorOnTargetArchitectureMismatch=None
if errorlevel 1 (
    echo.
    echo BUILD FAILED.
    exit /b 1
)

echo.
echo Build succeeded: "DOCX Viewer\bin\%CONFIG%\Viewer.exe"
exit /b 0

rem ---------------------------------------------------------------
rem :restore <package> <version>  -> extracts into packages\<package>.<version>\
:restore
set "PKGDIR=packages\%~1.%~2"
if exist "%PKGDIR%\lib\net40\%~1.dll" exit /b 0
echo Downloading package: %~1 %~2
if not exist "%PKGDIR%" mkdir "%PKGDIR%"
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='Stop';" ^
  "[Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12;" ^
  "$zip=Join-Path $env:TEMP '%~1.%~2.zip';" ^
  "(New-Object Net.WebClient).DownloadFile('https://www.nuget.org/api/v2/package/%~1/%~2',$zip);" ^
  "Add-Type -AssemblyName System.IO.Compression.FileSystem;" ^
  "[IO.Compression.ZipFile]::ExtractToDirectory($zip,(Resolve-Path '%PKGDIR%').Path);" ^
  "Remove-Item $zip"
if not exist "%PKGDIR%\lib\net40\%~1.dll" (
    echo ERROR: Failed to download or extract package %~1.
    exit /b 1
)
exit /b 0
