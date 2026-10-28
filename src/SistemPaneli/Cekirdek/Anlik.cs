using System.Collections.Generic;

namespace SistemPaneli.Cekirdek
{
    /// <summary>Bir saniyelik ölçümlerin değişmez kopyası. Sensör iş parçacığı üretir, arayüz okur.</summary>
    internal sealed class Anlik
    {
        /// <summary>Sensörler açıldı ve en az bir kez okundu.</summary>
        public bool Hazir;

        public string IslemciAdi;
        public double? IslemciSicaklik, IslemciYuk, IslemciFrekans, IslemciGuc; // °C, %, MHz, W

        public string EkranKartiAdi;
        public double? GpuSicaklik, GpuYuk, GpuFrekans, GpuGuc;                 // °C, %, MHz, W
        public double? VramKullanilan, VramToplam;                              // MB
        public double? GpuFan;                                                  // RPM (en hızlı fan)

        public double? RamYuk, RamKullanilan, RamToplam;                        // %, GB, GB

        /// <summary>Anakart fanları ve pompa (en az bir kez dönmüş olanlar).</summary>
        public List<SensorDegeri> Fanlar = new List<SensorDegeri>();
        /// <summary>Disk sıcaklıkları, sürücü harfi sırasıyla.</summary>
        public List<SensorDegeri> Diskler = new List<SensorDegeri>();

        public double? Indirme, Yukleme;                                        // bayt/sn

        /// <summary>Arayüz iş parçacığı her saniye doldurur (FPS ölçümü sensörlerden ayrı çalışır).</summary>
        public FpsDurumu Fps = FpsDurumu.Baslatiliyor;
    }

    internal sealed class SensorDegeri
    {
        /// <summary>LibreHardwareMonitor sensör kimliği (ayarlarda ad ve görünürlük için anahtar).</summary>
        public string Kimlik;
        /// <summary>Kullanıcı ad vermediyse gösterilecek ad ("Fan #2", "C:").</summary>
        public string VarsayilanAd;
        public double? Deger;
    }

    internal enum FpsHali
    {
        Baslatiliyor,
        IzinYok,
        Hata,
        OyunYok,
        Tamam,
    }

    internal sealed class FpsDurumu
    {
        public static readonly FpsDurumu Baslatiliyor = new FpsDurumu { Hal = FpsHali.Baslatiliyor };

        public FpsHali Hal;
        /// <summary>Son 1 saniyenin ortalama FPS'i.</summary>
        public int Deger;
        /// <summary>Son 10 saniyedeki en yavaş %1'lik karelerin FPS karşılığı; takılmaları gösterir.</summary>
        public int Yuzde1Dusuk;
        /// <summary>İki kare arasındaki ortalama süre (ms), yani 1000 / FPS.</summary>
        public double KareSuresi;
        /// <summary>Karenin hazırlanmaya başlamasından ekranda görünmesine kadar geçen ortalama süre (ms).</summary>
        public double? Gecikme;
        public string Uygulama;
    }
}
