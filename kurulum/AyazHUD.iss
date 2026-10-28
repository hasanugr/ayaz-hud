; Ayaz HUD kurulum betiği (Inno Setup 7)
; Derlemek için depo kökünde: powershell -ExecutionPolicy Bypass -File derle.ps1
; Dosya UTF-8 (BOM'lu) kaydedilmeli; Türkçe karakterler ancak öyle doğru okunur.

#ifndef Surum
  #define Surum "0.1.0"
#endif
#define Ad "Ayaz HUD"
#define Exe "AyazHUD.exe"
#define Kaynak "..\src\AyazHUD\bin\Release\net48"

[Setup]
AppId={{71296037-E8D5-4401-9E47-1654F3FC98CA}
AppName={#Ad}
AppVersion={#Surum}
AppVerName={#Ad} {#Surum}
AppPublisher=Hasan Uğur
VersionInfoVersion={#Surum}
VersionInfoDescription={#Ad} kurulumu
DefaultDirName={autopf}\AyazHUD
; Aynı AppId eski adla (Sistem Paneli) kurulmuş olabilir; yeni klasöre kurulsun
UsePreviousAppDir=no
DisableProgramGroupPage=yes
DisableDirPage=auto
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Bulanık cam için Windows 10 1803 ve sonrası
MinVersion=10.0.17134
OutputDir=..\cikti
OutputBaseFilename=AyazHUD-Kurulum-{#Surum}
SetupIconFile=..\src\AyazHUD\Varliklar\simge.ico
UninstallDisplayIcon={app}\{#Exe}
UninstallDisplayName={#Ad}
WizardStyle=modern
Compression=lzma2/max
SolidCompression=yes
; Dil seçimi: varsayılan Türkçe (listenin ilki); İngilizce seçilebilir ve uygulamanın dili de o olur
ShowLanguageDialog=yes
LanguageDetectionMethod=none
CloseApplications=no
; Kaldırırken kullanıcının ayar klasörü silinebilsin
UsedUserAreasWarning=no

[Languages]
Name: "tr"; MessagesFile: "compiler:Languages\Turkish.isl"; InfoBeforeFile: "bilgi.txt"
Name: "en"; MessagesFile: "compiler:Default.isl"; InfoBeforeFile: "bilgi-en.txt"

[CustomMessages]
tr.PawnIOKuruluyor=PawnIO sürücüsü kuruluyor...
en.PawnIOKuruluyor=Installing the PawnIO driver...
tr.GorevKuruluyor=Başlangıç görevi oluşturuluyor...
en.GorevKuruluyor=Creating the startup task...
tr.SimdiBaslat=Ayaz HUD'u şimdi başlat
en.SimdiBaslat=Launch Ayaz HUD now
tr.AyarlarSilinsin=Widget konumları, renkler ve günlük dosyası da silinsin mi?
en.AyarlarSilinsin=Also delete widget positions, colors and the log file?
tr.PawnIOKaldirilmadi=PawnIO sürücüsü kaldırılmadı; başka programlar (LibreHardwareMonitor, FanControl gibi) da kullanıyor olabilir. Kaldırmak istersen Windows Ayarlar > Uygulamalar > Yüklü uygulamalar listesinden "PawnIO"yu kaldırabilirsin.
en.PawnIOKaldirilmadi=The PawnIO driver was not removed; other apps (such as LibreHardwareMonitor or FanControl) may be using it. To remove it, uninstall "PawnIO" from Windows Settings > Apps > Installed apps.

[Files]
Source: "{#Kaynak}\{#Exe}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Kaynak}\{#Exe}.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Kaynak}\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Kaynak}\PresentMon-2.6.0-x64.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "lisanslar\*"; DestDir: "{app}\lisanslar"; Flags: ignoreversion
Source: "..\bagimliliklar\PawnIO\PawnIO_setup.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: PawnIOGerekli

[InstallDelete]
; Uygulamanın eski adı "Sistem Paneli" idi; o sürümden geçişte eski klasör ve kısayol kalmasın
Type: filesandordirs; Name: "{autopf}\SistemPaneli"
Type: files; Name: "{autoprograms}\Sistem Paneli.lnk"

[Icons]
; Başlat menüsünden açılınca uygulama zamanlanmış görevi çalıştırır; UAC sorulmaz
Name: "{autoprograms}\{#Ad}"; Filename: "{app}\{#Exe}"

[Run]
Filename: "{tmp}\PawnIO_setup.exe"; Parameters: "-install -silent"; StatusMsg: "{cm:PawnIOKuruluyor}"; Flags: waituntilterminated; Check: PawnIOGerekli
Filename: "{app}\{#Exe}"; Parameters: "--gorev-kur --dil {language}"; StatusMsg: "{cm:GorevKuruluyor}"; Flags: runhidden waituntilterminated
Filename: "{app}\{#Exe}"; Description: "{cm:SimdiBaslat}"; Flags: postinstall nowait skipifsilent runasoriginaluser

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
procedure UygulamayiKapat(const Klasor, ExeAdi: String);
var
  Kod: Integer;
begin
  if FileExists(Klasor + '\' + ExeAdi) then
  begin
    Exec(Klasor + '\' + ExeAdi, '--kapat', '', SW_HIDE, ewWaitUntilTerminated, Kod);
    Sleep(2500);
  end;
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /T /IM ' + ExeAdi, '', SW_HIDE, ewWaitUntilTerminated, Kod);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  Kod: Integer;
begin
  UygulamayiKapat(ExpandConstant('{app}'), UygulamaExe);
  // Eski adla ("Sistem Paneli") kurulmuş sürüm: kapat ve başlangıç görevini sil; klasörü [InstallDelete] siler
  if FileExists(ExpandConstant('{autopf}\SistemPaneli\SistemPaneli.exe')) then
  begin
    UygulamayiKapat(ExpandConstant('{autopf}\SistemPaneli'), 'SistemPaneli.exe');
    Exec(ExpandConstant('{sys}\schtasks.exe'), '/Delete /TN SistemPaneli /F', '', SW_HIDE, ewWaitUntilTerminated, Kod);
  end;
  Result := '';
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Kod: Integer;
begin
  if CurUninstallStep = usUninstall then
  begin
    UygulamayiKapat(ExpandConstant('{app}'), UygulamaExe);
    Exec(ExpandConstant('{app}\' + UygulamaExe), '--gorev-sil', '', SW_HIDE, ewWaitUntilTerminated, Kod);
  end;
  if (CurUninstallStep = usPostUninstall) and not UninstallSilent then
  begin
    if MsgBox(CustomMessage('AyarlarSilinsin'), mbConfirmation, MB_YESNO) = IDYES then
    begin
      DelTree(ExpandConstant('{userappdata}\AyazHUD'), True, True, True);
      DelTree(ExpandConstant('{localappdata}\AyazHUD'), True, True, True);
    end;
    MsgBox(CustomMessage('PawnIOKaldirilmadi'), mbInformation, MB_OK);
  end;
end;
