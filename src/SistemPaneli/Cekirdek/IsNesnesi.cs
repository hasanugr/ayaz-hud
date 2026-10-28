using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SistemPaneli.Yerel;

namespace SistemPaneli.Cekirdek
{
    /// <summary>
    /// Windows iş nesnesi: içine konan süreçler, uygulama nasıl kapanırsa kapansın (çökme dahil) onunla birlikte sonlanır.
    /// </summary>
    internal static class IsNesnesi
    {
        public static IntPtr Olustur()
        {
            IntPtr nesne = Win32.CreateJobObject(IntPtr.Zero, null);
            if (nesne == IntPtr.Zero) return IntPtr.Zero;
            var bilgi = new Win32.JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
            bilgi.BasicLimitInformation.LimitFlags = Win32.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;
            Win32.SetInformationJobObject(nesne, Win32.JobObjectExtendedLimitInformation, ref bilgi, Marshal.SizeOf(bilgi));
            // Tutamaç bilerek kapatılmaz: uygulama kapanınca sistem kapatır ve içindeki süreçler sonlanır
            return nesne;
        }

        public static void Ekle(IntPtr nesne, Process surec)
        {
            if (nesne == IntPtr.Zero) return;
            if (!Win32.AssignProcessToJobObject(nesne, surec.Handle))
                Gunluk.Yaz("Süreç iş nesnesine eklenemedi, hata " + Marshal.GetLastWin32Error());
        }
    }
}
