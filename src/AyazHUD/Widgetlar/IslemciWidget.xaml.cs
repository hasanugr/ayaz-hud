using System.Windows.Controls;
using AyazHUD.Arayuz;
using AyazHUD.Cekirdek;

namespace AyazHUD.Widgetlar
{
    internal partial class IslemciWidget : UserControl, IWidget
    {
        public IslemciWidget() => InitializeComponent();

        public void Guncelle(Anlik a)
        {
            Baslik.Sag = a.IslemciAdi ?? "";
            Halka.Ayarla(a.IslemciSicaklik, a.IslemciYuk);
            Yuk.Deger = Bicim.Sayi(a.IslemciYuk);
            Frekans.Deger = Bicim.Sayi(a.IslemciFrekans / 1000, 2);
            Guc.Deger = Bicim.Sayi(a.IslemciGuc);
            if (a.Hazir) Grafik.Ekle(a.IslemciYuk);
        }
    }
}
