using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using AyazHUD.Arayuz;
using AyazHUD.Ayar;
using AyazHUD.Cekirdek;
using AyazHUD.Yerel;

namespace AyazHUD.Widgetlar
{
    /// <summary>Widget pencerelerini oluşturur, yerleştirir, gösterir/gizler ve konumlarını kaydeder.</summary>
    internal sealed class WidgetYoneticisi
    {
        public sealed class Tanim
        {
            public string Kimlik, Ad;
            public bool Kart;
            public int Sutun; // varsayılan yerleşimde 0 = sol, 1 = sağ
            public Func<IWidget> Olustur;
        }

        // Varsayılan yerleşim (Wallpaper Engine sürümündeki gibi): monitör kenarından 44 px içeride iki sütun
        const double Kenar = 44;
        const double Aralik = 17.6;
        const double Yapisma = 14; // sürükleyip bırakınca hizalanma mesafesi

        public readonly List<Tanim> Tanimlar;
        readonly Ayarlar _ayarlar;
        readonly Action _kaydet;
        readonly Dictionary<string, WidgetPenceresi> _pencereler = new Dictionary<string, WidgetPenceresi>();
        readonly DispatcherTimer _ekranBekleme;
        bool _duzenleme;

        public WidgetYoneticisi(Ayarlar ayarlar, Action kaydet)
        {
            _ayarlar = ayarlar;
            _kaydet = kaydet;
            Tanimlar = new List<Tanim>
            {
                new Tanim { Kimlik = "saat", Ad = "Saat", Kart = false, Sutun = 0, Olustur = () => new SaatWidget() },
                new Tanim { Kimlik = "islemci", Ad = "İşlemci", Kart = true, Sutun = 0, Olustur = () => new IslemciWidget() },
                new Tanim { Kimlik = "bellek", Ad = "Bellek", Kart = true, Sutun = 0, Olustur = () => new BellekWidget() },
                new Tanim { Kimlik = "sogutma", Ad = "Soğutma ve depolama", Kart = true, Sutun = 0, Olustur = () => new SogutmaWidget(ayarlar) },
                new Tanim { Kimlik = "fps", Ad = "FPS", Kart = true, Sutun = 1, Olustur = () => new FpsWidget() },
                new Tanim { Kimlik = "ekran-karti", Ad = "Ekran kartı", Kart = true, Sutun = 1, Olustur = () => new EkranKartiWidget() },
                new Tanim { Kimlik = "ag", Ad = "Ağ", Kart = true, Sutun = 1, Olustur = () => new AgWidget() },
            };
            // Monitör değişikliğinde Windows birkaç mesaj üst üste gönderir; durulunca bir kez yerleştir
            _ekranBekleme = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            _ekranBekleme.Tick += (s, e) =>
            {
                _ekranBekleme.Stop();
                Yerlestir(false);
            };
        }

        public void Baslat()
        {
            foreach (var t in Tanimlar.Where(t => _ayarlar.Widget(t.Kimlik).Acik))
                Pencere(t).Show();
            Yerlestir(true);
        }

        public bool AcikMi(string kimlik) => _pencereler.TryGetValue(kimlik, out var p) && p.IsVisible;

        public void Ac(string kimlik, bool acik)
        {
            var t = Tanimlar.First(x => x.Kimlik == kimlik);
            _ayarlar.Widget(kimlik).Acik = acik;
            if (acik && !AcikMi(kimlik))
            {
                var p = Pencere(t);
                p.Duzenleme = _duzenleme;
                p.Show();
                Yerlestir(true);
            }
            else if (!acik && _pencereler.TryGetValue(kimlik, out var p))
            {
                p.Hide();
                BuzUygula();
            }
            _kaydet();
        }

        // ---------- Görünüm ----------

        /// <summary>Ayarlardaki genel ve widget'a özel görünümü pencerelere ve arka plan katmanına uygular.</summary>
        public void GorunumleriUygula()
        {
            foreach (var p in _pencereler.Values) GorunumUygula(p);
            BuzUygula();
        }

        void GorunumUygula(WidgetPenceresi p)
        {
            var w = _ayarlar.Widget(p.Kimlik);
            bool ozel = w.OzelGorunum;
            if (!Tema.RenkCoz(ozel ? w.Vurgu : null, out var renk) && !Tema.RenkCoz(_ayarlar.Vurgu, out renk)) renk = Tema.VarsayilanVurgu;
            p.Gorunum.Uygula(renk, ozel ? w.Koyuluk : _ayarlar.Koyuluk, Gorunum.StilCoz(ozel ? w.Stil : _ayarlar.Stil),
                ozel ? w.Buzlanma : _ayarlar.Buzlanma);
        }

