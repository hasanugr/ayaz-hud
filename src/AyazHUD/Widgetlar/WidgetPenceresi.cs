using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using AyazHUD.Arayuz;
using AyazHUD.Cekirdek;
using AyazHUD.Yerel;

namespace AyazHUD.Widgetlar
{
    /// <summary>Her widget'ın içeriği bu arayüzü uygular; saniyede bir yeni verilerle çağrılır.</summary>
    internal interface IWidget
    {
        void Guncelle(Anlik anlik);
    }

    /// <summary>
    /// Masaüstünde duran, kenarlıksız, arkası bulanık widget penceresi.
    /// - Alt+Tab'da ve görev çubuğunda görünmez, tıklanınca odak çalmaz.
    /// - Her zaman diğer pencerelerin altında, duvar kâğıdının üstünde kalır.
    /// - Kilitliyken tıklamaları arkasına geçirir; düzenleme kipinde sürüklenebilir.
    /// </summary>
    internal sealed class WidgetPenceresi : Window
    {
        public const double KartGenisligi = 384; // 24rem

        // Bulanıklık katmanı pencerenin dikdörtgenini kaplıyor ve SetWindowRgn ile kırpılamıyor (katmanlı pencere).
        // Windows 11'in kendi köşe yuvarlaması bulanıklığı da kırpıyor ama yarıçapı sabit 8 px; kart da ona uyar.
        // Windows 10'da yuvarlama yok, kart köşeli çizilir ki köşelerden bulanıklık taşmasın.
        internal static readonly bool Windows11 = Environment.OSVersion.Version.Build >= 22000;
        public static readonly double KoseYaricapi = Windows11 ? 8 : 0;

        public string Kimlik { get; }
        public IWidget Icerik { get; }
        public bool Kartli { get; }
        /// <summary>Bu widget'ın stili ve renkleri; içindeki denetimler de bunu kullanır.</summary>
        public Gorunum Gorunum { get; } = new Gorunum();
        public IntPtr Tutamac { get; private set; }

        /// <summary>Kullanıcı sürükleyip bıraktığında.</summary>
        public event Action<WidgetPenceresi> Tasindi;
        /// <summary>Monitör eklendi, çıkarıldı ya da çözünürlük değişti.</summary>
        public event Action EkranlarDegisti;

        readonly Grid _kok;
        readonly FrameworkElement _icerikOgesi;
        readonly Rectangle _duzenlemeCercevesi;
        readonly DropShadowEffect _yaziGolgesi = new DropShadowEffect { Color = Colors.Black, Opacity = 0.6, BlurRadius = 10, ShadowDepth = 1, Direction = 270 };
        bool _duzenleme, _surukleniyor;
        (int X, int Y)? _hedef;

        // Kartın hemen arkasındaki buzlanma katmanları: hafif düzeylerde masaüstü kopyalı tek katman,
        // güçlü düzeylerde kartın kendi bulanıklığını artıran ek bulanık katmanlar
        readonly List<KatmanPenceresi> _ekKatmanlar = new List<KatmanPenceresi>();
        Window _tabanSahip;
        int _arkaBuzlanma;
        double _arkaKarartma;
        // Masaüstü kopyası arka plan katmanının karartmasını göstermediği için o ton kartın içinde tekrarlanır
        readonly SolidColorBrush _arkaTon = new SolidColorBrush(Colors.Transparent);

        public WidgetPenceresi(string kimlik, IWidget icerik, bool kartli)
        {
            Kimlik = kimlik;
            Icerik = icerik;
            Kartli = kartli;

            Title = "Ayaz HUD";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            ShowActivated = false;
            SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = WindowStartupLocation.Manual;
            UseLayoutRounding = true;
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.Grayscale);

            // İçerikteki denetimler görünümü bu miras alınan özellikten bulur
            Gorunum.Ata(this, Gorunum);
            Gorunum.Degisti += () =>
            {
                GolgeUygula();
                EkKatmanlariKur();
            };

            _kok = new Grid();
            _icerikOgesi = (FrameworkElement)icerik;
            if (kartli)
            {
                _kok.Width = KartGenisligi;
                _kok.Children.Add(new Border { CornerRadius = new CornerRadius(KoseYaricapi), Background = _arkaTon });
                _kok.Children.Add(new Border
                {
                    CornerRadius = new CornerRadius(KoseYaricapi),
                    BorderThickness = new Thickness(1),
                    BorderBrush = Gorunum.KartCizgi,
                    Background = Gorunum.KartZemin,
                });
                // Üst kenarda vurgu renginde ince parıltı
                _kok.Children.Add(new Rectangle
                {
                    Height = 1,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(20.8, 0, 20.8, 0),
                    Fill = Gorunum.UstParilti,
                });
                _icerikOgesi.Margin = new Thickness(20.8, 18.4, 20.8, 19.2);
            }
            _kok.Children.Add(_icerikOgesi);

