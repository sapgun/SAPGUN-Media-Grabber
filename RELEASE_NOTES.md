# v0.2.2

Progress visibility and runtime isolation improvements.

### Added
- Live download percentage in the UI
- Download speed and ETA
- Downloaded size / total size when reported by yt-dlp
- Determinate X/Twitter conversion progress using FFmpeg `-progress`
- Conversion media time display, e.g. `01:14 / 03:04`
- ffprobe-based duration detection for conversion percentage

### Includes v0.2.1 hotfix
- Isolates bundled FFmpeg from Scoop `ffmpeg/current` and `ffmpeg-shared/current` junctions
- Uses `--ignore-config` so unrelated user yt-dlp configs cannot override the app profile
- Passes the bundled FFmpeg executable explicitly to yt-dlp
- Avoids Windows `WinError 448` caused by external Scoop FFmpeg junctions

### Existing features
- X / Twitter 1080p H.264 + AAC profile
- Original Best / 1080p MP4 / MP3 profiles
- Trim support
- Browser Cookies selector
- Installed/latest yt-dlp version display
- One-click yt-dlp update
- Optional yt-dlp auto-update on startup

The installer bundles yt-dlp, FFmpeg and ffprobe. Third-party components retain their respective licenses.
