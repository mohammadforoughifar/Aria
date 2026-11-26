@echo off
chcp 65001 >nul
title فروغ آریا پارسیان — اجرای سرور و رابط
cd /d "%~dp0"

echo.
echo  ساده‌ترین راه: فقط start-windows.cmd را اجرا کنید (یک پورت، یک پروسه).
echo.
echo  این فایل حالتِ توسعه را اجرا می‌کند: دو پنجره در دو پورت.
echo    API    : http://localhost:5182   (خودش سایت را هم نشان می‌دهد)
echo    کلاینت : http://localhost:5212   (حالتِ توسعه، با بارگذاریِ مجدد)
echo.

start "API (5182)" cmd /k ""%~dp0run-api.cmd""
timeout /t 8 /nobreak >nul
start "Client (5212)" cmd /k ""%~dp0run-client.cmd""

echo  دو پنجره باز شد. کمی صبر کنید، سپس باز کنید:
echo      http://localhost:5212
echo.
pause
