using System;
using System.Windows;
using System.Windows.Media;

namespace SistemPaneli.Arayuz
{
    /// <summary>
    /// Renkler, fırçalar ve yazı tipleri. Fırçalar tek örnek olarak paylaşılır ve yerinde değiştirilir;
    /// böylece vurgu rengi ya da koyuluk değişince bütün widget'lar kendiliğinden güncellenir.
    /// Ölçüler Wallpaper Engine sürümündeki tasarımdan gelir (1rem = 16 px).
    /// </summary>
    internal static class Tema
    {
        public static readonly FontFamily SayiYazi = new FontFamily("Bahnschrift");
        public static readonly FontFamily MetinYazi = new FontFamily("Segoe UI Variable Text, Segoe UI");

        public static readonly SolidColorBrush Metin = Donmus(Color.FromArgb(242, 255, 255, 255));  // %95
        public static readonly SolidColorBrush Soluk = Donmus(Color.FromArgb(148, 255, 255, 255));  // %58
        public static readonly SolidColorBrush KartCizgi = Donmus(Color.FromArgb(36, 255, 255, 255)); // %14
        public static readonly SolidColorBrush SatirCizgi = Donmus(Color.FromArgb(18, 255, 255, 255)); // %7
        public static readonly SolidColorBrush Iz = Donmus(Color.FromArgb(26, 255, 255, 255));       // %10

        public static Color VurguRengi { get; private set; } = Color.FromRgb(125, 211, 252);
        public static double Koyuluk { get; private set; } = 0.45;
        public static bool Cam { get; private set; } = true;

        public static readonly SolidColorBrush Vurgu = new SolidColorBrush();
        public static readonly SolidColorBrush Baslik = new SolidColorBrush();      // vurgu %95
        public static readonly SolidColorBrush GrafikDolgu = new SolidColorBrush(); // vurgu %16
        public static readonly SolidColorBrush KartZemin = new SolidColorBrush();
        public static readonly LinearGradientBrush UstParilti = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
        public static readonly LinearGradientBrush CubukDolgu = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };

        /// <summary>Tema değişince çizimini kendisi yapan denetimler kendini yeniler.</summary>
        public static event Action Degisti;

        static Tema() => Uygula(VurguRengi, Koyuluk, Cam);

        public static void Uygula(Color vurgu, double koyuluk, bool cam)
        {
            VurguRengi = vurgu;
            Koyuluk = Math.Max(0, Math.Min(0.95, koyuluk));
            Cam = cam;

            Vurgu.Color = vurgu;
            Baslik.Color = Alfa(vurgu, 0.95);
            GrafikDolgu.Color = Alfa(vurgu, 0.16);
            // Cam kapalıyken arkası bulanık olmadığı için zemin biraz daha koyu olmalı
            KartZemin.Color = Color.FromArgb((byte)Math.Round(255 * (cam ? Koyuluk : Math.Min(0.95, Koyuluk + 0.2))), 12, 14, 22);

            UstParilti.GradientStops = new GradientStopCollection
            {
                new GradientStop(Alfa(vurgu, 0), 0),
                new GradientStop(Alfa(vurgu, 0.7), 0.5),
                new GradientStop(Alfa(vurgu, 0), 1),
            };
            CubukDolgu.GradientStops = new GradientStopCollection
            {
                new GradientStop(Alfa(vurgu, 0.55), 0),
                new GradientStop(vurgu, 1),
            };

            // XAML'dan {DynamicResource Vurgu} ile kullanılır. Kaynak sözlüğü eklenen fırçayı dondurduğu için
            // paylaşılan fırça değil, her seferinde yeni bir kopya konur.
            if (Application.Current != null) Application.Current.Resources["Vurgu"] = Donmus(vurgu);
            Degisti?.Invoke();
        }

        public static Color Alfa(Color c, double a) => Color.FromArgb((byte)Math.Round(255 * a), c.R, c.G, c.B);

        /// <summary>35 °C'de buz mavisi, 65 °C civarı kehribar, 85 °C ve üstü kırmızı.</summary>
        public static Color SicaklikRengi(double? derece)
        {
            if (derece == null) return VurguRengi;
            double oran = Math.Min(1, Math.Max(0, (derece.Value - 35) / 50));
            return Hsl(195 - oran * 195, 0.9, oran > 0.7 ? 0.62 : 0.70);
        }

        public static Color Hsl(double ton, double doygunluk, double acik)
        {
            double c = (1 - Math.Abs(2 * acik - 1)) * doygunluk;
            double h = (ton % 360) / 60;
            double x = c * (1 - Math.Abs(h % 2 - 1));
            double r = 0, g = 0, b = 0;
            if (h < 1) { r = c; g = x; }
            else if (h < 2) { r = x; g = c; }
            else if (h < 3) { g = c; b = x; }
            else if (h < 4) { g = x; b = c; }
            else if (h < 5) { r = x; b = c; }
            else { r = c; b = x; }
            double m = acik - c / 2;
            return Color.FromRgb((byte)Math.Round((r + m) * 255), (byte)Math.Round((g + m) * 255), (byte)Math.Round((b + m) * 255));
        }

        public static bool RenkCoz(string metin, out Color renk)
        {
            renk = default;
            try
            {
                var c = ColorConverter.ConvertFromString(metin);
                if (c is Color r) { renk = Color.FromRgb(r.R, r.G, r.B); return true; }
            }
            catch (FormatException) { }
            return false;
        }

        public static string RenkMetni(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        static SolidColorBrush Donmus(Color c)
        {
            var f = new SolidColorBrush(c);
            f.Freeze();
            return f;
        }
    }
}
