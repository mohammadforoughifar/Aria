#!/usr/bin/env bash
# اجرای کلِ برنامه (رابط + API) روی یک آدرس: http://0.0.0.0:5210
#
# در محیطِ ابری دو چیز در ~/.cache نگه داشته می‌شود تا حجمِ workspace بالا نرود:
#   · SDK دات‌نت      (آن مسیر در اسنپ‌شاپ ذخیره نمی‌شود)
#   · خروجیِ بیلد    (با ARIA_SANDBOX؛ Directory.Build.props آن را می‌خواند)
set -euo pipefail

ROOT=${HOME:-/home/user}
APP="$ROOT/Aria/Client/Northwind.Client"
CACHE="$ROOT/.cache"
NUGET_DIR="$CACHE/nuget"
BUILD_DIR="$CACHE/build"
PUBLISH_DIR="$CACHE/publish"
PORT=5210

export PATH="$PATH:$CACHE/dotnet"
export NUGET_PACKAGES="$NUGET_DIR"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export ASPNETCORE_ENVIRONMENT=Development
export ARIA_SANDBOX=true
export ARIA_BUILD_DIR="$BUILD_DIR/"

# ---------- ۱. SDK ----------
ensure_sdk() {
  if command -v dotnet >/dev/null 2>&1 && [ -n "$(dotnet --list-sdks 2>/dev/null)" ]; then return 0; fi
  echo "▸ SDK یافت نشد — در حال نصب…"
  local dir="$CACHE/dotnet"
  rm -rf "$dir"; mkdir -p "$dir" "$NUGET_DIR" "$BUILD_DIR" "$PUBLISH_DIR"

  local installer="$CACHE/dotnet-install.sh"
  [ -f "$installer" ] || curl -sSL https://dot.net/v1/dotnet-install.sh -o "$installer"
  chmod +x "$installer"
  bash "$installer" --channel 9.0 --install-dir "$dir" --no-path >/dev/null 2>&1 || true

  if [ -z "$(dotnet --list-sdks 2>/dev/null)" ]; then
    echo "  ↳ استخراجِ دستی…"
    rm -rf "$dir"; mkdir -p "$dir"
    curl -sSL -o /tmp/dotnet-sdk.tar.gz \
      https://builds.dotnet.microsoft.com/dotnet/Sdk/9.0.320/dotnet-sdk-9.0.320-linux-x64.tar.gz
    tar -xzf /tmp/dotnet-sdk.tar.gz -C "$dir"
  fi
  chmod +x "$dir/dotnet" 2>/dev/null || true
}

ensure_sdk
echo "▸ dotnet $(dotnet --version)"

# ---------- ۲. انتشارِ کلاینت ----------
echo "▸ انتشارِ کلاینت…"
dotnet publish "$APP" --nologo -v q -o "$PUBLISH_DIR" /p:ServiceWorkerAssetsManifest="" 2>/dev/null \
  || dotnet publish "$APP" --nologo -v q -o "$PUBLISH_DIR"
CLIENT_WWWROOT="$PUBLISH_DIR/wwwroot"

# ---------- ۳. بیلد و اجرای سرور ----------
cd "$ROOT/Aria"
echo "▸ بیلدِ سرور…"
dotnet build Server/Northwind.Api --nologo -v q

echo "▸ اجرا روی http://0.0.0.0:$PORT  (رابط + API روی یک مبدأ)"
export ASPNETCORE_WEBROOT="$CLIENT_WWWROOT"
exec dotnet run --no-build --no-launch-profile --project Server/Northwind.Api \
  --urls "http://0.0.0.0:$PORT"
