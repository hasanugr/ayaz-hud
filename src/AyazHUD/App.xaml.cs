using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Threading;
using AyazHUD.Arayuz;
using AyazHUD.Ayar;
using AyazHUD.Cekirdek;
using AyazHUD.Widgetlar;

namespace AyazHUD
{
    /// <summary>
    /// Açılış akışı:
    /// 1. Kurulum komutları (--gorev-kur, --gorev-sil, --kapat) çalıştırılıp çıkılır.
    /// 2. Zaten çalışan bir kopya varsa ona "ayarları aç" denir ve çıkılır.
    /// 3. Yönetici değilsek "AyazHUD" görevi çalıştırılır (UAC'siz yönetici yetkisi); görev yoksa UAC sorulur.
    ///    UAC reddedilirse sınırlı kipte (sıcaklık ve FPS olmadan) devam edilir. --yetkisiz bu adımı atlar.
    /// 4. Sensörler, FPS ölçer, widget'lar ve tepsi simgesi başlatılır.
    /// </summary>
    public partial class App : Application
    {
        public static readonly string Surum = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
        static readonly string Klasor = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        Ayarlar _ayarlar;
        SensorServisi _sensorler;
        FpsOlcer _fps;
        WidgetYoneticisi _widgetlar;
        Tepsi _tepsi;
        DispatcherTimer _saniye, _kayitBekleme;
        AyarPenceresi _ayarPenceresi;
        bool _yonetici, _kapaniyor;

        internal Anlik SonAnlik { get; private set; } = new Anlik();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += (s, x) =>
            {
                Gunluk.Yaz("Beklenmeyen hata", x.Exception);
                x.Handled = true;
            };
            AppDomain.CurrentDomain.UnhandledException += (s, x) => Gunluk.Yaz("Beklenmeyen kritik hata", x.ExceptionObject as Exception);

            var arg = e.Args.Select(a => a.ToLowerInvariant()).ToList();
            _yonetici = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

            // ---------- Kurulum programının çağırdığı komutlar ----------
            if (arg.Contains("--gorev-kur"))
            {
                Komut(() => Gorev.Kur(Gorev.OturumAcilisinda() ?? true));
                return;
            }
            if (arg.Contains("--gorev-sil"))
            {
                Komut(Gorev.Sil);
                return;
            }
            if (arg.Contains("--kapat"))
            {
                TekKopya.IstekGonder("kapat");
                Shutdown();
                return;
            }

            // ---------- Tek kopya ve yetki ----------
            if (TekKopya.CalisiyorMu())
            {
                TekKopya.IstekGonder("ayarlar");
                Shutdown();
                return;
            }
            if (!_yonetici && !arg.Contains("--yetkisiz") && (Gorev.Calistir() || YukseltilmisBaslat()))
            {
                Shutdown();
                return;
            }
            if (!TekKopya.Al())
            {
                Shutdown();
                return;
            }

            Gunluk.Yaz($"Başladı: sürüm {Surum}, yönetici: {_yonetici}, Windows {Environment.OSVersion.Version}");
            _ayarlar = Ayarlar.Yukle();
            TemaUygula();

            _sensorler = new SensorServisi();
            _fps = new FpsOlcer(Path.Combine(Klasor, "PresentMon-2.6.0-x64.exe"));
            _fps.Baslat();

            _kayitBekleme = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _kayitBekleme.Tick += (s, x) =>
            {
                _kayitBekleme.Stop();
                _ayarlar.Kaydet();
            };

            _widgetlar = new WidgetYoneticisi(_ayarlar, AyarlarDegisti);
            _widgetlar.Baslat();
            _tepsi = new Tepsi(this, _widgetlar);
            TekKopya.Dinle(Dispatcher, Istek);
            SessionEnding += (s, x) => Temizle();

            // Saat saniye başında değişsin diye zamanlayıcı her turda bir sonraki saniyeye ayarlanır
            _saniye = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(50) };
            _saniye.Tick += (s, x) => Tik();
            _saniye.Start();

