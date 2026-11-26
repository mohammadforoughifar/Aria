<#
.SYNOPSIS
    رفعِ خطایِ NU1100 در بازیابیِ بسته‌هایِ NuGet (Package Source Mapping).

.DESCRIPTION
    خطایی که معمولاً این‌شکلی است:

        Unable to resolve 'Microsoft.NETCore.App.Runtime.Mono.browser-wasm (= 9.0.11)'
        PackageSourceMapping is enabled, the following source(s) were not considered: nuget.org

    یعنی رویِ سیستم، «نگاشتِ منبعِ بسته» روشن است ولی هیچ الگویی این بسته را به
    nuget.org وصل نمی‌کند؛ پس NuGet آن منبع را نگاه نمی‌کند.

    این اسکریپت سه کار می‌کند:
      ۱) از فایلِ تنظیماتِ NuGet کاربر نسخهٔ پشتیبان می‌گیرد و بخشِ
         <packageSourceMapping> را برمی‌دارد و مطمئن می‌شود nuget.org فعال است.
         (فایلِ NuGet.config در ریشهٔ همین Solution هم همین کار را برایِ پروژه می‌کند.)
      ۲) کشِ HTTP و پوشه‌هایِ obj/bin را پاک می‌کند تا بازیابیِ کهنه بمانع نشود.
      ۳) restore و build را اجرا می‌کند.

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File tools\fix-nuget.ps1

.EXAMPLE
    # فقط نشان بده چه می‌کند، تغییری نده:
    powershell -NoProfile -ExecutionPolicy Bypass -File tools\fix-nuget.ps1 -DryRun

.EXAMPLE
    # تنظیماتِ یک فایلِ دیگر (برایِ آزمون):
    pwsh tools\fix-nuget.ps1 -NuGetConfigPath /tmp/NuGet.Config -DryRun
#>
[CmdletBinding()]
param(
    [string] $NuGetConfigPath = (Join-Path $env:APPDATA 'NuGet\NuGet.Config'),
    [switch] $DryRun,
    [switch] $SkipBuild,
    [switch] $ClearPackageCache
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path (Join-Path $root 'Aria.sln'))) { $root = (Get-Location).Path }

function Write-Step($text) { Write-Host "`n▸ $text" -ForegroundColor Cyan }
function Write-Ok($text)   { Write-Host "  ✓ $text" -ForegroundColor Green }
function Write-Warn2($t)   { Write-Host "  ! $t" -ForegroundColor Yellow }

Write-Host "════════ رفعِ NU1100 (Package Source Mapping) ════════" -ForegroundColor White
Write-Host "  ریشهٔ Solution: $root"
Write-Host "  فایلِ تنظیمات: $NuGetConfigPath"
if ($DryRun) { Write-Warn2 'حالتِ آزمایشی — هیچ فایلی تغییر نمی‌کند.' }

# ─────────────────────────── ۱. dotnet هست؟ ───────────────────────────
Write-Step 'بررسیِ نصبِ dotnet'
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    Write-Host '  ✗ dotnet رویِ PATH نیست. .NET SDK 9 را نصب کن و دوباره اجرا کن:' -ForegroundColor Red
    Write-Host '      https://dotnet.microsoft.com/download/dotnet/9.0' -ForegroundColor Red
    exit 1
}
Write-Ok "dotnet $(& dotnet --version)"

# ─────────────────── ۲. وضعیتِ فعلیِ منابع (فقط نمایش) ───────────────────
Write-Step 'منابعِ فعلیِ NuGet'
try { & dotnet nuget list source | ForEach-Object { Write-Host "    $_" } }
catch { Write-Warn2 'فهرستِ منابع خوانده نشد.' }

Write-Host '  فایل‌هایِ تنظیماتی که NuGet می‌خواند (از نزدیک به دور):'
# مسیرِ تنظیماتِ سطحِ ماشین فقط رویِ ویندوز وجود دارد.
$machineCfg = $null
if ($env:ProgramFiles) { $machineCfg = Join-Path $env:ProgramFiles 'NuGet\Config\Microsoft.VisualStudio.Offline.config' }

$known = @((Join-Path $root 'NuGet.config'), $NuGetConfigPath, $machineCfg) |
         Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
foreach ($f in $known) {
    if ($f -and (Test-Path $f)) {
        $hasMapping = (Get-Content -Raw -Path $f) -match 'packageSourceMapping'
        $flag = if ($hasMapping) { '  ← نگاشتِ منبع دارد' } else { '' }
        Write-Host "    • $f$flag"
    }
}

