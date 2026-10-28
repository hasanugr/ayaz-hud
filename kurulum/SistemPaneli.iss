; Sistem Paneli kurulum betiği (Inno Setup 7)
; Derlemek için depo kökünde: powershell -ExecutionPolicy Bypass -File derle.ps1
; Dosya UTF-8 (BOM'lu) kaydedilmeli; Türkçe karakterler ancak öyle doğru okunur.

#ifndef Surum
  #define Surum "0.1.0"
#endif
#define Ad "Sistem Paneli"
#define Exe "SistemPaneli.exe"
#define Kaynak "..\src\SistemPaneli\bin\Release\net48"

[Setup]
AppId={{71296037-E8D5-4401-9E47-1654F3FC98CA}
AppName={#Ad}
AppVersion={#Surum}
AppVerName={#Ad} {#Surum}
AppPublisher=Hasan Uğur
VersionInfoVersion={#Surum}
VersionInfoDescription={#Ad} kurulumu
DefaultDirName={autopf}\SistemPaneli
DisableProgramGroupPage=yes
DisableDirPage=auto
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Bulanık cam için Windows 10 1803 ve sonrası
MinVersion=10.0.17134
OutputDir=..\cikti
OutputBaseFilename=SistemPaneli-Kurulum-{#Surum}
SetupIconFile=..\src\SistemPaneli\Varliklar\simge.ico
UninstallDisplayIcon={app}\{#Exe}
UninstallDisplayName={#Ad}
InfoBeforeFile=bilgi.txt
WizardStyle=modern
Compression=lzma2/max
SolidCompression=yes
ShowLanguageDialog=no
CloseApplications=no
; Kaldırırken kullanıcının ayar klasörü silinebilsin
UsedUserAreasWarning=no

[Languages]
Name: "tr"; MessagesFile: "compiler:Languages\Turkish.isl"

[Files]
Source: "{#Kaynak}\{#Exe}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Kaynak}\{#Exe}.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Kaynak}\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Kaynak}\PresentMon-2.6.0-x64.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "lisanslar\*"; DestDir: "{app}\lisanslar"; Flags: ignoreversion
Source: "..\bagimliliklar\PawnIO\PawnIO_setup.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: PawnIOGerekli

[Icons]
; Başlat menüsünden açılınca uygulama zamanlanmış görevi çalıştırır; UAC sorulmaz
Name: "{autoprograms}\{#Ad}"; Filename: "{app}\{#Exe}"

[Run]
Filename: "{tmp}\PawnIO_setup.exe"; Parameters: "-install -silent"; StatusMsg: "PawnIO sürücüsü kuruluyor..."; Flags: waituntilterminated; Check: PawnIOGerekli
Filename: "{app}\{#Exe}"; Parameters: "--gorev-kur"; StatusMsg: "Başlangıç görevi oluşturuluyor..."; Flags: runhidden waituntilterminated
Filename: "{app}\{#Exe}"; Description: "{#Ad}'ni şimdi başlat"; Flags: postinstall nowait skipifsilent runasoriginaluser

[Code]
const
  UygulamaExe = '{#Exe}';
  PawnIOSurumu = '2.2.0.0';

// PawnIO hiç yoksa ya da eski sürümse kurulur
function PawnIOGerekli: Boolean;
var
  Surum: String;
  Kurulu, Gereken: Int64;
begin
  Result := True;
  if RegQueryStringValue(HKLM64, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO', 'DisplayVersion', Surum) then
    if StrToVersion(Surum, Kurulu) and StrToVersion(PawnIOSurumu, Gereken) then
      Result := ComparePackedVersion(Kurulu, Gereken) < 0;
end;

// Çalışan uygulamaya kapanmasını söyler (sensör sürücüsünü düzgün bıraksın), kapanmazsa zorla kapatır
procedure UygulamayiKapat(const Klasor: String);
var
  Kod: Integer;
begin
  if FileExists(Klasor + '\' + UygulamaExe) then
  begin
    Exec(Klasor + '\' + UygulamaExe, '--kapat', '', SW_HIDE, ewWaitUntilTerminated, Kod);
    Sleep(2500);
  end;
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /T /IM ' + UygulamaExe, '', SW_HIDE, ewWaitUntilTerminated, Kod);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  UygulamayiKapat(ExpandConstant('{app}'));
  Result := '';
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Kod: Integer;
begin
  if CurUninstallStep = usUninstall then
  begin
    UygulamayiKapat(ExpandConstant('{app}'));
    Exec(ExpandConstant('{app}\' + UygulamaExe), '--gorev-sil', '', SW_HIDE, ewWaitUntilTerminated, Kod);
  end;
  if (CurUninstallStep = usPostUninstall) and not UninstallSilent then
  begin
    if MsgBox('Widget konumları, renkler ve günlük dosyası da silinsin mi?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      DelTree(ExpandConstant('{userappdata}\SistemPaneli'), True, True, True);
      DelTree(ExpandConstant('{localappdata}\SistemPaneli'), True, True, True);
    end;
    MsgBox('PawnIO sürücüsü kaldırılmadı; başka programlar (LibreHardwareMonitor, FanControl gibi) da kullanıyor olabilir. ' +
           'Kaldırmak istersen Windows Ayarlar > Uygulamalar > Yüklü uygulamalar listesinden "PawnIO"yu kaldırabilirsin.', mbInformation, MB_OK);
  end;
end;
