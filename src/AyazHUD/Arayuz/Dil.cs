using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Markup;

namespace AyazHUD.Arayuz
{
    /// <summary>
    /// Arayüz dili (Türkçe ya da İngilizce). Bütün görünen metinler buradaki tablodan gelir; kodda
    /// <c>Dil.M("anahtar")</c>, XAML'da <c>{a:M anahtar}</c> ile kullanılır. Dil değişince pencereler yeniden kurulur.
    /// Günlük dosyası geliştirici için olduğundan Türkçe kalır.
    /// </summary>
    internal static class Dil
    {
        public const string Turkce = "tr";
        public const string Ingilizce = "en";

        static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");

        public static string Kod { get; private set; } = Turkce;

        /// <summary>Sayı ve tarih biçimi: Türkçede "4,85 GHz", İngilizcede "4.85 GHz".</summary>
        public static CultureInfo Kultur => Kod == Ingilizce ? En : Tr;

        public static void Ayarla(string kod) => Kod = kod == Ingilizce ? Ingilizce : Turkce;

        public static string M(string anahtar)
        {
            if (!Metinler.TryGetValue(anahtar, out var m)) return anahtar;
            return Kod == Ingilizce ? m.En : m.Tr;
        }

        /// <summary>Yüzde yazımı: Türkçede "%41", İngilizcede "41%".</summary>
        public static string Yuzde(string sayi) => Kod == Ingilizce ? sayi + "%" : "%" + sayi;

