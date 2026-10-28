using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using AyazHUD.Yerel;

namespace AyazHUD.Widgetlar
{
    /// <summary>
    /// İçi boş, tıklamaları geçiren bir buzlanma penceresi. Arka plan katmanında monitörü, widget'larda kartı kaplar.
    /// - Arkası Windows bulanıklığıyla bulanık olabilir. Yarıçap sabit, ama üst üste konan her bulanık pencere
    ///   altındakini bir kez daha bulanıklaştırır.
    /// - İçine masaüstünün hafif bulanık kopyası çizilebilir (<see cref="Kopyala"/>).
    /// - Koyu bir tonu olabilir; kopya varsa onun altında kalır.
    /// Sahibi yoksa kendini en alta zorlar; varsa Windows onu sahibinin hemen üstünde tutar.
    /// </summary>
    internal sealed class KatmanPenceresi : Window
    {
        readonly SolidColorBrush _ton = new SolidColorBrush(Colors.Transparent);
        readonly Win32.AccentState _cam;
        readonly bool _yuvarlak;
        MasaustuKopyasi _kopya;
        double _kopyaYaricapi, _kopyaOpakligi;

        public IntPtr Tutamac { get; private set; }

        /// <param name="cam">Bulanıklık türü; Kapali ise yalnızca koyu ton.</param>
        /// <param name="yuvarlak">Windows 11'de köşeleri kart gibi yuvarla.</param>
        public KatmanPenceresi(Win32.AccentState cam, bool yuvarlak)
        {
            _cam = cam;
            _yuvarlak = yuvarlak;
            Title = "Ayaz HUD";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            ShowActivated = false;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = -32000;
            Top = -32000;
            Width = 1;
            Height = 1;
            Background = _ton;
            IsHitTestVisible = false;
        }

        public Win32.AccentState Cam => _cam;

        public double Karartma
        {
            set => _ton.Color = Ton(value);
        }

        /// <summary>Arka plan karartmasının rengi; kart da aynı tonu kullanır.</summary>
        public static Color Ton(double karartma) =>
            Color.FromArgb((byte)Math.Round(255 * Math.Max(0, Math.Min(0.9, karartma))), 8, 10, 16);

        /// <summary>Masaüstünün kopyasını çiz; opaklık 0 ise kaldır. Yarıçap %100 ölçekte piksel.</summary>
        public void Kopyala(double yaricap, double opaklik)
        {
            _kopyaYaricapi = yaricap;
            _kopyaOpakligi = opaklik;
            KopyayiYerlestir();
        }

        void KopyayiYerlestir()
        {
            if (Tutamac == IntPtr.Zero) return;
            if (_kopyaOpakligi <= 0)
            {
                _kopya?.Dispose();
                _kopya = null;
                return;
            }
            Win32.GetWindowRect(Tutamac, out var alan);
            if (alan.Genislik <= 1) return; // henüz yerleştirilmedi
            var ekran = Ekran.Penceresinin(Tutamac);
            if (ekran == null) return;
            if (_kopya == null) _kopya = new MasaustuKopyasi(Tutamac);
            _kopya.Ayarla(alan, ekran.Alan, _kopyaYaricapi * ekran.Olcek, _kopyaOpakligi);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            Tutamac = new WindowInteropHelper(this).Handle;
            Win32.ExStilDegistir(Tutamac, Win32.WS_EX_TOOLWINDOW | Win32.WS_EX_NOACTIVATE | Win32.WS_EX_TRANSPARENT, Win32.WS_EX_APPWINDOW);
            HwndSource.FromHwnd(Tutamac).AddHook(PencereIslemi);
            if (_cam != Win32.AccentState.Kapali)
                Win32.CamAyarla(Tutamac, _cam, _cam == Win32.AccentState.Akrilik ? 0x14FFFFFFu : 0x01000000u);
            if (_yuvarlak && WidgetPenceresi.Windows11)
            {
                // Bulanıklık yokken yuvarlak pencerenin altına açık renkli bir yüzey çizildiği için yuvarlanmaz
                int kose = _cam == Win32.AccentState.Kapali ? Win32.DWMWCP_DONOTROUND : Win32.DWMWCP_ROUND, cizgiYok = Win32.DWMWA_COLOR_NONE;
                Win32.DwmSetWindowAttribute(Tutamac, Win32.DWMWA_WINDOW_CORNER_PREFERENCE, ref kose, sizeof(int));
                Win32.DwmSetWindowAttribute(Tutamac, Win32.DWMWA_BORDER_COLOR, ref cizgiYok, sizeof(int));
            }
        }

        /// <summary>Fiziksel piksel cinsinden konum ve boyut; z sırasına dokunmaz (sahibi yoksa en alta koyar).</summary>
        public void Yerlestir(int x, int y, int genislik, int yukseklik)
        {
            if (Tutamac == IntPtr.Zero) return;
            bool enAlta = Owner == null && !MasaustuGosterimi.Acik;
            Win32.SetWindowPos(Tutamac, enAlta ? Win32.HWND_BOTTOM : IntPtr.Zero, x, y, genislik, yukseklik,
                Win32.SWP_NOACTIVATE | (enAlta ? 0 : Win32.SWP_NOZORDER));
            KopyayiYerlestir();
        }

        protected override void OnClosed(EventArgs e)
        {
            _kopya?.Dispose();
            _kopya = null;
            base.OnClosed(e);
        }

        IntPtr PencereIslemi(IntPtr hwnd, int mesaj, IntPtr wParam, IntPtr lParam, ref bool islendi)
        {
            if (mesaj == Win32.WM_WINDOWPOSCHANGING && Owner == null && !MasaustuGosterimi.Acik)
            {
                var konum = Marshal.PtrToStructure<Win32.WINDOWPOS>(lParam);
                if ((konum.flags & Win32.SWP_NOZORDER) == 0)
                {
                    konum.hwndInsertAfter = Win32.HWND_BOTTOM;
                    Marshal.StructureToPtr(konum, lParam, false);
                }
            }
            else if (mesaj == Win32.WM_MOUSEACTIVATE)
            {
                islendi = true;
                return new IntPtr(Win32.MA_NOACTIVATE);
            }
            return IntPtr.Zero;
        }
    }
}
