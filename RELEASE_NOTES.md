# v0.3.0 (Windows Latest candidate)

Avalonia 0.3 UI for Windows Setup.exe. Same AppId as v0.2.2 so the installer upgrades the existing per-user install.

### Added
- In-app **Check App Update** against GitHub Releases (separate from yt-dlp)
- Compact 2×2 profile cards with the download flow visible without opening extra panels
- ENGINE yt-dlp check/update on the main screen; APP updater stays folded
- URL drop, remembered cookie browser, optional yt-dlp auto-update on start
- SHA-256 verification; Windows Setup.exe never silent-installs

### Notes
Linux and macOS packages stay experimental. This Windows build is local-only (yt-dlp + FFmpeg).

# v0.3.0-alpha.2 (unreleased branch history)

### Added
- In-app **Check App Update** against GitHub Releases (separate from yt-dlp)
- Stable vs Prerelease update channels
- Strict platform asset matching (`Setup.exe`, `linux-x64.tar.gz`, `macos-arm64.tar.gz`)
- SHA-256 verification when a digest or `SHA256SUMS-*.txt` is published
- Windows: download installer, verify, launch installer only after confirmation, then quit
- Linux / macOS: download the matching tar.gz, verify, reveal the file (no self-replace)

### Notes
This is a prerelease of the Avalonia cross-platform line. Windows v0.2.2 remains the stable installer.

App updates are never installed silently.

# v0.2.2

### Added
- Detailed yt-dlp download progress with percentage, speed, ETA and size
- Detailed X/Twitter FFmpeg conversion progress with percentage and processed media time
- Dark / light mode toggle with local preference persistence
- Feedback / DM on X button linked to https://x.com/caro7370
- `Support me · Ko-fi` button linked to https://ko-fi.com/sapgun

### Carried forward
- Scoop FFmpeg junction isolation / WinError 448 hardening
- `--ignore-config` runtime isolation
- Bundled FFmpeg selection
- Browser Cookies for authenticated/403 cases
- yt-dlp version check and one-click updater

### Notes
The release bundles current yt-dlp and FFmpeg binaries at build time. yt-dlp can be updated from inside the app.
