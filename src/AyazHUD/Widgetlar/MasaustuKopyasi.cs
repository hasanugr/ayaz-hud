using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using AyazHUD.Cekirdek;
using AyazHUD.Yerel;

namespace AyazHUD.Widgetlar
{
    /// <summary>
    /// Bir pencerenin içine masaüstünün (duvar kâğıdı ve simgeler) canlı ve hafif bulanık kopyasını çizer.
    /// DWM küçük resmi (thumbnail) birkaç piksel kaydırılarak eşit ağırlıkla üst üste bindirilir; böylece
    /// Windows'un sabit ve güçlü bulanıklığından çok daha hafif bir buzlanma elde edilir. Çizimi DWM ekran
    /// kartında yapar, uygulama hiçbir kare işlemez; Wallpaper Engine gibi hareketli duvar kâğıtları da canlı görünür.
    /// Kopyalar pencerenin kendi içeriğinin üstüne çizildiği için yalnızca boş katman pencerelerinde kullanılır.
    /// </summary>
    internal sealed class MasaustuKopyasi : IDisposable
    {
        // Merkez, yarım yarıçapta 6 ve tam yarıçapta 12 örnek; aynı piksele düşenler birleştirilir
        const int IcHalka = 6, DisHalka = 12;

        readonly IntPtr _hedef;
        readonly List<(IntPtr Kopya, IntPtr Kaynak, int Dx, int Dy, byte Opaklik)> _kopyalar = new List<(IntPtr, IntPtr, int, int, byte)>();
        double _yaricap = -1, _opaklik = -1;
        List<IntPtr> _kurulanKaynaklar;
        Win32.RECT _alan, _sinir;

        public MasaustuKopyasi(IntPtr hedef)
        {
            _hedef = hedef;
            Izle(this);
        }

        /// <param name="alan">Hedef pencerenin ekrandaki yeri (fiziksel piksel).</param>
        /// <param name="sinir">Örneklerin taşmayacağı alan: pencerenin monitörü.</param>
        /// <param name="yaricap">Bulanıklık yarıçapı (fiziksel piksel).</param>
        /// <param name="opaklik">Kopyanın toplam opaklığı; 1'in altındaysa pencerenin arkası da görünür.</param>
        public void Ayarla(Win32.RECT alan, Win32.RECT sinir, double yaricap, double opaklik)
        {
            _alan = alan;
            _sinir = sinir;
            if (yaricap != _yaricap || opaklik != _opaklik || _kurulanKaynaklar != _kaynaklar) Kur(yaricap, opaklik);
            else Konumla();
        }

        void Kur(double yaricap, double opaklik)
        {
            Temizle();
            _yaricap = yaricap;
            _opaklik = opaklik;
            _kurulanKaynaklar = _kaynaklar;
            if (opaklik <= 0) return;

            // Üst üste bindirmede her örneğin payı ağırlığı kadar olsun: bir örneğin opaklığı, ağırlığının
            // kendisinden sonra çizilenlerin bıraktığı paya oranıdır
            var desen = Desen(yaricap);
            var opakliklar = new double[desen.Count];
            double ustundeki = 0;
            for (int i = desen.Count - 1; i >= 0; i--)
            {
                double pay = desen[i].Agirlik * opaklik;
                opakliklar[i] = pay / (1 - ustundeki);
                ustundeki += pay;
            }
            for (int i = 0; i < desen.Count; i++)
                foreach (var kaynak in _kaynaklar)
                    if (Win32.DwmRegisterThumbnail(_hedef, kaynak, out var kopya) == 0)
                        _kopyalar.Add((kopya, kaynak, desen[i].Dx, desen[i].Dy, (byte)Math.Max(1, Math.Round(255 * Math.Min(1, opakliklar[i])))));
            Konumla();
        }

        void Konumla()
        {
            var kaynakAlanlari = new Dictionary<IntPtr, Win32.RECT>();
            foreach (var k in _kopyalar)
            {
                if (!kaynakAlanlari.TryGetValue(k.Kaynak, out var ka))
                {
                    Win32.GetWindowRect(k.Kaynak, out ka);
                    kaynakAlanlari[k.Kaynak] = ka;
                }
                // Ekranda örneklenen alan: pencere kadar, (Dx, Dy) kaydırılmış; monitörün ve kaynağın dışı kırpılır.
                // Kırpılan kenar şeridinde bu örnek eksik kalır, orada pencerenin arkası görünür.
                int sol = Math.Max(_alan.Left + k.Dx, Math.Max(_sinir.Left, ka.Left));
                int ust = Math.Max(_alan.Top + k.Dy, Math.Max(_sinir.Top, ka.Top));
                int sag = Math.Min(_alan.Right + k.Dx, Math.Min(_sinir.Right, ka.Right));
                int alt = Math.Min(_alan.Bottom + k.Dy, Math.Min(_sinir.Bottom, ka.Bottom));
                var ozellik = new Win32.DWM_THUMBNAIL_PROPERTIES { dwFlags = Win32.DWM_TNP_VISIBLE };
                if (sag > sol && alt > ust)
                {
                    ozellik.dwFlags |= Win32.DWM_TNP_RECTDESTINATION | Win32.DWM_TNP_RECTSOURCE | Win32.DWM_TNP_OPACITY | Win32.DWM_TNP_SOURCECLIENTAREAONLY;
                    ozellik.fVisible = 1;
                    ozellik.rcSource = new Win32.RECT { Left = sol - ka.Left, Top = ust - ka.Top, Right = sag - ka.Left, Bottom = alt - ka.Top };
                    int x = _alan.Left + k.Dx, y = _alan.Top + k.Dy;
                    ozellik.rcDestination = new Win32.RECT { Left = sol - x, Top = ust - y, Right = sag - x, Bottom = alt - y };
                    ozellik.opacity = k.Opaklik;
                }
                Win32.DwmUpdateThumbnailProperties(k.Kopya, ref ozellik);
            }
        }

