#ifndef AppVersion
  #define AppVersion "1.2.6"
#endif
#ifndef AppSource
  #error AppSource must point to the published Windows x64 application.
#endif
#ifndef Bootstrapper
  #error Bootstrapper must point to the signed Microsoft WebView2 bootstrapper.
#endif
#ifndef SetupOutput
  #define SetupOutput "..\.."
#endif

[Setup]
AppId={{AC49183A-5198-441B-8E51-39382AF2DA83}
AppName=Yume
AppVersion={#AppVersion}
AppPublisher=Yume
AppSupportURL=https://github.com/Mattaann
DefaultDirName={localappdata}\Programs\Yume
DefaultGroupName=Yume
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
OutputDir={#SetupOutput}
OutputBaseFilename=Yume-{#AppVersion}-Setup-x64
SetupIconFile=..\Assets\Yume.ico
UninstallDisplayIcon={app}\Yume.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
VersionInfoDescription=Yume Setup
VersionInfoCompany=Yume
VersionInfoProductName=Yume

[Languages]
Name: english; MessagesFile: compiler:Default.isl
Name: swedish; MessagesFile: compiler:Languages\Swedish.isl

[Tasks]
Name: desktopicon; Description: {cm:CreateDesktopIcon}; GroupDescription: {cm:AdditionalIcons}; Flags: unchecked

[Files]
Source: "{#AppSource}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb,*.log,settings.json,WebView2\*,background-probe.json,integration-check.json"
Source: "{#Bootstrapper}"; DestName: "MicrosoftEdgeWebview2Setup.exe"; Flags: dontcopy

[Icons]
Name: "{autoprograms}\Yume"; Filename: "{app}\Yume.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\Yume"; Filename: "{app}\Yume.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\Yume.exe"; Description: "{cm:LaunchProgram,Yume}"; Flags: nowait postinstall skipifsilent

[Code]
const
  WebViewKey = 'Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}';
  RunKey = 'Software\Microsoft\Windows\CurrentVersion\Run';

function RuntimeInRegistry(Root: Integer): Boolean;
var Version: String;
begin
  Result := RegQueryStringValue(Root, WebViewKey, 'pv', Version) and (Version <> '') and (Version <> '0.0.0.0');
end;

function HasWebView: Boolean;
begin
  Result := RuntimeInRegistry(HKCU) or RuntimeInRegistry(HKLM32) or RuntimeInRegistry(HKLM64);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var ExitCode: Integer;
begin
  Result := '';
  if not HasWebView then begin
    ExtractTemporaryFile('MicrosoftEdgeWebview2Setup.exe');
    if not Exec(ExpandConstant('{tmp}\MicrosoftEdgeWebview2Setup.exe'), '/silent /install', '', SW_HIDE, ewWaitUntilTerminated, ExitCode) then begin
      Result := 'Could not start Microsoft WebView2 setup. Install WebView2 Runtime and try again.';
      Exit;
    end;
    if not HasWebView then
      Result := 'Microsoft WebView2 is required. Connect to the internet, install WebView2 Runtime, and try again.';
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var Existing: String;
begin
  if CurStep = ssPostInstall then
    if RegQueryStringValue(HKCU, RunKey, 'Yume', Existing) and (Existing <> '') then
      RegWriteExpandStringValue(HKCU, RunKey, 'Yume', '"' + ExpandConstant('{app}\Yume.exe') + '" --startup');
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var Existing: String;
begin
  if CurUninstallStep = usUninstall then
    if RegQueryStringValue(HKCU, RunKey, 'Yume', Existing) then
      if Pos(Lowercase(ExpandConstant('{app}\Yume.exe')), Lowercase(Existing)) > 0 then
        RegDeleteValue(HKCU, RunKey, 'Yume');
end;
