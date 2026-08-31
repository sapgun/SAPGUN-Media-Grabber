#!/usr/bin/env bash
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"
APP="$HERE/SAPGUN Media Grabber.app"
if [[ ! -d "$APP" ]]; then
  echo "Could not find: $APP" >&2
  exit 1
fi
xattr -dr com.apple.quarantine "$APP" || true
echo "Removed quarantine attributes from:"
echo "  $APP"
echo "If Gatekeeper still blocks the app, right-click it and choose Open."
