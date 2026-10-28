using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using SistemPaneli.Arayuz;
using SistemPaneli.Ayar;
using SistemPaneli.Cekirdek;
using SistemPaneli.Widgetlar;
using SistemPaneli.Yerel;

namespace SistemPaneli
{
    /// <summary>Ayarlar penceresi. Her değişiklik hemen uygulanır ve kısa bir gecikmeyle kaydedilir.</summary>
    internal partial class AyarPenceresi : Window
    {
        static readonly (string Ad, string Renk)[] HazirRenkler =
        {
            ("Buz mavisi", "#7DD3FC"), ("Nane", "#6EE7B7"), ("Lavanta", "#C4B5FD"), ("Pembe", "#F9A8D4"),
            ("Şeftali", "#FDBA74"), ("Limon", "#FDE68A"), ("Mercan", "#FCA5A5"), ("Gümüş", "#E2E8F0"),
        };

        readonly App _uygulama;
        readonly Ayarlar _ayarlar;
        readonly WidgetYoneticisi _widgetlar;
        readonly List<(Ellipse Halka, string Renk)> _renkHalkalari = new List<(Ellipse, string)>();
        bool _yukleniyor = true;

        public AyarPenceresi(App uygulama, Ayarlar ayarlar, WidgetYoneticisi widgetlar)
        {
            _uygulama = uygulama;
            _ayarlar = ayarlar;
            _widgetlar = widgetlar;
            InitializeComponent();
            Icon = Imaging.CreateBitmapSourceFromHIcon(Tepsi.Simge().Handle, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

            // Görünüm
            foreach (var (ad, renk) in HazirRenkler)
            {
                Tema.RenkCoz(renk, out var c);
                var halka = new Ellipse { Width = 34, Height = 34, StrokeThickness = 2, Stroke = Brushes.Transparent };
                var dugme = new Button
                {
                    ToolTip = ad,
                    Margin = new Thickness(0, 0, 6, 0),
                    Padding = new Thickness(0),
                    Template = DaireSablonu(),
                    Content = new Grid { Children = { halka, new Ellipse { Width = 24, Height = 24, Fill = new SolidColorBrush(c) } } },
                };
                string secilen = renk;
                dugme.Click += (s, e) => RenkSec(secilen);
                _renkHalkalari.Add((halka, renk));
                Renkler.Items.Add(dugme);
            }
            RenkKutusu.Text = _ayarlar.Vurgu;
            RenkKutusu.TextChanged += (s, e) =>
            {
                if (!_yukleniyor && Tema.RenkCoz(RenkKutusu.Text.Trim(), out _)) RenkSec(RenkKutusu.Text.Trim(), false);
            };
            RenkHalkalariniGuncelle();

            Koyuluk.Value = Math.Round(_ayarlar.Koyuluk * 100);
            KoyulukDegeri.Text = $"%{Koyuluk.Value:0}";
            Koyuluk.ValueChanged += (s, e) =>
            {
                KoyulukDegeri.Text = $"%{Koyuluk.Value:0}";
                if (_yukleniyor) return;
                _ayarlar.Koyuluk = Koyuluk.Value / 100;
                Uygula();
            };
            Bulaniklik.IsChecked = _ayarlar.Bulaniklik;
            Bulaniklik.Click += (s, e) =>
            {
                _ayarlar.Bulaniklik = Bulaniklik.IsChecked == true;
                Uygula();
            };

            // Widget'lar
            foreach (var t in _widgetlar.Tanimlar)
            {
                string kimlik = t.Kimlik;
                var kutu = new CheckBox { Content = t.Ad, IsChecked = _widgetlar.AcikMi(kimlik) };
                kutu.Click += (s, e) => _widgetlar.Ac(kimlik, kutu.IsChecked == true);
                WidgetKutulari.Children.Add(kutu);
            }
            DuzenleDugmesiYaz();

            SensorListesi(_uygulama.SonAnlik);

            // Başlangıç
            bool? baslangic = _uygulama.BaslangictaMi();
            Baslangic.IsChecked = baslangic == true;
            Baslangic.IsEnabled = baslangic != null;
            if (baslangic == null)
            {
                BaslangicNotu.Visibility = Visibility.Visible;
                BaslangicNotu.Text = "Bu ayar, uygulama kurulum dosyasıyla kurulduğunda ve yönetici yetkisiyle çalışırken değiştirilebilir.";
            }
            Baslangic.Click += (s, e) =>
            {
                _uygulama.BaslangicDegistir();
                Baslangic.IsChecked = _uygulama.BaslangictaMi() == true;
            };

            Surum.Text = $"Sistem Paneli {App.Surum}";
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

        void Uygula()
        {
            _uygulama.TemaUygula();
            _uygulama.AyarlarDegisti();
        }

        void RenkSec(string renk, bool kutuyuYaz = true)
        {
            _ayarlar.Vurgu = renk.ToUpperInvariant();
            if (kutuyuYaz)
            {
                _yukleniyor = true;
                RenkKutusu.Text = _ayarlar.Vurgu;
                _yukleniyor = false;
            }
            RenkHalkalariniGuncelle();
            Uygula();
        }

        void RenkHalkalariniGuncelle()
        {
            foreach (var (halka, renk) in _renkHalkalari)
                halka.Stroke = string.Equals(renk, _ayarlar.Vurgu, StringComparison.OrdinalIgnoreCase) ? Brushes.White : Brushes.Transparent;
        }

        static ControlTemplate DaireSablonu()
        {
            var sablon = new ControlTemplate(typeof(Button));
            var sunucu = new FrameworkElementFactory(typeof(ContentPresenter));
            sablon.VisualTree = sunucu;
            return sablon;
        }

        void DuzenleDugmesiYaz() =>
            DuzenleDugmesi.Content = _widgetlar.Duzenleme ? "Düzenlemeyi bitir" : "Düzenle (widget'ları taşı)";

        void Duzenle_Click(object sender, RoutedEventArgs e)
        {
            _uygulama.DuzenlemeDegistir();
            DuzenleDugmesiYaz();
        }

        void Sifirla_Click(object sender, RoutedEventArgs e) => _widgetlar.KonumlariSifirla();

        void Gunluk_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(Gunluk.Dosya)) Process.Start(new ProcessStartInfo("notepad.exe", $"\"{Gunluk.Dosya}\"") { UseShellExecute = false });
        }