        static readonly Dictionary<string, (string Tr, string En)> Metinler = new Dictionary<string, (string, string)>
        {
            // ---------- Widget'lar ----------
            ["w.saat"] = ("Saat", "Clock"),
            ["w.islemci"] = ("İşlemci", "CPU"),
            ["w.bellek"] = ("Bellek", "Memory"),
            ["w.sogutma"] = ("Soğutma ve depolama", "Cooling and storage"),
            ["w.fps"] = ("FPS", "FPS"),
            ["w.ekranKarti"] = ("Ekran kartı", "Graphics card"),
            ["w.ag"] = ("Ağ", "Network"),

            ["b.islemci"] = ("İŞLEMCİ", "CPU"),
            ["b.ekranKarti"] = ("EKRAN KARTI", "GPU"),
            ["b.vram"] = ("VRAM", "VRAM"),
            ["b.fps"] = ("FPS", "FPS"),
            ["b.bellek"] = ("BELLEK", "MEMORY"),
            ["b.sogutma"] = ("SOĞUTMA VE DEPOLAMA", "COOLING AND STORAGE"),
            ["b.ag"] = ("AĞ", "NETWORK"),
            ["sicaklik"] = ("SICAKLIK", "TEMP"),

            ["yuk"] = ("Yük", "Load"),
            ["frekans"] = ("Frekans", "Clock"),
            ["guc"] = ("Güç", "Power"),
            ["enDusuk"] = ("En düşük %1", "1% low"),
            ["kareSuresi"] = ("Kare süresi", "Frame time"),
            ["gecikme"] = ("Gecikme", "Latency"),
            ["indirme"] = ("İndirme", "Download"),
            ["yukleme"] = ("Yükleme", "Upload"),
            ["gpuFani"] = ("Ekran kartı fanı", "GPU fan"),
            ["duruyor"] = ("Duruyor", "Stopped"),
            ["disk"] = ("Disk", "Disk"),
            ["fps.izinYok"] = ("Yönetici izni gerekiyor", "Needs admin permission"),
            ["fps.hata"] = ("FPS ölçülemiyor", "Can't measure FPS"),
            ["fps.bekleniyor"] = ("Oyun bekleniyor", "Waiting for a game"),
            ["tarihBicimi"] = ("d MMMM dddd", "dddd, MMMM d"),

            // ---------- Stiller ----------
            ["stil.Bulanik"] = ("Buzlu cam", "Frosted glass"),
            ["stil.Akrilik"] = ("Akrilik", "Acrylic"),
            ["stil.Koyu"] = ("Koyu", "Dark"),
            ["stil.Seffaf"] = ("Şeffaf", "Transparent"),
            ["buz.0"] = ("Kapalı", "Off"),
            ["renk.buz"] = ("Buz mavisi", "Ice blue"),
            ["renk.nane"] = ("Nane", "Mint"),
            ["renk.lavanta"] = ("Lavanta", "Lavender"),
            ["renk.pembe"] = ("Pembe", "Pink"),
            ["renk.seftali"] = ("Şeftali", "Peach"),
            ["renk.limon"] = ("Limon", "Lemon"),
            ["renk.mercan"] = ("Mercan", "Coral"),
            ["renk.gumus"] = ("Gümüş", "Silver"),
            ["renk.ozel"] = ("Kendi rengin (#RRGGBB)", "Your own color (#RRGGBB)"),
            ["renk.secici"] = ("Renk seç", "Pick a color"),
            ["renk.kod"] = ("Kod", "Hex"),
            ["renk.kayitli"] = ("Kaydettiğin renkler", "Saved colors"),
            ["renk.kaydet"] = ("Bu rengi kaydet", "Save this color"),
            ["renk.sil"] = ("sağ tıkla: sil", "right-click to remove"),

            // ---------- Tepsi ve bildirimler ----------
            ["t.duzenle"] = ("Düzenle (widget'ları sürükle)", "Edit layout (drag widgets)"),
            ["t.widgetlar"] = ("Widget'lar", "Widgets"),
            ["t.ekran"] = ("Ekran", "Screen"),
            ["t.sifirla"] = ("Konumları sıfırla", "Reset positions"),
            ["t.ayarlar"] = ("Ayarlar…", "Settings…"),
            ["t.baslangic"] = ("Windows ile başlat", "Start with Windows"),
            ["t.cikis"] = ("Çıkış", "Exit"),
            ["t.ipucu"] = ("İşlemci {0} °C · Ekran kartı {1} °C", "CPU {0} °C · GPU {1} °C"),
            ["n.yetkisizBaslik"] = ("Yönetici izni olmadan çalışıyor", "Running without admin permission"),
            ["n.yetkisizMetin"] = ("İşlemci sıcaklığı, fanlar ve FPS gösterilemez. Uygulamayı Başlat menüsünden yeniden açıp izin verebilirsin.",
                                   "CPU temperature, fans and FPS can't be shown. Reopen the app from the Start menu and allow it."),
            ["n.pawnioBaslik"] = ("PawnIO sürücüsü kurulu değil", "PawnIO driver is not installed"),
            ["n.pawnioMetin"] = ("İşlemci sıcaklığı ve anakart fanları okunamaz. Ayaz HUD'u kurulum dosyasıyla yeniden kur.",
                                 "CPU temperature and motherboard fans can't be read. Reinstall Ayaz HUD with the setup file."),

            // ---------- Ayarlar penceresi ----------
            ["a.pencere"] = ("Ayaz HUD · Ayarlar", "Ayaz HUD · Settings"),
            ["a.surum"] = ("Sürüm {0}", "Version {0}"),
            ["a.menuGorunum"] = ("Görünüm", "Appearance"),
            ["a.menuWidgetlar"] = ("Widget'lar", "Widgets"),
            ["a.menuSensorler"] = ("Fanlar ve diskler", "Fans and disks"),
            ["a.menuGenel"] = ("Genel", "General"),

            ["a.gorunumAciklama"] = ("Bütün widget'ların varsayılan görünümü. Bir widget'a ayrı görünüm vermek için Widget'lar bölümüne bak.",
                                     "Default look of all widgets. To give a widget its own look, see the Widgets section."),
            ["a.kartStili"] = ("Kart stili", "Card style"),
            ["a.kartBuzlanmasi"] = ("Kart buzlanması", "Card frost"),
            ["a.kartKoyulugu"] = ("Kart koyuluğu", "Card darkness"),
            ["a.kartAciklama"] = ("Buzlanma, buzlu cam ve akrilik stillerde kartın arkasını ne kadar bulanıklaştıracağını belirler. Açık renkli duvar kâğıtlarında yazıların okunması için koyuluğu artır.",
                                  "Frost sets how much the frosted glass and acrylic styles blur what is behind the card. Increase darkness so text stays readable on bright wallpapers."),
            ["a.vurgu"] = ("Vurgu rengi", "Accent color"),
            ["a.arkaPlan"] = ("Arka plan", "Background"),
            ["a.arkaPlanAciklama"] = ("Widget'ların bulunduğu ekranın tamamına uygulanır; tıklamalar masaüstüne geçmeye devam eder. Hareketli ya da çok renkli duvar kâğıtlarında buzlanma, açık renkli olanlarda karartma widget'ları öne çıkarır.",
                                      "Applies to the whole screen the widgets are on; clicks still go through to the desktop. Frost helps on busy or animated wallpapers, darkening on bright ones."),
            ["a.buzlanma"] = ("Buzlanma", "Frost"),
            ["a.karartma"] = ("Karartma", "Darkening"),

            ["a.widgetAciklama"] = ("Hangi widget'ların görüneceğini seç ve istersen her birine ayrı bir görünüm ver.",
                                    "Choose which widgets are shown and optionally give each one its own look."),
            ["a.ekran"] = ("Ekran", "Screen"),
            ["a.ekranAciklama"] = ("Widget'ların durduğu ekran. Seçtiğinde bütün widget'lar düzenleri korunarak oraya taşınır; konumları sıfırlayınca da bu ekrana dizilirler. Düzenleme açıkken tek tek başka ekranlara da sürükleyebilirsin.",
                                   "The screen the widgets live on. Choosing one moves all widgets there, keeping their arrangement, and resetting positions lays them out on it. While editing, you can still drag single widgets to other screens."),
            ["a.ekranAdi"] = ("Ekran {0}", "Display {0}"),
            ["a.birincil"] = ("birincil", "primary"),
            ["a.widgetlarBurada"] = ("Widget'lar burada", "Widgets here"),
            ["a.yerlesim"] = ("Yerleşim", "Layout"),
            ["a.yerlesimAciklama"] = ("Düzenleme açıkken widget'ları sürükle; birbirlerine ve ekran kenarına yaklaşınca hizalanırlar.",
                                      "While editing, drag the widgets; they snap to each other and to the screen edges."),
            ["a.duzenle"] = ("Düzenle", "Edit"),
            ["a.duzenlemeyiBitir"] = ("Düzenlemeyi bitir", "Done editing"),
            ["a.sifirla"] = ("Konumları sıfırla", "Reset positions"),
            ["a.goster"] = ("Masaüstünde göster", "Show on desktop"),
            ["a.ozelGorunum"] = ("Bu widget'a ayrı görünüm ver", "Give this widget its own look"),

            ["a.sensorAciklama"] = ("Soğutma kartında görünecek adlar. Boş bırakırsan sensörün kendi adı kullanılır. Hiç dönmemiş fan yuvaları listelenmez.",
                                    "Names shown on the cooling card. Leave empty to use the sensor's own name. Fan headers that never spun are not listed."),
            ["a.kartta"] = ("Kartta göster", "Show on card"),
            ["a.varsayilan"] = ("Varsayılan: {0}", "Default: {0}"),
            ["a.sensorYok"] = ("Fan ya da disk sıcaklığı bulunamadı (yönetici izni olmadan okunamaz).", "No fan or disk temperature found (they need admin permission)."),
            ["a.sensorBekleniyor"] = ("Sensörler henüz okunmadı; birkaç saniye sonra yeniden bak.", "Sensors not read yet; check again in a few seconds."),

            ["a.dil"] = ("Dil", "Language"),
            ["a.dilAciklama"] = ("Uygulamanın dili. Değiştirince pencereler yeni dille yeniden açılır.", "The app's language. Windows reopen in the new language when you change it."),
            ["a.baslangic"] = ("Windows açılınca başlat", "Start when Windows starts"),
            ["a.baslangicAciklama"] = ("Oturum açılınca yönetici yetkisiyle, UAC sormadan başlar.", "Starts with admin rights at sign-in, without a UAC prompt."),
            ["a.baslangicKapali"] = ("Bu ayar, uygulama kurulum dosyasıyla kurulduğunda ve yönetici yetkisiyle çalışırken değiştirilebilir.",
                                     "This can be changed when the app is installed with the setup file and running with admin rights."),
            ["a.lisanslar"] = ("Kullanılan açık kaynak bileşenler: LibreHardwareMonitor (MPL-2.0), PresentMon (MIT), PawnIO (GPL-2.0). Lisans metinleri kurulum klasöründeki 'lisanslar' klasöründe.",
                               "Open source components: LibreHardwareMonitor (MPL-2.0), PresentMon (MIT), PawnIO (GPL-2.0). License texts are in the 'lisanslar' folder of the install directory."),
            ["a.gunluk"] = ("Günlük dosyasını aç", "Open log file"),
        };
    }

    /// <summary>XAML'da çeviri: <c>Text="{a:M a.kartStili}"</c>.</summary>
    [MarkupExtensionReturnType(typeof(string))]
    public sealed class M : MarkupExtension
    {
        public M(string anahtar) => Anahtar = anahtar;

        [ConstructorArgument("anahtar")]
        public string Anahtar { get; set; }

        public override object ProvideValue(IServiceProvider servis) => Dil.M(Anahtar);
    }
}
