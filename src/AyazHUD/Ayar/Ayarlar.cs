using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using AyazHUD.Cekirdek;

namespace AyazHUD.Ayar
{
    /// <summary>Kullanıcı ayarları: %APPDATA%\AyazHUD\ayarlar.json</summary>
    [DataContract]
    internal sealed class Ayarlar
    {
        // ---------- Bütün widget'ların varsayılan görünümü ----------
        [DataMember(Order = 1)] public string Vurgu { get; set; }
        /// <summary>Kart zemininin koyuluğu (0–0,9).</summary>
        [DataMember(Order = 2)] public double Koyuluk { get; set; }
        /// <summary>Kart stili: Bulanik, Akrilik, Koyu, Seffaf (bkz. Arayuz.KartStili).</summary>
        [DataMember(Order = 3)] public string Stil { get; set; }
        /// <summary>Kartların buzlanma gücü: 1 hafif, 2 orta, 3 güçlü.</summary>
        [DataMember(Order = 8)] public int Buzlanma { get; set; }

        // ---------- Arka plan: widget'ların olduğu ekranın tamamını buzlar ve/veya karartır ----------
        /// <summary>0 = yok, 1 hafif, 2 orta, 3 güçlü.</summary>
        [DataMember(Order = 4)] public int ArkaPlanBuzlanma { get; set; }
        /// <summary>0–0,7.</summary>
        [DataMember(Order = 5)] public double ArkaPlanKarartma { get; set; }

        public bool ArkaPlanAcik => ArkaPlanBuzlanma > 0 || ArkaPlanKarartma > 0.005;

        [DataMember(Order = 6)] public List<WidgetAyari> Widgetlar { get; set; }
        /// <summary>Fan ve disklere verilen adlar ve gizlenenler (anahtar: sensör kimliği).</summary>
        [DataMember(Order = 7)] public List<SensorEtiketi> Etiketler { get; set; }

        /// <summary>Arayüz dili: "tr" ya da "en".</summary>
        [DataMember(Order = 9)] public string Dil { get; set; }

        public Ayarlar() => Varsayilanlar();

        // Seri hale getirici kurucuyu çağırmadığı için eksik alanların varsayılanları burada verilir
        [OnDeserializing]
        void Okunurken(StreamingContext c) => Varsayilanlar();

        void Varsayilanlar()
        {
            Vurgu = "#7DD3FC";
            Koyuluk = 0.45;
            Stil = "Bulanik";
            Buzlanma = 1;
            ArkaPlanBuzlanma = 0;
            ArkaPlanKarartma = 0;
            Widgetlar = new List<WidgetAyari>();
            Etiketler = new List<SensorEtiketi>();
            Dil = "tr";
        }

        public WidgetAyari Widget(string kimlik)
        {
            var w = Widgetlar.FirstOrDefault(x => x.Kimlik == kimlik);
            if (w == null) Widgetlar.Add(w = new WidgetAyari { Kimlik = kimlik, Acik = true });
            return w;
        }

        public SensorEtiketi Etiket(string kimlik) => Etiketler.FirstOrDefault(x => x.Kimlik == kimlik);

        public SensorEtiketi EtiketOlustur(string kimlik)
        {
            var e = Etiket(kimlik);
            if (e == null) Etiketler.Add(e = new SensorEtiketi { Kimlik = kimlik });
            return e;
        }

        public string Ad(SensorDegeri s)
        {
            var ad = Etiket(s.Kimlik)?.Ad;
            return string.IsNullOrWhiteSpace(ad) ? s.VarsayilanAd : ad.Trim();
        }

        public bool Gizli(SensorDegeri s) => Etiket(s.Kimlik)?.Gizli == true;

        // ---------- Okuma / yazma ----------

        public static readonly string Klasor = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AyazHUD");
        static readonly string Dosya = Path.Combine(Klasor, "ayarlar.json");

        static DataContractJsonSerializer Serilestirici() =>
            new DataContractJsonSerializer(typeof(Ayarlar), new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });

        // Uygulamanın eski adı "Sistem Paneli" idi; ilk açılışta ayarlar oradan alınır
        static readonly string EskiDosya = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SistemPaneli", "ayarlar.json");

        public static Ayarlar Yukle()
        {
            try
            {
                string kaynak = File.Exists(Dosya) ? Dosya : File.Exists(EskiDosya) ? EskiDosya : null;
                if (kaynak != null)
                    using (var akis = File.OpenRead(kaynak))
                    {
                        var a = (Ayarlar)Serilestirici().ReadObject(akis) ?? new Ayarlar();
                        if (kaynak == EskiDosya) Gunluk.Yaz("Ayarlar eski klasörden (SistemPaneli) alındı");
                        return a;
                    }
            }
            catch (Exception e)
            {
                Gunluk.Yaz("Ayarlar okunamadı, varsayılanlar kullanılıyor", e);
            }
            return new Ayarlar();
        }

        public void Kaydet()
        {
            try
            {
                Directory.CreateDirectory(Klasor);
                string gecici = Dosya + ".yeni";
                using (var akis = File.Create(gecici))
                using (var yazici = JsonReaderWriterFactory.CreateJsonWriter(akis, Encoding.UTF8, false, true, "  "))
                {
                    Serilestirici().WriteObject(yazici, this);
                }
                if (File.Exists(Dosya)) File.Replace(gecici, Dosya, null);
                else File.Move(gecici, Dosya);
            }
            catch (Exception e)
            {
                Gunluk.Yaz("Ayarlar kaydedilemedi", e);
            }
        }
    }

    [DataContract]
    internal sealed class WidgetAyari
    {
        [DataMember(Order = 1)] public string Kimlik { get; set; }
        [DataMember(Order = 2)] public bool Acik { get; set; }
        /// <summary>Monitörün aygıt adı (\\.\DISPLAY1); boşsa konum kaydedilmemiş.</summary>
        [DataMember(Order = 3)] public string Monitor { get; set; }
        /// <summary>Monitörün sol üst köşesine göre fiziksel piksel.</summary>
        [DataMember(Order = 4)] public int X { get; set; }
        [DataMember(Order = 5)] public int Y { get; set; }

        /// <summary>true ise aşağıdaki görünüm kullanılır; false ise genel görünüm.</summary>
        [DataMember(Order = 6)] public bool OzelGorunum { get; set; }
        [DataMember(Order = 7)] public string Stil { get; set; }
        [DataMember(Order = 8)] public string Vurgu { get; set; }
        [DataMember(Order = 9)] public double Koyuluk { get; set; }
        [DataMember(Order = 10)] public int Buzlanma { get; set; }
    }

    [DataContract]
    internal sealed class SensorEtiketi
    {
        [DataMember(Order = 1)] public string Kimlik { get; set; }
        [DataMember(Order = 2)] public string Ad { get; set; }
        [DataMember(Order = 3)] public bool Gizli { get; set; }
    }
}
