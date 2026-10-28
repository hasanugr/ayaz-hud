using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace SistemPaneli.Arayuz
{
    /// <summary>
    /// 270°'lik çift halka ölçer: dış kalın halka sıcaklık (renk de sıcaklığa göre), iç ince halka yük.
    /// Ölçüler 120×120'lik SVG tasarımından gelir ve denetimin boyutuna göre ölçeklenir.
    /// Katmanlar ayrı tutulur ki halkalar ilerlerken yazılar her karede yeniden çizilmesin.
    /// </summary>
    internal sealed class HalkaOlcer : Grid
    {
        const double Tasarim = 120;
        const double Baslangic = 135; // sol alttan başlar, saat yönünde sağ alta kadar gider
        const double TamAci = 270;
        const double GecisEsigi = 0.03; // halkanın %3'ünden küçük değişimler geçişsiz

        public static readonly DependencyProperty DisOranProperty = DependencyProperty.Register(
            nameof(DisOran), typeof(double), typeof(HalkaOlcer), new PropertyMetadata(0.0, (d, e) => ((HalkaOlcer)d).YaylariGuncelle()));

        public static readonly DependencyProperty IcOranProperty = DependencyProperty.Register(
            nameof(IcOran), typeof(double), typeof(HalkaOlcer), new PropertyMetadata(0.0, (d, e) => ((HalkaOlcer)d).YaylariGuncelle()));

        public static readonly DependencyProperty RenkProperty = DependencyProperty.Register(
            nameof(Renk), typeof(Color), typeof(HalkaOlcer), new PropertyMetadata(Tema.VurguRengi, (d, e) => ((HalkaOlcer)d).RenkUygula((Color)e.NewValue)));

        public double DisOran { get => (double)GetValue(DisOranProperty); set => SetValue(DisOranProperty, value); }
        public double IcOran { get => (double)GetValue(IcOranProperty); set => SetValue(IcOranProperty, value); }
        public Color Renk { get => (Color)GetValue(RenkProperty); set => SetValue(RenkProperty, value); }

        static readonly SolidColorBrush IcDolguFircasi = Dondur(Color.FromArgb(191, 255, 255, 255)); // %75

        readonly Zemin _zemin;
        readonly Path _icDolgu, _disDolgu;
        readonly SolidColorBrush _disFirca = new SolidColorBrush(Tema.VurguRengi);
        readonly DropShadowEffect _parilti = new DropShadowEffect { ShadowDepth = 0, BlurRadius = 10, Opacity = 0.85, Color = Tema.VurguRengi };
        bool _sicaklikVar;

        public HalkaOlcer()
        {
            _zemin = new Zemin(this);
            _icDolgu = YayYolu(IcDolguFircasi);
            _disDolgu = YayYolu(_disFirca);
            _disDolgu.Effect = _parilti;
            Children.Add(_zemin);
            Children.Add(_icDolgu);
            Children.Add(_disDolgu);
            SizeChanged += (s, e) => YaylariGuncelle();
            Tema.Degisti += () =>
            {
                if (!_sicaklikVar) RenkUygula(Tema.VurguRengi);
            };
        }

        /// <param name="ustSinir">Dış halkanın dolu sayılacağı sıcaklık (°C).</param>
        public void Ayarla(double? sicaklik, double? yuk, double ustSinir = 95)
        {
            _zemin.Sicaklik = Bicim.Sayi(sicaklik);
            _sicaklikVar = sicaklik != null;
            bool canli = Canlandirma.Sayi(this, DisOranProperty, sicaklik == null ? 0 : Math.Min(1, Math.Max(0, sicaklik.Value / ustSinir)), GecisEsigi);
            Canlandirma.Sayi(this, IcOranProperty, yuk == null ? 0 : Math.Min(1, Math.Max(0, yuk.Value / 100)), GecisEsigi);
            Canlandirma.Renk(this, RenkProperty, Tema.SicaklikRengi(sicaklik), canli);
        }

        void RenkUygula(Color renk)
        {
            _disFirca.Color = renk;
            _parilti.Color = renk;
        }

        double Olcek => ActualWidth > 0 ? ActualWidth / Tasarim : 1;

        void YaylariGuncelle()
        {
            double s = Olcek;
            _disDolgu.StrokeThickness = 9 * s;
            _disDolgu.Data = Yay(60 * s, 52 * s, TamAci * DisOran);
            _icDolgu.StrokeThickness = 5 * s;
            _icDolgu.Data = Yay(60 * s, 41 * s, TamAci * IcOran);
        }

        static Path YayYolu(Brush firca) => new Path
        {
            Stroke = firca,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            IsHitTestVisible = false,
        };

        static Geometry Yay(double merkez, double yaricap, double aci)
        {
            if (aci < 0.5) return Geometry.Empty;
            double a0 = Baslangic * Math.PI / 180;
            double a1 = (Baslangic + aci) * Math.PI / 180;
            var g = new StreamGeometry();
            using (var c = g.Open())
            {
                c.BeginFigure(new Point(merkez + yaricap * Math.Cos(a0), merkez + yaricap * Math.Sin(a0)), false, false);
                c.ArcTo(new Point(merkez + yaricap * Math.Cos(a1), merkez + yaricap * Math.Sin(a1)),
                    new Size(yaricap, yaricap), 0, aci > 180, SweepDirection.Clockwise, true, false);
            }
            g.Freeze();
            return g;
        }

        static SolidColorBrush Dondur(Color c)
        {
            var f = new SolidColorBrush(c);
            f.Freeze();
            return f;
        }

        /// <summary>İz halkaları ve ortadaki yazılar; yalnızca sıcaklık yazısı değişince yeniden çizilir.</summary>
        sealed class Zemin : FrameworkElement
        {
            readonly HalkaOlcer _o;
            string _sicaklik = "--";

            public Zemin(HalkaOlcer o)
            {
                _o = o;
                IsHitTestVisible = false;
            }

            public string Sicaklik
            {
                set
                {
                    if (_sicaklik == value) return;
                    _sicaklik = value;
                    InvalidateVisual();
                }
            }

            protected override void OnRender(DrawingContext dc)
            {
                // Kendi boyutu kullanılır: ilk çizimde üst denetimin boyutu henüz atanmamış olabiliyor
                if (ActualWidth <= 0) return;
                double s = ActualWidth / Tasarim;
                double m = 60 * s;

                dc.DrawGeometry(null, Kalem(Tema.Iz, 9 * s), Yay(m, 52 * s, TamAci));
                dc.DrawGeometry(null, Kalem(Tema.Iz, 5 * s), Yay(m, 41 * s, TamAci));

                double ppd = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                var sayiYazi = new Typeface(Tema.SayiYazi, FontStyles.Normal, FontWeights.Medium, FontStretches.Normal);
                var deger = new FormattedText(_sicaklik, Bicim.Tr, FlowDirection.LeftToRight, sayiYazi, 30 * s, Tema.Metin, ppd);
                var birim = new FormattedText("°C", Bicim.Tr, FlowDirection.LeftToRight, sayiYazi, 11 * s, Tema.Soluk, ppd);
                double toplam = deger.WidthIncludingTrailingWhitespace + 2 * s + birim.WidthIncludingTrailingWhitespace;
                double x = m - toplam / 2;
                dc.DrawText(deger, new Point(x, 62 * s - deger.Baseline));
                dc.DrawText(birim, new Point(x + deger.WidthIncludingTrailingWhitespace + 2 * s, 48 * s - birim.Baseline));

                // "SICAKLIK" etiketi, harf aralığı 0,2em
                var etiketYazi = new Typeface(Tema.MetinYazi, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
                const string etiket = "SICAKLIK";
                double boyut = 7.5 * s;
                var harfler = new FormattedText[etiket.Length];
                double genislik = 0.2 * boyut * (etiket.Length - 1);
                for (int i = 0; i < etiket.Length; i++)
                {
                    harfler[i] = new FormattedText(etiket[i].ToString(), Bicim.Tr, FlowDirection.LeftToRight, etiketYazi, boyut, Tema.Soluk, ppd);
                    genislik += harfler[i].WidthIncludingTrailingWhitespace;
                }
                x = m - genislik / 2;
                foreach (var h in harfler)
                {
                    dc.DrawText(h, new Point(x, 84 * s - h.Baseline));
                    x += h.WidthIncludingTrailingWhitespace + 0.2 * boyut;
                }
            }

            static Pen Kalem(Brush f, double kalinlik) =>
                new Pen(f, kalinlik) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        }
    }
}
