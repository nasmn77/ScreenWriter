#define MyAppName "Screen Writer"
#define MyAppVersion "1.0"
#define MyAppPublisher "nasmn77"
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
OutputBaseFilename=Setup_ScreenWriter_v{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=C:\MyProjects\Screen Writer\ScreenWriter\Assets\icon.ico

[Languages]
Name: "arabic"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "إنشاء اختصار على سطح المكتب"; GroupDescription: "اختصارات إضافية:"; Flags: unchecked
Name: "startupicon"; Description: "تشغيل عند بدء Windows"; GroupDescription: "اختصارات إضافية:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\{#MyAppExeName}";         DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\D3DCompiler_47_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\PenImc_cor3.dll";         DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\PresentationNative_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\vcruntime140_cor3.dll";   DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\wpfgfx_cor3.dll";         DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\Assets\icon.ico";         DestDir: "{app}\Assets"; Flags: ignoreversion

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
