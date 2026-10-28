using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using SistemPaneli.Yerel;

namespace SistemPaneli.Widgetlar
{
    /// <summary>
    /// "Arka plan buzlanması": widget'ların olduğu monitörün tamamını kaplayan, bulanık ve hafif koyu katman.
    /// Tıklamaları arkasına (masaüstü simgelerine) geçirir. Widget'lar bu pencereye bağlı (sahipli) pencereler
    /// olarak açılır; Windows sahipli pencereleri her zaman sahibinin üstünde tuttuğu için katman widget'ların altında kalır.
    /// </summary>
    internal sealed class BuzKatmani : Window
    {
        readonly SolidColorBrush _ton = new SolidColorBrush();
        Ekran _ekran;
        IntPtr _tutamac;

        public string EkranAdi => _ekran.Ad;

        public BuzKatmani(Ekran ekran, double koyuluk)
        {
            _ekran = ekran;
            Title = "Sistem Paneli";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            ShowActivated = false;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Background = _ton;
            IsHitTestVisible = false;
            Koyuluk = koyuluk;
        }

        public double Koyuluk
        {
            set => _ton.Color = Color.FromArgb((byte)Math.Round(255 * Math.Max(0, Math.Min(0.9, value))), 8, 10, 16);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _tutamac = new WindowInteropHelper(this).Handle;
            Win32.ExStilDegistir(_tutamac, Win32.WS_EX_TOOLWINDOW | Win32.WS_EX_NOACTIVATE | Win32.WS_EX_TRANSPARENT, Win32.WS_EX_APPWINDOW);
            HwndSource.FromHwnd(_tutamac).AddHook(PencereIslemi);
            Win32.CamAyarla(_tutamac, Win32.AccentState.Bulanik, 0x01000000);
            Yerlestir(_ekran);
        }

        public void Yerlestir(Ekran ekran)
        {
            _ekran = ekran;
            if (_tutamac == IntPtr.Zero) return;
            var a = ekran.Alan;
            Win32.SetWindowPos(_tutamac, Win32.HWND_BOTTOM, a.Left, a.Top, a.Genislik, a.Yukseklik, Win32.SWP_NOACTIVATE);
        }

        IntPtr PencereIslemi(IntPtr hwnd, int mesaj, IntPtr wParam, IntPtr lParam, ref bool islendi)
        {
            if (mesaj == Win32.WM_WINDOWPOSCHANGING)
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
