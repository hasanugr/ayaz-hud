using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace AyazHUD.Arayuz
{
    /// <summary>Son 90 saniyenin çizgi grafiği; altı vurgu renginde hafif dolgulu.</summary>
    internal sealed class CizgiGrafik : FrameworkElement
    {
        public const int Uzunluk = 90;

        readonly double[] _degerler = new double[Uzunluk];
        int _adet;

        /// <summary>Grafiğin tepe değeri; boşsa son değerlerin en büyüğüne göre ayarlanır.</summary>
        public double? UstSinir { get; set; }

        public CizgiGrafik() => IsHitTestVisible = false;

        public void Ekle(double? deger)
        {
            if (_adet == Uzunluk) Array.Copy(_degerler, 1, _degerler, 0, Uzunluk - 1);
            else _adet++;
            _degerler[_adet - 1] = deger ?? 0;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (_adet < 2 || ActualWidth <= 0) return;

            // Tasarım 300×44'lük bir alanda; denetimin boyutuna esnetilir
            double sx = ActualWidth / 300;
            double sy = ActualHeight / 44;
            double tepe = UstSinir ?? Math.Max(_degerler.Take(_adet).Max(), 1) * 1.15;
            double adim = 300.0 / (Uzunluk - 1);
            double bas = (Uzunluk - _adet) * adim;

            Point Nokta(int i) => new Point((bas + i * adim) * sx, (42 - Math.Min(_degerler[i], tepe) / tepe * 40) * sy);

            var cizgi = new StreamGeometry();
            using (var c = cizgi.Open())
            {
                c.BeginFigure(Nokta(0), false, false);
                for (int i = 1; i < _adet; i++) c.LineTo(Nokta(i), true, true);
            }
            cizgi.Freeze();

            var alan = new StreamGeometry();
            using (var c = alan.Open())
            {
                c.BeginFigure(Nokta(0), true, true);
                for (int i = 1; i < _adet; i++) c.LineTo(Nokta(i), false, true);
                c.LineTo(new Point(300 * sx, 44 * sy), false, false);
                c.LineTo(new Point(bas * sx, 44 * sy), false, false);
            }
            alan.Freeze();

            var g = Gorunum.Al(this);
            dc.DrawGeometry(g.GrafikDolgu, null, alan);
            dc.DrawGeometry(null, new Pen(g.Vurgu, 1.6) { LineJoin = PenLineJoin.Round }, cizgi);
        }
    }
}
