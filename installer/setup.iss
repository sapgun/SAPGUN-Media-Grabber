#define MyAppName "SAPGUN Media Grabber"
#define MyAppVersion "0.2.0"
#define MyAppPublisher "SAPGUN"
#define MyAppExeName "SAPGUN Media Grabber.exe"

[Setup]
AppId={{E6EFEC95-85B3-4EDC-8D9B-F9056D5E3150}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\SAPGUN Media Grabber
DefaultGroupName=SAPGUN Media Grabber
PrivilegesRequired=lowest
OutputDir=..\dist
OutputBaseFilename=SAPGUN-Media-Grabber-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany=SAPGUN
VersionInfoDescription=Lightweight yt-dlp + FFmpeg GUI
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

[Files]
Source: "..\build\SAPGUN Media Grabber.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\build\bin\yt-dlp.exe"; DestDir: "{app}\bin"; Flags: ignoreversion
Source: "..\build\bin\ffmpeg.exe"; DestDir: "{app}\bin"; Flags: ignoreversion
Source: "..\build\bin\ffprobe.exe"; DestDir: "{app}\bin"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\SAPGUN Media Grabber"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\SAPGUN Media Grabber"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch SAPGUN Media Grabber"; Flags: nowait postinstall skipifsilent
