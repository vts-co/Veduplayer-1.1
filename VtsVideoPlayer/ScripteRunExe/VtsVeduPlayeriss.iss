[Setup]
AppName=VtsVeduPlayer
AppVersion=1.0
DefaultDirName={localappdata}\VtsVeduPlayer
DisableProgramGroupPage=yes
OutputDir=.
OutputBaseFilename=VtsVeduPlayer
Compression=lzma
SolidCompression=yes
PrivilegesRequired=lowest
SetupIconFile=favicon.ico

[Files]
Source: "VtsVideoPlayer.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "AWSSDK.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "AWSSDK.S3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "AxInterop.WMPLib.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Interop.WMPLib.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "setup.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "TLMS.ObjectStorageS3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Microsoft.Web.WebView2.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Microsoft.Web.WebView2.WinForms.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Microsoft.Web.WebView2.Wpf.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "WebView2Loader.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "VeduPlayer.msi"; DestDir: "{app}"; Flags: ignoreversion

[Run]
Filename: "{app}\VtsVideoPlayer.exe"; Description: "تشغيل VtsVeduPlayer"; Flags: nowait postinstall skipifsilent

[Icons]
; لا يتم إنشاء اختصارات

[Registry]
Root: HKCU; Subkey: "Software\Classes\vtsplayer"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\vtsplayer"; ValueType: string; ValueName: ""; ValueData: "URL:VTS Video Player Protocol"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\vtsplayer"; ValueType: string; ValueName: "URL Protocol"; ValueData: ""
Root: HKCU; Subkey: "Software\Classes\vtsplayer\shell\open\command"; ValueType: string; ValueData: """{app}\VtsVideoPlayer.exe"" ""%1"""

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\VtsVeduPlayer"

[Code]
function IsWebView2Installed(): Boolean;
begin
  Result := DirExists('C:\Program Files (x86)\Microsoft\EdgeWebView\Application') or
            DirExists('C:\Program Files\Microsoft\EdgeWebView\Application');
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ErrorCode: Integer;
begin
  if (CurStep = ssPostInstall) and (not IsWebView2Installed()) then
  begin
    MsgBox('WebView2 غير مثبت على هذا الجهاز. سيتم فتح صفحة التثبيت الآن.', mbInformation, MB_OK);
    ShellExec('', 'https://go.microsoft.com/fwlink/p/?LinkId=2124703', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
  end;
end;
