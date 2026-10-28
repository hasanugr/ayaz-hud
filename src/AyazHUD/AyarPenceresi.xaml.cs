using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AyazHUD.Arayuz;
using AyazHUD.Ayar;
using AyazHUD.Cekirdek;
using AyazHUD.Widgetlar;
using AyazHUD.Yerel;

namespace AyazHUD
{
    /// <summary>Ayarlar penceresi. Her değişiklik widget'lara hemen uygulanır ve kısa bir gecikmeyle kaydedilir.</summary>
    internal partial class AyarPenceresi : Window
    {
        readonly App _uygulama;
        readonly Ayarlar _ayarlar;
        readonly WidgetYoneticisi _widgetlar;

        StilSecici _genelStil, _ozelStil;
        RenkSecici _genelRenk, _ozelRenk;
        string _seciliWidget;
        bool _yukleniyor = true;

        public AyarPenceresi(App uygulama, Ayarlar ayarlar, WidgetYoneticisi widgetlar)
        {
            _uygulama = uygulama;
            _ayarlar = ayarlar;
            _widgetlar = widgetlar;
            InitializeComponent();
            var simge = Imaging.CreateBitmapSourceFromHIcon(Tepsi.Simge().Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            Icon = simge;
            Logo.Source = simge;
            SurumYazisi.Text = string.Format(Dil.M("a.surum"), App.Surum);

            GorunumSayfasi();
            WidgetSayfasi();
            SensorListesi(_uygulama.SonAnlik);
            HakkindaSayfasi();

            Menu.SelectedIndex = 0;
            Closed += (s, e) => _uygulama.AyarlarDegisti();
            // Düzenleme tepsi menüsünden de açılıp kapanabiliyor
            Activated += (s, e) => DuzenleDugmesiYaz();
            _yukleniyor = false;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // Koyu başlık çubuğu
            int evet = 1;
            Win32.DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, Win32.DWMWA_USE_IMMERSIVE_DARK_MODE, ref evet, sizeof(int));
        }

        void Menu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string sayfa = (Menu.SelectedItem as ListBoxItem)?.Tag as string;
            SayfaGorunum.Visibility = sayfa == "Gorunum" ? Visibility.Visible : Visibility.Collapsed;
            SayfaWidgetlar.Visibility = sayfa == "Widgetlar" ? Visibility.Visible : Visibility.Collapsed;
            SayfaSensorler.Visibility = sayfa == "Sensorler" ? Visibility.Visible : Visibility.Collapsed;
            SayfaHakkinda.Visibility = sayfa == "Hakkinda" ? Visibility.Visible : Visibility.Collapsed;
            if (sayfa == "Sensorler") SensorListesi(_uygulama.SonAnlik);
            if (sayfa == "Widgetlar") EkranlariCiz();
        }

        /// <summary>Değişikliği widget'lara uygular ve kaydı tetikler.</summary>
        void Uygula()
        {
            if (_yukleniyor) return;
            _uygulama.TemaUygula();
            _uygulama.AyarlarDegisti();
            _genelStil?.Yenile();
            _ozelStil?.Yenile();
            BuzOnizlemeYenile();
            BuzlanmaEtkinlikleri();
        }

        /// <summary>Buzlanma yalnızca buzlu cam ve akrilik stillerde anlamlı.</summary>
        void BuzlanmaEtkinlikleri()
        {
            bool Bulanik(string stil) => Gorunum.StilCoz(stil) == KartStili.Bulanik || Gorunum.StilCoz(stil) == KartStili.Akrilik;
            GenelBuzlanma.IsEnabled = Bulanik(_ayarlar.Stil);
            if (_seciliWidget != null) OzelBuzlanma.IsEnabled = Bulanik(_ayarlar.Widget(_seciliWidget).Stil ?? _ayarlar.Stil);
        }

        static Color Renk(string metin) => Tema.RenkCoz(metin, out var c) ? c : Tema.VarsayilanVurgu;

        // ---------- Görünüm ----------

