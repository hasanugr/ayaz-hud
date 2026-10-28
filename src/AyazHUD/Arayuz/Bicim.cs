namespace AyazHUD.Arayuz
{
    /// <summary>Seçili dile göre sayı biçimi: Türkçede "3.075 RPM", "4,85 GHz"; İngilizcede "3,075 RPM", "4.85 GHz".</summary>
    internal static class Bicim
    {
        public static string Sayi(double? d, int basamak = 0) =>
            d == null || double.IsNaN(d.Value) || double.IsInfinity(d.Value) ? "--" : d.Value.ToString("N" + basamak, Dil.Kultur);

        /// <summary>Bayt/sn değerini 1 MB/s altında KB/s, üstünde MB/s olarak verir.</summary>
        public static (string Deger, string Birim) Hiz(double? baytSn)
        {
            if (baytSn == null) return ("--", "MB/s");
            double mb = baytSn.Value / 1048576;
            if (mb < 1) return (Sayi(baytSn.Value / 1024), "KB/s");
            return (Sayi(mb, mb < 10 ? 1 : 0), "MB/s");
        }
    }
}
