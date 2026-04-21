#define MyAppName "Screen Writer"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Naser Almadi"
#define MyAppURL "https://docs.naseralmadi.cloud/apps/ar/"
#define MyAppExeName "ScreenWriter.exe"
#define PublishDir "..\publish"

[Setup]
AppId={{A3F2B1C4-7E5D-4F8A-9B2C-1D3E5F6A7B8C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=..\installer_output
OutputBaseFilename=Setup_ScreenWriter
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=..\ScreenWriter\Assets\icon.ico
UninstallDisplayIcon={app}\ScreenWriter.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked
Name: "startupicon"; Description: "Run at Windows startup"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}";       Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\icon.ico"
Name: "{group}\Uninstall";          Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\icon.ico"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; \
  ValueType: string; ValueName: "{#MyAppName}"; \
  ValueData: """{app}\{#MyAppExeName}"""; \
  Flags: uninsdeletevalue; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName} now"; \
  Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
const
  UninstallKey = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{A3F2B1C4-7E5D-4F8A-9B2C-1D3E5F6A7B8C}_is1';
  RunKey       = 'SOFTWARE\Microsoft\Windows\CurrentVersion\Run';

var
  AlreadyInstalledPage : TWizardPage;
  RbRepair             : TRadioButton;
  RbUninstall          : TRadioButton;

// Windows API
function ExitProcess(uExitCode: UINT): BOOL; external 'ExitProcess@kernel32.dll stdcall';

// Read UninstallString from registry
function GetUninstallString(): String;
var
  sVal: String;
begin
  sVal := '';
  if not RegQueryStringValue(HKCU, UninstallKey, 'UninstallString', sVal) then
    RegQueryStringValue(HKLM, UninstallKey, 'UninstallString', sVal);
  Result := sVal;
end;

// Clean up registry after uninstall
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    RegDeleteValue(HKCU, RunKey, '{#MyAppName}');
    RegDeleteValue(HKLM, RunKey, '{#MyAppName}');
    RegDeleteKeyIncludingSubkeys(HKCU, UninstallKey);
    RegDeleteKeyIncludingSubkeys(HKLM, UninstallKey);
  end;
end;

// Create custom Wizard page when app is already installed
procedure InitializeWizard();
var
  sUninstall : String;
  Lbl        : TLabel;
begin
  sUninstall := GetUninstallString();
  if sUninstall = '' then Exit;

  AlreadyInstalledPage := CreateCustomPage(
    wpWelcome,
    '{#MyAppName} is already installed',
    'Choose what you would like to do:');

  Lbl          := TLabel.Create(AlreadyInstalledPage);
  Lbl.Parent   := AlreadyInstalledPage.Surface;
  Lbl.Caption  := '{#MyAppName} is already installed on this computer. What would you like to do?';
  Lbl.Left     := 0;
  Lbl.Top      := 0;
  Lbl.Width    := AlreadyInstalledPage.SurfaceWidth;
  Lbl.WordWrap := True;
  Lbl.AutoSize := True;

  RbRepair         := TRadioButton.Create(AlreadyInstalledPage);
  RbRepair.Parent  := AlreadyInstalledPage.Surface;
  RbRepair.Caption := 'Repair / Reinstall';
  RbRepair.Left    := 0;
  RbRepair.Top     := Lbl.Top + Lbl.Height + 20;
  RbRepair.Width   := AlreadyInstalledPage.SurfaceWidth;
  RbRepair.Checked := True;

  RbUninstall         := TRadioButton.Create(AlreadyInstalledPage);
  RbUninstall.Parent  := AlreadyInstalledPage.Surface;
  RbUninstall.Caption := 'Uninstall completely and remove all files';
  RbUninstall.Left    := 0;
  RbUninstall.Top     := RbRepair.Top + RbRepair.Height + 10;
  RbUninstall.Width   := AlreadyInstalledPage.SurfaceWidth;
end;

// Handle Next button click
function NextButtonClick(CurPageID: Integer): Boolean;
var
  sUninstall : String;
  sAppDir    : String;
  iCode      : Integer;
begin
  Result := True;

  if (AlreadyInstalledPage <> nil) and (CurPageID = AlreadyInstalledPage.ID) then
  begin
    if RbUninstall.Checked then
    begin
      if MsgBox(
        '{#MyAppName} will be completely removed from your computer,' +
        ' including all files and settings.' +
        Chr(13) + Chr(10) + Chr(13) + Chr(10) + 'Are you sure?',
        mbConfirmation, MB_YESNO) = IDYES then
      begin
        sUninstall := GetUninstallString();

        // Save install location before uninstall
        RegQueryStringValue(HKCU, UninstallKey, 'InstallLocation', sAppDir);
        if sAppDir = '' then
          RegQueryStringValue(HKLM, UninstallKey, 'InstallLocation', sAppDir);

        // Stop the app if running
        Exec('taskkill.exe', '/F /IM {#MyAppExeName}', '', SW_HIDE,
             ewWaitUntilTerminated, iCode);

        // Run uninstaller silently
        Exec(RemoveQuotes(sUninstall), '/VERYSILENT /SUPPRESSMSGBOXES',
             '', SW_HIDE, ewWaitUntilTerminated, iCode);

        // Delete folder if it still exists
        if (sAppDir <> '') and DirExists(sAppDir) then
          DelTree(sAppDir, True, True, True);

        // Clean up registry
        RegDeleteValue(HKCU, RunKey, '{#MyAppName}');
        RegDeleteValue(HKLM, RunKey, '{#MyAppName}');
        RegDeleteKeyIncludingSubkeys(HKCU, UninstallKey);
        RegDeleteKeyIncludingSubkeys(HKLM, UninstallKey);

        MsgBox('{#MyAppName} has been completely uninstalled.', mbInformation, MB_OK);
        ExitProcess(0);
      end;
      Result := False;
    end;
  end;
end;