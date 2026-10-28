using System;
using System.Windows;
using System.Windows.Media;

namespace AyazHUD.Arayuz
{
    /// <summary>Yuvarlak uçlu doluluk çubuğu (bellek, VRAM).</summary>
    internal sealed class Cubuk : FrameworkElement
    {
        public static readonly DependencyProperty OranProperty = DependencyProperty.Register(
            nameof(Oran), typeof(double), typeof(Cubuk), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public double Oran { get => (double)GetValue(OranProperty); set => SetValue(OranProperty, value); }

        public Cubuk()
        {
            Height = 8.8;
            IsHitTestVisible = false;
        }

        public void Ayarla(double? oran) => Canlandirma.Sayi(this, OranProperty, Math.Min(1, Math.Max(0, oran ?? 0)), 0.03);

        protected override void OnRender(DrawingContext dc)
        {
            double w = ActualWidth, h = ActualHeight, r = h / 2;
            dc.DrawRoundedRectangle(Tema.Iz, null, new Rect(0, 0, w, h), r, r);
            double dolu = w * Oran;
            if (dolu < 0.5) return;
            // Çok küçük oranlarda da yuvarlak uç bozulmasın
            dc.PushClip(new RectangleGeometry(new Rect(0, 0, w, h), r, r));
            dc.DrawRoundedRectangle(Gorunum.Al(this).CubukDolgu, null, new Rect(Math.Min(0, dolu - h), 0, Math.Max(dolu, h), h), r, r);
            dc.Pop();
        }
    }
}
