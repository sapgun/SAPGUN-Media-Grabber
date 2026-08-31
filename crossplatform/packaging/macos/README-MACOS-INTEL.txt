SAPGUN Media Grabber — macOS Intel x64 (experimental / beta)

This archive contains:

  SAPGUN Media Grabber.app

It is self-contained (yt-dlp + FFmpeg/ffprobe). You do not need to install
Python, .NET, Node, or Homebrew to try the alpha.

This build is for Intel Macs (x86_64). Apple Silicon users should use the
macos-arm64 archive instead.

Open:

  1. Extract this tar.gz.
  2. Right-click SAPGUN Media Grabber.app → Open
     (unsigned alpha builds often need this once because of Gatekeeper)
  3. If macOS still blocks it, run:

       ./remove-quarantine.sh

     then try Open again.

This build is NOT signed or notarized. Finder launch, Gatekeeper, and
browser cookies still need real-user testing. Do not treat it as a
stable macOS release.

The in-app APP updater downloads a new tar.gz and reveals it. It does not
replace this .app automatically.

Feedback: https://x.com/caro7370
Support:  https://ko-fi.com/sapgun
Source:   https://github.com/sapgun/SAPGUN-Media-Grabber
