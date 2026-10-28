using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using AyazHUD.Arayuz;
using AyazHUD.Yerel;

namespace AyazHUD.Widgetlar
{
    /// <summary>
    /// Arka plan ayarı: widget'ların olduğu monitörün tamamını kaplayan, tıklamaları geçiren buzlanma ve karartma.
    /// Buzlanmanın nasıl yapıldığı <see cref="BuzSeviyesi"/>'nde; karartma en üst katmanın tonudur. Masaüstü kopyası
    /// pencerenin tonunu örttüğü için kopya varken karartma ayrı bir katmanda durur.
    /// Katmanlar ve widget'lar birbirine bağlı (sahipli) pencereler olduğu için sıra hep: masaüstü, katmanlar, widget'lar.
    /// </summary>
    internal sealed class BuzKatmani
    {
        readonly List<KatmanPenceresi> _katmanlar = new List<KatmanPenceresi>();
        Ekran _ekran;

        public string EkranAdi => _ekran.Ad;

        /// <summary>En üstteki katman; widget'lar buna bağlanır.</summary>
        public Window Ust => _katmanlar.Count > 0 ? _katmanlar[_katmanlar.Count - 1] : null;

        public BuzKatmani(Ekran ekran) => _ekran = ekran;

        /// <param name="buzlanma">Yüzde (0 = yok).</param>
        /// <param name="karartma">Koyu ton (0–0,9).</param>
        public void Ayarla(int buzlanma, double karartma)
        {
            var s = BuzSeviyesi.Al(buzlanma);
            // Buzlanma yoksa yalnızca karartan tek bir katman
            var camlar = Enumerable.Repeat(s.Katman > 0 ? Win32.AccentState.Bulanik : Win32.AccentState.Kapali, Math.Max(1, s.Katman)).ToList();
            int kopyali = camlar.Count - 1;
            if (s.Kopyali && karartma > 0.005) camlar.Add(Win32.AccentState.Kapali);
            if (!camlar.SequenceEqual(_katmanlar.Select(k => k.Cam)))
            {
                Kapat();
                foreach (var cam in camlar)
                {
                    var k = new KatmanPenceresi(cam, false) { Owner = _katmanlar.LastOrDefault() };
                    _katmanlar.Add(k);
                    k.Show();
                }
            }
            for (int i = 0; i < _katmanlar.Count; i++)
            {
                _katmanlar[i].Kopyala(s.Yaricap, i == kopyali && s.Kopyali ? s.Opaklik : 0);
                _katmanlar[i].Karartma = i == _katmanlar.Count - 1 ? karartma : 0;
            }
            Yerlestir(_ekran);
        }

        public void Yerlestir(Ekran ekran)
        {
            _ekran = ekran;
            var a = ekran.Alan;
            foreach (var k in _katmanlar) k.Yerlestir(a.Left, a.Top, a.Genislik, a.Yukseklik);
        }

        /// <summary>Önce widget'lar ayrılmış olmalı: sahip pencere kapanınca ona bağlı pencereler de kapanır.</summary>
        public void Kapat()
        {
            foreach (var k in _katmanlar) k.Owner = null;
            foreach (var k in _katmanlar) k.Close();
            _katmanlar.Clear();
        }
    }
}
