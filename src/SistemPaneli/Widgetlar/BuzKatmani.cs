using System;
using System.Collections.Generic;
using System.Windows;
using SistemPaneli.Yerel;

namespace SistemPaneli.Widgetlar
{
    /// <summary>
    /// Arka plan ayarı: widget'ların olduğu monitörün tamamını kaplayan, tıklamaları geçiren buzlanma ve karartma.
    /// Buzlanma gücü üst üste konan bulanık katman sayısıdır (1 hafif, 2 orta, 3 güçlü); karartma en üst katmanın tonudur.
    /// Katmanlar ve widget'lar birbirine bağlı (sahipli) pencereler olduğu için sıra hep: masaüstü, katmanlar, widget'lar.
    /// </summary>
    internal sealed class BuzKatmani
    {
        public const int EnGucluBuzlanma = 3;

        readonly List<KatmanPenceresi> _katmanlar = new List<KatmanPenceresi>();
        Ekran _ekran;

        public string EkranAdi => _ekran.Ad;

        /// <summary>En üstteki katman; widget'lar buna bağlanır.</summary>
        public Window Ust => _katmanlar.Count > 0 ? _katmanlar[_katmanlar.Count - 1] : null;

        public BuzKatmani(Ekran ekran) => _ekran = ekran;

        /// <param name="buzlanma">0 = yok, 1–3 bulanık katman sayısı.</param>
        /// <param name="karartma">En üst katmanın koyu tonu (0–0,9).</param>
        public void Ayarla(int buzlanma, double karartma)
        {
            buzlanma = Math.Max(0, Math.Min(EnGucluBuzlanma, buzlanma));
            // Buzlanma yoksa yalnızca karartan tek bir katman
            int adet = Math.Max(1, buzlanma);
            var cam = buzlanma > 0 ? Win32.AccentState.Bulanik : Win32.AccentState.Kapali;
            if (_katmanlar.Count != adet || _katmanlar[0].Cam != cam)
            {
                Kapat();
                for (int i = 0; i < adet; i++)
                {
                    var k = new KatmanPenceresi(cam, false) { Owner = i == 0 ? null : _katmanlar[i - 1] };
                    _katmanlar.Add(k);
                    k.Show();
                }
                Yerlestir(_ekran);
            }
            for (int i = 0; i < _katmanlar.Count; i++)
                _katmanlar[i].Karartma = i == _katmanlar.Count - 1 ? karartma : 0;
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
