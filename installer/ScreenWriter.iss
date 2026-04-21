#define MyAppName "Screen Writer"
#define MyAppVersion "1.0"
#define MyAppPublisher "Naser Almadi"
#define MyAppURL "https://github.com/nasmn77/ScreenWriter"
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

[Languages]
Name: "arabic"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "إنشاء اختصار على سطح المكتب"; GroupDescription: "اختصارات إضافية:"; Flags: unchecked
Name: "startupicon"; Description: "تشغيل عند بدء Windows"; GroupDescription: "اختصارات إضافية:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}";          Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\icon.ico"
Name: "{group}\إلغاء التثبيت";         Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}";    Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\icon.ico"; Tasks: desktopicon

[Registry]
; تشغيل عند بدء Windows
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; \
  ValueType: string; ValueName: "{#MyAppName}"; \
  ValueData: """{app}\{#MyAppExeName}"""; \
  Flags: uninsdeletevalue; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "تشغيل {#MyAppName} الآن"; \
  Flags: nowait postinstall skipifsilent

[Code]
const
  UninstallKey = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{A3F2B1C4-7E5D-4F8A-9B2C-1D3E5F6A7B8C}_is1';

function GetUninstallString(): String;
var
  sVal: String;
begin
  sVal := '';
  if not RegQueryStringValue(HKCU, UninstallKey, 'UninstallString', sVal) then
    RegQueryStringValue(HKLM, UninstallKey, 'UninstallString', sVal);
  Result := sVal;
end;

function InitializeSetup(): Boolean;
var
  sUninstall: String;
  iCode: Integer;
  iBtn: Integer;
begin
  Result := True;

  sUninstall := GetUninstallString();
  if sUninstall = '' then Exit;

  iBtn := MsgBox(
    '{#MyAppName} مثبَّت بالفعل على هذا الجهاز.' + #13#10#13#10 +
    'اضغط "نعم" لإصلاح التطبيق وإعادة تثبيته.' + #13#10 +
    'اضغط "لا" لإلغاء التثبيت نهائياً.' + #13#10 +
    'اضغط "إلغاء" للخروج بدون تغيير.',
    mbConfirmation, MB_YESNOCANCEL);

  if iBtn = IDYES then
  begin
    Result := True;
  end
  else if iBtn = IDNO then
  begin
    Exec(RemoveQuotes(sUninstall), '/SILENT', '', SW_SHOW, ewWaitUntilTerminated, iCode);
    Result := False;
  end
  else
  begin
    Result := False;
  end;
end;
