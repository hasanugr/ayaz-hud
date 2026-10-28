using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using AyazHUD.Yerel;

namespace AyazHUD.Cekirdek
{
    /// <summary>
    /// Oyun FPS'ini Intel PresentMon ile ölçer. PresentMon, Windows'un ETW olaylarından her karenin ne zaman
    /// ekrana verildiğini okur; oyuna hiçbir şey enjekte etmez. Yönetici yetkisi ister.
    /// </summary>
    internal sealed class FpsOlcer : IDisposable
    {
        const double FpsPencereMs = 1000;   // anlık FPS için son 1 saniye
        const double DusukPencereMs = 10000; // %1 düşük için son 10 saniye
        const double OyunEsikFps = 15;      // bunun altındaki süreçler oyun sayılmaz
        const int YenidenBaslatmaMs = 30000;

        // Kare çizen ama oyun olmayan programlar (ad içinde geçmesi yeterli)
        static readonly string[] Haric =
        {
            "dwm", "explorer", "wallpaper", "steamwebhelper", "msedge", "chrome", "firefox", "opera", "brave",
            "discord", "claude", "code", "librehardwaremonitor", "searchhost", "searchapp", "startmenuexperiencehost",
            "shellexperiencehost", "textinputhost", "lockapp", "applicationframehost", "nvidia", "nvcontainer",
            "armoury", "zalman", "steam.exe", "epicgameslauncher", "eadesktop", "ubisoftconnect", "spotify",
            "whatsapp", "obs64", "gamebar", "sistempaneli", "presentmon", "<unknown>",
        };

        sealed class Surec
        {
            public string Ad;
            public readonly Queue<Kare> Kareler = new Queue<Kare>();
        }

        struct Kare
        {
            public double Zaman, Ms;
            /// <summary>Karenin hazırlanmaya başlamasından ekranda görünmesine kadar geçen süre; ekrana çıkmadıysa NaN.</summary>
            public double Gecikme;
        }

        readonly string _exe;
        readonly IntPtr _isNesnesi;
        readonly object _kilit = new object();
        readonly Dictionary<int, Surec> _surecler = new Dictionary<int, Surec>();
        Process _pm;
        Timer _yenidenDene;
        bool _kapandi;
        volatile FpsHali _hal = FpsHali.Baslatiliyor;

        public FpsOlcer(string exe)
        {
            _exe = exe;
            // Uygulama nasıl kapanırsa kapansın PresentMon da kapansın
            _isNesnesi = IsNesnesi.Olustur();
        }

        static double Simdi => Stopwatch.GetTimestamp() * 1000.0 / Stopwatch.Frequency;