        void GorunumSayfasi()
        {
            _genelStil = new StilSecici(GenelStiller,
                () => Gorunum.StilCoz(_ayarlar.Stil),
                s => { _ayarlar.Stil = s.ToString(); Uygula(); },
                () => (Renk(_ayarlar.Vurgu), _ayarlar.Koyuluk, _ayarlar.Buzlanma));
            _genelRenk = new RenkSecici(GenelRenkler, () => _ayarlar.Vurgu, r => { _ayarlar.Vurgu = r; Uygula(); });

            KaydiriciBagla(GenelKoyuluk, GenelKoyulukYazisi, () => _ayarlar.Koyuluk, v => _ayarlar.Koyuluk = v);
            BuzlanmaBagla(GenelBuzlanma, GenelBuzlanmaYazisi, () => _ayarlar.Buzlanma, v => _ayarlar.Buzlanma = v);
            KaydiriciBagla(Karartma, KarartmaYazisi, () => _ayarlar.ArkaPlanKarartma, v => _ayarlar.ArkaPlanKarartma = v);

            BuzlanmaBagla(Buzlanma, BuzlanmaYazisi, () => _ayarlar.ArkaPlanBuzlanma, v => _ayarlar.ArkaPlanBuzlanma = v);
            BuzOnizlemeYenile();
        }

        // Dil değişebildiği için her seferinde tablodan okunur
        static string BuzlanmaMetni(double yuzde) => yuzde < 5 ? Dil.M("buz.0") : Dil.Yuzde($"{yuzde:0}");

        void BuzOnizlemeYenile()
        {
            BuzOnizleme.Ayarla(Gorunum.StilCoz(_ayarlar.Stil), Renk(_ayarlar.Vurgu), _ayarlar.Koyuluk, _ayarlar.Buzlanma);
            BuzOnizleme.Buzlanma(_ayarlar.ArkaPlanBuzlanma, _ayarlar.ArkaPlanKarartma);
        }

        /// <summary>Buzlanma kaydırıcısı: 10'ar 10'ar yüzde (arka planda 0 = kapalı).</summary>
        void BuzlanmaBagla(Slider kaydirici, TextBlock yazi, Func<int> oku, Action<int> yaz)
        {
            kaydirici.Value = Math.Max(kaydirici.Minimum, BuzSeviyesi.Yuvarla(oku()));
            yazi.Text = BuzlanmaMetni(kaydirici.Value);
            kaydirici.ValueChanged += (s, e) =>
            {
                yazi.Text = BuzlanmaMetni(kaydirici.Value);
                if (_yukleniyor) return;
                yaz(BuzSeviyesi.Yuvarla((int)Math.Round(kaydirici.Value)));
                Uygula();
            };
        }

        void KaydiriciBagla(Slider kaydirici, TextBlock yazi, Func<double> oku, Action<double> yaz)
        {
            kaydirici.Value = Math.Round(oku() * 100);
            yazi.Text = Dil.Yuzde($"{kaydirici.Value:0}");
            kaydirici.ValueChanged += (s, e) =>
            {
                yazi.Text = Dil.Yuzde($"{kaydirici.Value:0}");
                if (_yukleniyor) return;
                yaz(kaydirici.Value / 100);
                Uygula();
            };
        }

        // ---------- Widget'lar ----------

        void WidgetSayfasi()
        {
            foreach (var t in _widgetlar.Tanimlar)
                WidgetListesi.Items.Add(new ListBoxItem { Content = t.Ad, Tag = t.Kimlik });
            DuzenleDugmesiYaz();

            // Ekran seçimi; widget'lar taşınınca çizim kendini yeniler
            Ekranlar.Secildi += ad => _widgetlar.EkranSec(ad);
            _widgetlar.YerlesimDegisti += EkranlariCiz;
            Closed += (s, e) => _widgetlar.YerlesimDegisti -= EkranlariCiz;

            WidgetGoster.Click += (s, e) =>
            {
                if (_seciliWidget != null) _widgetlar.Ac(_seciliWidget, WidgetGoster.IsChecked == true);
            };
            OzelGorunum.Click += (s, e) =>
            {
                var w = _ayarlar.Widget(_seciliWidget);
                w.OzelGorunum = OzelGorunum.IsChecked == true;
                // İlk açılışta genel görünümden başlasın
                if (w.OzelGorunum && string.IsNullOrEmpty(w.Stil))
                {
                    w.Stil = _ayarlar.Stil;
                    w.Vurgu = _ayarlar.Vurgu;
                    w.Koyuluk = _ayarlar.Koyuluk;
                    w.Buzlanma = _ayarlar.Buzlanma;
                }
                WidgetAyarlariniYaz();
                Uygula();
            };
            _ozelStil = new StilSecici(OzelStiller,
                () => Gorunum.StilCoz(_ayarlar.Widget(_seciliWidget ?? "islemci").Stil),
                s => { _ayarlar.Widget(_seciliWidget).Stil = s.ToString(); Uygula(); },
                () =>
                {
                    var w = _ayarlar.Widget(_seciliWidget ?? "islemci");
                    return (Renk(w.Vurgu ?? _ayarlar.Vurgu), w.Koyuluk, w.Buzlanma);
                });
            BuzlanmaBagla(OzelBuzlanma, OzelBuzlanmaYazisi, () => _ayarlar.Widget(_seciliWidget ?? "islemci").Buzlanma, v =>
            {
                if (_seciliWidget != null) _ayarlar.Widget(_seciliWidget).Buzlanma = v;
            });
            _ozelRenk = new RenkSecici(OzelRenkler, () => _ayarlar.Widget(_seciliWidget ?? "islemci").Vurgu, r => { _ayarlar.Widget(_seciliWidget).Vurgu = r; Uygula(); });
            OzelKoyuluk.ValueChanged += (s, e) =>
            {
                OzelKoyulukYazisi.Text = Dil.Yuzde($"{OzelKoyuluk.Value:0}");
                if (_yukleniyor || _seciliWidget == null) return;
                _ayarlar.Widget(_seciliWidget).Koyuluk = OzelKoyuluk.Value / 100;
                Uygula();
            };
            WidgetListesi.SelectedIndex = 1;
        }

