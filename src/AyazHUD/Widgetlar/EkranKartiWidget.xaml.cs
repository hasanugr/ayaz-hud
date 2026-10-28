using System.Windows.Controls;
using AyazHUD.Arayuz;
using AyazHUD.Cekirdek;

namespace AyazHUD.Widgetlar
{
    internal partial class EkranKartiWidget : UserControl, IWidget
    {
        public EkranKartiWidget() => InitializeComponent();

        public void Guncelle(Anlik a)
        {
            Baslik.Sag = a.EkranKartiAdi ?? "";
            Halka.Ayarla(a.GpuSicaklik, a.GpuYuk);
            Yuk.Deger = Bicim.Sayi(a.GpuYuk);
            Frekans.Deger = Bicim.Sayi(a.GpuFrekans);
            Guc.Deger = Bicim.Sayi(a.GpuGuc);
            if (a.VramKullanilan != null && a.VramToplam > 0)
            {
                VramBaslik.Sag = $"{Bicim.Sayi(a.VramKullanilan / 1024, 1)} / {Bicim.Sayi(a.VramToplam / 1024)} GB";
                VramCubuk.Ayarla(a.VramKullanilan / a.VramToplam);
            }
            if (a.Hazir) Grafik.Ekle(a.GpuYuk);
        }
    }
}
