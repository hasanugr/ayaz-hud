using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using AyazHUD.Yerel;

namespace AyazHUD.Arayuz
{
    /// <summary>
    /// Monitörleri Windows'taki yerleşimleriyle çizen seçici (Ekran ayarlarındaki gibi). Her monitörde numarası,
    /// modeli ve çözünürlüğü, üstünde de açık widget'ların küçük taslakları görünür. Tıklanan monitör widget'ların
    /// ekranı olur.
    /// </summary>
    internal sealed class EkranSecici : Canvas
    {
        const double Aralik = 8; // monitörler arası boşluk

        List<Ekran> _ekranlar = new List<Ekran>();
        string _secili;
        List<Win32.RECT> _widgetlar = new List<Win32.RECT>();

        public event Action<string> Secildi;

        public EkranSecici()
        {
            ClipToBounds = true;
            SizeChanged += (s, e) => Ciz();
        }

        /// <param name="widgetlar">Açık widget'ların ekrandaki yerleri (fiziksel piksel).</param>
        public void Ayarla(List<Ekran> ekranlar, string secili, List<Win32.RECT> widgetlar)
        {
            _ekranlar = ekranlar;
            _secili = secili;
            _widgetlar = widgetlar;
            Ciz();
        }

        void Ciz()
        {
            Children.Clear();
            if (_ekranlar.Count == 0 || ActualWidth <= 0 || ActualHeight <= 0) return;

            // Sanal masaüstünü alana sığdır ve ortala
            int sol = _ekranlar.Min(e => e.Alan.Left), ust = _ekranlar.Min(e => e.Alan.Top);
            int sag = _ekranlar.Max(e => e.Alan.Right), alt = _ekranlar.Max(e => e.Alan.Bottom);
            double olcek = Math.Min(ActualWidth / (sag - sol), ActualHeight / (alt - ust));
            double ox = (ActualWidth - (sag - sol) * olcek) / 2, oy = (ActualHeight - (alt - ust) * olcek) / 2;
            var vurgu = (Application.Current.Resources["Vurgu"] as SolidColorBrush)?.Color ?? Tema.VarsayilanVurgu;

            foreach (var e in _ekranlar)
            {
                bool secili = e.Ad == _secili;
                double x = ox + (e.Alan.Left - sol) * olcek + Aralik / 2, y = oy + (e.Alan.Top - ust) * olcek + Aralik / 2;
                double w = Math.Max(20, e.Alan.Genislik * olcek - Aralik), h = Math.Max(20, e.Alan.Yukseklik * olcek - Aralik);
                const double kenar = 2;
                var yuz = new Grid { Width = w - 2 * kenar, Height = h - 2 * kenar, ClipToBounds = true };

                // Widget taslakları: monitörün içindeki yerleriyle, aynı ölçekte
                var taslak = new Canvas();
                foreach (var r in _widgetlar)
                {
                    int mx = r.Left + r.Genislik / 2, my = r.Top + r.Yukseklik / 2;
                    if (mx < e.Alan.Left || mx >= e.Alan.Right || my < e.Alan.Top || my >= e.Alan.Bottom) continue;
                    var kutu = new Rectangle
                    {
                        Width = Math.Max(2, r.Genislik * olcek),
                        Height = Math.Max(2, r.Yukseklik * olcek),
                        RadiusX = 2,
                        RadiusY = 2,
                        Fill = new SolidColorBrush(secili ? Tema.Alfa(vurgu, 0.3) : Color.FromArgb(40, 255, 255, 255)),
                    };
                    SetLeft(kutu, (r.Left - e.Alan.Left) * olcek - kenar);
                    SetTop(kutu, (r.Top - e.Alan.Top) * olcek - kenar);
                    taslak.Children.Add(kutu);
                }
                yuz.Children.Add(taslak);

                var yazi = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 6, 0) };
                yazi.Children.Add(new TextBlock
                {
                    Text = e.Numara.ToString(),
                    FontFamily = Tema.SayiYazi,
                    FontSize = Math.Max(16, Math.Min(34, h * 0.24)),
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = secili ? new SolidColorBrush(vurgu) : Brushes.White,
                });
                yazi.Children.Add(new TextBlock
                {
                    Text = e.Model ?? string.Format(Dil.M("a.ekranAdi"), e.Numara),
                    FontSize = 12.5,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Foreground = new SolidColorBrush(Color.FromRgb(232, 235, 242)),
                });
                yazi.Children.Add(new TextBlock
                {
                    Text = $"{e.Alan.Genislik} × {e.Alan.Yukseklik}" + (e.Birincil ? " · " + Dil.M("a.birincil") : ""),
                    FontSize = 11.5,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Foreground = new SolidColorBrush(Color.FromRgb(153, 160, 174)),
                });
                yuz.Children.Add(yazi);

                if (secili)
                    yuz.Children.Add(new Border
                    {
                        CornerRadius = new CornerRadius(8),
                        Background = new SolidColorBrush(vurgu),
                        Padding = new Thickness(7, 1, 7, 2),
                        Margin = new Thickness(7),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Child = new TextBlock { Text = Dil.M("a.widgetlarBurada"), FontSize = 10.5, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(14, 16, 22)) },
                    });

                var dugme = new Button
                {
                    Content = yuz,
                    Width = w,
                    Height = h,
                    Padding = new Thickness(0),
                    Margin = new Thickness(0),
                    BorderThickness = new Thickness(kenar),
                    BorderBrush = new SolidColorBrush(secili ? vurgu : Color.FromRgb(52, 58, 74)),
                    Background = new SolidColorBrush(secili ? Tema.Alfa(vurgu, 0.12) : Color.FromRgb(30, 34, 45)),
                };
                AutomationProperties.SetName(dugme, string.Format(Dil.M("a.ekranAdi"), e.Numara));
                string ad = e.Ad;
                dugme.Click += (s, a) => Secildi?.Invoke(ad);
                SetLeft(dugme, x);
                SetTop(dugme, y);
                Children.Add(dugme);
            }
        }
    }
}
