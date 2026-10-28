using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SistemPaneli.Arayuz
{
    /// <summary>
    /// Değer değişimlerinde ~0,9 sn'lik yumuşak geçiş (CSS'teki cubic-bezier(.2,.8,.2,1)).
    /// Widget pencereleri saydam olduğu için her kare görece pahalı; bu yüzden küçük değişimler
    /// (her saniye olan yük dalgalanmaları gibi) geçişsiz uygulanır, yalnızca belirgin sıçramalar canlandırılır.
    /// </summary>
    internal static class Canlandirma
    {
        public static readonly TimeSpan Sure = TimeSpan.FromSeconds(0.9);
        static readonly KeySpline Egri = Dondur(new KeySpline(0.2, 0.8, 0.2, 1));
        const int KareHizi = 24;

        /// <param name="esik">Bundan küçük değişimler geçişsiz uygulanır.</param>
        /// <returns>Geçiş başlatıldıysa true.</returns>
        public static bool Sayi(DependencyObject hedef, DependencyProperty ozellik, double deger, double esik)
        {
            double simdi = (double)hedef.GetValue(ozellik);
            if (Math.Abs(simdi - deger) < 1e-4) return false;
            var a = (IAnimatable)hedef;
            if (Math.Abs(simdi - deger) < esik)
            {
                a.BeginAnimation(ozellik, null);
                hedef.SetValue(ozellik, deger);
                return false;
            }
            var gecis = new DoubleAnimationUsingKeyFrames();
            gecis.KeyFrames.Add(new SplineDoubleKeyFrame(deger, KeyTime.FromTimeSpan(Sure), Egri));
            Timeline.SetDesiredFrameRate(gecis, KareHizi);
            gecis.Freeze();
            a.BeginAnimation(ozellik, gecis);
            return true;
        }

        public static void Renk(DependencyObject hedef, DependencyProperty ozellik, Color deger, bool canli)
        {
            if ((Color)hedef.GetValue(ozellik) == deger) return;
            var a = (IAnimatable)hedef;
            if (!canli)
            {
                a.BeginAnimation(ozellik, null);
                hedef.SetValue(ozellik, deger);
                return;
            }
            var gecis = new ColorAnimation(deger, new Duration(Sure));
            Timeline.SetDesiredFrameRate(gecis, KareHizi);
            gecis.Freeze();
            a.BeginAnimation(ozellik, gecis);
        }

        static KeySpline Dondur(KeySpline k)
        {
            k.Freeze();
            return k;
        }
    }
}
