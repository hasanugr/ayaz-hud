using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AyazHUD.Arayuz
{
    /// <summary>
    /// Ayarlar penceresindeki küçük önizleme: örnek bir duvar kâğıdı üstünde, seçilen stil, renk ve
    /// koyulukla çizilmiş minik bir kart. Gerçek bulanıklık masaüstünde Windows tarafından yapılır;
    /// burada WPF'in bulanıklık efektiyle taklit edilir.
    /// </summary>
    internal sealed class StilOnizleme : Grid
    {
        static readonly Brush DuvarKagidi = OrnekDuvarKagidi();
        static readonly Brush Doku = Gurultu();

        KartStili _stil = KartStili.Bulanik;
        Color _vurgu = Tema.VarsayilanVurgu;
        double _koyuluk = 0.45;
        int _buzlanma, _kartBuzlanmasi = Gorunum.VarsayilanBuzlanma;
        double _karartma;

        public StilOnizleme()
        {
            ClipToBounds = true;
            Kur();
            SizeChanged += (s, e) => Kur();
        }

        public void Ayarla(KartStili stil, Color vurgu, double koyuluk, int kartBuzlanmasi)
        {
            _stil = stil;
            _vurgu = vurgu;
            _koyuluk = koyuluk;
            _kartBuzlanmasi = Gorunum.KartBuzlanmasi(kartBuzlanmasi);
            Kur();
        }

        /// <summary>Arka plan önizlemesi: kartın arkasındaki bütün duvar kâğıdı buzlu (yüzde) ve/veya karartılmış.</summary>
        public void Buzlanma(int yuzde, double karartma)
        {
            _buzlanma = yuzde;
            _karartma = karartma;
            Kur();
        }

        void Kur()
        {
            Children.Clear();
            double w = ActualWidth > 0 ? ActualWidth : 160, h = ActualHeight > 0 ? ActualHeight : 100;

            Children.Add(new Rectangle { Fill = DuvarKagidi });
            Buzlu(_buzlanma, null);
            if (_karartma > 0)
                Children.Add(new Rectangle { Fill = new SolidColorBrush(Color.FromArgb((byte)(255 * _karartma), 8, 10, 16)) });

            // Kart: önizlemenin ortasında
            var kart = new Rect(w * 0.12, h * 0.16, w * 0.76, h * 0.68);
            var sekil = new RectangleGeometry(kart, 6, 6);
            sekil.Freeze();
            if (_stil == KartStili.Bulanik || _stil == KartStili.Akrilik)
            {
                // Masaüstündeki gibi: arka plan buzluysa kartın buzlanması onunla toplanır
                Children.Add(new Rectangle { Fill = DuvarKagidi, Clip = sekil });
                Buzlu(Math.Min(100, _kartBuzlanmasi + _buzlanma), sekil);
                if (_karartma > 0)
                    Children.Add(new Rectangle { Fill = new SolidColorBrush(Color.FromArgb((byte)(255 * _karartma), 8, 10, 16)), Clip = sekil });
                if (_stil == KartStili.Akrilik)
                {
                    Children.Add(new Rectangle { Fill = new SolidColorBrush(Color.FromArgb(34, 255, 255, 255)), Clip = sekil });
                    Children.Add(new Rectangle { Fill = Doku, Clip = sekil });
                }
            }
            double zemin = _stil == KartStili.Koyu ? Math.Max(0.8, _koyuluk) : _stil == KartStili.Seffaf ? 0 : _stil == KartStili.Akrilik ? _koyuluk * 0.5 : _koyuluk;
            var kartYuzu = new Path
            {
                Data = sekil,
                Fill = new SolidColorBrush(Color.FromArgb((byte)Math.Round(255 * zemin), 12, 14, 22)),
                Stroke = _stil == KartStili.Seffaf ? null : new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                StrokeThickness = 1,
            };
            Children.Add(kartYuzu);

            // Minik içerik: başlık, halka, sayı
            var icerik = new Canvas();
            var baslik = new TextBlock { Text = Dil.M("b.islemci"), FontSize = 8, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(_vurgu) };
            Canvas.SetLeft(baslik, kart.Left + 9);
            Canvas.SetTop(baslik, kart.Top + 6);
            icerik.Children.Add(baslik);
            double r = kart.Height * 0.27, mx = kart.Left + 9 + r + 2, my = kart.Top + kart.Height * 0.6;
            icerik.Children.Add(Yay(mx, my, r, 270, new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 3.2));
            icerik.Children.Add(Yay(mx, my, r, 160, new SolidColorBrush(_vurgu), 3.2));
            var sayi = new TextBlock { Text = "56°", FontFamily = Tema.SayiYazi, FontSize = r * 0.85, Foreground = Brushes.White };
            sayi.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(sayi, mx - sayi.DesiredSize.Width / 2);
            Canvas.SetTop(sayi, my - sayi.DesiredSize.Height / 2);
            icerik.Children.Add(sayi);
            for (int i = 0; i < 3; i++)
            {
                var satir = new Rectangle
                {
                    Width = kart.Width * 0.32,
                    Height = 3,
                    RadiusX = 1.5,
                    RadiusY = 1.5,
                    Fill = new SolidColorBrush(Color.FromArgb((byte)(i == 0 ? 200 : 110), 255, 255, 255)),
                };
                Canvas.SetLeft(satir, kart.Right - kart.Width * 0.32 - 10);
                Canvas.SetTop(satir, my - r * 0.8 + i * r * 0.8);
                icerik.Children.Add(satir);
            }
            if (_stil == KartStili.Seffaf)
                icerik.Effect = new DropShadowEffect { Color = Colors.Black, Opacity = 0.7, BlurRadius = 6, ShadowDepth = 1, Direction = 270 };
            Children.Add(icerik);
        }

        /// <summary>
        /// Buzlanmanın taklidi, masaüstündekiyle aynı yapıda: Windows bulanıklığı büyük yarıçaplı, masaüstü kopyası
        /// küçük yarıçaplı bir bulanıklık; ara düzeylerde kopya bulanıklığın üstünde yarı saydam.
        /// </summary>
        void Buzlu(int yuzde, Geometry kirpma)
        {
            var s = BuzSeviyesi.Al(yuzde);
            // Kenarlarda bulanıklık soluklaşmasın diye kırpılmayan katman taşırılır
            var tasma = kirpma == null ? new Thickness(-40) : new Thickness(0);
            if (s.Katman > 0)
                Children.Add(new Rectangle { Fill = DuvarKagidi, Effect = new BlurEffect { Radius = 8 + 8 * s.Katman }, Clip = kirpma, Margin = tasma });
            if (s.Kopyali)
                Children.Add(new Rectangle { Fill = DuvarKagidi, Effect = new BlurEffect { Radius = 1.5 * s.Yaricap }, Opacity = s.Opaklik, Clip = kirpma, Margin = tasma });
        }

        static Path Yay(double mx, double my, double r, double aci, Brush firca, double kalinlik)
        {
            double a0 = 135 * Math.PI / 180, a1 = (135 + aci) * Math.PI / 180;
            var g = new StreamGeometry();
            using (var c = g.Open())
            {
                c.BeginFigure(new Point(mx + r * Math.Cos(a0), my + r * Math.Sin(a0)), false, false);
                c.ArcTo(new Point(mx + r * Math.Cos(a1), my + r * Math.Sin(a1)), new Size(r, r), 0, aci > 180, SweepDirection.Clockwise, true, false);
            }
            g.Freeze();
            return new Path { Data = g, Stroke = firca, StrokeThickness = kalinlik, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };
        }

        /// <summary>Renkli lekelerden oluşan soyut bir duvar kâğıdı; bulanıklığın farkı görülsün diye canlı renkli.</summary>
        static Brush OrnekDuvarKagidi()
        {
            var grup = new DrawingGroup();
            grup.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(24, 20, 44)), null, new RectangleGeometry(new Rect(0, 0, 160, 100))));
            void Leke(double x, double y, double r, Color c) =>
                grup.Children.Add(new GeometryDrawing(new RadialGradientBrush(c, Color.FromArgb(0, c.R, c.G, c.B)), null, new EllipseGeometry(new Point(x, y), r, r)));
            Leke(30, 20, 55, Color.FromRgb(139, 92, 246));
            Leke(135, 85, 60, Color.FromRgb(251, 146, 60));
            Leke(95, 35, 38, Color.FromRgb(45, 212, 191));
            Leke(20, 95, 40, Color.FromRgb(236, 72, 153));
            // Keskin ayrıntılar: bulanıklık açıkken bunlar dağılır
            for (int i = 0; i < 9; i++)
                grup.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)), null,
                    new RectangleGeometry(new Rect(12 + i * 17, 46 + (i % 3) * 9, 2, 22))));
            var f = new DrawingBrush(grup) { Stretch = Stretch.Fill };
            f.Freeze();
            return f;
        }

        /// <summary>Akriliğin hafif kumlu dokusu.</summary>
        static Brush Gurultu()
        {
            const int boyut = 64;
            var piksel = new byte[boyut * boyut * 4];
            var rastgele = new Random(7);
            for (int i = 0; i < boyut * boyut; i++)
            {
                byte v = (byte)rastgele.Next(256);
                piksel[i * 4] = v;
                piksel[i * 4 + 1] = v;
                piksel[i * 4 + 2] = v;
                piksel[i * 4 + 3] = 14;
            }
            var resim = BitmapSource.Create(boyut, boyut, 96, 96, PixelFormats.Bgra32, null, piksel, boyut * 4);
            var f = new ImageBrush(resim) { TileMode = TileMode.Tile, Viewport = new Rect(0, 0, boyut, boyut), ViewportUnits = BrushMappingMode.Absolute };
            f.Freeze();
            return f;
        }
    }
}
