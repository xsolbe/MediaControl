#ifndef AppVersion
  #error AppVersion must be provided by build-installer.ps1
#endif
#ifndef PayloadDir
  #error PayloadDir must be provided by build-installer.ps1
#endif

[Setup]
AppId={{0653B087-99DB-42C1-BFF9-F5F50559D82D}
AppName=MediaControl
AppVersion={#AppVersion}
AppPublisher=xsolbe
AppPublisherURL=https://github.com/xsolbe/MediaControl
AppSupportURL=https://github.com/xsolbe/MediaControl/issues
AppUpdatesURL=https://github.com/xsolbe/MediaControl/releases
DefaultDirName={autopf}\MediaControl
DefaultGroupName=MediaControl
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\MediaControl.exe
SetupIconFile=..\Assets\MediaControl.ico
LicenseFile=..\LICENSE
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
CloseApplications=yes
OutputBaseFilename=MediaControl-Setup-v{#AppVersion}-win-x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PayloadDir}\MediaControl.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\Assets\Lucide\LUCIDE-LICENSE.txt"; DestDir: "{app}\Assets\Lucide"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\MediaControl"; Filename: "{app}\MediaControl.exe"
Name: "{autodesktop}\MediaControl"; Filename: "{app}\MediaControl.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\MediaControl.exe"; Description: "{cm:LaunchProgram,MediaControl}"; Flags: nowait postinstall skipifsilent