        /// <summary>Fan ve disklerin ad ve görünürlük satırları.</summary>
        void SensorListesi(Anlik a)
        {
            Sensorler.Children.Clear();
            Sensorler.RowDefinitions.Clear();
            Sensorler.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Sensorler.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Sensorler.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(96) });

            var satirlar = a.Fanlar.Select(f => (f, "RPM")).Concat(a.Diskler.Select(d => (d, "°C"))).ToList();
            if (satirlar.Count == 0)
            {
                Sensorler.Children.Add(new TextBlock
                {
                    Text = a.Hazir ? "Fan ya da disk sıcaklığı bulunamadı (yönetici izni olmadan okunamaz)." : "Sensörler henüz okunmadı; pencereyi birkaç saniye sonra yeniden aç.",
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
                var goster = new CheckBox { IsChecked = etiket?.Gizli != true, ToolTip = "Kartta göster", Margin = new Thickness(0, 0, 10, 8) };
                var ad = new TextBox { Text = etiket?.Ad ?? "", Margin = new Thickness(0, 0, 10, 8), ToolTip = "Varsayılan: " + s.VarsayilanAd };
                var yer = new TextBlock
                {
                    Text = s.VarsayilanAd,
                    Foreground = new SolidColorBrush(Color.FromArgb(110, 255, 255, 255)),
                    IsHitTestVisible = false,
                    Margin = new Thickness(9, 0, 0, 8),
                    VerticalAlignment = VerticalAlignment.Center,
                    Visibility = string.IsNullOrEmpty(ad.Text) ? Visibility.Visible : Visibility.Collapsed,
                };
                var deger = new TextBlock
                {
                    Text = $"{Bicim.Sayi(s.Deger)} {birim}",
                    Foreground = (Brush)FindResource("Soluk"),
                    TextAlignment = TextAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 8),
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
    }
}
