#!/usr/bin/env bash
#
# Cloud Agent environment bootstrap for SAPGUN Media Grabber.
#
# Installs the .NET 10 SDK (pinned channel) into the user's home, ensures the
# GUI smoke-test dependencies are present, then restores, builds and tests the
# cross-platform Avalonia solution. Safe to run repeatedly.
set -euo pipefail

DOTNET_CHANNEL="10.0"
DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

export DOTNET_ROOT
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

echo "==> Ensuring GUI smoke-test dependencies (xvfb, xz-utils)"
if command -v apt-get >/dev/null 2>&1; then
  SUDO=""
  if [ "$(id -u)" -ne 0 ]; then
    SUDO="sudo"
  fi
  if ! command -v xvfb-run >/dev/null 2>&1 || ! command -v xz >/dev/null 2>&1; then
    $SUDO apt-get update
    $SUDO apt-get install -y --no-install-recommends xvfb xz-utils
  fi
fi

if [ ! -x "$DOTNET_ROOT/dotnet" ] || ! "$DOTNET_ROOT/dotnet" --list-sdks 2>/dev/null | grep -q "^${DOTNET_CHANNEL%.*}\."; then
  echo "==> Installing .NET SDK (channel $DOTNET_CHANNEL) into $DOTNET_ROOT"
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  chmod +x /tmp/dotnet-install.sh
  /tmp/dotnet-install.sh --channel "$DOTNET_CHANNEL" --install-dir "$DOTNET_ROOT"
else
  echo "==> .NET SDK already present: $("$DOTNET_ROOT/dotnet" --version)"
fi

echo "==> dotnet version: $(dotnet --version)"

echo "==> Persisting .NET environment for future shells"
PROFILE_SNIPPET="$HOME/.bashrc"
if [ -w "$(dirname "$PROFILE_SNIPPET")" ] && ! grep -q 'SAPGUN .NET SDK' "$PROFILE_SNIPPET" 2>/dev/null; then
  cat >> "$PROFILE_SNIPPET" <<'EOF'

# SAPGUN .NET SDK
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
EOF
fi

cd "$REPO_ROOT"

echo "==> Restoring projects"
dotnet restore crossplatform/SAPGUN.MediaGrabber/SAPGUN.MediaGrabber.csproj
dotnet restore crossplatform/SAPGUN.MediaGrabber.Tests/SAPGUN.MediaGrabber.Tests.csproj

echo "==> Building app (Release)"
dotnet build crossplatform/SAPGUN.MediaGrabber/SAPGUN.MediaGrabber.csproj -c Release --no-restore

echo "==> Running tests (Release)"
dotnet test crossplatform/SAPGUN.MediaGrabber.Tests/SAPGUN.MediaGrabber.Tests.csproj -c Release --no-restore

echo "==> Environment setup complete."
