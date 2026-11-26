@echo off
REM ────────────────────────────────────────────────────────────────
REM  رفعِ خطایِ NU1100 (Package Source Mapping) — ویندوز
REM  رویِ همین فایل دوبار کلیک کن یا از ریشهٔ Solution اجرا کن:
REM      tools\fix-nuget.cmd
REM ────────────────────────────────────────────────────────────────
setlocal
chcp 65001 >nul
set "SCRIPT=%~dp0fix-nuget.ps1"

where pwsh >nul 2>nul
if %ERRORLEVEL%==0 (
    pwsh -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" %*
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" %*
)

echo.
pause
endlocal
