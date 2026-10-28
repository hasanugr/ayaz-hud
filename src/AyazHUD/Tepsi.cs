using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using AyazHUD.Arayuz;
using AyazHUD.Widgetlar;
using AyazHUD.Yerel;

namespace AyazHUD
{
    /// <summary>Bildirim alanı simgesi ve menüsü.</summary>
    internal sealed class Tepsi : IDisposable
    {
        readonly App _uygulama;
        readonly WidgetYoneticisi _widgetlar;
        readonly NotifyIcon _simge;
        readonly ToolStripMenuItem _duzenle, _widgetMenusu, _ekranMenusu, _baslangic;

        public Tepsi(App uygulama, WidgetYoneticisi widgetlar)
        {
            _uygulama = uygulama;
            _widgetlar = widgetlar;

            var menu = new ContextMenuStrip { Renderer = new KoyuCizici(), ShowImageMargin = false, ShowCheckMargin = true };
            var baslik = new ToolStripMenuItem($"Ayaz HUD {App.Surum}") { Enabled = false };
            baslik.Font = new Font(baslik.Font, System.Drawing.FontStyle.Bold);
            menu.Items.Add(baslik);
            menu.Items.Add(new ToolStripSeparator());

            _duzenle = new ToolStripMenuItem(Dil.M("t.duzenle"), null, (s, e) => _uygulama.DuzenlemeDegistir());
            menu.Items.Add(_duzenle);
            _widgetMenusu = new ToolStripMenuItem(Dil.M("t.widgetlar"));
            ((ToolStripDropDownMenu)_widgetMenusu.DropDown).ShowImageMargin = false;
            ((ToolStripDropDownMenu)_widgetMenusu.DropDown).ShowCheckMargin = true;
            foreach (var t in widgetlar.Tanimlar)
            {
                string kimlik = t.Kimlik;
                var oge = new ToolStripMenuItem(t.Ad) { Tag = kimlik };
                oge.Click += (s, e) => _widgetlar.Ac(kimlik, !_widgetlar.AcikMi(kimlik));
                _widgetMenusu.DropDownItems.Add(oge);
            }
            menu.Items.Add(_widgetMenusu);
            // Monitörler değişebildiği için menü her açılışta doldurulur
            _ekranMenusu = new ToolStripMenuItem(Dil.M("t.ekran"));
            ((ToolStripDropDownMenu)_ekranMenusu.DropDown).ShowImageMargin = false;
            ((ToolStripDropDownMenu)_ekranMenusu.DropDown).ShowCheckMargin = true;
            menu.Items.Add(_ekranMenusu);
            menu.Items.Add(Dil.M("t.sifirla"), null, (s, e) => _widgetlar.KonumlariSifirla());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Dil.M("t.ayarlar"), null, (s, e) => _uygulama.AyarlariAc());
            _baslangic = new ToolStripMenuItem(Dil.M("t.baslangic"), null, (s, e) => _uygulama.BaslangicDegistir());
            menu.Items.Add(_baslangic);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Dil.M("t.cikis"), null, (s, e) => _uygulama.Cikis());

            menu.Opening += (s, e) =>
            {
                _duzenle.Checked = _widgetlar.Duzenleme;
                foreach (ToolStripMenuItem oge in _widgetMenusu.DropDownItems) oge.Checked = _widgetlar.AcikMi((string)oge.Tag);
                var ekranlar = Ekran.Hepsi();
                var ana = _widgetlar.AnaEkran(ekranlar);
                _ekranMenusu.DropDownItems.Clear();
                foreach (var ekran in ekranlar)
                {
                    string ad = ekran.Ad;
                    string model = ekran.Model ?? string.Format(Dil.M("a.ekranAdi"), ekran.Numara);
                    _ekranMenusu.DropDownItems.Add(new ToolStripMenuItem($"{ekran.Numara} · {model} ({ekran.Alan.Genislik} × {ekran.Alan.Yukseklik})", null,
                        (o, a) => _widgetlar.EkranSec(ad)) { Checked = ad == ana?.Ad });
                }
                _ekranMenusu.Visible = ekranlar.Count > 1;
                bool? baslangic = _uygulama.BaslangictaMi();
                _baslangic.Enabled = baslangic != null;
                _baslangic.Checked = baslangic == true;
            };

            _simge = new NotifyIcon
            {
                Icon = Simge(),
                Text = "Ayaz HUD",
                ContextMenuStrip = menu,
                Visible = true,
            };
            _simge.MouseDoubleClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left) _uygulama.AyarlariAc();
            };
        }

        public static Icon Simge()
        {
            using (var akis = Assembly.GetExecutingAssembly().GetManifestResourceStream("simge.ico"))
                return new Icon(akis, SystemInformation.SmallIconSize);
        }

        public void Bildir(string baslik, string metin) => _simge.ShowBalloonTip(8000, baslik, metin, ToolTipIcon.Info);

        /// <summary>Simgenin üstüne gelince görünen kısa özet (en fazla 63 karakter).</summary>
        public void IpucuYaz(string metin)
        {
            if (metin.Length > 63) metin = metin.Substring(0, 63);
            if (_simge.Text != metin) _simge.Text = metin;
        }

        public void Dispose()
        {
            _simge.Visible = false;
            _simge.Dispose();
        }

        // ---------- Koyu menü ----------

        sealed class KoyuCizici : ToolStripProfessionalRenderer
        {
            public KoyuCizici() : base(new KoyuRenkler()) => RoundedEdges = false;

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = e.Item.Enabled ? Color.FromArgb(240, 242, 247) : Color.FromArgb(150, 156, 170);
                base.OnRenderItemText(e);
            }

            protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
            {
                e.ArrowColor = Color.FromArgb(220, 224, 232);
                base.OnRenderArrow(e);
            }

            protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
            {
                var r = e.ImageRectangle;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var kalem = new Pen(Color.FromArgb(125, 211, 252), 2f))
                    e.Graphics.DrawLines(kalem, new[]
                    {
                        new PointF(r.Left + r.Width * 0.2f, r.Top + r.Height * 0.52f),
                        new PointF(r.Left + r.Width * 0.42f, r.Top + r.Height * 0.74f),
                        new PointF(r.Left + r.Width * 0.82f, r.Top + r.Height * 0.3f),
                    });
            }
        }

        sealed class KoyuRenkler : ProfessionalColorTable
        {
            static readonly Color Zemin = Color.FromArgb(28, 30, 38);
            static readonly Color Secili = Color.FromArgb(48, 54, 70);
            static readonly Color Cizgi = Color.FromArgb(64, 68, 82);

            public override Color ToolStripDropDownBackground => Zemin;
            public override Color ImageMarginGradientBegin => Zemin;
            public override Color ImageMarginGradientMiddle => Zemin;
            public override Color ImageMarginGradientEnd => Zemin;
            public override Color MenuBorder => Cizgi;
            public override Color MenuItemBorder => Secili;
            public override Color MenuItemSelected => Secili;
            public override Color MenuItemSelectedGradientBegin => Secili;
            public override Color MenuItemSelectedGradientEnd => Secili;
            public override Color MenuItemPressedGradientBegin => Secili;
            public override Color MenuItemPressedGradientEnd => Secili;
            public override Color CheckBackground => Zemin;
            public override Color CheckSelectedBackground => Secili;
            public override Color CheckPressedBackground => Secili;
            public override Color SeparatorDark => Cizgi;
            public override Color SeparatorLight => Zemin;
        }
    }
}
