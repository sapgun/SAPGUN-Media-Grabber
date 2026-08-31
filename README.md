# SAPGUN Media Grabber

A lightweight local desktop GUI for **yt-dlp + FFmpeg**.

Built for the simple workflow most people actually need:

**paste URL → choose output → download**

[![Download Latest Windows Release](https://img.shields.io/badge/Download-Latest%20Windows%20Release-4C6FFF?style=for-the-badge&logo=windows11&logoColor=white)](https://github.com/sapgun/SAPGUN-Media-Grabber/releases/latest/download/SAPGUN-Media-Grabber-Setup.exe)
[![View Latest Release](https://img.shields.io/badge/View-Latest%20Release-24292F?style=for-the-badge&logo=github&logoColor=white)](https://github.com/sapgun/SAPGUN-Media-Grabber/releases/latest)
[![Star on GitHub](https://img.shields.io/badge/★-Star%20on%20GitHub-F6C343?style=for-the-badge&logo=github&logoColor=black)](https://github.com/sapgun/SAPGUN-Media-Grabber)

> **Current Windows line:** Avalonia **v0.3.0** (this branch). GitHub Latest still shows v0.2.2 until the Windows Latest publisher runs.  
> Linux and macOS remain experimental alpha packages.

## Features

- **X / Twitter 1080p** — downloads up to 1080p and normalizes the result to H.264 + AAC for reliable X uploads
- **Original Best**
- **1080p MP4**
- **Audio MP3**
- **Trim / section download**
- Detailed **download progress** — percentage, speed, ETA and transferred size
- Detailed **X conversion progress** — FFmpeg percentage and processed media time
- Optional **browser cookies** for 403 / sign-in protected media
- Shows **installed yt-dlp version vs latest**
- **Check** and **Update yt-dlp** in a dedicated ENGINE section
- **Check App Update** against GitHub Releases (separate from yt-dlp; never silent-installs)
- Stable vs Prerelease app-update channels
- **Dark / light mode** with local preference persistence
- **Feedback / DM on X** → https://x.com/caro7370
- **Support me · Ko-fi** → https://ko-fi.com/sapgun
- **★ Star on GitHub** → https://github.com/sapgun/SAPGUN-Media-Grabber
- Local processing — no web converter upload

## Install

### Windows — stable

Click **Download Latest Windows Release** above, or download `SAPGUN-Media-Grabber-Setup.exe` from the latest GitHub Release and run it.

**Direct latest installer:**  
https://github.com/sapgun/SAPGUN-Media-Grabber/releases/latest/download/SAPGUN-Media-Grabber-Setup.exe

**Latest release page:**  
https://github.com/sapgun/SAPGUN-Media-Grabber/releases/latest

The installer bundles current upstream `yt-dlp` and FFmpeg binaries. No Rust, Node.js, npm, Python, or PATH setup is required.

### Linux x64 — v0.3.0 alpha

The Avalonia/.NET Linux x64 build now passes CI on Ubuntu, including:

- self-contained application publish
- bundled yt-dlp, FFmpeg and ffprobe
- executable verification
- actual GUI launch under Xvfb
- packaged `.tar.gz` artifact

A public **v0.3.0-alpha.1** Linux x64 archive is on GitHub Releases (prerelease, not Latest). The in-app updater in this branch looks for `SAPGUN-Media-Grabber-vX.Y.Z-linux-x64.tar.gz` plus `SHA256SUMS-linux-x64.txt`.

After extracting, run `./app/sapgun-media-grabber`. Optional: `./app/install-desktop.sh` writes a `.desktop` launcher into `~/.local/share/applications`.

CI also builds a Linux **AppImage** (x64 and arm64). The in-app updater still prefers the `.tar.gz` when both exist.

Stable users should keep using the Windows installer from **Latest Release**. Do not treat Linux alpha as production.

### macOS Apple Silicon — v0.3.0 alpha

The native arm64 build now passes CI on an Apple Silicon GitHub Actions runner, including:

- self-contained `.app` creation
- native `yt-dlp_macos`
- arm64 FFmpeg / ffprobe integrity verification
- `libx264` + AAC availability check
- real X-ready H.264 / AAC / yuv420p conversion smoke test
- actual GUI process launch on macOS
- packaged `.tar.gz` artifact

A public **v0.3.0-alpha.1** macOS arm64 archive is on GitHub Releases (prerelease). The in-app updater looks for `SAPGUN-Media-Grabber-vX.Y.Z-macos-arm64.tar.gz` plus `SHA256SUMS-macos-arm64.txt`.

Newer alpha tarballs also include `README-MACOS.txt` and `remove-quarantine.sh`. The app is **not signed**. Prefer right-click → Open; the script only clears the quarantine xattr if Gatekeeper still blocks launch.

The macOS build is still **alpha**. Finder launch, Gatekeeper behavior, browser-cookie UX, signing and notarization should be verified with real users before a stable release. This updater **downloads and reveals** the archive; it does not replace the `.app` automatically.

## Quick use

1. Paste the media URL.
2. Pick a profile.
3. Leave **Browser Cookies = None** for normal public media.
4. Click **DOWNLOAD**.
5. Follow percentage / speed / ETA in the progress area.
6. If a site returns `HTTP 403` or requires a signed-in session, select the browser where you are already signed in and retry.
7. If a site suddenly stops working, click **Check version** → **Update yt-dlp**.

## Why this exists

yt-dlp is excellent, but most people do not need to remember format selectors or FFmpeg commands every time. This project puts a deliberately small GUI around the parts used most often.

## X / Twitter profile

The X profile targets a conservative upload-compatible output:

- MP4
- H.264 / AVC High Profile
- AAC audio
- yuv420p
- max 1080p
- max 30 fps
- `faststart`

It also handles source files delivered as AV1 or VP9 by normalizing the final X-ready output.

## 403 errors

A 403 is **not always a version problem**. Sites can require cookies, signed-in sessions, tokens, or change extraction behavior.

Try, in order:

1. Update yt-dlp.
2. Select **Browser Cookies** for the browser where you are signed in.
3. Retry.
4. Check upstream yt-dlp issues if the site has changed globally.

## Upstream

This project is a GUI wrapper built on top of:

- [yt-dlp](https://github.com/yt-dlp/yt-dlp)
- [FFmpeg](https://ffmpeg.org/)

SAPGUN Media Grabber is not affiliated with those projects. Upstream software remains under its respective license.

## Responsible use

Download only content you own, have permission to download, or may lawfully download under the relevant service rules and applicable law.

## License

MIT — applies to the SAPGUN Media Grabber source code in this repository. Third-party components retain their own licenses.

## Releases

GitHub Releases are the canonical distribution channel for SAPGUN Media Grabber.

**Always use:**  
https://github.com/sapgun/SAPGUN-Media-Grabber/releases/latest

The current stable Windows artifact is:

`SAPGUN-Media-Grabber-Setup.exe`

Linux and macOS artifacts will be added to the same release flow as the v0.3.0 line matures.

### Maintainer: show “Publish Windows Latest” in Actions

GitHub only lists **workflow_dispatch** workflows that exist on the **default branch (`main`)**. The publish job in this 0.3 branch is invisible in the Actions tab until that YAML is also on `main`. It still never runs on push; it only publishes when dispatched with `confirm=make-windows-latest`.

### Windows SmartScreen

Early unsigned open-source releases may trigger a Windows SmartScreen reputation warning. Source code and the GitHub Actions build workflow are public so users can inspect how the installer is produced. Code signing can be added later if distribution volume justifies it.
