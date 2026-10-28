using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using LibreHardwareMonitor.Hardware;

namespace SistemPaneli.Cekirdek
{
    /// <summary>
    /// LibreHardwareMonitor'ı arka plandaki kendi iş parçacığında açar ve saniyede bir okur.
    /// Arayüz son okumayı <see cref="Son"/> üzerinden alır; iki taraf birbirini beklemez.
    /// </summary>
    internal sealed class SensorServisi : IDisposable
    {
        readonly Thread _is;
        readonly ManualResetEvent _dur = new ManualResetEvent(false);
        readonly HashSet<string> _donenFanlar = new HashSet<string>();
        volatile Anlik _son = new Anlik();

        public Anlik Son => _son;

        public SensorServisi()
        {
            _is = new Thread(Calis) { IsBackground = true, Name = "Sensorler", Priority = ThreadPriority.BelowNormal };
            _is.Start();
        }

        void Calis()
        {
            Computer bilgisayar;
            try
            {
                bilgisayar = new Computer
                {
                    IsCpuEnabled = true,
                    IsGpuEnabled = true,
                    IsMemoryEnabled = true,
                    IsMotherboardEnabled = true,
                    IsStorageEnabled = true,
                    IsNetworkEnabled = true,
                };
                bilgisayar.Open();
                Gunluk.Yaz("Sensörler açıldı: " + string.Join(", ", bilgisayar.Hardware.Select(h => h.Name)));
            }
            catch (Exception e)
            {
                Gunluk.Yaz("Sensörler açılamadı", e);
                return;
            }

            var saat = Stopwatch.StartNew();
            Secim secim = null;
            long sonSecim = 0;
            for (int tur = 0; ; tur++)
            {
                long bas = saat.ElapsedMilliseconds;
                try
                {
                    // Bazı sensörler (fanlar, ekran kartı belleği) ilk okumalardan sonra beliriyor; seçim 3. turda
                    // tekrarlanır, sonra dakikada bir yenilenir (ağ bağdaştırıcısı değişebilir). Seçim turlarında
                    // bütün donanım okunur, diğer turlarda yalnızca gösterilen sensörlerin donanımı.
                    bool secimTuru = secim == null || tur <= 3 || bas - sonSecim > 60000;
                    if (secimTuru)
                    {
                        foreach (var h in bilgisayar.Hardware) Guncelle(h);
                        secim = DonanimTanima.Sec(bilgisayar);
                        sonSecim = bas;
                        if (tur == 3) Gunluk.Yaz(secim.Ozet());
                    }
                    else
                    {
                        foreach (var h in secim.Donanimlar)
                            // Disk sıcaklığı yavaş değişir; diskler 5 saniyede bir okunur
                            if (h.HardwareType != HardwareType.Storage || tur % 5 == 0) h.Update();
                    }
                    _son = secim.Oku(_donenFanlar);
                }
                catch (Exception e)
                {
                    Gunluk.Yaz("Sensörler okunamadı", e);
                }
                long bekle = 1000 - (saat.ElapsedMilliseconds - bas);
                if (_dur.WaitOne((int)Math.Max(50, bekle))) break;
            }

            try
            {
                bilgisayar.Close();
            }
            catch (Exception e)
            {
                Gunluk.Yaz("Sensörler kapatılamadı", e);
            }
        }

        static void Guncelle(IHardware h)
        {
            h.Update();
            foreach (var alt in h.SubHardware) Guncelle(alt);
        }

        public void Dispose()
        {
            _dur.Set();
            _is.Join(3000);
        }
    }
}