        static List<(int Dx, int Dy, double Agirlik)> Desen(double yaricap)
        {
            var noktalar = new List<(int, int)> { (0, 0) };
            void Halka(int adet, double r, double baslangic)
            {
                for (int i = 0; i < adet; i++)
                {
                    double a = baslangic + i * 2 * Math.PI / adet;
                    noktalar.Add(((int)Math.Round(r * Math.Cos(a)), (int)Math.Round(r * Math.Sin(a))));
                }
            }
            if (yaricap > 0)
            {
                Halka(IcHalka, yaricap / 2, Math.PI / IcHalka);
                Halka(DisHalka, yaricap, 0);
            }
            return noktalar.GroupBy(n => n).Select(g => (g.Key.Item1, g.Key.Item2, g.Count() / (double)noktalar.Count)).ToList();
        }

        void Temizle()
        {
            foreach (var k in _kopyalar) Win32.DwmUnregisterThumbnail(k.Kopya);
            _kopyalar.Clear();
        }

        public void Dispose()
        {
            Temizle();
            _hepsi.Remove(this);
            if (_hepsi.Count == 0) _izleyici?.Stop();
        }

        // ---------- Masaüstü pencereleri ----------
        // Explorer yeniden başlarsa ya da canlı duvar kâğıdı açılıp kapanırsa pencereler değişir; birkaç saniyede bir bakılır.

        static List<IntPtr> _kaynaklar = KaynaklariBul(true);
        static readonly List<MasaustuKopyasi> _hepsi = new List<MasaustuKopyasi>();
        static DispatcherTimer _izleyici;

        static void Izle(MasaustuKopyasi k)
        {
            _hepsi.Add(k);
            if (_izleyici == null)
            {
                _izleyici = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                _izleyici.Tick += (s, e) =>
                {
                    var yeni = KaynaklariBul(false);
                    if (yeni.SequenceEqual(_kaynaklar)) return;
                    _kaynaklar = KaynaklariBul(true);
                    foreach (var x in _hepsi.ToList()) x.Kur(x._yaricap, x._opaklik);
                };
            }
            _izleyici.Start();
        }

        /// <summary>
        /// Masaüstünü oluşturan görünür pencereler, alttan üste. Windows 11 24H2'den beri duvar kâğıdı (Wallpaper Engine
        /// dahil) ve simgeler tek bir Progman penceresinin içinde. Öncesinde canlı duvar kâğıdı ve simgeler Explorer'ın
        /// ayrı WorkerW pencerelerinde; o zaman Progman onların altında örtülü kalır.
        /// </summary>
        static List<IntPtr> KaynaklariBul(bool yaz)
        {
            var progman = new List<IntPtr>();
            var workerw = new List<IntPtr>();
            Win32.EnumWindows((h, v) =>
            {
                if (!Win32.IsWindowVisible(h)) return true;
                string sinif = Win32.SinifAdi(h);
                if (sinif == "Progman") progman.Add(h);
                else if (sinif == "WorkerW") workerw.Add(h);
                return true;
            }, IntPtr.Zero);
            uint explorer = 0;
            if (progman.Count > 0) Win32.GetWindowThreadProcessId(progman[0], out explorer);
            workerw.RemoveAll(h =>
            {
                Win32.GetWindowThreadProcessId(h, out uint p);
                Win32.GetWindowRect(h, out var r);
                return p != explorer || r.Genislik < 200 || r.Yukseklik < 200;
            });
            var liste = workerw.Count > 0 ? workerw : progman;
            liste.Reverse();
            if (yaz) Gunluk.Yaz("Masaüstü kopyası kaynağı: " + (liste.Count == 0 ? "yok" : string.Join(", ", liste.Select(h => $"{Win32.SinifAdi(h)} 0x{h.ToInt64():X}"))));
            return liste;
        }
    }
}