            if (!_yonetici)
                _tepsi.Bildir("Yönetici izni olmadan çalışıyor", "İşlemci sıcaklığı, fanlar ve FPS gösterilemez. Uygulamayı Başlat menüsünden yeniden açıp izin verebilirsin.");
            else if (!PawnIOKurulu())
                _tepsi.Bildir("PawnIO sürücüsü kurulu değil", "İşlemci sıcaklığı ve anakart fanları okunamaz. Ayaz HUD'u kurulum dosyasıyla yeniden kur.");
        }

        void Tik()
        {
            var simdi = DateTime.Now;
            _saniye.Interval = TimeSpan.FromMilliseconds(1000 - simdi.Millisecond + 10);
            var a = _sensorler.Son;
            a.Fps = _fps.Hesapla();
            SonAnlik = a;
            _widgetlar.Guncelle(a);
            _tepsi.IpucuYaz($"Ayaz HUD\nİşlemci {Bicim.Sayi(a.IslemciSicaklik)} °C · Ekran kartı {Bicim.Sayi(a.GpuSicaklik)} °C");
        }

        void Istek(string komut)
        {
            if (komut == "kapat") Cikis();
            else if (komut == "ayarlar") AyarlariAc();
            else if (komut == "duzenle") DuzenlemeDegistir();
        }

        // ---------- Tepsi ve ayarlar penceresinin kullandığı işlemler ----------

        internal void AyarlariAc()
        {
            if (_ayarPenceresi == null)
            {
                _ayarPenceresi = new AyarPenceresi(this, _ayarlar, _widgetlar);
                _ayarPenceresi.Closed += (s, e) => _ayarPenceresi = null;
                _ayarPenceresi.Show();
            }
            if (_ayarPenceresi.WindowState == WindowState.Minimized) _ayarPenceresi.WindowState = WindowState.Normal;
            _ayarPenceresi.Activate();
        }

        internal void DuzenlemeDegistir() => _widgetlar.Duzenleme = !_widgetlar.Duzenleme;

        /// <summary>Windows ile başlama durumu; değiştirilemiyorsa (görev yok ya da yönetici değil) null.</summary>
        internal bool? BaslangictaMi() => _yonetici ? Gorev.OturumAcilisinda() : null;

        internal void BaslangicDegistir()
        {
            bool? simdi = BaslangictaMi();
            if (simdi == null) return;
            try
            {
                Gorev.Kur(!simdi.Value);
            }
            catch (Exception e)
            {
                Gunluk.Yaz("Başlangıç ayarı değiştirilemedi", e);
            }
        }

        internal void TemaUygula()
        {
            if (!Tema.RenkCoz(_ayarlar.Vurgu, out var renk)) renk = Tema.VarsayilanVurgu;
            Tema.GenelVurguYaz(renk);
            _widgetlar?.GorunumleriUygula();
        }

        /// <summary>Ayar değişince kısa bir süre bekleyip kaydeder (kaydırıcı sürüklenirken diske sürekli yazılmasın).</summary>
        internal void AyarlarDegisti()
        {
            _kayitBekleme.Stop();
            _kayitBekleme.Start();
        }

        internal void Cikis()
        {
            Temizle();
            Shutdown();
        }

        void Temizle()
        {
            if (_kapaniyor) return;
            _kapaniyor = true;
            Gunluk.Yaz("Kapanıyor");
            _saniye?.Stop();
            _ayarPenceresi?.Close();
            if (_ayarlar != null) _ayarlar.Kaydet();
            _tepsi?.Dispose();
            _widgetlar?.Kapat();
            _fps?.Dispose();
            _sensorler?.Dispose();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Temizle();
            base.OnExit(e);
        }

        // ---------- Yardımcılar ----------

        void Komut(Action islem)
        {
            try
            {
                islem();
            }
            catch (Exception e)
            {
                Gunluk.Yaz("Komut çalıştırılamadı", e);
                Shutdown(1);
                return;
            }
            Shutdown();
        }

        static bool YukseltilmisBaslat()
        {
            try
            {
                Process.Start(new ProcessStartInfo(Assembly.GetExecutingAssembly().Location) { UseShellExecute = true, Verb = "runas" });
                return true;
            }
            catch (Win32Exception)
            {
                // Kullanıcı UAC penceresinde "Hayır" dedi
                return false;
            }
        }

        static bool PawnIOKurulu()
        {
            try
            {
                using (var s = new ServiceController("PawnIO"))
                    return s.Status == ServiceControllerStatus.Running || s.Status == ServiceControllerStatus.Stopped;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }
}
