# Sistem Paneli

Oyun oynarken yan ekranda FPS'i, işlemci ve ekran kartı sıcaklığını, belleği, fanları ve ağı gösteren masaüstü widget'ları. Widget'lar duvar kâğıdının (Wallpaper Engine dahil) üstünde, açık pencerelerin altında durur; arkaları bulanık camdır.

- **Tek kurulum dosyası:** Kurulum internetten bir şey indirmez. Gereken her şey içindedir: .NET Framework 4.8 Windows 10/11'de zaten var.
- **Gösterilenler:**
  - Saat ve tarih.
  - İşlemci: sıcaklık, yük, frekans, güç ve grafik.
  - Ekran kartı: sıcaklık, yük, frekans, güç, VRAM ve grafik.
  - FPS: anlık, %1 düşük, kare süresi, oyunun adı ve grafik.
  - Bellek.
  - Pompa ve fanlar, disk sıcaklıkları.
  - Ağ hızı.
- **Donanımı kendisi tanır:** Sensörler ada ve türe göre seçilir, böylece farklı bilgisayarlarda da çalışır. Seçimin özeti günlük dosyasına yazılır.
- **Düzenleme:** Widget'lar sürüklenir, birbirine hizalanır, açılıp kapatılır. Vurgu rengi, kart koyuluğu ve bulanıklık ayarlardan değişir.

## Kurulum

`SistemPaneli-Kurulum-<sürüm>.exe` dosyasını çalıştır. Kurulum şunları yapar:

1. Uygulamayı `C:\Program Files\SistemPaneli` klasörüne kurar.
2. Kurulu değilse [PawnIO](https://github.com/namazso/PawnIO) sürücüsünü kurar. İşlemci sıcaklığı ve anakart fanları bu imzalı sürücüyle okunur.
3. `SistemPaneli` zamanlanmış görevini oluşturur. Uygulama sensörler ve FPS için yönetici yetkisi ister; bu görev sayesinde her açılışta UAC sormadan başlar.

Kurulum dosyası imzasız olduğu için SmartScreen uyarısı çıkabilir: "Ek bilgi" > "Yine de çalıştır".

## Derleme

Gerekenler:

- [.NET SDK](https://dotnet.microsoft.com/download) 8 veya üstü. Proje `net48` hedefliyor ve SDK ile derleniyor.
- [Inno Setup 7](https://jrsoftware.org/isdl.php). Yalnızca kurulum dosyası için gerekir.

```powershell
# Derle ve kurulum dosyasını üret (cikti\SistemPaneli-Kurulum-<sürüm>.exe)
powershell -ExecutionPolicy Bypass -File derle.ps1

# Yalnızca uygulamayı derle
dotnet build src\SistemPaneli\SistemPaneli.csproj -c Debug
```

Geliştirirken yönetici yetkisi olmadan denemek için uygulamayı `--yetkisiz` ile başlat. Bu kipte sıcaklık, fanlar ve FPS okunmaz; arayüz çalışır.

```powershell
src\SistemPaneli\bin\Debug\net48\SistemPaneli.exe --yetkisiz
```

Sürüm numarası `src/SistemPaneli/SistemPaneli.csproj` içindeki `<Version>` alanında. Üçüncü taraf dosyalar `bagimliliklar/` klasöründe; kaynakları ve özetleri o klasörün README'sinde.

## Nasıl çalışıyor

| Parça | Dosya | Açıklama |
|---|---|---|
| Açılış ve yetki | `App.xaml.cs`, `Cekirdek/Gorev.cs` | Yönetici değilse zamanlanmış görevi çalıştırıp çıkar. Görev yoksa UAC sorar; UAC reddedilirse sınırlı kipte açılır. |
| Sensörler | `Cekirdek/SensorServisi.cs`, `Cekirdek/DonanimTanima.cs` | LibreHardwareMonitor ayrı bir iş parçacığında saniyede bir okunur. Seçim turlarında bütün donanım okunur, diğer turlarda yalnızca gösterilenler. |
| FPS | `Cekirdek/FpsOlcer.cs` | PresentMon'un CSV çıktısı okunur. Ön plandaki ya da en yüksek FPS'li süreç oyun sayılır; listedeki tarayıcı, Discord gibi programlar hariç tutulur. PresentMon bir iş nesnesine bağlıdır, uygulama kapanınca o da kapanır. |
| Widget penceresi | `Widgetlar/WidgetPenceresi.cs` | Saydam, kenarlıksız WPF penceresi. Bulanıklık için `SetWindowCompositionAttribute` kullanılır, çünkü DWM'nin resmi arka planı pencere etkin değilken kayboluyor. Köşeler Windows 11'in kendi yuvarlamasıyla kırpılır. Pencere hep en altta durur, odak çalmaz, kilitliyken tıklamaları arkasına geçirir. |
| Tasarım | `Arayuz/` | Halka ölçer, çizgi grafik, çubuk, harf aralıklı başlık; renkler `Tema.cs` içinde. |
| Ayarlar | `Ayar/Ayarlar.cs`, `AyarPenceresi.xaml` | `%APPDATA%\SistemPaneli\ayarlar.json` |
| Günlük | `Cekirdek/Gunluk.cs` | `%LOCALAPPDATA%\SistemPaneli\gunluk.txt` |
| Kurulum | `kurulum/SistemPaneli.iss` | Inno Setup betiği. PawnIO'yu `-install -silent` ile kurar, görevi `--gorev-kur` ile oluşturur. |

Komut satırı seçenekleri:

- `--yetkisiz`: Yönetici yetkisi istemeden başlatır.
- `--gorev-kur`: Zamanlanmış görevi kurar; yönetici gerekir.
- `--gorev-sil`: Zamanlanmış görevi siler.
- `--kapat`: Çalışan kopyayı kapatır.

## Bilinen sınırlar

- Win+D (masaüstünü göster) widget'ları da gizler.
- Çekirdek seviyesinde hile koruması olan oyunlarla (Vanguard, FACEIT) uyum denenmedi.
- Windows 10'da kart köşeleri yuvarlak değil.

## Lisans

Uygulamanın kendi lisansı henüz seçilmedi. Kullanılan bileşenlerin lisansları `kurulum/lisanslar/` klasöründe:

- LibreHardwareMonitor: MPL-2.0.
- PresentMon: MIT.
- PawnIO: GPL-2.0.
