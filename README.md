# SAPGUN Media Grabber

A small Windows GUI for **yt-dlp + FFmpeg**.

Built for the simple workflow most people actually need:

**paste URL → choose output → download**

[![Download Latest Windows Release](https://img.shields.io/badge/Download-Latest%20Windows%20Release-4C6FFF?style=for-the-badge&logo=windows11&logoColor=white)](https://github.com/sapgun/SAPGUN-Media-Grabber/releases/latest/download/SAPGUN-Media-Grabber-Setup.exe)
[![View Latest Release](https://img.shields.io/badge/View-Latest%20Release-24292F?style=for-the-badge&logo=github&logoColor=white)](https://github.com/sapgun/SAPGUN-Media-Grabber/releases/latest)

> **Current stable release:** Windows. macOS and Linux versions are in development.

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
- **Check version** and **Update yt-dlp** buttons
- Optional yt-dlp auto-update on startup
- **Dark / light mode** with local preference persistence
- **Feedback / DM on X** → https://x.com/caro7370
- **Support me · Ko-fi** → https://ko-fi.com/sapgun
- Local processing — no web converter upload

## Install

### Windows

Click **Download Latest Windows Release** above, or download `SAPGUN-Media-Grabber-Setup.exe` from the latest GitHub Release and run it.

**Direct latest installer:**  
https://github.com/sapgun/SAPGUN-Media-Grabber/releases/latest/download/SAPGUN-Media-Grabber-Setup.exe

**Latest release page:**  
https://github.com/sapgun/SAPGUN-Media-Grabber/releases/latest

The installer bundles current upstream `yt-dlp` and FFmpeg binaries. No Rust, Node.js, npm, Python, PATH setup, or Tauri runtime is required.

### macOS / Linux

Cross-platform builds are currently in development. When released, platform-specific downloads will be published on the same **Latest Release** page.

## Quick use

1. Paste the media URL.
2. Pick a profile.
3. Leave **Browser Cookies = None** for normal public media.
4. Click **DOWNLOAD**.
5. Follow percentage / speed / ETA in the progress area.
6. If a site returns `HTTP 403` or requires a signed-in session, select the browser where you are already signed in (Edge / Chrome / Brave / Firefox) and retry.
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

It also handles source files delivered as AV1 or VP9.

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

The Windows release is published as:

`SAPGUN-Media-Grabber-Setup.exe`

Future macOS and Linux artifacts will be added to the same Release page, so users do not need to search for separate download pages.

### Maintainer: Publish Windows Latest (Avalonia)

GitHub only shows **Run workflow** for files that exist on `main`. This repository now includes `.github/workflows/publish-windows-latest-avalonia.yml`.

It does **not** run on push and does **not** change Latest by itself. After this file is on `main`, open Actions → **Publish Windows Latest (Avalonia 0.3 UI)** → Run workflow, with:

- `confirm` = `make-windows-latest`
- `windows_run_id` = a green **Windows x64 v0.3.0 Alpha** run that uploaded `SAPGUN-Media-Grabber-Setup.exe`
- `tag` = `v0.3.0` (must not already exist; must not contain alpha/beta/rc)

Until that dispatch succeeds, Latest stays on the current WinForms installer.

### Windows SmartScreen

Early unsigned open-source releases may trigger a Windows SmartScreen reputation warning. Source code and the GitHub Actions build workflow are public so users can inspect how the installer is produced. Code signing can be added later if distribution volume justifies it.
