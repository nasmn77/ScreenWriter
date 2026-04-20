#define MyAppName "Screen Writer"
#define MyAppVersion "1.0"
#define MyAppPublisher "nasmn77"
#define MyAppURL "https://github.com/nasmn77/ScreenWriter"
#define MyAppExeName "ScreenWriter.exe"
#define PublishDir "..\publish_small"

[Setup]
AppId={{B4G3C2D5-8F6E-5G9B-0C3D-2E4F6G7H8I9J}
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
OutputBaseFilename=Setup_ScreenWriter_v{#MyAppVersion}_NoNet
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "arabic"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "إنشاء اختصار على سطح المكتب"; GroupDescription: "اختصارات إضافية:"; Flags: unchecked
Name: "startupicon"; Description: "تشغيل عند بدء Windows"; GroupDescription: "اختصارات إضافية:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\{#MyAppExeName}";      DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\ScreenWriter.dll";     DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\ScreenWriter.deps.json";       DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\ScreenWriter.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}";          Filename: "{app}\{#MyAppExeName}"
Name: "{group}\إلغاء التثبيت";         Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}";    Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; تشغيل عند بدء Windows
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; \
  ValueType: string; ValueName: "{#MyAppName}"; \
  ValueData: """{app}\{#MyAppExeName}"""; \
  Flags: uninsdeletevalue; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "تشغيل {#MyAppName} الآن"; \
  Flags: nowait postinstall skipifsilent