# ─────────────── ۳. پاک‌سازیِ نگاشت در تنظیماتِ کاربر ───────────────
Write-Step 'اصلاحِ فایلِ تنظیماتِ کاربر'
if (-not (Test-Path $NuGetConfigPath)) {
    Write-Warn2 "فایل پیدا نشد: $NuGetConfigPath — رد شد (تنظیماتِ پروژه کافی است)."
}
else {
    try {
        [xml] $xml = Get-Content -Raw -Path $NuGetConfigPath
    }
    catch {
        Write-Host "  ✗ فایلِ تنظیمات قابلِ خواندن نیست: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }

    $cfg    = $xml.configuration
    $mapped = $cfg.SelectSingleNode('packageSourceMapping')
    $changed = $false

    if ($mapped) {
        if ($DryRun) { Write-Ok 'نگاشتِ منبع هست و برداشته می‌شود (آزمایشی).' }
        else {
            $backup = "$NuGetConfigPath.bak-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
            Copy-Item -Path $NuGetConfigPath -Destination $backup -Force
            Write-Ok "پشتیبان گرفته شد: $backup"
            [void] $cfg.RemoveChild($mapped)
            $changed = $true
            Write-Ok '<packageSourceMapping> حذف شد.'
        }
    }
    else { Write-Ok 'این فایل نگاشتِ منبع ندارد.' }

    # nuget.org باید وجود داشته باشد…
    $sources = $cfg.SelectSingleNode('packageSources')
    if (-not $sources) {
        if (-not $DryRun) {
            $sources = $xml.CreateElement('packageSources')
            [void] $cfg.PrependChild($sources)
            $changed = $true
        }
        Write-Ok '<packageSources> ساخته می‌شود.'
    }

    $org = $null
    if ($sources) { $org = $sources.SelectSingleNode("add[@key='nuget.org']") }
    if (-not $org) {
        if (-not $DryRun) {
            $add = $xml.CreateElement('add')
            $add.SetAttribute('key', 'nuget.org')
            $add.SetAttribute('value', 'https://api.nuget.org/v3/index.json')
            $add.SetAttribute('protocolVersion', '3')
            [void] $sources.AppendChild($add)
            $changed = $true
        }
        Write-Ok 'منبعِ nuget.org اضافه می‌شود.'
    }
    else { Write-Ok 'منبعِ nuget.org هست.' }

    # …و غیرفعال نباشد
    $disabled = $cfg.SelectSingleNode("disabledPackageSources/add[@key='nuget.org']")
    if ($disabled) {
        if (-not $DryRun) {
            [void] $disabled.ParentNode.RemoveChild($disabled)
            $changed = $true
        }
        Write-Ok 'غیرفعال‌بودنِ nuget.org برداشته می‌شود.'
    }

    if ($changed -and -not $DryRun) {
        $settings = New-Object System.Xml.XmlWriterSettings
        $settings.Indent = $true
        $settings.Encoding = New-Object System.Text.UTF8Encoding($false)
        $writer = [System.Xml.XmlWriter]::Create($NuGetConfigPath, $settings)
        try { $xml.Save($writer) } finally { $writer.Dispose() }
        Write-Ok 'فایل ذخیره شد.'
    }
}

# ─────────────────────────── ۴. پاک‌کردنِ کش‌ها ───────────────────────────
Write-Step 'پاک‌سازیِ کش و خروجیِ بیلد'
if ($DryRun) { Write-Warn2 'در حالتِ آزمایشی پاک نمی‌شود.' }
else {
    try { & dotnet nuget locals http-cache --clear | Out-Null; Write-Ok 'کشِ HTTPِ NuGet پاک شد.' }
    catch { Write-Warn2 "کشِ HTTP پاک نشد: $($_.Exception.Message)" }

    if ($ClearPackageCache) {
        try { & dotnet nuget locals global-packages --clear | Out-Null; Write-Ok 'بسته‌هایِ سراسری هم پاک شدند.' }
        catch { Write-Warn2 'بسته‌هایِ سراسری پاک نشدند.' }
    }

    $removed = 0
    foreach ($dir in @('obj', 'bin')) {
        Get-ChildItem -Path $root -Directory -Recurse -Force -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -eq $dir -and $_.FullName -notmatch '\\node_modules\\' } |
            ForEach-Object { Remove-Item -Path $_.FullName -Recurse -Force -ErrorAction SilentlyContinue; $removed++ }
    }
    Write-Ok "$removed پوشهٔ obj/bin پاک شد."
}

# ─────────────────────────── ۵. بازیابی و بیلد ───────────────────────────
if ($SkipBuild -or $DryRun) { Write-Warn2 'restore/build اجرا نشد.'; exit 0 }

Write-Step 'dotnet restore Aria.sln'
Push-Location $root
try {
    & dotnet restore 'Aria.sln' --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ✗ بازیابی شکست خورد (کدِ $LASTEXITCODE)." -ForegroundColor Red
        Write-Host '    اگر باز هم NU1100 بود، پیامِ کامل را ببین و لاگِ بالا را نگه دار.' -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Ok 'بازیابی موفق.'

    Write-Step 'dotnet build Aria.sln'
    & dotnet build 'Aria.sln' --nologo
    if ($LASTEXITCODE -ne 0) { Write-Host "  ✗ بیلد شکست خورد (کدِ $LASTEXITCODE)." -ForegroundColor Red; exit $LASTEXITCODE }
    Write-Ok 'بیلد موفق — مشکل حل شد.'
}
finally { Pop-Location }
