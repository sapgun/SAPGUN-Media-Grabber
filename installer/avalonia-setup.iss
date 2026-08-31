#ifndef MyAppVersion
#define MyAppVersion "0.3.0"
#endif
#ifndef MyAppVersionInfo
#define MyAppVersionInfo "0.3.0"
#endif

#define MyAppName "SAPGUN Media Grabber"
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
LicenseFile=..\LICENSE
OutputDir=..\dist
OutputBaseFilename=SAPGUN-Media-Grabber-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
VersionInfoVersion={#MyAppVersionInfo}
VersionInfoCompany=SAPGUN
VersionInfoDescription=Local yt-dlp + FFmpeg GUI (Avalonia)
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersionInfo}

[Files]
Source: "..\dist\windows-x64\app\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[InstallDelete]
Type: filesandordirs; Name: "{app}\bin"

[Icons]
Name: "{autoprograms}\SAPGUN Media Grabber"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\SAPGUN Media Grabber"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch SAPGUN Media Grabber"; Flags: nowait postinstall skipifsilent unchecked
