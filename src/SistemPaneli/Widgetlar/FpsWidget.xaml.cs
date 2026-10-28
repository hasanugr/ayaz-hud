using System.Windows;
using System.Windows.Controls;
using SistemPaneli.Arayuz;
using SistemPaneli.Cekirdek;

namespace SistemPaneli.Widgetlar
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
            Parilti.Color = Tema.VurguRengi;
            Tema.Degisti += () => Parilti.Color = Tema.VurguRengi;
        }

        public void Guncelle(Anlik a)
        {
            var f = a.Fps;
            bool oyun = f.Hal == FpsHali.Tamam;
            Deger.Visibility = oyun ? Visibility.Visible : Visibility.Collapsed;
            Bos.Visibility = oyun ? Visibility.Collapsed : Visibility.Visible;
            if (oyun)
            {
                Deger.Text = f.Deger.ToString(Bicim.Tr);
                Dusuk.Deger = f.Yuzde1Dusuk.ToString(Bicim.Tr);
                KareSuresi.Deger = Bicim.Sayi(f.KareSuresi, 1);
                Baslik.Sag = f.Uygulama;
                Grafik.Ekle(f.Deger);
            }
            else
            {
                Bos.Text = f.Hal == FpsHali.IzinYok ? "Yönetici izni gerekiyor"
                         : f.Hal == FpsHali.Hata ? "FPS ölçülemiyor"
                         : "Oyun bekleniyor";
                Dusuk.Deger = "--";
                KareSuresi.Deger = "--";
                Baslik.Sag = "";
                Grafik.Ekle(0);
            }
        }
    }
}
