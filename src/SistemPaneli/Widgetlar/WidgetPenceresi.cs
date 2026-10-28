using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using SistemPaneli.Arayuz;
using SistemPaneli.Cekirdek;
using SistemPaneli.Yerel;

namespace SistemPaneli.Widgetlar
{
    /// <summary>Her widget'ın içeriği bu arayüzü uygular; saniyede bir yeni verilerle çağrılır.</summary>
    internal interface IWidget
    {
        void Guncelle(Anlik anlik);
    }

    /// <summary>
    /// Masaüstünde duran, kenarlıksız, arkası bulanık widget penceresi.
    /// - Alt+Tab'da ve görev çubuğunda görünmez, tıklanınca odak çalmaz.
    /// - Her zaman diğer pencerelerin altında, duvar kâğıdının üstünde kalır.
    /// - Kilitliyken tıklamaları arkasına geçirir; düzenleme kipinde sürüklenebilir.
    /// </summary>
    internal sealed class WidgetPenceresi : Window
    {
        public const double KartGenisligi = 384; // 24rem

        // Bulanıklık katmanı pencerenin dikdörtgenini kaplıyor ve SetWindowRgn ile kırpılamıyor (katmanlı pencere).
        // Windows 11'in kendi köşe yuvarlaması bulanıklığı da kırpıyor ama yarıçapı sabit 8 px; kart da ona uyar.
        // Windows 10'da yuvarlama yok, kart köşeli çizilir ki köşelerden bulanıklık taşmasın.
        static readonly bool Windows11 = Environment.OSVersion.Version.Build >= 22000;
        public static readonly double KoseYaricapi = Windows11 ? 8 : 0;

        public string Kimlik { get; }
        public IWidget Icerik { get; }
        public bool Kartli { get; }
        public IntPtr Tutamac { get; private set; }

        /// <summary>Kullanıcı sürükleyip bıraktığında.</summary>
        public event Action<WidgetPenceresi> Tasindi;
        /// <summary>Monitör eklendi, çıkarıldı ya da çözünürlük değişti.</summary>
        public event Action EkranlarDegisti;

        readonly Grid _kok;
        readonly Rectangle _duzenlemeCercevesi;
        bool _duzenleme, _surukleniyor;
        (int X, int Y)? _hedef;

        public WidgetPenceresi(string kimlik, IWidget icerik, bool kartli)
        {
            Kimlik = kimlik;
            Icerik = icerik;
            Kartli = kartli;

            Title = "Sistem Paneli";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            ShowActivated = false;
            SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = WindowStartupLocation.Manual;
            UseLayoutRounding = true;
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.Grayscale);

            _kok = new Grid();
            var icerikOgesi = (FrameworkElement)icerik;
            if (kartli)
            {
                _kok.Width = KartGenisligi;
                _kok.Children.Add(new Border
                {
                    CornerRadius = new CornerRadius(KoseYaricapi),
                    BorderThickness = new Thickness(1),
                    BorderBrush = Tema.KartCizgi,
                    Background = Tema.KartZemin,
                });
                // Üst kenarda vurgu renginde ince parıltı
                _kok.Children.Add(new Rectangle
                {
                    Height = 1,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(20.8, 0, 20.8, 0),
                    Fill = Tema.UstParilti,
                });
                icerikOgesi.Margin = new Thickness(20.8, 18.4, 20.8, 19.2);
            }
            _kok.Children.Add(icerikOgesi);

            _duzenlemeCercevesi = new Rectangle
            {
                RadiusX = kartli ? KoseYaricapi : 10,
                RadiusY = kartli ? KoseYaricapi : 10,
                Stroke = Tema.Vurgu,
                StrokeThickness = 1.5,
                StrokeDashArray = new DoubleCollection { 4, 3 },
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false,
            };
            _kok.Children.Add(_duzenlemeCercevesi);
            Content = _kok;

