@echo off
chcp 65001 >nul
title فروغ آریا پارسیان — اجرای سرور و رابط

echo.
echo  در حال اجرایِ دو پروژه در دو پنجرهٔ جداگانه:
echo    سرور  (API)  :  http://localhost:5182
echo    رابط (وب)    :  http://localhost:5212
echo.
echo  هر دو پنجره را باز بگذارید. برای توقف، در هر پنجره Ctrl+C بزنید.
echo.

start "سرورِ فروغ آریا پارسیان (5182)" cmd /k "dotnet run --project "%~dp0Server\Northwind.Server" --urls "http://localhost:5182""
timeout /t 6 /nobreak >nul
start "رابطِ فروغ آریا پارسیان (5212)" cmd /k "dotnet run --project "%~dp0Client\Northwind.Client""

echo  پنجره‌ها باز شدند. کمی صبر کنید تا سرور آماده شود،
echo  سپس در مرورگر این نشانی را باز کنید:
echo      http://localhost:5212
echo.
pause
