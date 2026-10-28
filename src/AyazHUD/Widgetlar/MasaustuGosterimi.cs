using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using AyazHUD.Yerel;

namespace AyazHUD.Widgetlar
{
    /// <summary>
    /// "Masaüstünü göster" (Win+D) sırasında widget'ları görünür tutar. Windows bu komutta pencereleri küçültür ve
    /// masaüstü penceresini (Progman) bütün pencerelerin üstüne çıkarır; en altta duran widget'lar onun altında kalır.
    /// Ön plan masaüstüne geçince masaüstünün widget'ların üstüne çıkıp çıkmadığına bakılır; çıktıysa widget'lar onun
    /// üstüne alınır. Başka bir pencere öne gelince yine en alta inerler.
    /// </summary>
    internal sealed class MasaustuGosterimi : IDisposable
    {
        /// <summary>Widget'lar masaüstünün üstüne alındı; pencereler bu sırada kendini en alta zorlamaz.</summary>
        public static bool Acik { get; private set; }

        readonly Func<IEnumerable<IntPtr>> _koklar;
        readonly Win32.WinEventDelegate _geriCagri; // çöp toplayıcı silmesin diye alanda tutulur
        readonly IntPtr _kanca;
        readonly DispatcherTimer _yeniden;

        /// <param name="koklar">Sahipsiz (zincirin en altındaki) pencereler; sahipli olanlar onlarla birlikte taşınır.</param>
        public MasaustuGosterimi(Func<IEnumerable<IntPtr>> koklar)
        {
            _koklar = koklar;
            // Masaüstü bazen ön plan olayından biraz sonra öne çıkıyor; kısa bir süre sonra bir kez daha bakılır
            _yeniden = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _yeniden.Tick += (s, e) =>
            {
                _yeniden.Stop();
                Denetle();
            };
            _geriCagri = (kanca, olay, hwnd, nesne, cocuk, isParcacigi, zaman) =>
            {
                Denetle();
                _yeniden.Stop();
                _yeniden.Start();
            };
            _kanca = Win32.SetWinEventHook(Win32.EVENT_SYSTEM_FOREGROUND, Win32.EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _geriCagri, 0, 0, Win32.WINEVENT_OUTOFCONTEXT);
        }

        void Denetle()
        {
            var koklar = _koklar().Where(h => h != IntPtr.Zero).ToList();
            if (koklar.Count == 0) return;
            var onPlan = Win32.GetForegroundWindow();
            string sinif = Win32.SinifAdi(onPlan);
            bool masaustu = sinif == "Progman" || sinif == "WorkerW";

            if (masaustu && !Acik && Win32.UstundeMi(onPlan, koklar[0]))
            {
                Acik = true;
                foreach (var h in koklar)
                    Win32.SetWindowPos(h, Win32.HWND_TOP, 0, 0, 0, 0, Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_NOACTIVATE);
            }
            else if (!masaustu && Acik)
            {
                Acik = false;
                foreach (var h in koklar)
                    Win32.SetWindowPos(h, Win32.HWND_BOTTOM, 0, 0, 0, 0, Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_NOACTIVATE);
            }
        }

        public void Dispose()
        {
            _yeniden.Stop();
            if (_kanca != IntPtr.Zero) Win32.UnhookWinEvent(_kanca);
            Acik = false;
        }
    }
}
