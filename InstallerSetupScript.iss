; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "Reflex Track Manager"
#define MyAppVersion "0.8.115"
#define MyAppPublisher "Gnarlysoft LLC"
#define MyAppURL "https://github.com/JKapostins/Reflex-TrackManager"
#define MyAppExeName "TrackManager.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{2783D510-E6BF-4618-A40A-E7BCE45DFCCC}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf}\{#MyAppName}
DisableProgramGroupPage=yes
OutputDir=E:\GS\reflex\ReflexTrackManager\local
OutputBaseFilename=ReflexTrackManagerSetup
ExtraDiskSpaceRequired=10000000000
Compression=lzma
SolidCompression=yes
UninstallDisplayIcon=E:\GS\reflex\ReflexTrackManager\trackmanager\TrackManager\Reflex.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "E:\GS\reflex\ReflexTrackManager\local\TrackManager\TrackManager.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "E:\GS\reflex\ReflexTrackManager\local\TrackManager\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{commonprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