            _duzenlemeCercevesi = new Rectangle
            {
                RadiusX = kartli ? KoseYaricapi : 10,
                RadiusY = kartli ? KoseYaricapi : 10,
                Stroke = Gorunum.Vurgu,
                StrokeThickness = 1.5,
                StrokeDashArray = new DoubleCollection { 4, 3 },
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false,
            };
            _kok.Children.Add(_duzenlemeCercevesi);
            Content = _kok;

            DpiChanged += (s, e) =>
            {
                // Ölçeği farklı monitöre taşınınca Windows pencereyi kaydırabiliyor; istenen konuma geri koy
                if (_hedef is (int x, int y) && !_surukleniyor)
                    Dispatcher.BeginInvoke(new Action(() => KonumaGit(x, y)));
            };
            // Ek katmanlar kartı izler
            LocationChanged += (s, e) => EkKatmanlariYerlestir();
            SizeChanged += (s, e) => EkKatmanlariYerlestir();
            IsVisibleChanged += (s, e) => EkKatmanlariKur();
            Closed += (s, e) =>
            {
                Owner = null;
                foreach (var k in _ekKatmanlar) k.Close();
                _ekKatmanlar.Clear();
            };
        }

        /// <summary>
        /// Pencerenin altında kalacağı arka plan katmanı ve onun buzlanma/karartma ayarı. Ek katmanlar varsa sıra:
        /// arka plan katmanı, ek katmanlar, widget.
        /// </summary>
        public void TabanAyarla(Window sahip, int arkaBuzlanma, double arkaKarartma)
        {
            _tabanSahip = sahip;
            _arkaBuzlanma = arkaBuzlanma;
            _arkaKarartma = arkaKarartma;
            EkKatmanlariKur();
        }

        /// <summary>Arka plan katmanından ayırır (katman kapanırken widget da kapanmasın); katmanlara dokunmaz.</summary>
        public void TabandanAyir()
        {
            _tabanSahip = null;
            if (_ekKatmanlar.Count > 0) _ekKatmanlar[0].Owner = null;
            else Owner = null;
        }

        /// <summary>
        /// Kartın arkasının buzlanması. Arka plan katmanının üstündeyken kartın arkası zaten buzlu; kart çevresinden
        /// net görünmesin diye iki yüzde toplanır.
        /// </summary>
        BuzSeviyesi Seviye()
        {
            if (!Kartli || !Gorunum.Bulanik) return BuzSeviyesi.Al(0);
            return BuzSeviyesi.Al(Gorunum.Buzlanma + (_tabanSahip != null ? _arkaBuzlanma : 0));
        }

        void EkKatmanlariKur()
        {
            var s = Seviye();
            // Masaüstü kopyası kartın içeriğini örtmesin diye ayrı bir katmanda; o zaman kartın kendisi bulanık değil
            int gereken = !Kartli || !IsVisible ? 0 : s.Kopyali ? 1 : Math.Max(0, s.Katman - 1);
            var cam = s.Kopyali && s.Katman == 0 ? Win32.AccentState.Kapali
                    : Gorunum.Stil == KartStili.Akrilik ? Win32.AccentState.Akrilik : Win32.AccentState.Bulanik;
            if (_ekKatmanlar.Count != gereken || (_ekKatmanlar.Count > 0 && _ekKatmanlar[0].Cam != cam))
            {
                // Sahip pencere kapanınca ona bağlı pencereler de kapanır; önce bağlar çözülür
                Owner = null;
                foreach (var k in _ekKatmanlar) k.Owner = null;
                foreach (var k in _ekKatmanlar) k.Close();
                _ekKatmanlar.Clear();
                for (int i = 0; i < gereken; i++)
                {
                    var k = new KatmanPenceresi(cam, true);
                    _ekKatmanlar.Add(k);
                    k.Show();
                }
            }
            Window onceki = _tabanSahip;
            foreach (var k in _ekKatmanlar)
            {
                if (k.Owner != onceki) k.Owner = onceki;
                k.Kopyala(s.Yaricap, s.Opaklik);
                onceki = k;
            }
            if (Owner != onceki) Owner = onceki;
            _arkaTon.Color = KatmanPenceresi.Ton(_tabanSahip != null && s.Kopyali ? _arkaKarartma * s.Opaklik : 0);
            CamUygula();
            EkKatmanlariYerlestir();
        }

        void EkKatmanlariYerlestir()
        {
            if (_ekKatmanlar.Count == 0 || Tutamac == IntPtr.Zero) return;
            var r = Dikdortgen;
            foreach (var k in _ekKatmanlar) k.Yerlestir(r.Left, r.Top, r.Genislik, r.Yukseklik);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            Tutamac = new WindowInteropHelper(this).Handle;
            Win32.ExStilDegistir(Tutamac, Win32.WS_EX_TOOLWINDOW | Win32.WS_EX_NOACTIVATE, Win32.WS_EX_APPWINDOW);
            if (Kartli && Windows11)
            {
                int cizgiYok = Win32.DWMWA_COLOR_NONE;
                Win32.DwmSetWindowAttribute(Tutamac, Win32.DWMWA_BORDER_COLOR, ref cizgiYok, sizeof(int));
            }
            HwndSource.FromHwnd(Tutamac).AddHook(PencereIslemi);
            CamUygula();
            GolgeUygula();
            DuzenlemeUygula();
        }

