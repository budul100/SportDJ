; SportDJ Inno Setup script
; Build the installer:      iscc /DPublishDir=<publish folder> SportDJ.iss
; Build the portable setup: iscc /DPublishDir=<publish folder> /DPortable SportDJ.iss
; The publish folder is the self-contained publish output, Build.bat passes it in.
; The setups are written to _Setup in the solution folder.

#ifndef PublishDir
  #error PublishDir is not defined, pass it with /DPublishDir=<folder> (see Build.bat)
#endif

#define AppName "SportDJ"
#define AppExe "SportDJ.exe"
#define AppVersion GetVersionNumbersString(PublishDir + "\" + AppExe)

[Setup]
AppId={{B7E2D3A4-5C1F-4E8A-9D62-3F0A7C41B9E5}
AppName={#AppName}
AppVersion={#AppVersion}
VersionInfoVersion={#AppVersion}
DefaultGroupName={#AppName}
OutputDir=..\..\_Setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\Icons\SportDJ.ico
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Replace a running instance cleanly instead of failing on locked files
CloseApplications=yes
RestartApplications=no

#ifdef Portable
; Portable: only unpacks into a folder of the user's choice, leaves no traces on the system
OutputBaseFilename={#AppName}-Portable-{#AppVersion}
DefaultDirName={src}\{#AppName}
PrivilegesRequired=lowest
Uninstallable=no
CreateUninstallRegKey=no
DisableProgramGroupPage=yes
DisableReadyPage=yes
#else
; Installer: per user by default, the wizard offers an all users installation
OutputBaseFilename={#AppName}-Setup-{#AppVersion}
DefaultDirName={autopf}\{#AppName}
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
UninstallDisplayIcon={app}\{#AppExe}
#endif

[Languages]
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

#ifndef Portable
[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"; IconFilename: "{app}\SportDJ.ico"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent
#endif

; The user data (sessions, settings, audio) lives outside of the application folder
; and is deliberately never touched by an update or an uninstall.
