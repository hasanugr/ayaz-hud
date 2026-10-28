using System;
using System.Windows;
using System.Windows.Media;

namespace SistemPaneli.Arayuz
{
    internal enum KartStili
    {
        /// <summary>Arkası bulanık, üstünde hafif koyu ton.</summary>
        Bulanik,
        /// <summary>Windows'un akrilik malzemesi: bulanıklık + hafif doku ve parlaklık.</summary>
        Akrilik,
        /// <summary>Bulanıklık yok, neredeyse opak koyu kart.</summary>
        Koyu,
        /// <summary>Kart yok; yazılar gölgeli olarak doğrudan duvar kâğıdının üstünde.</summary>
        Seffaf,
    }

    /// <summary>
    /// Bir widget'ın görünümü: stil, vurgu rengi, koyuluk ve bunlardan türeyen fırçalar.
    /// Her widget penceresinin kendi örneği vardır; pencerenin içindeki denetimler onu
    /// <see cref="Al"/> ile (miras alınan bağlı özellik üzerinden) bulur. Fırçalar yerinde
    /// değiştirildiği için renk değişince çizimler kendiliğinden güncellenir.
    /// </summary>
    internal sealed class Gorunum
    {
        public static readonly DependencyProperty OrnekProperty = DependencyProperty.RegisterAttached(
            "Ornek", typeof(Gorunum), typeof(Gorunum), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>Pencereye bağlı olmayan denetimler (ör. ayar önizlemeleri) için.</summary>
        public static readonly Gorunum Varsayilan = new Gorunum();

        public static Gorunum Al(DependencyObject d) => d?.GetValue(OrnekProperty) as Gorunum ?? Varsayilan;

        public static void Ata(DependencyObject d, Gorunum g) => d.SetValue(OrnekProperty, g);

        public Color VurguRengi { get; private set; } = Color.FromRgb(125, 211, 252);
        public double Koyuluk { get; private set; } = 0.45;
        public KartStili Stil { get; private set; } = KartStili.Bulanik;

        public readonly SolidColorBrush Vurgu = new SolidColorBrush();
        public readonly SolidColorBrush Baslik = new SolidColorBrush();      // vurgu %95
        public readonly SolidColorBrush GrafikDolgu = new SolidColorBrush(); // vurgu %16
        public readonly SolidColorBrush KartZemin = new SolidColorBrush();
        public readonly SolidColorBrush KartCizgi = new SolidColorBrush();
        public readonly LinearGradientBrush UstParilti = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
        public readonly LinearGradientBrush CubukDolgu = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };

        /// <summary>Renk ya da stil değişti; fırça dışında renk kullanan denetimler (parıltılar) kendini yeniler.</summary>
        public event Action Degisti;

        public Gorunum() => Uygula(VurguRengi, Koyuluk, Stil);

        public bool Bulanik => Stil == KartStili.Bulanik || Stil == KartStili.Akrilik;

        public void Uygula(Color vurgu, double koyuluk, KartStili stil)
        {
            VurguRengi = vurgu;
            Koyuluk = Math.Max(0, Math.Min(0.95, koyuluk));
            Stil = stil;

            Vurgu.Color = vurgu;
            Baslik.Color = Tema.Alfa(vurgu, 0.95);
            GrafikDolgu.Color = Tema.Alfa(vurgu, 0.16);
            KartCizgi.Color = stil == KartStili.Seffaf ? Colors.Transparent : Color.FromArgb(36, 255, 255, 255);
            double zemin;
            switch (stil)
            {
                case KartStili.Koyu: zemin = Math.Max(0.8, Koyuluk); break;
                case KartStili.Seffaf: zemin = 0; break;
                // Akrilikte Windows'un kendi açık tonu var; koyu ton yarıya indirilir ki buzlu görünüm kaybolmasın
                case KartStili.Akrilik: zemin = Koyuluk * 0.5; break;
                default: zemin = Koyuluk; break;
            }
            KartZemin.Color = Color.FromArgb((byte)Math.Round(255 * zemin), 12, 14, 22);
            UstParilti.GradientStops = new GradientStopCollection
            {
                new GradientStop(Tema.Alfa(vurgu, 0), 0),
                new GradientStop(Tema.Alfa(vurgu, stil == KartStili.Seffaf ? 0 : 0.7), 0.5),
                new GradientStop(Tema.Alfa(vurgu, 0), 1),
            };
            CubukDolgu.GradientStops = new GradientStopCollection
            {
                new GradientStop(Tema.Alfa(vurgu, 0.55), 0),
                new GradientStop(vurgu, 1),
            };
            Degisti?.Invoke();
        }

        public static KartStili StilCoz(string ad) =>
            Enum.TryParse(ad, true, out KartStili s) ? s : KartStili.Bulanik;

        public static string StilAdi(KartStili s)
        {
            switch (s)
            {
                case KartStili.Akrilik: return "Akrilik";
                case KartStili.Koyu: return "Koyu";
                case KartStili.Seffaf: return "Şeffaf";
                default: return "Buzlu cam";
            }
        }
    }
}
