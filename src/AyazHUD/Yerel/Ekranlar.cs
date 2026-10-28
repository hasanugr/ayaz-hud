using System;
using System.Collections.Generic;
using System.Linq;

namespace AyazHUD.Yerel
{
    /// <summary>Bağlı monitörlerin fiziksel piksel cinsinden konumları ve ölçekleri.</summary>
    internal sealed class Ekran
    {
        public string Ad;              // \\.\DISPLAY1 gibi
        public Win32.RECT Alan;        // monitörün tamamı
        public Win32.RECT CalismaAlani; // görev çubuğu hariç
        public bool Birincil;
        public double Olcek = 1;       // 1.0 = %100
        /// <summary>Monitörün modeli ("AOC 27G1G4"); yalnızca <see cref="Hepsi"/> doldurur, okunamazsa boş.</summary>
        public string Model;

        /// <summary>Windows'un Ekran ayarlarındaki numara: aygıt adının sonundaki sayı (\\.\DISPLAY2 → 2).</summary>
        public int Numara
        {
            get
            {
                int i = Ad.Length;
                while (i > 0 && char.IsDigit(Ad[i - 1])) i--;
                return i < Ad.Length && int.TryParse(Ad.Substring(i), out int n) ? n : 0;
            }
        }

        public static List<Ekran> Hepsi()
        {
            var liste = new List<Ekran>();
            Win32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr tutamac, IntPtr hdc, ref Win32.RECT r, IntPtr veri) =>
            {
                var e = Olustur(tutamac);
                if (e != null) liste.Add(e);
                return true;
            }, IntPtr.Zero);
            var adlar = Win32.MonitorAdlari();
            foreach (var e in liste) e.Model = adlar.TryGetValue(e.Ad, out var m) ? m : null;
            return liste.OrderBy(e => e.Numara).ToList();
        }

        /// <summary>Pencerenin (çoğunu) içeren monitör.</summary>
        public static Ekran Penceresinin(IntPtr hwnd) => Olustur(Win32.MonitorFromWindow(hwnd, Win32.MONITOR_DEFAULTTONEAREST));

        static Ekran Olustur(IntPtr tutamac)
        {
            var bilgi = new Win32.MONITORINFOEX { cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Win32.MONITORINFOEX)) };
            if (!Win32.GetMonitorInfo(tutamac, ref bilgi)) return null;
            double olcek = 1;
            if (Win32.GetDpiForMonitor(tutamac, 0, out uint dpiX, out _) == 0 && dpiX > 0) olcek = dpiX / 96.0;
            return new Ekran
            {
                Ad = bilgi.szDevice,
                Alan = bilgi.rcMonitor,
                CalismaAlani = bilgi.rcWork,
                Birincil = (bilgi.dwFlags & Win32.MONITORINFOF_PRIMARY) != 0,
                Olcek = olcek,
            };
        }

        /// <summary>Widget'ların varsayılan olarak gideceği monitör: varsa birincil olmayan (yan) ekran.</summary>
        public static Ekran Varsayilan(List<Ekran> ekranlar) =>
            ekranlar.FirstOrDefault(e => !e.Birincil) ?? ekranlar.FirstOrDefault(e => e.Birincil) ?? ekranlar.FirstOrDefault();

        /// <summary>Noktayı içeren monitör; hiçbiri içermiyorsa en yakını.</summary>
        public static Ekran Iceren(List<Ekran> ekranlar, int x, int y)
        {
            Ekran enYakin = null;
            double enKisa = double.MaxValue;
            foreach (var e in ekranlar)
            {
                if (x >= e.Alan.Left && x < e.Alan.Right && y >= e.Alan.Top && y < e.Alan.Bottom) return e;
                double dx = Math.Max(Math.Max(e.Alan.Left - x, 0), x - e.Alan.Right);
                double dy = Math.Max(Math.Max(e.Alan.Top - y, 0), y - e.Alan.Bottom);
                double uzaklik = dx * dx + dy * dy;
                if (uzaklik < enKisa) { enKisa = uzaklik; enYakin = e; }
            }
            return enYakin;
        }
    }
}
