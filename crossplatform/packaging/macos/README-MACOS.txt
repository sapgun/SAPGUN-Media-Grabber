SAPGUN Media Grabber — macOS Apple Silicon (experimental / beta)

This archive contains:

  SAPGUN Media Grabber.app

It is self-contained (yt-dlp + FFmpeg/ffprobe). You do not need to install
Python, .NET, Node, or Homebrew to try the alpha.

Open:

  1. If you have the .dmg, open it and copy SAPGUN Media Grabber.app.
     If you have the tar.gz, extract it first.
  2. Right-click SAPGUN Media Grabber.app → Open
     (unsigned alpha builds often need this once because of Gatekeeper)
  3. If macOS still blocks it, run:

       ./remove-quarantine.sh

     then try Open again.

This build is NOT signed or notarized. Finder launch, Gatekeeper, Retina
layout, and browser cookies still need real-user testing. Do not treat it
as a stable macOS release.

The in-app APP updater downloads a new tar.gz and reveals it. It does not
replace this .app automatically.

Feedback: https://x.com/caro7370
Support:  https://ko-fi.com/sapgun
Source:   https://github.com/sapgun/SAPGUN-Media-Grabber
