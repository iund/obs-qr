#ifndef Version
#define Version "0.0.0"
#endif
[Setup]
AppId={{6F3B8C52-1D0E-4F6A-9B7C-2A5E8D41C0B7}
AppName=OBS QR Stream Control
AppVersion={#Version}
DefaultDirName={autopf}\ObsQr
DefaultGroupName=OBS QR Stream Control
OutputDir=..\dist
OutputBaseFilename=ObsQr-Setup-{#Version}
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\ObsQr.exe

[Tasks]
Name: "startup"; Description: "Start with Windows"

[Files]
Source: "..\publish\ObsQr.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\OBS QR Stream Control"; Filename: "{app}\ObsQr.exe"

[Registry]
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "ObsQr"; ValueData: """{app}\ObsQr.exe"""; Tasks: startup; Flags: uninsdeletevalue

[Run]
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall add rule name=""OBS QR Stream Control"" dir=in action=allow protocol=TCP localport=5000 profile=private,domain program=""{app}\ObsQr.exe"""; Flags: runhidden
Filename: "{app}\ObsQr.exe"; Description: "Start OBS QR Stream Control"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""OBS QR Stream Control"""; Flags: runhidden
