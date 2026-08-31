#!/usr/bin/env bash
set -euo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
BIN="$HERE/sapgun-media-grabber"
ICON="$HERE/sapgun-media-grabber.svg"
TEMPLATE="$HERE/sapgun-media-grabber.desktop"
APPS="${XDG_DATA_HOME:-$HOME/.local/share}/applications"
DESKTOP="$APPS/sapgun-media-grabber.desktop"

if [[ ! -x "$BIN" ]]; then
  echo "Missing launcher: $BIN" >&2
  exit 1
fi
if [[ ! -f "$TEMPLATE" ]]; then
  echo "Missing desktop template: $TEMPLATE" >&2
  exit 1
fi

mkdir -p "$APPS"
sed \
  -e "s|@EXEC@|$BIN|g" \
  -e "s|@DIR@|$HERE|g" \
  -e "s|@ICON@|$ICON|g" \
  "$TEMPLATE" > "$DESKTOP"
chmod 644 "$DESKTOP"

if command -v update-desktop-database >/dev/null 2>&1; then
  update-desktop-database "$APPS" >/dev/null 2>&1 || true
fi

echo "Desktop launcher installed:"
echo "  $DESKTOP"
echo "Move this folder and re-run ./install-desktop.sh so Exec stays correct."
