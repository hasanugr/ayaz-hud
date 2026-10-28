# Üçüncü taraf bileşenler

Bu klasördeki dosyalar derleme ve kurulum için depoya eklendi; internetten indirmeye gerek kalmadan proje derlenir ve kurulum dosyası üretilir. Hepsi resmi kaynaklarından alındı ve özetleri aşağıda.

| Bileşen | Sürüm | Lisans | Kaynak |
|---|---|---|---|
| LibreHardwareMonitorLib ve bağımlılıkları | 0.9.6 | MPL-2.0 (HidSharp: Apache-2.0; System.*: MIT) | https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/releases/tag/v0.9.6 |
| PresentMon (konsol) | 2.6.0 | MIT | https://github.com/GameTechDev/PresentMon/releases/tag/v2.6.0 |
| PawnIO (sürücü kurulumu) | 2.2.0 | GPL-2.0 | https://github.com/namazso/PawnIO.Setup/releases |

- **LibreHardwareMonitor**: sensörleri okuyan kütüphane. `net472` derlemesi kullanılıyor (uygulama .NET Framework 4.8 hedefliyor). DLL'ler 0.9.6 sürümünün resmi zip paketinden çıkarıldı.
- **PresentMon**: oyunların FPS'ini Windows'un ETW olaylarından ölçer. Intel tarafından imzalı.
- **PawnIO**: LibreHardwareMonitor'ın işlemci sıcaklığı ve anakart fanları için kullandığı imzalı çekirdek sürücüsü (eski WinRing0'ın yerini aldı). Kurulum dosyası, PawnIO kurulu değilse onu sessizce kurar.

## SHA-256 özetleri

```
6EBC194316536BA61AF5BE24508AD9FCBB2ECC685E716C12E787C79530F66BF0  LibreHardwareMonitor/LibreHardwareMonitorLib.dll
CAFB93AFCC8D8A367E21F619673D05C06887D8964867FED1371F02DED1CD3E23  LibreHardwareMonitor/BlackSharp.Core.dll
1ACBF51B3C10C51C986CF43021680D34A2E38D9A5BA652BCFA9A1B5F7FC09800  LibreHardwareMonitor/DiskInfoToolkit.dll
D86690EFDE30EA9179F669320F39148853793B743A98B531AFEAF30598E22F54  LibreHardwareMonitor/HidSharp.dll
B6882354C7C8EC186617E421507743DBFAE09C5C1FC24CEF76A1D0C0C26651DE  LibreHardwareMonitor/RAMSPDToolkit-NDD.dll
2D78D770C9CB997199154AE8C018B9F1D1EFBC86729F7264DDE6DBAD2A12CAC3  LibreHardwareMonitor/System.Buffers.dll
D5E8E4866F9CFA66F7765660F84B210198893E55335487AFE5EBDA342C0E913D  LibreHardwareMonitor/System.Memory.dll
20C2FA81B8C70D651099D762954F285FD4F942E63B2D7217C145DAB8D4B2F4C9  LibreHardwareMonitor/System.Numerics.Vectors.dll
08CBD7278B66F1E68425A82D4B97181A4130D93E3DD91831407ABA7212CCDACF  LibreHardwareMonitor/System.Runtime.CompilerServices.Unsafe.dll
B2A706BC6AD475749E3B7E3409263AA1E6906D45BDCF993F6DBC0F660188F1AF  PresentMon/PresentMon-2.6.0-x64.exe
1F519A22E47187F70A1379A48CA604981C4FCF694F4E65B734AAA74A9FBA3032  PawnIO/PawnIO_setup.exe
```

PowerShell ile doğrulamak için:

```powershell
Get-ChildItem bagimliliklar -Recurse -File -Exclude *.md | Get-FileHash -Algorithm SHA256
```

## Güncellerken

1. Yeni sürümü resmi sayfasından indir, imzasını (`Get-AuthenticodeSignature`) ya da yayımlanan özetini kontrol et.
2. Dosyayı buraya koy, yukarıdaki tabloyu ve özetleri güncelle.
3. PresentMon'un dosya adı değişirse `src/SistemPaneli/SistemPaneli.csproj`, `App.xaml.cs` ve `kurulum/SistemPaneli.iss` içindeki adı da değiştir.