        readonly List<BuzKatmani> _buzlar = new List<BuzKatmani>();

        /// <summary>Arka plan ayarı açıksa widget'ların bulunduğu her monitöre buzlanma/karartma katmanı koyar.</summary>
        void BuzUygula()
        {
            var acik = _pencereler.Values.Where(p => p.IsVisible).ToList();
            var ekranlar = Ekran.Hepsi();
            Ekran EkraniBul(WidgetPenceresi p)
            {
                var r = p.Dikdortgen;
                return Ekran.Iceren(ekranlar, r.Left + r.Genislik / 2, r.Top + r.Yukseklik / 2);
            }
            var gereken = !_ayarlar.ArkaPlanAcik ? new List<Ekran>() : acik
                .Select(EkraniBul).Where(e => e != null).GroupBy(e => e.Ad).Select(g => g.First()).ToList();

            // Katman kapanınca ona bağlı pencereler de kapanır; önce widget'lar ayrılır
            foreach (var p in acik) p.TabanSahip = null;
            foreach (var b in _buzlar.Where(b => gereken.All(e => e.Ad != b.EkranAdi)).ToList())
            {
                b.Kapat();
                _buzlar.Remove(b);
            }
            foreach (var e in gereken)
            {
                var b = _buzlar.FirstOrDefault(x => x.EkranAdi == e.Ad);
                if (b == null) _buzlar.Add(b = new BuzKatmani(e));
                b.Ayarla(_ayarlar.ArkaPlanBuzlanma, _ayarlar.ArkaPlanKarartma);
                b.Yerlestir(e);
            }
            foreach (var p in acik)
            {
                var e = EkraniBul(p);
                p.TabanSahip = _buzlar.FirstOrDefault(b => b.EkranAdi == e?.Ad)?.Ust;
            }
        }

        public void Guncelle(Anlik a)
        {
            foreach (var p in _pencereler.Values)
                if (p.IsVisible) p.Icerik.Guncelle(a);
        }

        public bool Duzenleme
        {
            get => _duzenleme;
            set
            {
                _duzenleme = value;
                foreach (var p in _pencereler.Values) p.Duzenleme = value;
            }
        }

        public void KonumlariSifirla()
        {
            foreach (var w in _ayarlar.Widgetlar) w.Monitor = null;
            Yerlestir(false);
            _kaydet();
        }

        public void Kapat()
        {
            foreach (var p in _pencereler.Values)
            {
                p.TabanSahip = null;
                p.Close();
            }
            _pencereler.Clear();
            foreach (var b in _buzlar) b.Kapat();
            _buzlar.Clear();
        }

        WidgetPenceresi Pencere(Tanim t)
        {
            if (_pencereler.TryGetValue(t.Kimlik, out var p)) return p;
            p = new WidgetPenceresi(t.Kimlik, t.Olustur(), t.Kart) { Opacity = 0 };
            GorunumUygula(p);
            p.Tasindi += Tasindi;
            p.EkranlarDegisti += () =>
            {
                _ekranBekleme.Stop();
                _ekranBekleme.Start();
            };
            _pencereler[t.Kimlik] = p;
            return p;
        }

        // ---------- Yerleşim ----------

