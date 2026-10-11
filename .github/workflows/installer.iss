[Setup]
AppName=Hotyn Helper
AppVersion=1.0.0
AppPublisher=Hotyn
DefaultDirName={autopf}\Hotyn Helper
DefaultGroupName=Hotyn Helper
OutputDir=installer_output
OutputBaseFilename=HotynHelper-Setup
Compression=lzma
SolidCompression=yes
SetupIconFile=crystal.ico
UninstallDisplayIcon={app}\HotynHelper.exe
WizardStyle=modern
MinVersion=10.0.15063
PrivilegesRequired=lowest
DisableProgramGroupPage=yes

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Files]
Source: "publish\HotynHelper.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Hotyn Helper"; Filename: "{app}\HotynHelper.exe"; IconFilename: "{app}\HotynHelper.exe"
Name: "{group}\Удалить Hotyn Helper"; Filename: "{uninstallexe}"
Name: "{userdesktop}\Hotyn Helper"; Filename: "{app}\HotynHelper.exe"; IconFilename: "{app}\HotynHelper.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительные задачи:"

[Run]
Filename: "{app}\HotynHelper.exe"; Description: "Запустить Hotyn Helper"; Flags: nowait postinstall skipifsilent
