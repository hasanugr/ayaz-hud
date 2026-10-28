using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using LibreHardwareMonitor.Hardware;

namespace AyazHUD.Cekirdek
{
    /// <summary>
    /// LibreHardwareMonitor'ın bulduğu yüzlerce sensörden panelde gösterilecekleri seçer.
    /// Kurallar farklı donanımlarda da çalışacak şekilde ada ve türe göre; sabit kimlik kullanılmaz.
    /// </summary>
    internal static class DonanimTanima
    {
        public static Secim Sec(IComputer bilgisayar)
        {
            var donanimlar = Duzlestir(bilgisayar.Hardware).ToList();
            var s = new Secim();

            // ---------- İşlemci ----------
            var islemci = donanimlar.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
            if (islemci != null)
            {
                s.IslemciAdi = KisaIslemciAdi(islemci.Name);
                var sicakliklar = Sensorler(islemci, SensorType.Temperature);
                s.IslemciSicaklik =
                    Ada(sicakliklar, "Core (Tctl/Tdie)") ??
                    Icerenler(sicakliklar, "Tctl", "Tdie").FirstOrDefault() ??
                    Ada(sicakliklar, "CPU Package") ??
                    Ada(sicakliklar, "Core Max") ??
                    Ada(sicakliklar, "Core Average") ??
                    sicakliklar.FirstOrDefault();
                s.IslemciYuk = Ada(Sensorler(islemci, SensorType.Load), "CPU Total");
                var saatler = Sensorler(islemci, SensorType.Clock);
                s.IslemciFrekans = Ada(saatler, "Cores (Average)");
                if (s.IslemciFrekans == null)
                    s.IslemciCekirdekSaatleri = saatler.Where(x => x.Name.Contains("Core") && !x.Name.Contains("Effective")).ToArray();
                var gucler = Sensorler(islemci, SensorType.Power);
                s.IslemciGuc = Ada(gucler, "Package") ?? Ada(gucler, "CPU Package") ?? gucler.FirstOrDefault();
            }

            // ---------- Ekran kartı: ayrık kart öncelikli (en çok kendi belleği olan) ----------
            var ekranKarti = donanimlar
                .Where(h => h.HardwareType == HardwareType.GpuNvidia || h.HardwareType == HardwareType.GpuAmd || h.HardwareType == HardwareType.GpuIntel)
                .OrderByDescending(h => (GpuBellegi(h) ?? 0) + TurPuani(h.HardwareType))
                .FirstOrDefault();
            if (ekranKarti != null)
            {
                s.EkranKartiAdi = KisaEkranKartiAdi(ekranKarti.Name);
                var sicakliklar = Sensorler(ekranKarti, SensorType.Temperature);
                s.GpuSicaklik = Ada(sicakliklar, "GPU Core") ?? sicakliklar.FirstOrDefault();
                var yukler = Sensorler(ekranKarti, SensorType.Load);
                s.GpuYuk = Ada(yukler, "GPU Core") ?? Ada(yukler, "D3D 3D") ?? yukler.FirstOrDefault();
                s.GpuFrekans = Ada(Sensorler(ekranKarti, SensorType.Clock), "GPU Core");
                var gucler = Sensorler(ekranKarti, SensorType.Power);
                s.GpuGuc = Ada(gucler, "GPU Package") ?? Ada(gucler, "GPU Power") ?? Ada(gucler, "GPU Core") ?? gucler.FirstOrDefault();
                var veriler = Sensorler(ekranKarti, SensorType.SmallData);
                s.VramKullanilan = Ada(veriler, "GPU Memory Used") ?? Ada(veriler, "D3D Dedicated Memory Used");
                s.VramToplam = Ada(veriler, "GPU Memory Total") ?? Ada(veriler, "D3D Dedicated Memory Total");
                s.GpuFanlari = Sensorler(ekranKarti, SensorType.Fan).ToArray();
            }

            // ---------- Bellek: "Total Memory" (/ram) ----------
            var bellek = donanimlar.FirstOrDefault(h => h.HardwareType == HardwareType.Memory && h.Identifier.ToString() == "/ram")
                ?? donanimlar.FirstOrDefault(h => h.HardwareType == HardwareType.Memory && Sensorler(h, SensorType.Load).Any());
            if (bellek != null)
            {
                s.RamYuk = Sensorler(bellek, SensorType.Load).FirstOrDefault();
                var veriler = Sensorler(bellek, SensorType.Data);
                s.RamKullanilan = Ada(veriler, "Memory Used");
                s.RamBos = Ada(veriler, "Memory Available");
            }

            // ---------- Anakart fanları ve pompa ----------
            foreach (var h in donanimlar.Where(h => h.HardwareType == HardwareType.Motherboard || h.HardwareType == HardwareType.SuperIO || h.HardwareType == HardwareType.EmbeddedController))
                foreach (var fan in Sensorler(h, SensorType.Fan))
                    s.Fanlar.Add((fan, FanAdi(fan.Name)));

            // ---------- Diskler (sürücü harfiyle) ----------
            var harfler = SurucuHarfleri();
            var diskler = new List<(ISensor Sensor, string Ad, string Sira)>();
            foreach (var h in donanimlar.Where(h => h.HardwareType == HardwareType.Storage))
            {
                var sicaklik = Sensorler(h, SensorType.Temperature)
                    .FirstOrDefault(x => !Regex.IsMatch(x.Name, "Warning|Critical|Limit|Threshold", RegexOptions.IgnoreCase));
                if (sicaklik == null) continue;
                var eslesen = harfler.Where(c => c.Key.IndexOf(h.Name.Trim(), StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                 h.Name.IndexOf(c.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                                     .SelectMany(c => c.Value).Distinct().OrderBy(x => x).ToList();
                string ad = eslesen.Count > 0 ? string.Join(" ", eslesen) : h.Name.Trim();
                diskler.Add((sicaklik, ad, eslesen.Count > 0 ? eslesen[0] : "~" + ad));
            }
            s.Diskler = diskler.OrderBy(d => d.Sira, StringComparer.Ordinal).Select(d => (d.Sensor, d.Ad)).ToList();

            // ---------- Ağ: internete çıkan bağdaştırıcı ----------
            var aglar = donanimlar.Where(h => h.HardwareType == HardwareType.Network).ToList();
            var etkin = EtkinAgKimlikleri();
            var ag = aglar.FirstOrDefault(h => etkin.Any(k => Uri.UnescapeDataString(h.Identifier.ToString()).IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0))
                ?? aglar.OrderByDescending(h => Sensorler(h, SensorType.Data).Sum(x => x.Value ?? 0)).FirstOrDefault();
            if (ag != null)
            {
                var hizlar = Sensorler(ag, SensorType.Throughput);
                s.Indirme = Ada(hizlar, "Download Speed");
                s.Yukleme = Ada(hizlar, "Upload Speed");
            }

            // Her saniye yalnızca bu donanımlar okunur (bellek modülleri, tümleşik grafik, sanal ağlar atlanır)
            s.Donanimlar = new[]
                {
                    s.IslemciSicaklik, s.IslemciYuk, s.IslemciFrekans, s.IslemciGuc, s.GpuSicaklik, s.GpuYuk, s.GpuFrekans,
                    s.GpuGuc, s.VramKullanilan, s.VramToplam, s.RamYuk, s.RamKullanilan, s.RamBos, s.Indirme, s.Yukleme,
                }
                .Concat(s.IslemciCekirdekSaatleri).Concat(s.GpuFanlari)
                .Concat(s.Fanlar.Select(f => f.Sensor)).Concat(s.Diskler.Select(d => d.Sensor))
                .Where(x => x != null).Select(x => x.Hardware).Distinct().ToList();
            return s;
        }

        static IEnumerable<IHardware> Duzlestir(IEnumerable<IHardware> liste)
        {
            foreach (var h in liste)
            {
                yield return h;
                foreach (var a in Duzlestir(h.SubHardware)) yield return a;
            }
        }

        static List<ISensor> Sensorler(IHardware h, SensorType tur) => h.Sensors.Where(x => x.SensorType == tur).ToList();

        static ISensor Ada(IEnumerable<ISensor> liste, string ad) =>
            liste.FirstOrDefault(x => string.Equals(x.Name, ad, StringComparison.OrdinalIgnoreCase));

        static IEnumerable<ISensor> Icerenler(IEnumerable<ISensor> liste, params string[] parcalar) =>
            liste.Where(x => parcalar.Any(p => x.Name.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0));

        static double? GpuBellegi(IHardware h)
        {
            var veriler = Sensorler(h, SensorType.SmallData);
            return (Ada(veriler, "GPU Memory Total") ?? Ada(veriler, "D3D Dedicated Memory Total"))?.Value;
        }

        // Bellek bilgisi eşitse (ya da yoksa) NVIDIA > AMD > Intel
        static double TurPuani(HardwareType t) => t == HardwareType.GpuNvidia ? 0.3 : t == HardwareType.GpuAmd ? 0.2 : 0.1;

        static string KisaIslemciAdi(string ad)
        {
            ad = Regex.Replace(ad ?? "", @"\((R|TM)\)", "", RegexOptions.IgnoreCase);
            ad = Regex.Replace(ad, @"^(AMD|Intel)\s+", "", RegexOptions.IgnoreCase);
            ad = Regex.Replace(ad, @"\s+\d+-Core Processor$|\s+Processor$|\s+CPU\s+@.*$", "", RegexOptions.IgnoreCase);
            return Regex.Replace(ad, @"\s{2,}", " ").Trim();
        }

        static string KisaEkranKartiAdi(string ad)
        {
            ad = Regex.Replace(ad ?? "", @"\((R|TM)\)", "", RegexOptions.IgnoreCase);
            ad = Regex.Replace(ad, @"^NVIDIA\s+GeForce\s+|^NVIDIA\s+|^AMD\s+|^Intel\s+", "", RegexOptions.IgnoreCase);
            ad = Regex.Replace(ad, @"\s+Graphics$", "", RegexOptions.IgnoreCase);
            return Regex.Replace(ad, @"\s{2,}", " ").Trim();
        }

        static string FanAdi(string ad) => Regex.Replace(ad ?? "", @"^Fan #(\d+)$", "Fan $1");

        /// <summary>İnternete bağlı (ağ geçidi olan, çalışan) bağdaştırıcıların kimlikleri; kablolu olan önce.</summary>
        static List<string> EtkinAgKimlikleri()
        {
            try
            {
                return NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                                n.GetIPProperties().GatewayAddresses.Any(g => !g.Address.Equals(System.Net.IPAddress.Any) && !g.Address.Equals(System.Net.IPAddress.IPv6Any)))
                    .OrderBy(n => n.NetworkInterfaceType == NetworkInterfaceType.Ethernet ? 0 : 1)
                    .Select(n => n.Id)
                    .ToList();
            }
            catch (NetworkInformationException)
            {
                return new List<string>();
            }
        }

        static bool _harfHatasiYazildi;

        /// <summary>
        /// Disk modeli → sürücü harfleri ("C:", "D:"). WMI'nın iki ilişki tablosu (disk→bölüm, bölüm→sürücü)
        /// doğrudan okunur; ASSOCIATORS sorguları bazı disklerde "Not found" verebiliyor.
        /// </summary>
        static Dictionary<string, List<string>> SurucuHarfleri()
        {
            var sonuc = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var modeller = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // \\.\PHYSICALDRIVE0 → model
                using (var sorgu = new ManagementObjectSearcher("SELECT DeviceID, Model FROM Win32_DiskDrive"))
                    foreach (ManagementObject disk in sorgu.Get())
                        if (disk["DeviceID"] is string kimlik && disk["Model"] is string model) modeller[kimlik] = model.Trim();

                var bolumDiski = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // "Disk #0, Partition #1" → \\.\PHYSICALDRIVE0
                using (var sorgu = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDriveToDiskPartition"))
                    foreach (ManagementObject iliski in sorgu.Get())
                        bolumDiski[YolKimligi(iliski["Dependent"])] = YolKimligi(iliski["Antecedent"]);

                using (var sorgu = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDiskToPartition"))
                    foreach (ManagementObject iliski in sorgu.Get())
                    {
                        if (!bolumDiski.TryGetValue(YolKimligi(iliski["Antecedent"]), out var disk) || !modeller.TryGetValue(disk, out var model)) continue;
                        if (!sonuc.TryGetValue(model, out var liste)) sonuc[model] = liste = new List<string>();
                        liste.Add(YolKimligi(iliski["Dependent"]));
                    }
            }
            catch (Exception e)
            {
                if (!_harfHatasiYazildi) Gunluk.Yaz("Sürücü harfleri okunamadı", e);
                _harfHatasiYazildi = true;
            }
            return sonuc;
        }

        /// <summary>WMI nesne yolundan DeviceID değeri: ...Win32_DiskPartition.DeviceID="Disk #0, Partition #1"</summary>
        static string YolKimligi(object yol)
        {
            var m = Regex.Match(yol as string ?? "", "DeviceID=\"(.*)\"$");
            return m.Success ? m.Groups[1].Value.Replace(@"\\", @"\") : "";
        }
    }

    /// <summary>Seçilen sensörler; her saniye <see cref="Oku"/> ile anlık değerlere dönüştürülür.</summary>
    internal sealed class Secim
    {
        public string IslemciAdi, EkranKartiAdi;
        public ISensor IslemciSicaklik, IslemciYuk, IslemciFrekans, IslemciGuc;
        public ISensor[] IslemciCekirdekSaatleri = new ISensor[0];
        public ISensor GpuSicaklik, GpuYuk, GpuFrekans, GpuGuc, VramKullanilan, VramToplam;
        public ISensor[] GpuFanlari = new ISensor[0];
        public ISensor RamYuk, RamKullanilan, RamBos;
        public List<(ISensor Sensor, string Ad)> Fanlar = new List<(ISensor, string)>();
        public List<(ISensor Sensor, string Ad)> Diskler = new List<(ISensor, string)>();
        public ISensor Indirme, Yukleme;
        public List<IHardware> Donanimlar = new List<IHardware>();

        /// <param name="donenFanlar">Bir kez bile dönmüş fanların kimlikleri; boş fan yuvaları gösterilmez.</param>
        public Anlik Oku(HashSet<string> donenFanlar)
        {
            var a = new Anlik
            {
                Hazir = true,
                IslemciAdi = IslemciAdi,
                // Sürücüye erişilemeyince (yönetici değilken) sıcaklık ve frekans 0 gelir; 0 gösterilmez
                IslemciSicaklik = Pozitif(D(IslemciSicaklik)),
                IslemciYuk = D(IslemciYuk),
                IslemciFrekans = Pozitif(D(IslemciFrekans) ?? Ortalama(IslemciCekirdekSaatleri)),
                IslemciGuc = Pozitif(D(IslemciGuc)),
                EkranKartiAdi = EkranKartiAdi,
                GpuSicaklik = Pozitif(D(GpuSicaklik)),
                GpuYuk = D(GpuYuk),
                GpuFrekans = Pozitif(D(GpuFrekans)),
                GpuGuc = Pozitif(D(GpuGuc)),
                VramKullanilan = D(VramKullanilan),
                VramToplam = D(VramToplam),
                GpuFan = GpuFanlari.Length == 0 ? (double?)null : GpuFanlari.Max(f => D(f) ?? 0),
                RamYuk = D(RamYuk),
                RamKullanilan = D(RamKullanilan),
                Indirme = D(Indirme),
                Yukleme = D(Yukleme),
            };
            var bos = D(RamBos);
            if (a.RamKullanilan != null && bos != null) a.RamToplam = a.RamKullanilan + bos;

            foreach (var (sensor, ad) in Fanlar)
            {
                string kimlik = sensor.Identifier.ToString();
                var deger = D(sensor);
                if (deger > 0) donenFanlar.Add(kimlik);
                if (donenFanlar.Contains(kimlik)) a.Fanlar.Add(new SensorDegeri { Kimlik = kimlik, VarsayilanAd = ad, Deger = deger });
            }
            foreach (var (sensor, ad) in Diskler)
                a.Diskler.Add(new SensorDegeri { Kimlik = sensor.Identifier.ToString(), VarsayilanAd = ad, Deger = Pozitif(D(sensor)) });
            return a;
        }

        static double? Pozitif(double? d) => d > 0 ? d : null;

        static double? D(ISensor s)
        {
            float? v = s?.Value;
            return v == null || float.IsNaN(v.Value) || float.IsInfinity(v.Value) ? (double?)null : v.Value;
        }

        static double? Ortalama(ISensor[] liste)
        {
            var degerler = liste.Select(D).Where(v => v != null).ToList();
            return degerler.Count == 0 ? null : degerler.Average();
        }

        /// <summary>Günlüğe yazılan özet: arkadaşların bilgisayarında yanlış sensör seçilirse buradan anlaşılır.</summary>
        public string Ozet()
        {
            var b = new StringBuilder("Seçilen sensörler:");
            void Ekle(string baslik, ISensor s) => b.Append($"\n  {baslik}: {(s == null ? "-" : $"{s.Hardware.Name} / {s.Name} [{s.Identifier}] = {s.Value}")}");
            b.Append($"\n  İşlemci: {IslemciAdi ?? "-"}, ekran kartı: {EkranKartiAdi ?? "-"}");
            Ekle("İşlemci sıcaklığı", IslemciSicaklik);
            Ekle("İşlemci yükü", IslemciYuk);
            Ekle("İşlemci frekansı", IslemciFrekans);
            Ekle("İşlemci gücü", IslemciGuc);
            Ekle("GPU sıcaklığı", GpuSicaklik);
            Ekle("GPU yükü", GpuYuk);
            Ekle("GPU frekansı", GpuFrekans);
            Ekle("GPU gücü", GpuGuc);
            Ekle("VRAM kullanılan", VramKullanilan);
            Ekle("VRAM toplam", VramToplam);
            Ekle("RAM yükü", RamYuk);
            Ekle("İndirme", Indirme);
            Ekle("Yükleme", Yukleme);
            foreach (var f in GpuFanlari) Ekle("GPU fanı", f);
            foreach (var (s, ad) in Fanlar) Ekle("Fan " + ad, s);
            foreach (var (s, ad) in Diskler) Ekle("Disk " + ad, s);
            return b.ToString();
        }
    }
}
