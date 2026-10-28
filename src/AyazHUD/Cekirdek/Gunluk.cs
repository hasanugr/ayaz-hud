using System;
using System.IO;
using System.Text;

namespace AyazHUD.Cekirdek
{
    /// <summary>
    /// %LOCALAPPDATA%\AyazHUD\gunluk.txt dosyasına kısa kayıtlar yazar. Bir arkadaşın bilgisayarında
    /// bir şey ters giderse (yanlış sensör, FPS yok) neden olduğu bu dosyadan anlaşılır.
    /// </summary>
    internal static class Gunluk
    {
        public static readonly string Klasor = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AyazHUD");
        public static readonly string Dosya = Path.Combine(Klasor, "gunluk.txt");
        static readonly object Kilit = new object();

        public static void Yaz(string mesaj)
        {
            try
            {
                lock (Kilit)
                {
                    Directory.CreateDirectory(Klasor);
                    var bilgi = new FileInfo(Dosya);
                    if (bilgi.Exists && bilgi.Length > 1_000_000)
                    {
                        string eski = Dosya + ".1";
                        if (File.Exists(eski)) File.Delete(eski);
                        File.Move(Dosya, eski);
                    }
                    File.AppendAllText(Dosya, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {mesaj}{Environment.NewLine}", Encoding.UTF8);
                }
            }
            catch (Exception)
            {
                // Günlük yazılamıyorsa yapacak bir şey yok; uygulama çalışmaya devam etmeli
            }
        }

        public static void Yaz(string baslik, Exception e) => Yaz($"{baslik}: {e}");
    }
}
