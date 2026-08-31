#!/usr/bin/env bash
set -euo pipefail

APP_DIR="${1:?app dir}"
OUT_IMAGE="${2:?output AppImage path}"
ARCH="${3:?x86_64 or aarch64}"

if [ ! -x "$APP_DIR/sapgun-media-grabber" ]; then
  echo "missing launcher $APP_DIR/sapgun-media-grabber" >&2
  exit 1
fi

WORKDIR="$(mktemp -d)"
cleanup() { rm -rf "$WORKDIR"; }
trap cleanup EXIT

APPDIR="$WORKDIR/SAPGUN_Media_Grabber.AppDir"
mkdir -p "$APPDIR/usr/bin"
cp -a "$APP_DIR"/. "$APPDIR/usr/bin/"

cp "$APP_DIR/sapgun-media-grabber.svg" "$APPDIR/sapgun-media-grabber.svg"

cat > "$APPDIR/sapgun-media-grabber.desktop" <<'EOF'
[Desktop Entry]
Type=Application
Name=SAPGUN Media Grabber
Comment=Local yt-dlp + FFmpeg media grabber
Exec=sapgun-media-grabber
Icon=sapgun-media-grabber
Terminal=false
Categories=AudioVideo;Video;
StartupNotify=true
StartupWMClass=SAPGUN Media Grabber
EOF

cat > "$APPDIR/AppRun" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
HERE="$(dirname "$(readlink -f "$0")")"
export PATH="$HERE/usr/bin:$PATH"
exec "$HERE/usr/bin/sapgun-media-grabber" "$@"
EOF
chmod +x "$APPDIR/AppRun" "$APPDIR/usr/bin/sapgun-media-grabber"

TOOL_URL="https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-${ARCH}.AppImage"
curl -fL --retry 3 "$TOOL_URL" -o "$WORKDIR/appimagetool.AppImage"
chmod +x "$WORKDIR/appimagetool.AppImage"
( cd "$WORKDIR" && ./appimagetool.AppImage --appimage-extract >/dev/null )

mkdir -p "$(dirname "$OUT_IMAGE")"
ARCH="$ARCH" "$WORKDIR/squashfs-root/AppRun" "$APPDIR" "$OUT_IMAGE"
chmod +x "$OUT_IMAGE"
test -s "$OUT_IMAGE"
echo "Wrote $OUT_IMAGE"
