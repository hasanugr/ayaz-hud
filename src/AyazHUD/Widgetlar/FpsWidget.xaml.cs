using System.Windows;
using System.Windows.Controls;
using AyazHUD.Arayuz;
using AyazHUD.Cekirdek;

namespace AyazHUD.Widgetlar
{
    internal partial class FpsWidget : UserControl, IWidget
    {
        public FpsWidget()
        {
            InitializeComponent();
            Deger.FontFamily = Tema.SayiYazi;
            Deger.Foreground = Tema.Metin;
            Bos.FontFamily = Tema.MetinYazi;
            Bos.Foreground = Tema.Soluk;
            Gorunum g = null;
            Loaded += (s, e) =>
            {
                if (g != null) return;
                g = Gorunum.Al(this);
                Parilti.Color = g.VurguRengi;
                g.Degisti += () => Parilti.Color = g.VurguRengi;
            };
        }

        public void Guncelle(Anlik a)
        {
            var f = a.Fps;
            bool oyun = f.Hal == FpsHali.Tamam;
            Deger.Visibility = oyun ? Visibility.Visible : Visibility.Collapsed;
            Bos.Visibility = oyun ? Visibility.Collapsed : Visibility.Visible;
            if (oyun)
            {
                Deger.Text = f.Deger.ToString(Dil.Kultur);
                Dusuk.Deger = f.Yuzde1Dusuk.ToString(Dil.Kultur);
                KareSuresi.Deger = Bicim.Sayi(f.KareSuresi, 1);
                Gecikme.Deger = Bicim.Sayi(f.Gecikme, f.Gecikme < 100 ? 1 : 0);
                Baslik.Sag = f.Uygulama;
                Grafik.Ekle(f.Deger);
            }
            else
            {
                Bos.Text = Dil.M(f.Hal == FpsHali.IzinYok ? "fps.izinYok" : f.Hal == FpsHali.Hata ? "fps.hata" : "fps.bekleniyor");
                Dusuk.Deger = "--";
                KareSuresi.Deger = "--";
                Gecikme.Deger = "--";
                Baslik.Sag = "";
                Grafik.Ekle(0);
            }
        }
    }
}
