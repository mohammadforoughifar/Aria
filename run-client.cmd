@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo  کلاینت (حالتِ توسعه) روی http://localhost:5212  —  API باید رویِ ۵۱۸۲ باشد.
dotnet run --project "Client\Northwind.Client" --launch-profile http
pause