        public void Baslat()
        {
            lock (_kilit)
                if (_kapandi) return;
            if (!File.Exists(_exe))
            {
                _hal = FpsHali.Hata;
                Gunluk.Yaz("PresentMon bulunamadı: " + _exe);
                return;
            }

            // Varsayılan (2.x) ölçümler: kare süresi ve görüntü gecikmesi. Gecikme için karelerin
            // ekrana çıkışı izlenir; ekran kartı ve fare/klavye izlenmez.
            var bilgi = new ProcessStartInfo(_exe,
                "--output_stdout --no_console_stats --session_name AyazHUD --stop_existing_session " +
                "--no_track_input --no_track_gpu")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
            };
            try
            {
                var p = new Process { StartInfo = bilgi };
                var hata = new StringBuilder();
                p.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null) lock (hata) hata.AppendLine(e.Data);
                };
                p.Start();
                IsNesnesi.Ekle(_isNesnesi, p);
                p.BeginErrorReadLine();
                lock (_kilit) _pm = p;
                new Thread(() => Oku(p, hata)) { IsBackground = true, Name = "PresentMon" }.Start();
            }
            catch (Exception e)
            {
                _hal = FpsHali.Hata;
                Gunluk.Yaz("PresentMon başlatılamadı", e);
                YenidenDene();
            }
        }

        void Oku(Process p, StringBuilder hata)
        {
            int ad = -1, pid = -1, ms = -1, gecikme = -1, ekranaCikis = -1, cpuSuresi = -1;
            bool baslik = true;
            try
            {
                string satir;
                while ((satir = p.StandardOutput.ReadLine()) != null)
                {
                    if (satir.Length == 0) continue;
                    var alan = satir.Split(',');
                    if (baslik)
                    {
                        // CSV başlığından gerekli sütunların yerini bul (sürümler arasında adlar değişebiliyor)
                        int Sutun(params string[] adlar) => Array.FindIndex(alan, a => adlar.Any(x => a.Equals(x, StringComparison.OrdinalIgnoreCase)));
                        ad = Sutun("Application");
                        pid = Sutun("ProcessID");
                        ms = Sutun("MsBetweenPresents", "FrameTime", "MsBetweenAppStart");
                        gecikme = Sutun("DisplayLatency", "MsDisplayLatency");
                        // 2.6'da hazır gecikme sütunu yok: karenin işlenmeye başlamasından Present() çağrısına kadar
                        // geçen süre (MsCPUBusy) ile Present()'ten ekrana çıkışa kadar geçen süre (MsUntilDisplayed) toplanır
                        ekranaCikis = Sutun("MsUntilDisplayed");
                        cpuSuresi = Sutun("MsCPUBusy");
                        if (ad < 0 || pid < 0 || ms < 0) Gunluk.Yaz("PresentMon başlığı beklenmedik: " + satir);
                        else if (gecikme < 0 && (ekranaCikis < 0 || cpuSuresi < 0)) Gunluk.Yaz("PresentMon gecikme sütunu yok: " + satir);
                        baslik = false;
                        _hal = FpsHali.OyunYok;
                        Gunluk.Yaz("PresentMon başladı, sütunlar: " + satir);
                        continue;
                    }
                    if (ad < 0 || pid < 0 || ms < 0 || alan.Length <= new[] { ad, pid, ms, gecikme, ekranaCikis, cpuSuresi }.Max()) continue;
                    if (!double.TryParse(alan[ms], NumberStyles.Float, CultureInfo.InvariantCulture, out double sure) || sure <= 0 || sure > 1000) continue;
                    if (!int.TryParse(alan[pid], out int no)) continue;
                    // Ekrana çıkmayan karelerde gecikme "NA" yazar
                    double Deger(int sutun) =>
                        sutun >= 0 && double.TryParse(alan[sutun], NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : double.NaN;
                    double gec = gecikme >= 0 ? Deger(gecikme) : Deger(ekranaCikis) + Deger(cpuSuresi);
                    if (!(gec > 0 && gec <= 1000)) gec = double.NaN;

                    double t = Simdi;
                    lock (_kilit)
                    {
                        if (!_surecler.TryGetValue(no, out var s)) _surecler[no] = s = new Surec { Ad = alan[ad] };
                        s.Kareler.Enqueue(new Kare { Zaman = t, Ms = sure, Gecikme = gec });
                        while (s.Kareler.Peek().Zaman < t - DusukPencereMs) s.Kareler.Dequeue();
                    }
                }
            }
            catch (Exception e)
            {
                Gunluk.Yaz("PresentMon çıktısı okunamadı", e);
            }

            p.WaitForExit(3000);
            int kod = p.HasExited ? p.ExitCode : -1;
            string metin;
            lock (hata) metin = hata.ToString().Trim();
            _hal = kod == 6 || Regex.IsMatch(metin, "access denied|administrative privileges", RegexOptions.IgnoreCase) ? FpsHali.IzinYok : FpsHali.Hata;
            lock (_kilit) _surecler.Clear();
            if (!_kapandi) Gunluk.Yaz($"PresentMon kapandı (kod {kod}): {metin.Split('\n').FirstOrDefault()}");
            YenidenDene();
        }

        void YenidenDene()
        {
            lock (_kilit)
            {
                if (_kapandi) return;
                _yenidenDene?.Dispose();
                _yenidenDene = new Timer(_ => Baslat(), null, YenidenBaslatmaMs, Timeout.Infinite);
            }
        }

        /// <summary>Son karelere göre oyunun FPS'i. Ön plandaki pencere kare çiziyorsa o seçilir.</summary>
        public FpsDurumu Hesapla()
        {
            double simdi = Simdi;
            int onPlan = OnPlandakiSurec();
            lock (_kilit)
            {
                Surec enIyi = null;
                List<Kare> enIyiSon = null;
                double enIyiFps = 0;
                bool enIyiOnde = false;
                foreach (var no in _surecler.Keys.ToList())
                {
                    var s = _surecler[no];
                    while (s.Kareler.Count > 0 && s.Kareler.Peek().Zaman < simdi - DusukPencereMs) s.Kareler.Dequeue();
                    if (s.Kareler.Count == 0)
                    {
                        _surecler.Remove(no);
                        continue;
                    }
                    if (HaricMi(s.Ad)) continue;
                    var son = s.Kareler.Where(k => k.Zaman >= simdi - FpsPencereMs).ToList();
                    if (son.Count < 2) continue;
                    double fps = son.Count * 1000 / son.Sum(k => k.Ms);
                    if (fps < OyunEsikFps) continue;
                    bool onde = no == onPlan;
                    if (enIyi == null || (onde && !enIyiOnde) || (onde == enIyiOnde && fps > enIyiFps))
                    {
                        enIyi = s;
                        enIyiSon = son;
                        enIyiFps = fps;
                        enIyiOnde = onde;
                    }
                }
                if (enIyi == null) return new FpsDurumu { Hal = _hal };

                // %1 düşük: son 10 saniyedeki en yavaş %1'lik karelerin ortalaması
                var sureler = enIyi.Kareler.Select(k => k.Ms).OrderByDescending(x => x).ToList();
                int adet = Math.Max(1, sureler.Count / 100);
                double yavas = sureler.Take(adet).Average();
                var gecikmeler = enIyiSon.Where(k => !double.IsNaN(k.Gecikme)).Select(k => k.Gecikme).ToList();
                return new FpsDurumu
                {
                    Hal = FpsHali.Tamam,
                    Deger = (int)Math.Round(enIyiFps),
                    Yuzde1Dusuk = (int)Math.Round(1000 / yavas),
                    KareSuresi = Math.Round(enIyiSon.Average(k => k.Ms), 1),
                    Gecikme = gecikmeler.Count == 0 ? (double?)null : Math.Round(gecikmeler.Average(), 1),
                    Uygulama = Regex.Replace(enIyi.Ad, @"\.exe$", "", RegexOptions.IgnoreCase),
                };
            }
        }

        static bool HaricMi(string ad)
        {
            string k = ad.ToLowerInvariant();
            return Haric.Any(h => k.Contains(h));
        }

        static int OnPlandakiSurec()
        {
            Win32.GetWindowThreadProcessId(Win32.GetForegroundWindow(), out uint pid);
            return (int)pid;
        }

        public void Dispose()
        {
            Process p;
            lock (_kilit)
            {
                _kapandi = true;
                _yenidenDene?.Dispose();
                p = _pm;
            }
            try
            {
                if (p != null && !p.HasExited) p.Kill();
            }
            catch (Exception e)
            {
                Gunluk.Yaz("PresentMon kapatılamadı", e);
            }
        }
    }
}