        void WidgetListesi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _seciliWidget = (WidgetListesi.SelectedItem as ListBoxItem)?.Tag as string;
            if (_seciliWidget == null) return;
            WidgetAyarlariniYaz();
        }

        /// <summary>Seçili widget'ın ayarlarını sağdaki alana doldurur.</summary>
        void WidgetAyarlariniYaz()
        {
            bool eski = _yukleniyor;
            _yukleniyor = true;
            var t = _widgetlar.Tanimlar.First(x => x.Kimlik == _seciliWidget);
            var w = _ayarlar.Widget(_seciliWidget);
            WidgetAdi.Text = t.Ad;
            WidgetGoster.IsChecked = _widgetlar.AcikMi(_seciliWidget);
            OzelGorunum.IsChecked = w.OzelGorunum;
            OzelAyarlar.IsEnabled = w.OzelGorunum;
            OzelAyarlar.Opacity = w.OzelGorunum ? 1 : 0.4;
            // Saatin kartı yok; yalnızca renk ayarı anlamlı
            var kartGorunumu = t.Kart ? Visibility.Visible : Visibility.Collapsed;
            OzelKartAyarlari.Visibility = kartGorunumu;
            OzelKoyulukSatiri.Visibility = kartGorunumu;
            OzelKoyuluk.Visibility = kartGorunumu;
            OzelKoyuluk.Value = Math.Round((w.OzelGorunum ? w.Koyuluk : _ayarlar.Koyuluk) * 100);
            OzelKoyulukYazisi.Text = Dil.Yuzde($"{OzelKoyuluk.Value:0}");
            OzelBuzlanma.Value = Gorunum.KartBuzlanmasi(w.OzelGorunum ? w.Buzlanma : _ayarlar.Buzlanma);
            OzelBuzlanmaYazisi.Text = BuzlanmaMetni(OzelBuzlanma.Value);
            BuzlanmaEtkinlikleri();
            _ozelStil.Yenile();
            _ozelRenk.Yenile();
            _yukleniyor = eski;
        }

        void EkranlariCiz()
        {
            var ekranlar = Ekran.Hepsi();
            var widgetlar = _widgetlar.Tanimlar.Select(t => _widgetlar.Konum(t.Kimlik)).Where(r => r != null).Select(r => r.Value).ToList();
            Ekranlar.Ayarla(ekranlar, _widgetlar.AnaEkran(ekranlar)?.Ad, widgetlar);
        }

        void DuzenleDugmesiYaz() =>
            DuzenleDugmesi.Content = _widgetlar.Duzenleme ? Dil.M("a.duzenlemeyiBitir") : Dil.M("a.duzenle");

        void Duzenle_Click(object sender, RoutedEventArgs e)
        {
            _uygulama.DuzenlemeDegistir();
            DuzenleDugmesiYaz();
        }

        void Sifirla_Click(object sender, RoutedEventArgs e) => _widgetlar.KonumlariSifirla();

        // ---------- Fanlar ve diskler ----------

        void SensorListesi(Anlik a)
        {
            Sensorler.Children.Clear();
            Sensorler.RowDefinitions.Clear();
            Sensorler.ColumnDefinitions.Clear();
            Sensorler.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Sensorler.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Sensorler.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });

            var satirlar = a.Fanlar.Select(f => (f, "RPM")).Concat(a.Diskler.Select(d => (d, "°C"))).ToList();
            if (satirlar.Count == 0)
            {
                Sensorler.Children.Add(new TextBlock
                {
                    Text = Dil.M(a.Hazir ? "a.sensorYok" : "a.sensorBekleniyor"),
                    Foreground = (Brush)FindResource("Soluk"),
                    TextWrapping = TextWrapping.Wrap,
                });
                return;
            }
            int i = 0;
            foreach (var (s, birim) in satirlar)
            {
                Sensorler.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var etiket = _ayarlar.Etiket(s.Kimlik);
                var goster = new CheckBox { IsChecked = etiket?.Gizli != true, ToolTip = Dil.M("a.kartta"), Margin = new Thickness(0, 0, 12, 10) };
                var ad = new TextBox { Text = etiket?.Ad ?? "", Margin = new Thickness(0, 0, 12, 10), ToolTip = string.Format(Dil.M("a.varsayilan"), s.VarsayilanAd) };
                var yer = new TextBlock
                {
                    Text = s.VarsayilanAd,
                    Foreground = new SolidColorBrush(Color.FromArgb(110, 255, 255, 255)),
                    IsHitTestVisible = false,
                    Margin = new Thickness(9, 0, 0, 10),
                    VerticalAlignment = VerticalAlignment.Center,
                    Visibility = string.IsNullOrEmpty(ad.Text) ? Visibility.Visible : Visibility.Collapsed,
                };
                var deger = new TextBlock
                {
                    Text = $"{Bicim.Sayi(s.Deger)} {birim}",
                    Foreground = (Brush)FindResource("Soluk"),
                    TextAlignment = TextAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 10),
                };
                string kimlik = s.Kimlik;
                goster.Click += (o, e) =>
                {
                    _ayarlar.EtiketOlustur(kimlik).Gizli = goster.IsChecked != true;
                    _uygulama.AyarlarDegisti();
                };
                ad.TextChanged += (o, e) =>
                {
                    yer.Visibility = string.IsNullOrEmpty(ad.Text) ? Visibility.Visible : Visibility.Collapsed;
                    _ayarlar.EtiketOlustur(kimlik).Ad = string.IsNullOrWhiteSpace(ad.Text) ? null : ad.Text.Trim();
                    _uygulama.AyarlarDegisti();
                };
                foreach (var (oge, sutun) in new (UIElement, int)[] { (goster, 0), (ad, 1), (yer, 1), (deger, 2) })
                {
                    Grid.SetRow(oge, i);
                    Grid.SetColumn(oge, sutun);
                    Sensorler.Children.Add(oge);
                }
                i++;
            }
        }

        // ---------- Başlangıç ve hakkında ----------

        void HakkindaSayfasi()
        {
            bool? baslangic = _uygulama.BaslangictaMi();
            Baslangic.IsChecked = baslangic == true;
            Baslangic.IsEnabled = baslangic != null;
            if (baslangic == null)
                BaslangicNotu.Text = Dil.M("a.baslangicKapali");
            Baslangic.Click += (s, e) =>
            {
                _uygulama.BaslangicDegistir();
                Baslangic.IsChecked = _uygulama.BaslangictaMi() == true;
            };
            Surum.Text = $"Ayaz HUD {App.Surum}";

            // Seçili dilin düğmesi vurgu renginde çerçeveli
            var secili = Dil.Kod == Dil.Ingilizce ? DilIngilizce : DilTurkce;
            secili.BorderBrush = (Brush)FindResource("Vurgu");
            secili.BorderThickness = new Thickness(2);
        }

        void DilTurkce_Click(object sender, RoutedEventArgs e) => _uygulama.DilDegistir(Dil.Turkce);

        void DilIngilizce_Click(object sender, RoutedEventArgs e) => _uygulama.DilDegistir(Dil.Ingilizce);

        /// <summary>Dil değişince pencere yeniden açılır; kullanıcı aynı sayfada kalsın.</summary>
        internal void GenelSayfasiniAc() => Menu.SelectedIndex = 3;

        void Gunluk_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(Gunluk.Dosya)) Process.Start(new ProcessStartInfo("notepad.exe", $"\"{Gunluk.Dosya}\"") { UseShellExecute = false });
        }

        // ---------- Yardımcı seçiciler ----------

        /// <summary>Önizlemeli kart stili kutuları.</summary>
        sealed class StilSecici
        {
            readonly List<(KartStili Stil, Border Cerceve, StilOnizleme Onizleme)> _kutular = new List<(KartStili, Border, StilOnizleme)>();
            readonly Func<KartStili> _secili;
            readonly Func<(Color Vurgu, double Koyuluk, int Buzlanma)> _renk;

            public StilSecici(Panel panel, Func<KartStili> secili, Action<KartStili> sec, Func<(Color, double, int)> renk)
            {
                _secili = secili;
                _renk = renk;
                foreach (KartStili stil in Enum.GetValues(typeof(KartStili)))
                {
                    var onizleme = new StilOnizleme { Width = 132, Height = 82 };
                    var cerceve = new Border
                    {
                        CornerRadius = new CornerRadius(10),
                        BorderThickness = new Thickness(2),
                        Padding = new Thickness(3),
                        Child = new Border { CornerRadius = new CornerRadius(7), ClipToBounds = true, Child = onizleme },
                    };
                    var kutu = new StackPanel { Margin = new Thickness(0, 0, 12, 12), Cursor = System.Windows.Input.Cursors.Hand, Background = Brushes.Transparent };
                    kutu.Children.Add(cerceve);
                    kutu.Children.Add(new TextBlock { Text = Gorunum.StilAdi(stil), Margin = new Thickness(4, 6, 0, 0), HorizontalAlignment = HorizontalAlignment.Center });
                    var s = stil;
                    kutu.MouseLeftButtonUp += (o, e) => sec(s);
                    panel.Children.Add(kutu);
                    _kutular.Add((stil, cerceve, onizleme));
                }
                Yenile();
            }

            public void Yenile()
            {
                var secili = _secili();
                var (vurgu, koyuluk, buzlanma) = _renk();
                foreach (var (stil, cerceve, onizleme) in _kutular)
                {
                    cerceve.BorderBrush = stil == secili ? new SolidColorBrush(vurgu) : new SolidColorBrush(Color.FromRgb(39, 43, 56));
                    onizleme.Ayarla(stil, vurgu, koyuluk, buzlanma);
                }
            }
        }

        /// <summary>Hazır renkler ve elle renk kodu.</summary>
        sealed class RenkSecici
        {
            static readonly (string Ad, string Renk)[] HazirRenkler =
            {
                ("renk.buz", "#7DD3FC"), ("renk.nane", "#6EE7B7"), ("renk.lavanta", "#C4B5FD"), ("renk.pembe", "#F9A8D4"),
                ("renk.seftali", "#FDBA74"), ("renk.limon", "#FDE68A"), ("renk.mercan", "#FCA5A5"), ("renk.gumus", "#E2E8F0"),
            };

            readonly List<(Ellipse Halka, string Renk)> _halkalar = new List<(Ellipse, string)>();
            readonly TextBox _kutu = new TextBox { Width = 92, Margin = new Thickness(8, 0, 0, 0), ToolTip = Dil.M("renk.ozel") };
            readonly Func<string> _secili;
            bool _yaziliyor;

            public RenkSecici(Panel panel, Func<string> secili, Action<string> sec)
            {
                _secili = secili;
                foreach (var (ad, renk) in HazirRenkler)
                {
                    Tema.RenkCoz(renk, out var c);
                    var halka = new Ellipse { Width = 34, Height = 34, StrokeThickness = 2, Stroke = Brushes.Transparent };
                    var dugme = new Grid { Margin = new Thickness(0, 0, 6, 0), Cursor = System.Windows.Input.Cursors.Hand, Background = Brushes.Transparent, ToolTip = Dil.M(ad) };
                    dugme.Children.Add(halka);
                    dugme.Children.Add(new Ellipse { Width = 24, Height = 24, Fill = new SolidColorBrush(c) });
                    string r = renk;
                    dugme.MouseLeftButtonUp += (o, e) =>
                    {
                        sec(r);
                        Yenile();
                    };
                    _halkalar.Add((halka, renk));
                    panel.Children.Add(dugme);
                }
                _kutu.TextChanged += (o, e) =>
                {
                    string metin = _kutu.Text.Trim();
                    if (_yaziliyor || !Tema.RenkCoz(metin, out var c)) return;
                    sec(Tema.RenkMetni(c));
                    Halkalar();
                };
                panel.Children.Add(_kutu);
                Yenile();
            }

            public void Yenile()
            {
                _yaziliyor = true;
                _kutu.Text = _secili() ?? "";
                _yaziliyor = false;
                Halkalar();
            }

            void Halkalar()
            {
                string secili = _secili();
                foreach (var (halka, renk) in _halkalar)
                    halka.Stroke = string.Equals(renk, secili, StringComparison.OrdinalIgnoreCase) ? Brushes.White : Brushes.Transparent;
            }
        }
    }
}
