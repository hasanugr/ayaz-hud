using System;

namespace AyazHUD.Arayuz
{
    /// <summary>
    /// Buzlanma yüzdesinin (10'ar 10'ar, 0–100) nasıl elde edildiği. Windows'un bulanıklık yarıçapı sabit ve
    /// güçlü olduğu için:
    /// - %10–50: masaüstünün kaydırılmış kopyaları üst üste bindirilir (bkz. Widgetlar.MasaustuKopyasi), yarıçap küçük.
    /// - %60–70: bu kopya Windows bulanıklığının üstüne yarı saydam konur.
    /// - %80–100: 1–3 Windows bulanıklığı katmanı üst üste.
    /// </summary>
    internal readonly struct BuzSeviyesi
    {
        /// <summary>Üst üste Windows bulanıklığı katmanı sayısı (0–3).</summary>
        public readonly int Katman;
        /// <summary>Masaüstü kopyasının bulanıklık yarıçapı, %100 ölçekte piksel.</summary>
        public readonly double Yaricap;
        /// <summary>Masaüstü kopyasının opaklığı; 0 ise kopya yok.</summary>
        public readonly double Opaklik;

        BuzSeviyesi(int katman, double yaricap, double opaklik)
        {
            Katman = katman;
            Yaricap = yaricap;
            Opaklik = opaklik;
        }

        public bool Kopyali => Opaklik > 0;
        public bool Var => Katman > 0 || Kopyali;

        public static int Yuvarla(int yuzde) => Math.Max(0, Math.Min(100, (int)Math.Round(yuzde / 10.0) * 10));

        public static BuzSeviyesi Al(int yuzde)
        {
            switch (Yuvarla(yuzde))
            {
                case 0: return new BuzSeviyesi(0, 0, 0);
                case 10: return new BuzSeviyesi(0, 1, 1);
                case 20: return new BuzSeviyesi(0, 2, 1);
                case 30: return new BuzSeviyesi(0, 3, 1);
                case 40: return new BuzSeviyesi(0, 4, 1);
                case 50: return new BuzSeviyesi(0, 5.5, 1);
                case 60: return new BuzSeviyesi(1, 7, 0.7);
                case 70: return new BuzSeviyesi(1, 7, 0.4);
                case 80: return new BuzSeviyesi(1, 0, 0);
                case 90: return new BuzSeviyesi(2, 0, 0);
                default: return new BuzSeviyesi(3, 0, 0);
            }
        }
    }
}
