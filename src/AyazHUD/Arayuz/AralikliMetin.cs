using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace AyazHUD.Arayuz
{
    /// <summary>
    /// Harf aralıklı metin. WPF'in TextBlock'unda harf aralığı ayarı yok; kart başlıklarındaki
    /// "İŞLEMCİ" gibi geniş aralıklı yazılar için harfler tek tek çizilir.
    /// </summary>
    internal sealed class AralikliMetin : FrameworkElement
    {
        const FrameworkPropertyMetadataOptions Olcu = FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender;

        public static readonly DependencyProperty MetinProperty = Kaydet(nameof(Metin), typeof(string), "");
        public static readonly DependencyProperty BoyutProperty = Kaydet(nameof(Boyut), typeof(double), 13.6);
        public static readonly DependencyProperty KalinlikProperty = Kaydet(nameof(Kalinlik), typeof(FontWeight), FontWeights.SemiBold);
        public static readonly DependencyProperty AralikProperty = Kaydet(nameof(Aralik), typeof(double), 0.16);
        public static readonly DependencyProperty RenkProperty = Kaydet(nameof(Renk), typeof(Brush), null);
        public static readonly DependencyProperty YaziTipiProperty = Kaydet(nameof(YaziTipi), typeof(FontFamily), null);

        public string Metin { get => (string)GetValue(MetinProperty); set => SetValue(MetinProperty, value); }
        /// <summary>Yazı boyutu (px).</summary>
        public double Boyut { get => (double)GetValue(BoyutProperty); set => SetValue(BoyutProperty, value); }
        public FontWeight Kalinlik { get => (FontWeight)GetValue(KalinlikProperty); set => SetValue(KalinlikProperty, value); }
        /// <summary>Harf aralığı, yazı boyutunun katı olarak (CSS'teki em).</summary>
        public double Aralik { get => (double)GetValue(AralikProperty); set => SetValue(AralikProperty, value); }
        public Brush Renk { get => (Brush)GetValue(RenkProperty); set => SetValue(RenkProperty, value); }
        public FontFamily YaziTipi { get => (FontFamily)GetValue(YaziTipiProperty); set => SetValue(YaziTipiProperty, value); }

        FormattedText[] _harfler;
        double _genislik, _yukseklik;

        static DependencyProperty Kaydet(string ad, System.Type tur, object varsayilan) =>
            DependencyProperty.Register(ad, tur, typeof(AralikliMetin), new FrameworkPropertyMetadata(varsayilan, Olcu, (d, e) => ((AralikliMetin)d)._harfler = null));

        protected override Size MeasureOverride(Size kisit)
        {
            Olustur();
            return new Size(_genislik, _yukseklik);
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (_harfler == null) Olustur();
            double x = 0;
            foreach (var h in _harfler)
            {
                dc.DrawText(h, new Point(x, 0));
                x += h.WidthIncludingTrailingWhitespace + Aralik * Boyut;
            }
        }

        protected override void OnDpiChanged(DpiScale eski, DpiScale yeni)
        {
            _harfler = null;
            InvalidateMeasure();
        }

        void Olustur()
        {
            string metin = Metin ?? "";
            var yazi = new Typeface(YaziTipi ?? Tema.MetinYazi, FontStyles.Normal, Kalinlik, FontStretches.Normal);
            double ppd = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var renk = Renk ?? Gorunum.Al(this).Baslik;
            _harfler = new FormattedText[metin.Length];
            _genislik = 0;
            _yukseklik = new FormattedText("Ğ", Bicim.Tr, FlowDirection.LeftToRight, yazi, Boyut, renk, ppd).Height;
            for (int i = 0; i < metin.Length; i++)
            {
                var h = new FormattedText(metin[i].ToString(), Bicim.Tr, FlowDirection.LeftToRight, yazi, Boyut, renk, ppd);
                _harfler[i] = h;
                _genislik += h.WidthIncludingTrailingWhitespace;
            }
            if (metin.Length > 1) _genislik += Aralik * Boyut * (metin.Length - 1);
        }
    }
}
