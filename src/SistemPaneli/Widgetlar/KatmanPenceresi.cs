using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using SistemPaneli.Yerel;

namespace SistemPaneli.Widgetlar
{
    /// <summary>
    /// İçi boş, tıklamaları geçiren, arkası bulanık ve/veya koyu bir pencere. Buzlanmayı güçlendirmek için
    /// kullanılır: Windows'un bulanıklık yarıçapı sabit, ama üst üste konan her bulanık pencere altındakini
    /// bir kez daha bulanıklaştırır. Arka plan katmanında monitörü, widget'larda kartı kaplar.
    /// Sahibi yoksa kendini en alta zorlar; varsa Windows onu sahibinin hemen üstünde tutar.
    /// </summary>
    internal sealed class KatmanPenceresi : Window
    {
        readonly SolidColorBrush _ton = new SolidColorBrush(Colors.Transparent);
        readonly Win32.AccentState _cam;
        readonly bool _yuvarlak;

        public IntPtr Tutamac { get; private set; }

        /// <param name="cam">Bulanıklık türü; Kapali ise yalnızca koyu ton.</param>
        /// <param name="yuvarlak">Windows 11'de köşeleri kart gibi yuvarla.</param>
        public KatmanPenceresi(Win32.AccentState cam, bool yuvarlak)
        {
            _cam = cam;
            _yuvarlak = yuvarlak;
            Title = "Sistem Paneli";
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
            set => _ton.Color = Color.FromArgb((byte)Math.Round(255 * Math.Max(0, Math.Min(0.9, value))), 8, 10, 16);
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
                int kose = Win32.DWMWCP_ROUND, cizgiYok = Win32.DWMWA_COLOR_NONE;
                Win32.DwmSetWindowAttribute(Tutamac, Win32.DWMWA_WINDOW_CORNER_PREFERENCE, ref kose, sizeof(int));
                Win32.DwmSetWindowAttribute(Tutamac, Win32.DWMWA_BORDER_COLOR, ref cizgiYok, sizeof(int));
            }
        }

        /// <summary>Fiziksel piksel cinsinden konum ve boyut; z sırasına dokunmaz (sahibi yoksa en alta koyar).</summary>
        public void Yerlestir(int x, int y, int genislik, int yukseklik)
        {
            if (Tutamac == IntPtr.Zero) return;
            bool sahipsiz = Owner == null;
            Win32.SetWindowPos(Tutamac, sahipsiz ? Win32.HWND_BOTTOM : IntPtr.Zero, x, y, genislik, yukseklik,
                Win32.SWP_NOACTIVATE | (sahipsiz ? 0 : Win32.SWP_NOZORDER));
        }

        IntPtr PencereIslemi(IntPtr hwnd, int mesaj, IntPtr wParam, IntPtr lParam, ref bool islendi)
        {
            if (mesaj == Win32.WM_WINDOWPOSCHANGING && Owner == null)
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
