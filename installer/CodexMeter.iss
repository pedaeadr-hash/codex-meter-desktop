#define MyAppName "Codex Meter"
#define MyAppVersion "1.0.1"
#define MyAppPublisher "pedaeadr-hash"
#define MyAppExeName "CodexMeter.exe"

[Setup]
AppId={{6F6144A1-3E27-4F7B-9A28-E7598AB83DB0}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\Codex Meter
DefaultGroupName=Codex Meter
OutputDir=..\artifacts\installer
OutputBaseFilename=CodexMeter-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
ChangesEnvironment=yes
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "startup"; Description: "Iniciar o Codex Meter com o Windows"; GroupDescription: "Inicializacao:"; Flags: checkedonce

[Files]
Source: "..\artifacts\desktop\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\artifacts\cli\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Codex Meter"; Filename: "{app}\{#MyAppExeName}"
Name: "{userdesktop}\Codex Meter"; Filename: "{app}\{#MyAppExeName}"; Tasks: startup

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "CodexMeter"; ValueData: """{app}\{#MyAppExeName}"""; Tasks: startup; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Environment"; ValueType: expandsz; ValueName: "Path"; ValueData: "{olddata};{app}"; Check: NeedsAddPath(ExpandConstant('{app}')); Flags: preservestringtype

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir o Codex Meter"; Flags: nowait postinstall skipifsilent

[Code]
function PathContains(const Haystack, Needle: string): Boolean;
begin
  Result := Pos(';' + Lowercase(Needle) + ';', ';' + Lowercase(Haystack) + ';') > 0;
end;

function NeedsAddPath(const AppPath: string): Boolean;
var
  CurrentPath: string;
begin
  if not RegQueryStringValue(HKCU, 'Environment', 'Path', CurrentPath) then
    CurrentPath := '';
  Result := not PathContains(CurrentPath, AppPath);
end;

procedure AddAppToPath;
var
  CurrentPath: string;
  AppPath: string;
begin
  AppPath := ExpandConstant('{app}');
  RegQueryStringValue(HKCU, 'Environment', 'Path', CurrentPath);
  if not PathContains(CurrentPath, AppPath) then
  begin
    if (CurrentPath <> '') and (CurrentPath[Length(CurrentPath)] <> ';') then
      CurrentPath := CurrentPath + ';';
    RegWriteExpandStringValue(HKCU, 'Environment', 'Path', CurrentPath + AppPath);
  end;
end;

procedure RemoveAppFromPath;
var
  CurrentPath: string;
  AppPath: string;
  Wrapped: string;
begin
  AppPath := ExpandConstant('{app}');
  if RegQueryStringValue(HKCU, 'Environment', 'Path', CurrentPath) then
  begin
    Wrapped := ';' + CurrentPath + ';';
    StringChangeEx(Wrapped, ';' + AppPath + ';', ';', True);
    while Pos(';;', Wrapped) > 0 do
      StringChangeEx(Wrapped, ';;', ';', True);
    if (Length(Wrapped) > 0) and (Wrapped[1] = ';') then
      Delete(Wrapped, 1, 1);
    if (Length(Wrapped) > 0) and (Wrapped[Length(Wrapped)] = ';') then
      Delete(Wrapped, Length(Wrapped), 1);
    RegWriteExpandStringValue(HKCU, 'Environment', 'Path', Wrapped);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    AddAppToPath;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
    RemoveAppFromPath;
end;

