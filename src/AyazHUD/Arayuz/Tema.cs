using System;
using System.Windows;
using System.Windows.Media;

namespace AyazHUD.Arayuz
{
    /// <summary>
    /// Bütün widget'larda ortak renkler ve yazı tipleri. Vurgu rengi ve kart stili gibi widget'a göre
    /// değişebilenler <see cref="Gorunum"/> içinde. Ölçüler Wallpaper Engine sürümündeki tasarımdan gelir (1rem = 16 px).
    /// </summary>
    internal static class Tema
    {
        public static readonly FontFamily SayiYazi = new FontFamily("Bahnschrift");
        public static readonly FontFamily MetinYazi = new FontFamily("Segoe UI Variable Text, Segoe UI");

        public static readonly SolidColorBrush Metin = Donmus(Color.FromArgb(242, 255, 255, 255));  // %95
        public static readonly SolidColorBrush Soluk = Donmus(Color.FromArgb(148, 255, 255, 255));  // %58
        public static readonly SolidColorBrush SatirCizgi = Donmus(Color.FromArgb(18, 255, 255, 255)); // %7
        public static readonly SolidColorBrush Iz = Donmus(Color.FromArgb(26, 255, 255, 255));       // %10

        public static readonly Color VarsayilanVurgu = Color.FromRgb(125, 211, 252);

        /// <summary>Ayarlar penceresi gibi XAML'dan {DynamicResource Vurgu} ile kullanılan genel vurgu rengi.</summary>
        public static void GenelVurguYaz(Color vurgu)
        {
            // Kaynak sözlüğü eklenen fırçayı dondurduğu için her seferinde yeni bir fırça konur
            if (Application.Current != null) Application.Current.Resources["Vurgu"] = Donmus(vurgu);
        }

        public static Color Alfa(Color c, double a) => Color.FromArgb((byte)Math.Round(255 * a), c.R, c.G, c.B);

        /// <summary>35 °C'de buz mavisi, 65 °C civarı kehribar, 85 °C ve üstü kırmızı. Değer yoksa vurgu rengi.</summary>
        public static Color SicaklikRengi(double? derece, Color vurgu)
        {
            if (derece == null) return vurgu;
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

        /// <summary>Ton (0–360), doygunluk ve parlaklık (0–1) → renk. Renk seçicideki çark ve parlaklık çubuğu bunu kullanır.</summary>
        public static Color Hsv(double ton, double doygunluk, double parlaklik)
        {
            double c = parlaklik * doygunluk;
            double h = ((ton % 360) + 360) % 360 / 60;
            double x = c * (1 - Math.Abs(h % 2 - 1));
            double r = 0, g = 0, b = 0;
            if (h < 1) { r = c; g = x; }
            else if (h < 2) { r = x; g = c; }
            else if (h < 3) { g = c; b = x; }
            else if (h < 4) { g = x; b = c; }
            else if (h < 5) { r = x; b = c; }
            else { r = c; b = x; }
            double m = parlaklik - c;
            return Color.FromRgb((byte)Math.Round((r + m) * 255), (byte)Math.Round((g + m) * 255), (byte)Math.Round((b + m) * 255));
        }

        public static (double Ton, double Doygunluk, double Parlaklik) HsvCoz(Color renk)
        {
            double r = renk.R / 255.0, g = renk.G / 255.0, b = renk.B / 255.0;
            double enBuyuk = Math.Max(r, Math.Max(g, b)), enKucuk = Math.Min(r, Math.Min(g, b)), fark = enBuyuk - enKucuk;
            double ton = 0;
            if (fark > 0)
            {
                if (enBuyuk == r) ton = 60 * (((g - b) / fark) % 6);
                else if (enBuyuk == g) ton = 60 * ((b - r) / fark + 2);
                else ton = 60 * ((r - g) / fark + 4);
            }
            if (ton < 0) ton += 360;
            return (ton, enBuyuk > 0 ? fark / enBuyuk : 0, enBuyuk);
        }

        public static bool RenkCoz(string metin, out Color renk)
        {
            renk = default;
            if (string.IsNullOrWhiteSpace(metin)) return false;
            try
            {
                if (ColorConverter.ConvertFromString(metin.Trim()) is Color r)
                {
                    renk = Color.FromRgb(r.R, r.G, r.B);
                    return true;
                }
            }
            catch (FormatException)
            {
            }
            return false;
        }

        public static string RenkMetni(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        public static SolidColorBrush Donmus(Color c)
        {
            var f = new SolidColorBrush(c);
            f.Freeze();
            return f;
        }
    }
}
