using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace AyazHUD.Arayuz
{
    /// <summary>Kart başlığı: solda harf aralıklı büyük harfli başlık, sağda soluk renkli bilgi (cihaz adı vb.).</summary>
    internal sealed class KartBasligi : DockPanel
    {
        readonly AralikliMetin _baslik = new AralikliMetin { VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 2) };
        readonly TextBlock _sag = new TextBlock
        {
            FontSize = 15.2,
            FontWeight = FontWeights.Medium,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(16, 0, 0, 0),
        };

        public KartBasligi()
        {
            LastChildFill = true;
            _sag.FontFamily = Tema.MetinYazi;
            _sag.Foreground = Tema.Soluk;
            SetDock(_baslik, Dock.Left);
            Children.Add(_baslik);
            Children.Add(_sag);
        }

        public string Baslik
        {
            get => _baslik.Metin;
            set => _baslik.Metin = value;
        }

        public string Sag
        {
            get => _sag.Text;
            set
            {
                if (_sag.Text != value) _sag.Text = value ?? "";
            }
        }
    }

    /// <summary>Halka ölçerin yanındaki satır: solda etiket, sağda büyük değer ve küçük birim.</summary>
    internal sealed class DegerSatiri : Border
    {
        readonly TextBlock _etiket = new TextBlock { FontSize = 16, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 3) };
        readonly Run _deger = new Run("--");
        readonly Run _birim = new Run();

        public DegerSatiri()
        {
            _etiket.FontFamily = Tema.MetinYazi;
            _etiket.Foreground = Tema.Soluk;
            var deger = new TextBlock { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom };
            DegerYazisi(deger, _deger, _birim, 28);
            var panel = new DockPanel();
            DockPanel.SetDock(_etiket, Dock.Left);
            panel.Children.Add(_etiket);
            panel.Children.Add(deger);
            Child = panel;
            Son = false;
        }

        public string Etiket
        {
            get => _etiket.Text;
            set => _etiket.Text = value;
        }

        public string Birim
        {
            get => _birim.Text.Trim();
            set => _birim.Text = string.IsNullOrEmpty(value) ? "" : " " + value;
        }

        public string Deger
        {
            get => _deger.Text;
            set
            {
                if (_deger.Text != value) _deger.Text = value;
            }
        }

        /// <summary>Listenin son satırında alt çizgi ve boşluk olmaz.</summary>
        public bool Son
        {
            get => BorderThickness.Bottom == 0;
            set
            {
                BorderBrush = Tema.SatirCizgi;
                BorderThickness = new Thickness(0, 0, 0, value ? 0 : 1);
                Padding = new Thickness(0, 0, 0, value ? 0 : 6.4);
                Margin = new Thickness(0, 0, 0, value ? 0 : 8.8);
            }
        }

        internal static void DegerYazisi(TextBlock t, Run deger, Run birim, double boyut)
        {
            t.FontFamily = Tema.SayiYazi;
            t.FontSize = boyut;
            t.FontWeight = FontWeights.Medium;
            t.Foreground = Tema.Metin;
            birim.FontFamily = Tema.MetinYazi;
            birim.FontSize = 12.8;
            birim.FontWeight = FontWeights.Normal;
            birim.Foreground = Tema.Soluk;
            t.Inlines.Add(deger);
            t.Inlines.Add(birim);
        }
    }

    /// <summary>Izgaradaki küçük değer: üstte etiket, altta değer ve birim.</summary>
    internal sealed class KucukDeger : StackPanel
    {
        readonly TextBlock _etiket = new TextBlock { FontSize = 14.4, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 0, 0, 2.4) };
        readonly Run _deger = new Run("--");
        readonly Run _birim = new Run();

        public KucukDeger()
        {
            _etiket.FontFamily = Tema.MetinYazi;
            _etiket.Foreground = Tema.Soluk;
            var deger = new TextBlock();
            DegerSatiri.DegerYazisi(deger, _deger, _birim, 25.6);
            Children.Add(_etiket);
            Children.Add(deger);
        }

        public string Etiket
        {
            get => _etiket.Text;
            set
            {
                if (_etiket.Text != value) _etiket.Text = value;
            }
        }

        public string Birim
        {
            get => _birim.Text.Trim();
            set
            {
                string yeni = string.IsNullOrEmpty(value) ? "" : " " + value;
                if (_birim.Text != yeni) _birim.Text = yeni;
            }
        }

        public string Deger
        {
            get => _deger.Text;
            set
            {
                if (_deger.Text != value) _deger.Text = value;
            }
        }
    }
}