        IntPtr PencereIslemi(IntPtr hwnd, int mesaj, IntPtr wParam, IntPtr lParam, ref bool islendi)
        {
            switch (mesaj)
            {
                case Win32.WM_WINDOWPOSCHANGING:
                    // Z sırası değişecekse pencereyi en alta (masaüstünün hemen üstüne) zorla
                    var konum = Marshal.PtrToStructure<Win32.WINDOWPOS>(lParam);
                    if ((konum.flags & Win32.SWP_NOZORDER) == 0)
                    {
                        konum.hwndInsertAfter = Win32.HWND_BOTTOM;
                        Marshal.StructureToPtr(konum, lParam, false);
                    }
                    break;
                case Win32.WM_MOUSEACTIVATE:
                    islendi = true;
                    return new IntPtr(Win32.MA_NOACTIVATE);
                case Win32.WM_DISPLAYCHANGE:
                    EkranlarDegisti?.Invoke();
                    break;
            }
            return IntPtr.Zero;
        }

        void CamUygula()
        {
            if (Tutamac == IntPtr.Zero || !Kartli) return;
            // Hafif buzlanmada bulanıklık kartın arkasındaki katmanda
            var durum = Seviye().Kopyali ? Win32.AccentState.Kapali
                      : Gorunum.Stil == KartStili.Bulanik ? Win32.AccentState.Bulanik
                      : Gorunum.Stil == KartStili.Akrilik ? Win32.AccentState.Akrilik
                      : Win32.AccentState.Kapali;
            // Koyu ton WPF tarafında (kenar yumuşatmalı) verilir; bulanıklığın kendi tonu neredeyse şeffaf.
            // Akrilikte hafif beyaz ton buzlu, açık bir görünüm verir.
            Win32.CamAyarla(Tutamac, durum, durum == Win32.AccentState.Akrilik ? 0x14FFFFFFu : 0x01000000u);
            if (Windows11)
            {
                // Köşe yuvarlaması yalnızca bulanıklığı kırpmak için; bulanıklık yokken Windows yuvarlak
                // pencerenin altına açık renkli bir yüzey çizdiği için kapatılır
                int kose = durum == Win32.AccentState.Kapali ? Win32.DWMWCP_DONOTROUND : Win32.DWMWCP_ROUND;
                Win32.DwmSetWindowAttribute(Tutamac, Win32.DWMWA_WINDOW_CORNER_PREFERENCE, ref kose, sizeof(int));
            }
        }

        /// <summary>Şeffaf kartta zemin olmadığı için yazılar duvar kâğıdından gölgeyle ayrılır.</summary>
        void GolgeUygula()
        {
            if (!Kartli) return;
            _icerikOgesi.Effect = Gorunum.Stil == KartStili.Seffaf ? _yaziGolgesi : null;
        }

        public bool Duzenleme
        {
            get => _duzenleme;
            set
            {
                _duzenleme = value;
                DuzenlemeUygula();
            }
        }

        void DuzenlemeUygula()
        {
            _duzenlemeCercevesi.Visibility = _duzenleme ? Visibility.Visible : Visibility.Collapsed;
            // Düzenlemede saydam yerler de tıklanabilsin; kilitliyken pencere tıklamaları tamamen geçirir
            _kok.Background = _duzenleme ? new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)) : null;
            Cursor = _duzenleme ? Cursors.SizeAll : null;
            if (Tutamac != IntPtr.Zero)
            {
                if (_duzenleme) Win32.ExStilDegistir(Tutamac, 0, Win32.WS_EX_TRANSPARENT);
                else Win32.ExStilDegistir(Tutamac, Win32.WS_EX_TRANSPARENT, 0);
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (!_duzenleme) return;
            _surukleniyor = true;
            try
            {
                DragMove();
            }
            catch (InvalidOperationException)
            {
                return;
            }
            finally
            {
                _surukleniyor = false;
            }
            var r = Dikdortgen;
            _hedef = (r.Left, r.Top);
            Tasindi?.Invoke(this);
        }

        // ---------- Fiziksel piksel cinsinden konum ----------

        public Win32.RECT Dikdortgen
        {
            get
            {
                Win32.GetWindowRect(Tutamac, out var r);
                return r;
            }
        }

        public void KonumaGit(int x, int y)
        {
            _hedef = (x, y);
            Win32.SetWindowPos(Tutamac, IntPtr.Zero, x, y, 0, 0, Win32.SWP_NOSIZE | Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);
        }

        /// <summary>Pencerenin görünen kısmının kenarlarına olan boşluk (fiziksel piksel); saatte gölge payı.</summary>
        public (int Left, int Top, int Right, int Bottom) IcKenar(double olcek)
        {
            if (Kartli) return (0, 0, 0, 0);
            int b = (int)Math.Round(SaatWidget.Bosluk * olcek);
            return (b, b, b, b);
        }
    }
}
