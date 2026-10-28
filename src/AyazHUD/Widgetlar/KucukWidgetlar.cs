using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Effects;
using AyazHUD.Arayuz;
using AyazHUD.Ayar;
using AyazHUD.Cekirdek;

namespace AyazHUD.Widgetlar
{
    /// <summary>Saat ve tarih. Kart değil; gölgeli düz metin.</summary>
    internal sealed class SaatWidget : StackPanel, IWidget
    {
        /// <summary>Gölge pencerenin kenarında kesilmesin diye içerik etrafında bırakılan boşluk.</summary>
        public const double Bosluk = 16;

        readonly Run _saat = new Run("--:--");
        readonly Run _saniye = new Run("--");
        readonly TextBlock _tarih = new TextBlock { FontSize = 21.6, Margin = new Thickness(0, 5.6, 0, 0), Text = " " };

        public SaatWidget()
        {
            Margin = new Thickness(Bosluk + 6.4, Bosluk, Bosluk + 6.4, Bosluk + 6.4);
            var ana = new TextBlock { FontFamily = Tema.SayiYazi, Foreground = Tema.Metin, LineHeight = 102.4, LineStackingStrategy = LineStackingStrategy.BlockLineHeight };
            _saat.FontSize = 102.4;
            _saat.FontWeight = FontWeights.Light;
            _saniye.FontSize = 32;
            Loaded += (s, e) => _saniye.Foreground = Gorunum.Al(this).Baslik;
            ana.Inlines.Add(_saat);
            ana.Inlines.Add(new Run("  ") { FontSize = 16 });
            ana.Inlines.Add(_saniye);
            _tarih.FontFamily = Tema.MetinYazi;
            _tarih.Foreground = Tema.Soluk;
            Children.Add(ana);
            Children.Add(_tarih);
            Effect = new DropShadowEffect { Color = Colors.Black, Opacity = 0.55, BlurRadius = 18, ShadowDepth = 2, Direction = 270 };
            Tik(DateTime.Now);
        }

        public void Tik(DateTime t)
        {
            _saat.Text = t.ToString("HH:mm", Dil.Kultur);
            _saniye.Text = t.ToString("ss", Dil.Kultur);
            _tarih.Text = t.ToString(Dil.M("tarihBicimi"), Dil.Kultur);
        }

        public void Guncelle(Anlik anlik) => Tik(DateTime.Now);
    }

    internal sealed class BellekWidget : StackPanel, IWidget
    {
        readonly KartBasligi _baslik = new KartBasligi { Baslik = Dil.M("b.bellek") };
        readonly Cubuk _cubuk = new Cubuk { Margin = new Thickness(0, 12, 0, 0) };

        public BellekWidget()
        {
            Children.Add(_baslik);
            Children.Add(_cubuk);
        }

        public void Guncelle(Anlik a)
        {
            if (a.RamKullanilan == null || !(a.RamToplam > 0)) return;
            _baslik.Sag = $"{Bicim.Sayi(a.RamKullanilan, 1)} / {Bicim.Sayi(Math.Round(a.RamToplam.Value))} GB · {Dil.Yuzde(Bicim.Sayi(a.RamYuk))}";
            _cubuk.Ayarla((a.RamYuk ?? a.RamKullanilan / a.RamToplam * 100) / 100);
        }
    }

    /// <summary>Pompa ve kasa fanları, ekran kartı fanı, disk sıcaklıkları.</summary>
    internal sealed class SogutmaWidget : StackPanel, IWidget
    {
        readonly Ayarlar _ayarlar;
        readonly UniformGrid _izgara = new UniformGrid { Columns = 2, Margin = new Thickness(0, 12.8, -16, -14.4) };
        readonly List<KucukDeger> _hucreler = new List<KucukDeger>();
        string _duzen = "";

        public SogutmaWidget(Ayarlar ayarlar)
        {
            _ayarlar = ayarlar;
            Children.Add(new KartBasligi { Baslik = Dil.M("b.sogutma") });
            Children.Add(_izgara);
        }

        public void Guncelle(Anlik a)
        {
            var satirlar = new List<(string Etiket, string Deger, string Birim)>();
            foreach (var fan in a.Fanlar.Where(f => !_ayarlar.Gizli(f)))
                satirlar.Add((_ayarlar.Ad(fan), Bicim.Sayi(fan.Deger), "RPM"));
            if (a.GpuFan != null)
                satirlar.Add((Dil.M("gpuFani"), a.GpuFan == 0 ? Dil.M("duruyor") : Bicim.Sayi(a.GpuFan), a.GpuFan == 0 ? "" : "RPM"));
            // Diskler ikişer ikişer aynı hücrede; daha fazlası hücreye sığmıyor
            var diskler = a.Diskler.Where(d => !_ayarlar.Gizli(d)).ToList();
            for (int i = 0; i < diskler.Count; i += 2)
            {
                var ikili = diskler.Skip(i).Take(2).ToList();
                satirlar.Add((Dil.M("disk") + " " + string.Join(" / ", ikili.Select(_ayarlar.Ad)),
                              string.Join(" / ", ikili.Select(d => Bicim.Sayi(d.Deger))), "°C"));
            }

            // Satırların sayısı değiştiyse hücreleri yeniden kur, yoksa yalnızca değerleri güncelle
            string duzen = satirlar.Count.ToString();
            if (duzen != _duzen)
            {
                _duzen = duzen;
                _izgara.Children.Clear();
                _hucreler.Clear();
                foreach (var _ in satirlar)
                {
                    var h = new KucukDeger { Margin = new Thickness(0, 0, 16, 14.4) };
                    _hucreler.Add(h);
                    _izgara.Children.Add(h);
                }
            }
            for (int i = 0; i < satirlar.Count; i++)
            {
                _hucreler[i].Etiket = satirlar[i].Etiket;
                _hucreler[i].Deger = satirlar[i].Deger;
                _hucreler[i].Birim = satirlar[i].Birim;
            }
        }
    }

    internal sealed class AgWidget : StackPanel, IWidget
    {
        readonly KucukDeger _indirme = new KucukDeger { Etiket = Dil.M("indirme"), Birim = "MB/s", Margin = new Thickness(0, 0, 16, 0) };
        readonly KucukDeger _yukleme = new KucukDeger { Etiket = Dil.M("yukleme"), Birim = "MB/s" };

        public AgWidget()
        {
            Children.Add(new KartBasligi { Baslik = Dil.M("b.ag") });
            var izgara = new UniformGrid { Columns = 2, Margin = new Thickness(0, 12.8, 0, 0) };
            izgara.Children.Add(_indirme);
            izgara.Children.Add(_yukleme);
            Children.Add(izgara);
        }

        public void Guncelle(Anlik a)
        {
            (_indirme.Deger, _indirme.Birim) = Bicim.Hiz(a.Indirme);
            (_yukleme.Deger, _yukleme.Birim) = Bicim.Hiz(a.Yukleme);
        }
    }
}
