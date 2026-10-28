using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AyazHUD.Arayuz
{
    /// <summary>
    /// Açılır renk seçici: ton/doygunluk çarkı, parlaklık çubuğu, R/G/B ve renk kodu kutuları, hazır renkler ve
    /// kullanıcının kaydettiği renkler. Sürüklerken renk sürekli bildirilir (<see cref="Degisti"/>).
    /// </summary>
    internal sealed class RenkPaleti : Border
    {
        const double Genislik = 236;
        const double Cap = 200;     // çark
        const double Imlec = 16;    // parlaklık çubuğunun tutamağı
        const int EnFazlaKayitli = 13;

        static readonly string[] HazirRenkler =
        {
            "#EF4444", "#F472B6", "#C084FC", "#818CF8", "#3B82F6", "#38BDF8", "#22D3EE",
            "#34D399", "#4ADE80", "#FACC15", "#FBBF24", "#FB923C", "#F8FAFC", "#94A3B8",
        };

        /// <summary>Ton (açı) ve doygunluk (merkezden uzaklık) çarkı, tam parlaklıkta. Renk seçici düğmesi de bunu kullanır.</summary>
        public static readonly BitmapSource CarkResmi = CarkCiz();

        static readonly Brush Soluk = Tema.Donmus(Color.FromRgb(153, 160, 174));

        readonly Func<List<string>> _kayitlilar;
        readonly Action _kaydet;
        double _ton, _doygunluk, _parlaklik = 1;
        bool _yaziliyor;

        readonly Canvas _cark = new Canvas { Width = Cap, Height = Cap, Background = Brushes.Transparent, Cursor = Cursors.Cross };
        readonly Ellipse _karartma = new Ellipse { Width = Cap, Height = Cap, Fill = Brushes.Black, IsHitTestVisible = false };
        readonly Ellipse _carkImleci = new Ellipse { Width = 16, Height = 16, Stroke = Brushes.White, StrokeThickness = 2.5, IsHitTestVisible = false, Effect = new DropShadowEffect { BlurRadius = 4, ShadowDepth = 0, Opacity = 0.8 } };
        readonly Canvas _cubuk = new Canvas { Width = Genislik, Height = 22, Background = Brushes.Transparent, Cursor = Cursors.Hand, Margin = new Thickness(0, 14, 0, 0) };
        readonly LinearGradientBrush _cubukDolgu = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
        readonly Ellipse _cubukImleci = new Ellipse { Width = Imlec, Height = Imlec, Stroke = Brushes.White, StrokeThickness = 2.5, IsHitTestVisible = false, Effect = new DropShadowEffect { BlurRadius = 4, ShadowDepth = 0, Opacity = 0.8 } };
        readonly TextBox _r = Kutu(), _g = Kutu(), _b = Kutu(), _kod = Kutu();
        readonly Border _ornek = new Border { Width = 14, Height = 14, CornerRadius = new CornerRadius(3), Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center };
        readonly WrapPanel _kayitliPanel = new WrapPanel { Width = Genislik + 8 };

        /// <summary>Kullanıcı rengi değiştirdi (sürüklerken her adımda).</summary>
        public event Action<Color> Degisti;
        /// <summary>Kapat düğmesine basıldı.</summary>
        public event Action Kapat;

        public Color Renk => Tema.Hsv(_ton, _doygunluk, _parlaklik);

        /// <param name="kayitlilar">Kaydedilen renklerin listesi (ayarlardaki, ortak).</param>
        /// <param name="kaydet">Liste değişince ayarları kaydetmek için.</param>
        public RenkPaleti(Func<List<string>> kayitlilar, Action kaydet)
        {
            _kayitlilar = kayitlilar;
            _kaydet = kaydet;
            Background = new SolidColorBrush(Color.FromRgb(23, 26, 35));
            BorderBrush = new SolidColorBrush(Color.FromRgb(46, 51, 66));
            BorderThickness = new Thickness(1);
            CornerRadius = new CornerRadius(12);
            Padding = new Thickness(16, 12, 16, 16);
            Margin = new Thickness(12); // gölgeye yer
            Effect = new DropShadowEffect { BlurRadius = 18, ShadowDepth = 4, Opacity = 0.5 };

            var yigin = new StackPanel { Width = Genislik };
            Child = yigin;

            // Başlık ve kapat
            var baslik = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
            var kapat = new Button { Content = "✕", Padding = new Thickness(8, 2, 8, 3), Margin = new Thickness(0), FontSize = 12 };
            kapat.Click += (s, e) => Kapat?.Invoke();
            DockPanel.SetDock(kapat, Dock.Right);
            baslik.Children.Add(kapat);
            baslik.Children.Add(new TextBlock { Text = Dil.M("renk.secici"), FontSize = 15, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
            yigin.Children.Add(baslik);

            // Çark: parlaklık düştükçe üstündeki siyah daire koyulaşır
            _cark.Children.Add(new Image { Source = CarkResmi, Width = Cap, Height = Cap, IsHitTestVisible = false });
            _cark.Children.Add(_karartma);
            _cark.Children.Add(_carkImleci);
            _cark.MouseLeftButtonDown += (s, e) =>
            {
                _cark.CaptureMouse();
                CarktanSec(e.GetPosition(_cark));
                e.Handled = true;
            };
            _cark.MouseMove += (s, e) =>
            {
                if (_cark.IsMouseCaptured) CarktanSec(e.GetPosition(_cark));
            };
            _cark.MouseLeftButtonUp += (s, e) => _cark.ReleaseMouseCapture();
            yigin.Children.Add(new Border { Child = _cark, HorizontalAlignment = HorizontalAlignment.Center });

            // Parlaklık çubuğu
            var yol = new Border { Width = Genislik, Height = 10, CornerRadius = new CornerRadius(5), Background = _cubukDolgu, IsHitTestVisible = false };
            Canvas.SetTop(yol, 6);
            _cubuk.Children.Add(yol);
            Canvas.SetTop(_cubukImleci, 3);
            _cubuk.Children.Add(_cubukImleci);
            _cubuk.MouseLeftButtonDown += (s, e) =>
            {
                _cubuk.CaptureMouse();
                CubuktanSec(e.GetPosition(_cubuk));
                e.Handled = true;
            };
            _cubuk.MouseMove += (s, e) =>
            {
                if (_cubuk.IsMouseCaptured) CubuktanSec(e.GetPosition(_cubuk));
            };
            _cubuk.MouseLeftButtonUp += (s, e) => _cubuk.ReleaseMouseCapture();
            yigin.Children.Add(_cubuk);

            // R, G, B ve renk kodu
            var kutular = new Grid { Margin = new Thickness(0, 12, 0, 0) };
            for (int i = 0; i < 4; i++)
                kutular.ColumnDefinitions.Add(new ColumnDefinition { Width = i < 3 ? new GridLength(48) : new GridLength(1, GridUnitType.Star) });
            Alan(kutular, 0, "R", _r);
            Alan(kutular, 1, "G", _g);
            Alan(kutular, 2, "B", _b);
            var kodSatiri = new DockPanel();
            DockPanel.SetDock(_ornek, Dock.Left);
            kodSatiri.Children.Add(_ornek);
            kodSatiri.Children.Add(_kod);
            Alan(kutular, 3, Dil.M("renk.kod"), kodSatiri);
            yigin.Children.Add(kutular);
            foreach (var k in new[] { _r, _g, _b }) k.TextChanged += (s, e) => KutudanSec((TextBox)s);
            _kod.TextChanged += (s, e) => KutudanSec(_kod);

            // Hazır renkler
            var hazir = new WrapPanel { Width = Genislik + 8, Margin = new Thickness(0, 14, 0, 0) };
            foreach (var metin in HazirRenkler)
                if (Tema.RenkCoz(metin, out var c)) hazir.Children.Add(Ornek(c, metin, null));
            yigin.Children.Add(hazir);

            // Kaydedilen renkler
            yigin.Children.Add(new TextBlock { Text = Dil.M("renk.kayitli"), FontSize = 12, Foreground = Soluk, Margin = new Thickness(0, 6, 0, 8) });
            yigin.Children.Add(_kayitliPanel);
            KayitlilariCiz();
        }

        /// <summary>Seçicinin gösterdiği rengi değiştirir; <see cref="Degisti"/> çağrılmaz.</summary>
        public void Ayarla(Color renk)
        {
            var (ton, doygunluk, parlaklik) = Tema.HsvCoz(renk);
            // Gri ve siyahta ton (siyahta doygunluk da) belirsiz; çarktaki yer kaymasın diye öncekiler kalır
            if (doygunluk > 0.001 && parlaklik > 0.001) _ton = ton;
            if (parlaklik > 0.001) _doygunluk = doygunluk;
            _parlaklik = parlaklik;
            Ciz();
            KutulariYaz(null);
        }

        void CarktanSec(Point p)
        {
            double r = Cap / 2, dx = p.X - r, dy = r - p.Y;
            _ton = (Math.Atan2(dy, dx) * 180 / Math.PI + 360) % 360;
            _doygunluk = Math.Min(1, Math.Sqrt(dx * dx + dy * dy) / r);
            // Siyaha yakınken çarkta seçim görünmez; parlaklık açılır
            if (_parlaklik < 0.15) _parlaklik = 1;
            Bildir(null);
        }

        void CubuktanSec(Point p)
        {
            _parlaklik = Math.Max(0, Math.Min(1, (p.X - Imlec / 2) / (Genislik - Imlec)));
            Bildir(null);
        }

        void KutudanSec(TextBox kaynak)
        {
            if (_yaziliyor) return;
            Color renk;
            if (kaynak == _kod)
            {
                string metin = _kod.Text.Trim().TrimStart('#');
                if (metin.Length != 6 || !Tema.RenkCoz("#" + metin, out renk)) return;
            }
            else
            {
                if (!byte.TryParse(_r.Text.Trim(), out byte r) || !byte.TryParse(_g.Text.Trim(), out byte g) || !byte.TryParse(_b.Text.Trim(), out byte b)) return;
                renk = Color.FromRgb(r, g, b);
            }
            var (ton, doygunluk, parlaklik) = Tema.HsvCoz(renk);
            if (doygunluk > 0.001 && parlaklik > 0.001) _ton = ton;
            if (parlaklik > 0.001) _doygunluk = doygunluk;
            _parlaklik = parlaklik;
            Bildir(kaynak);
        }

        /// <summary>Çizimi ve (yazılan kutu hariç) kutuları günceller, rengi bildirir.</summary>
        void Bildir(TextBox yazilan)
        {
            Ciz();
            KutulariYaz(yazilan);
            Degisti?.Invoke(Renk);
        }

        void Ciz()
        {
            double r = Cap / 2;
            double a = _ton * Math.PI / 180;
            Canvas.SetLeft(_carkImleci, r + Math.Cos(a) * _doygunluk * r - _carkImleci.Width / 2);
            Canvas.SetTop(_carkImleci, r - Math.Sin(a) * _doygunluk * r - _carkImleci.Height / 2);
            _carkImleci.Fill = new SolidColorBrush(Renk);
            _karartma.Opacity = 1 - _parlaklik;
            _cubukDolgu.GradientStops = new GradientStopCollection
            {
                new GradientStop(Colors.Black, 0),
                new GradientStop(Tema.Hsv(_ton, _doygunluk, 1), 1),
            };
            Canvas.SetLeft(_cubukImleci, _parlaklik * (Genislik - Imlec));
            _cubukImleci.Fill = new SolidColorBrush(Renk);
            _ornek.Background = new SolidColorBrush(Renk);
        }

        void KutulariYaz(TextBox yazilan)
        {
            var c = Renk;
            _yaziliyor = true;
            if (yazilan != _r && yazilan != _g && yazilan != _b)
            {
                _r.Text = c.R.ToString();
                _g.Text = c.G.ToString();
                _b.Text = c.B.ToString();
            }
            if (yazilan != _kod) _kod.Text = Tema.RenkMetni(c).Substring(1);
            _yaziliyor = false;
        }

        void KayitlilariCiz()
        {
            _kayitliPanel.Children.Clear();
            var ekle = new Grid { Width = 24, Height = 24, Margin = new Thickness(0, 0, 8, 8), Cursor = Cursors.Hand, Background = Brushes.Transparent, ToolTip = Dil.M("renk.kaydet") };
            ekle.Children.Add(new Ellipse { Stroke = Soluk, StrokeThickness = 1.5, StrokeDashArray = new DoubleCollection { 2, 2 } });
            ekle.Children.Add(new TextBlock { Text = "+", Foreground = Soluk, FontSize = 15, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, -2, 0, 0) });
            ekle.MouseLeftButtonUp += (s, e) =>
            {
                var liste = _kayitlilar();
                string metin = Tema.RenkMetni(Renk);
                liste.RemoveAll(x => string.Equals(x, metin, StringComparison.OrdinalIgnoreCase));
                liste.Insert(0, metin);
                if (liste.Count > EnFazlaKayitli) liste.RemoveRange(EnFazlaKayitli, liste.Count - EnFazlaKayitli);
                _kaydet();
                KayitlilariCiz();
            };
            _kayitliPanel.Children.Add(ekle);
            foreach (var metin in _kayitlilar().ToList())
                if (Tema.RenkCoz(metin, out var c))
                    _kayitliPanel.Children.Add(Ornek(c, metin + " · " + Dil.M("renk.sil"), () =>
                    {
                        _kayitlilar().RemoveAll(x => string.Equals(x, metin, StringComparison.OrdinalIgnoreCase));
                        _kaydet();
                        KayitlilariCiz();
                    }));
        }

        /// <summary>Tıklanınca o rengi seçen yuvarlak; silinebilirse sağ tıkla silinir.</summary>
        FrameworkElement Ornek(Color renk, string ipucu, Action sil)
        {
            var e = new Ellipse
            {
                Width = 24,
                Height = 24,
                Margin = new Thickness(0, 0, 8, 8),
                Fill = new SolidColorBrush(renk),
                Stroke = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                StrokeThickness = 1,
                Cursor = Cursors.Hand,
                ToolTip = ipucu,
            };
            e.MouseLeftButtonUp += (s, a) =>
            {
                Ayarla(renk);
                Degisti?.Invoke(Renk);
            };
            if (sil != null) e.MouseRightButtonUp += (s, a) => sil();
            return e;
        }

        static TextBox Kutu() => new TextBox { MaxLength = 7, Padding = new Thickness(5, 3, 5, 3), FontSize = 13 };

        static void Alan(Grid izgara, int sutun, string etiket, FrameworkElement icerik)
        {
            var yigin = new StackPanel { Margin = new Thickness(0, 0, sutun < 3 ? 6 : 0, 0) };
            yigin.Children.Add(new TextBlock { Text = etiket, FontSize = 11.5, Foreground = Soluk, Margin = new Thickness(2, 0, 0, 3) });
            yigin.Children.Add(icerik);
            Grid.SetColumn(yigin, sutun);
            izgara.Children.Add(yigin);
        }

        static BitmapSource CarkCiz()
        {
            // Keskin görünsün diye iki kat çözünürlükte (192 DPI) çizilir
            const int boyut = 400;
            double r = boyut / 2.0;
            var piksel = new byte[boyut * boyut * 4];
            for (int y = 0; y < boyut; y++)
                for (int x = 0; x < boyut; x++)
                {
                    double dx = x + 0.5 - r, dy = r - (y + 0.5), d = Math.Sqrt(dx * dx + dy * dy);
                    double alfa = Math.Max(0, Math.Min(1, r - d + 0.5)); // kenar yumuşatma
                    if (alfa <= 0) continue;
                    var c = Tema.Hsv(Math.Atan2(dy, dx) * 180 / Math.PI, Math.Min(1, d / r), 1);
                    int i = (y * boyut + x) * 4;
                    piksel[i] = c.B;
                    piksel[i + 1] = c.G;
                    piksel[i + 2] = c.R;
                    piksel[i + 3] = (byte)Math.Round(255 * alfa);
                }
            var resim = BitmapSource.Create(boyut, boyut, 192, 192, PixelFormats.Bgra32, null, piksel, boyut * 4);
            resim.Freeze();
            return resim;
        }
    }
}
