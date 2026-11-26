@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo  API روی http://localhost:5182  —  همین آدرس خودش سایت را هم نشان می‌دهد.
dotnet run --project "Server\Northwind.Api" --launch-profile http
pause
