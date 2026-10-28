using System;
using System.IO;
using System.Threading;
using System.Windows.Threading;

namespace AyazHUD.Cekirdek
{
    /// <summary>
    /// Uygulamanın tek kopya çalışmasını sağlar ve ikinci kopyadan gelen istekleri (ayarları aç, kapat) iletir.
    /// Çalışan kopya yönetici, yeni açılan kopya değilse Windows aralarındaki pencere mesajlarını ve olayları
    /// engelliyor; bu yüzden istekler kullanıcının yerel klasörüne bırakılan küçük dosyalarla iletilir.
    /// </summary>
    internal static class TekKopya
    {
        const string MutexAdi = "Local\\AyazHUD.TekKopya";
        const string IstekOnEki = "istek-";

        static Mutex _mutex;
        static FileSystemWatcher _izleyici;

        public static bool Al()
        {
            try
            {
                _mutex = new Mutex(true, MutexAdi, out bool yeni);
                return yeni;
            }
            catch (UnauthorizedAccessException)
            {
                // Yönetici yetkisiyle çalışan kopya var
                return false;
            }
        }

        public static bool CalisiyorMu()
        {
            try
            {
                Mutex.OpenExisting(MutexAdi).Dispose();
                return true;
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return true;
            }
        }

        /// <summary>Çalışan kopyaya istek bırakır ("ayarlar", "kapat").</summary>
        public static void IstekGonder(string komut)
        {
            try
            {
                Directory.CreateDirectory(Gunluk.Klasor);
                File.WriteAllText(Path.Combine(Gunluk.Klasor, IstekOnEki + komut), DateTime.Now.ToString("O"));
            }
            catch (Exception e)
            {
                Gunluk.Yaz("İstek gönderilemedi", e);
            }
        }

        public static void Dinle(Dispatcher arayuz, Action<string> istekGeldi)
        {
            Directory.CreateDirectory(Gunluk.Klasor);
            foreach (var eski in Directory.GetFiles(Gunluk.Klasor, IstekOnEki + "*")) Sil(eski);
            _izleyici = new FileSystemWatcher(Gunluk.Klasor, IstekOnEki + "*") { NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite };
            string sonKomut = null;
            DateTime sonZaman = DateTime.MinValue;
            FileSystemEventHandler isle = (s, e) =>
            {
                string komut = Path.GetFileName(e.FullPath).Substring(IstekOnEki.Length);
                Sil(e.FullPath);
                // Bir dosya yazılınca hem "oluşturuldu" hem "değişti" olayı gelir; aynı istek bir kez işlenir
                lock (_izleyici)
                {
                    if (komut == sonKomut && DateTime.UtcNow - sonZaman < TimeSpan.FromSeconds(1)) return;
                    sonKomut = komut;
                    sonZaman = DateTime.UtcNow;
                }
                arayuz.BeginInvoke(new Action(() => istekGeldi(komut)));
            };
            _izleyici.Created += isle;
            _izleyici.Changed += isle;
            _izleyici.EnableRaisingEvents = true;
        }

        static void Sil(string yol)
        {
            try
            {
                File.Delete(yol);
            }
            catch (IOException)
            {
                // Dosya hâlâ yazılıyor; bir sonraki olayda silinir
            }
        }
    }
}
