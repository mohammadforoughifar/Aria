@echo off
REM ────────────────────────────────────────────────────────────────────────
REM  اجرایِ سادهٔ آریا روی ویندوز — همه‌چیز رویِ یک پورت (۵۱۸۲)
REM
REM  چرا این‌طور؟ در این حالت فقط یک پروسه لازم است: API همان چیزی را که در
REM  پوشهٔ publish ساخته شده سرو می‌کند. نه به پنجرهٔ دوم نیاز است، نه به CORS،
REM  نه به انتخابِ نشانیِ API. کافی است این فایل را اجرا کنید و صبر کنید.
REM
REM  اگر برایِ کارِ توسعه می‌خواهید کلاینت را جدا (با hot reload) اجرا کنید،
REM  به‌جایِ این از اجرای-همه.bat استفاده کنید.
REM ────────────────────────────────────────────────────────────────────────
chcp 65001 >nul
setlocal
cd /d "%~dp0"
set "PORT=5182"

echo.
echo  [۱/۲] ساختِ نسخهٔ نهاییِ کلاینت…
dotnet publish "Client\Northwind.Client" -c Release -o "%~dp0.publish\client" --nologo -v q
if errorlevel 1 goto :fail

echo  [۲/۲] اجرا روی http://localhost:%PORT%
echo        (بارِ اول چند ثانیه طول می‌کشد؛ مرورگر خودش باز می‌شود)
set "ASPNETCORE_ENVIRONMENT=Development"
set "ASPNETCORE_WEBROOT=%~dp0.publish\client\wwwroot"

start "" "http://localhost:%PORT%"
dotnet run --project "Server\Northwind.Api" --urls "http://localhost:%PORT%"
goto :eof

:fail
echo.
echo  ✗ ساختِ کلاینت شکست خورد. پیامِ بالا را بخوانید.
pause
