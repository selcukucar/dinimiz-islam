#ifndef Surum
  #define Surum "1.0.2"
#endif
#ifndef Yayin
  #define Yayin "..\portable"
#endif
#ifndef Cikti
  #define Cikti "..\artifacts"
#endif
[Setup]
AppId={{48C65683-082E-4E03-9D65-A6CA5852ACBB}
AppName=Dinimiz İslam
AppVersion={#Surum}
AppVerName=Dinimiz İslam {#Surum}
VersionInfoVersion={#Surum}.0
DefaultDirName={localappdata}\Programs\DinimizIslam
DefaultGroupName=Dinimiz İslam
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
OutputDir={#Cikti}
OutputBaseFilename=DinimizIslam-WPF-{#Surum}-Kurulum-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName=Dinimiz İslam
UninstallDisplayIcon={app}\DinimizIslam.exe
CloseApplications=yes
RestartApplications=no
SetupLogging=yes
Uninstallable=not TestKurulumu
LicenseFile=..\LICENSE
[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
[Tasks]
Name: "desktopicon"; Description: "Masaüstü kısayolu oluştur"; Flags: unchecked
[Files]
Source: "{#Yayin}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{userprograms}\Dinimiz İslam"; Filename: "{app}\DinimizIslam.exe"; Check: not TestKurulumu
Name: "{userdesktop}\Dinimiz İslam"; Filename: "{app}\DinimizIslam.exe"; Tasks: desktopicon; Check: not TestKurulumu
[Run]
Filename: "{app}\DinimizIslam.exe"; Description: "Dinimiz İslam uygulamasını aç"; Flags: nowait postinstall skipifsilent
[Code]
function TestKurulumu: Boolean;
begin
  Result := ExpandConstant('{param:TestKurulumu|0}') = '1';
end;
