#!/usr/bin/env bash
# اجرای سرورِ API روی پورت ۵۲۱۱ (پایگاه‌دادهٔ SQLite داخلِ workspace می‌ماند)
set -euo pipefail

ROOT=${HOME:-/home/user}
CACHE="$ROOT/.cache"
BUILD_DIR="$CACHE/build"

export PATH="$PATH:$CACHE/dotnet"
export NUGET_PACKAGES="$CACHE/nuget"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export ASPNETCORE_ENVIRONMENT=Development
export ARIA_SANDBOX=true
export ARIA_BUILD_DIR="$BUILD_DIR/"

# ---------- ۱. SDK ----------
if ! command -v dotnet >/dev/null 2>&1 || [ -z "$(dotnet --list-sdks 2>/dev/null)" ]; then
  echo "▸ نصبِ SDK…"
  mkdir -p "$CACHE/dotnet" "$CACHE/nuget" "$BUILD_DIR"
  curl -sSL -o /tmp/dotnet-sdk.tar.gz \
    https://builds.dotnet.microsoft.com/dotnet/Sdk/9.0.320/dotnet-sdk-9.0.320-linux-x64.tar.gz
  tar -xzf /tmp/dotnet-sdk.tar.gz -C "$CACHE/dotnet"
  chmod +x "$CACHE/dotnet/dotnet"
fi
echo "▸ dotnet $(dotnet --version)"

# ---------- ۲. بیلد و اجرا ----------
cd "$ROOT/Aria"
echo "▸ بیلدِ سرور…"
dotnet build Server/Northwind.Server --nologo -v q

echo "▸ اجرا روی http://0.0.0.0:5211"
exec dotnet run --no-build --no-launch-profile --project Server/Northwind.Server \
  --urls http://0.0.0.0:5211