            DpiChanged += (s, e) =>
            {
                // Ölçeği farklı monitöre taşınınca Windows pencereyi kaydırabiliyor; istenen konuma geri koy
                if (_hedef is (int x, int y) && !_surukleniyor)
                    Dispatcher.BeginInvoke(new Action(() => KonumaGit(x, y)));
            };
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            Tutamac = new WindowInteropHelper(this).Handle;
            Win32.ExStilDegistir(Tutamac, Win32.WS_EX_TOOLWINDOW | Win32.WS_EX_NOACTIVATE, Win32.WS_EX_APPWINDOW);
            if (Kartli && Windows11)
            {
                int yuvarlak = Win32.DWMWCP_ROUND, cizgiYok = Win32.DWMWA_COLOR_NONE;
                Win32.DwmSetWindowAttribute(Tutamac, Win32.DWMWA_WINDOW_CORNER_PREFERENCE, ref yuvarlak, sizeof(int));
                Win32.DwmSetWindowAttribute(Tutamac, Win32.DWMWA_BORDER_COLOR, ref cizgiYok, sizeof(int));
            }
            HwndSource.FromHwnd(Tutamac).AddHook(PencereIslemi);
            CamUygula();
            DuzenlemeUygula();
        }

        IntPtr PencereIslemi(IntPtr hwnd, int mesaj, IntPtr wParam, IntPtr lParam, ref bool islendi)
        {
            switch (mesaj)
            {
                case Win32.WM_WINDOWPOSCHANGING:
                    // Z sırası değişecekse pencereyi en alta (masaüstünün hemen üstüne) zorla
                    var konum = Marshal.PtrToStructure<Win32.WINDOWPOS>(lParam);
                    if ((konum.flags & Win32.SWP_NOZORDER) == 0)
                    {
                        konum.hwndInsertAfter = Win32.HWND_BOTTOM;
                        Marshal.StructureToPtr(konum, lParam, false);
                    }
                    break;
                case Win32.WM_MOUSEACTIVATE:
                    islendi = true;
                    return new IntPtr(Win32.MA_NOACTIVATE);
                case Win32.WM_DISPLAYCHANGE:
                    EkranlarDegisti?.Invoke();
                    break;
            }
            return IntPtr.Zero;
        }

        public void CamUygula()
        {
            if (Tutamac == IntPtr.Zero || !Kartli) return;
            // Renk tonu WPF tarafında (yuvarlak, kenar yumuşatmalı) verildiği için akriliğin kendi tonu neredeyse şeffaf
            Win32.CamAyarla(Tutamac, Tema.Cam ? Win32.AccentState.Bulanik : Win32.AccentState.Kapali, 0x01000000);
        }

        public bool Duzenleme
        {
            get => _duzenleme;
            set
            {
                _duzenleme = value;
                DuzenlemeUygula();
            }
        }

        void DuzenlemeUygula()
        {
            _duzenlemeCercevesi.Visibility = _duzenleme ? Visibility.Visible : Visibility.Collapsed;
            // Düzenlemede saydam yerler de tıklanabilsin; kilitliyken pencere tıklamaları tamamen geçirir
            _kok.Background = _duzenleme ? new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)) : null;
            Cursor = _duzenleme ? Cursors.SizeAll : null;
            if (Tutamac != IntPtr.Zero)
            {
                if (_duzenleme) Win32.ExStilDegistir(Tutamac, 0, Win32.WS_EX_TRANSPARENT);
                else Win32.ExStilDegistir(Tutamac, Win32.WS_EX_TRANSPARENT, 0);
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (!_duzenleme) return;
            _surukleniyor = true;
            try
            {
                DragMove();
            }
            catch (InvalidOperationException)
            {
                return;
            }
            finally
            {
                _surukleniyor = false;
            }
            var r = Dikdortgen;
            _hedef = (r.Left, r.Top);
            Tasindi?.Invoke(this);
        }

        // ---------- Fiziksel piksel cinsinden konum ----------

        public Win32.RECT Dikdortgen
        {
            get
            {
                Win32.GetWindowRect(Tutamac, out var r);
                return r;
            }
        }

        public void KonumaGit(int x, int y)
        {
            _hedef = (x, y);
            Win32.SetWindowPos(Tutamac, IntPtr.Zero, x, y, 0, 0, Win32.SWP_NOSIZE | Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);
        }

        /// <summary>Pencerenin görünen kısmının kenarlarına olan boşluk (fiziksel piksel); saatte gölge payı.</summary>
        public (int Left, int Top, int Right, int Bottom) IcKenar(double olcek)
        {
            if (Kartli) return (0, 0, 0, 0);
            int b = (int)Math.Round(SaatWidget.Bosluk * olcek);
            return (b, b, b, b);
        }
    }
}