        /// <summary>Açık widget'ları kayıtlı konumlarına, kaydı olmayanları varsayılan yerleşime koyar.</summary>
        void Yerlestir(bool belirerek)
        {
            var ekranlar = Ekran.Hepsi();
            if (ekranlar.Count == 0) return;
            var acik = Tanimlar.Where(t => AcikMi(t.Kimlik)).Select(t => _pencereler[t.Kimlik]).ToList();
            foreach (var p in acik) p.UpdateLayout();
            var varsayilan = VarsayilanKonumlar(ekranlar);

            foreach (var p in acik)
            {
                var w = _ayarlar.Widget(p.Kimlik);
                var ekran = w.Monitor == null ? null : ekranlar.FirstOrDefault(e => e.Ad == w.Monitor);
                if (ekran != null)
                {
                    // Çözünürlük küçüldüyse ekran dışında kalmasın
                    var r = p.Dikdortgen;
                    int x = Math.Max(0, Math.Min(w.X, ekran.Alan.Genislik - r.Genislik));
                    int y = Math.Max(0, Math.Min(w.Y, ekran.Alan.Yukseklik - r.Yukseklik));
                    p.KonumaGit(ekran.Alan.Left + x, ekran.Alan.Top + y);
                }
                else
                {
                    var (x, y) = varsayilan[p.Kimlik];
                    p.KonumaGit(x, y);
                }
                if (p.Opacity < 1)
                {
                    if (belirerek) p.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, TimeSpan.FromSeconds(0.4)));
                    else p.Opacity = 1;
                }
            }
            BuzUygula();
        }

        Dictionary<string, (int X, int Y)> VarsayilanKonumlar(List<Ekran> ekranlar)
        {
            var ekran = Ekran.Varsayilan(ekranlar);
            double s = ekran.Olcek;
            var alan = ekran.CalismaAlani;
            int kenar = (int)Math.Round(Kenar * s);
            int aralik = (int)Math.Round(Aralik * s);
            int genislik = (int)Math.Round(WidgetPenceresi.KartGenisligi * s);
            int[] sutunX = { alan.Left + kenar, alan.Right - kenar - genislik };
            int[] sutunY = { alan.Top + kenar, alan.Top + kenar };

            var sonuc = new Dictionary<string, (int, int)>();
            foreach (var t in Tanimlar)
            {
                if (!AcikMi(t.Kimlik)) continue;
                var p = _pencereler[t.Kimlik];
                var ic = p.IcKenar(s);
                // Pencerenin görünen kısmı (saatin gölge boşluğu hariç) sütuna hizalanır
                sonuc[t.Kimlik] = (sutunX[t.Sutun] - ic.Left, sutunY[t.Sutun] - ic.Top);
                sutunY[t.Sutun] += (int)Math.Round(p.ActualHeight * s) - ic.Top - ic.Bottom + aralik;
            }
            return sonuc;
        }

        void Tasindi(WidgetPenceresi p)
        {
            var ekranlar = Ekran.Hepsi();
            var r = p.Dikdortgen;
            var ekran = Ekran.Iceren(ekranlar, r.Left + r.Genislik / 2, r.Top + r.Yukseklik / 2);
            if (ekran == null) return;

            // Diğer widget'ların ve monitör kenar boşluğunun kenarlarına hizala
            double s = ekran.Olcek;
            var ic = p.IcKenar(s);
            int sol = r.Left + ic.Left, ust = r.Top + ic.Top, sag = r.Right - ic.Right, alt = r.Bottom - ic.Bottom;
            int esik = (int)Math.Round(Yapisma * s), aralik = (int)Math.Round(Aralik * s), kenar = (int)Math.Round(Kenar * s);
            var adayX = new List<int> { ekran.CalismaAlani.Left + kenar - sol, ekran.CalismaAlani.Right - kenar - sag };
            var adayY = new List<int> { ekran.CalismaAlani.Top + kenar - ust, ekran.CalismaAlani.Bottom - kenar - alt };
            foreach (var q in _pencereler.Values.Where(q => q != p && q.IsVisible))
            {
                var rq = q.Dikdortgen;
                var iq = q.IcKenar(s);
                int qSol = rq.Left + iq.Left, qUst = rq.Top + iq.Top, qSag = rq.Right - iq.Right, qAlt = rq.Bottom - iq.Bottom;
                adayX.AddRange(new[] { qSol - sol, qSag - sag, qSag + aralik - sol, qSol - aralik - sag });
                adayY.AddRange(new[] { qUst - ust, qAlt + aralik - ust, qUst - aralik - alt });
            }
            int dx = adayX.Where(d => Math.Abs(d) <= esik).OrderBy(Math.Abs).DefaultIfEmpty(0).First();
            int dy = adayY.Where(d => Math.Abs(d) <= esik).OrderBy(Math.Abs).DefaultIfEmpty(0).First();
            if (dx != 0 || dy != 0) p.KonumaGit(r.Left + dx, r.Top + dy);

            var w = _ayarlar.Widget(p.Kimlik);
            w.Monitor = ekran.Ad;
            w.X = r.Left + dx - ekran.Alan.Left;
            w.Y = r.Top + dy - ekran.Alan.Top;
            _kaydet();
            // Başka monitöre taşındıysa arka plan katmanı da onu izlesin
            if (_ayarlar.ArkaPlanAcik) BuzUygula();
        }
    }
}
