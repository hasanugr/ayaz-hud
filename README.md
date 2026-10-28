# Ayaz HUD

Oyun oynarken yan ekranda FPS'i, işlemci ve ekran kartı sıcaklığını, belleği, fanları ve ağı gösteren masaüstü widget'ları. Widget'lar duvar kâğıdının (Wallpaper Engine dahil) üstünde, açık pencerelerin altında durur; arkaları bulanık camdır.

- **Tek kurulum dosyası:** Kurulum internetten bir şey indirmez. Gereken her şey içindedir: .NET Framework 4.8 Windows 10/11'de zaten var.
- **Gösterilenler:**
  - Saat ve tarih.
  - İşlemci: sıcaklık, yük, frekans, güç ve grafik.
  - Ekran kartı: sıcaklık, yük, frekans, güç, VRAM ve grafik.
  - FPS: anlık, en düşük %1, kare süresi, gecikme, oyunun adı ve grafik.
  - Bellek.
  - Pompa ve fanlar, disk sıcaklıkları.
  - Ağ hızı.
- **Donanımı kendisi tanır:** Sensörler ada ve türe göre seçilir, böylece farklı bilgisayarlarda da çalışır. Seçimin özeti günlük dosyasına yazılır.
- **Düzenleme:** Widget'lar sürüklenir, birbirine hizalanır, açılıp kapatılır.
- **Görünüm:**
  - Dört kart stili var: buzlu cam, akrilik, koyu ve şeffaf.
  - Vurgu rengi, kart buzlanması (hafif, orta, güçlü) ve kart koyuluğu bütün widget'lar için ya da her widget için ayrı seçilebilir.
  - Widget'ların olduğu ekranın tamamı istenirse buzlanır ve/veya kararır ("arka plan" ayarı).
  - Ayarlar penceresinde her seçeneğin önizlemesi görünür.
- **Dil:** Türkçe ve İngilizce. Kurulumda seçilir (varsayılan Türkçe), sonra ayarlardan değiştirilebilir. Metinler `Arayuz/Dil.cs` içindeki tabloda.

## Kurulum

`AyazHUD-Kurulum-<sürüm>.exe` dosyasını çalıştır. Kurulum şunları yapar:

1. Uygulamayı `C:\Program Files\AyazHUD` klasörüne kurar.
2. Kurulu değilse [PawnIO](https://github.com/namazso/PawnIO) sürücüsünü kurar. İşlemci sıcaklığı ve anakart fanları bu imzalı sürücüyle okunur.
3. `AyazHUD` zamanlanmış görevini oluşturur. Uygulama sensörler ve FPS için yönetici yetkisi ister; bu görev sayesinde her açılışta UAC sormadan başlar.

Kurulum dosyası imzasız olduğu için SmartScreen uyarısı çıkabilir: "Ek bilgi" > "Yine de çalıştır".

Uygulamanın eski adı "Sistem Paneli" idi. O adla kurulmuş sürüm varsa, kurulum onu kapatır, eski klasörünü, görevini ve kısayolunu siler. Ayarlar da ilk açılışta eski klasörden alınır.

## Derleme

Gerekenler:

- [.NET SDK](https://dotnet.microsoft.com/download) 8 veya üstü. Proje `net48` hedefliyor ve SDK ile derleniyor.
- [Inno Setup 7](https://jrsoftware.org/isdl.php). Yalnızca kurulum dosyası için gerekir.

```powershell
# Derle ve kurulum dosyasını üret (cikti\AyazHUD-Kurulum-<sürüm>.exe)
powershell -ExecutionPolicy Bypass -File derle.ps1

# Yalnızca uygulamayı derle
dotnet build src\AyazHUD\AyazHUD.csproj -c Debug
```

Geliştirirken yönetici yetkisi olmadan denemek için uygulamayı `--yetkisiz` ile başlat. Bu kipte sıcaklık, fanlar ve FPS okunmaz; arayüz çalışır.

```powershell
src\AyazHUD\bin\Debug\net48\AyazHUD.exe --yetkisiz
```

Sürüm numarası `src/AyazHUD/AyazHUD.csproj` içindeki `<Version>` alanında. Üçüncü taraf dosyalar `bagimliliklar/` klasöründe; kaynakları ve özetleri o klasörün README'sinde.

## Nasıl çalışıyor

| Parça | Dosya | Açıklama |
|---|---|---|
| Açılış ve yetki | `App.xaml.cs`, `Cekirdek/Gorev.cs` | Yönetici değilse zamanlanmış görevi çalıştırıp çıkar. Görev yoksa UAC sorar; UAC reddedilirse sınırlı kipte açılır. |
| Sensörler | `Cekirdek/SensorServisi.cs`, `Cekirdek/DonanimTanima.cs` | LibreHardwareMonitor ayrı bir iş parçacığında saniyede bir okunur. Seçim turlarında bütün donanım okunur, diğer turlarda yalnızca gösterilenler. |
| FPS | `Cekirdek/FpsOlcer.cs` | PresentMon'un CSV çıktısı okunur. Ön plandaki ya da en yüksek FPS'li süreç oyun sayılır; listedeki tarayıcı, Discord gibi programlar hariç tutulur. PresentMon bir iş nesnesine bağlıdır, uygulama kapanınca o da kapanır. |
| Widget penceresi | `Widgetlar/WidgetPenceresi.cs` | Saydam, kenarlıksız WPF penceresi. Bulanıklık için `SetWindowCompositionAttribute` kullanılır, çünkü DWM'nin resmi arka planı pencere etkin değilken kayboluyor. Köşeler Windows 11'in kendi yuvarlamasıyla kırpılır. Pencere hep en altta durur, odak çalmaz, kilitliyken tıklamaları arkasına geçirir. |
| Tasarım | `Arayuz/` | Halka ölçer, çizgi grafik, çubuk, harf aralıklı başlık. Ortak renkler `Tema.cs` içinde. Widget'a göre değişen stil ve renk `Gorunum.cs` içinde; her pencere kendi `Gorunum` örneğini içindeki denetimlere miras olarak verir. |
| Arka plan buzlanması | `Widgetlar/BuzKatmani.cs` | Monitörü kaplayan, tıklamaları geçiren bulanık pencere. Widget'lar ona bağlı (sahipli) pencereler olduğu için hep onun üstünde kalır. |
| Ayarlar | `Ayar/Ayarlar.cs`, `AyarPenceresi.xaml` | `%APPDATA%\AyazHUD\ayarlar.json` |
| Günlük | `Cekirdek/Gunluk.cs` | `%LOCALAPPDATA%\AyazHUD\gunluk.txt` |
| Kurulum | `kurulum/AyazHUD.iss` | Inno Setup betiği. PawnIO'yu `-install -silent` ile kurar, görevi `--gorev-kur` ile oluşturur. |

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
