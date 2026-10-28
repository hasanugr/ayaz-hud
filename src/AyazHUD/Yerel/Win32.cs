using System;
using System.Runtime.InteropServices;

namespace AyazHUD.Yerel
{
    /// <summary>Widget pencereleri, monitörler ve iş nesnesi için kullanılan Windows API çağrıları.</summary>
    internal static class Win32
    {
        // ---------- Pencere stilleri ve mesajlar ----------

        public const int GWL_EXSTYLE = -20;
        public const long WS_EX_TRANSPARENT = 0x20;
        public const long WS_EX_TOOLWINDOW = 0x80;
        public const long WS_EX_APPWINDOW = 0x40000;
        public const long WS_EX_NOACTIVATE = 0x08000000;

        public const int WM_WINDOWPOSCHANGING = 0x0046;
        public const int WM_MOUSEACTIVATE = 0x0021;
        public const int WM_DISPLAYCHANGE = 0x007E;
        public const int MA_NOACTIVATE = 3;

        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_FRAMECHANGED = 0x0020;

        public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x, y, cx, cy;
            public uint flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
            public int Genislik => Right - Left;
            public int Yukseklik => Bottom - Top;
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        public static void ExStilDegistir(IntPtr hwnd, long ekle, long cikar)
        {
            long stil = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
            long yeni = (stil | ekle) & ~cikar;
            if (yeni != stil) SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(yeni));
        }

        // ---------- Bulanık cam (belgelenmemiş SetWindowCompositionAttribute) ----------
        // DWM'nin resmi arka plan türleri pencere etkin değilken düz renge dönüyor; widget'lar hiç
        // etkinleşmediği için bu eski yol kullanılıyor. Windows 10 1803+ ve 11'de çalışıyor.

        public enum AccentState
        {
            Kapali = 0,
            Bulanik = 3,
            Akrilik = 4,
        }

        [StructLayout(LayoutKind.Sequential)]
        struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public uint GradientColor; // ABGR
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct WindowCompositionAttributeData
        {
            public int Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        const int WCA_ACCENT_POLICY = 19;

        [DllImport("user32.dll")]
        static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        public static void CamAyarla(IntPtr hwnd, AccentState durum, uint renkAbgr)
        {
            // AccentFlags = 2 akrilikte tonun saydamlığını yok sayıp pencereyi opak yapıyor; 0 kullanılır
            var politika = new AccentPolicy
            {
                AccentState = durum,
                AccentFlags = 0,
                GradientColor = renkAbgr,
            };
            int boyut = Marshal.SizeOf(politika);
            IntPtr bellek = Marshal.AllocHGlobal(boyut);
            try
            {
                Marshal.StructureToPtr(politika, bellek, false);
                var veri = new WindowCompositionAttributeData { Attribute = WCA_ACCENT_POLICY, Data = bellek, SizeOfData = boyut };
                SetWindowCompositionAttribute(hwnd, ref veri);
            }
            finally
            {
                Marshal.FreeHGlobal(bellek);
            }
        }

        // ---------- DWM ----------

        public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        public const int DWMWA_BORDER_COLOR = 34;
        public const int DWMWCP_DONOTROUND = 1;
        public const int DWMWCP_ROUND = 2;
        public const int DWMWA_COLOR_NONE = unchecked((int)0xFFFFFFFE);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        // ---------- DWM küçük resmi (bir pencerenin canlı kopyası) ----------

        public const int DWM_TNP_RECTDESTINATION = 0x1;
        public const int DWM_TNP_RECTSOURCE = 0x2;
        public const int DWM_TNP_OPACITY = 0x4;
        public const int DWM_TNP_VISIBLE = 0x8;
        public const int DWM_TNP_SOURCECLIENTAREAONLY = 0x10;

        [StructLayout(LayoutKind.Sequential)]
        public struct DWM_THUMBNAIL_PROPERTIES
        {
            public int dwFlags;
            public RECT rcDestination;
            public RECT rcSource;
            public byte opacity;
            public int fVisible;
            public int fSourceClientAreaOnly;
        }

        [DllImport("dwmapi.dll")]
        public static extern int DwmRegisterThumbnail(IntPtr destination, IntPtr source, out IntPtr thumbnail);

        [DllImport("dwmapi.dll")]
        public static extern int DwmUnregisterThumbnail(IntPtr thumbnail);

        [DllImport("dwmapi.dll")]
        public static extern int DwmUpdateThumbnailProperties(IntPtr thumbnail, ref DWM_THUMBNAIL_PROPERTIES properties);

        // ---------- Pencere listesi ----------

        public delegate bool WindowEnumProc(IntPtr hwnd, IntPtr data);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(WindowEnumProc proc, IntPtr data);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int GetClassName(IntPtr hwnd, System.Text.StringBuilder name, int size);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hwnd);

        public static string SinifAdi(IntPtr hwnd)
        {
            var ad = new System.Text.StringBuilder(64);
            GetClassName(hwnd, ad, ad.Capacity);
            return ad.ToString();
        }

        // ---------- Monitörler ----------

        public const uint MONITOR_DEFAULTTONEAREST = 2;
        public const uint MONITORINFOF_PRIMARY = 1;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct MONITORINFOEX
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, ref RECT rect, IntPtr data);

        [DllImport("user32.dll")]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc proc, IntPtr data);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX info);

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

        [DllImport("shcore.dll")]
        public static extern int GetDpiForMonitor(IntPtr hMonitor, int dpiType, out uint dpiX, out uint dpiY);

        // ---------- Monitör adları (Windows'un Ekran ayarlarında görünen "AOC 27G1G4" gibi) ----------

        public const uint QDC_ONLY_ACTIVE_PATHS = 2;
        public const uint DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1;
        public const uint DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME = 2;

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_PATH_INFO
        {
            public LUID sourceAdapterId;
            public uint sourceId;
            public uint sourceModeInfoIdx;
            public uint sourceStatusFlags;
            public LUID targetAdapterId;
            public uint targetId;
            public uint targetModeInfoIdx;
            public uint outputTechnology;
            public uint rotation;
            public uint scaling;
            public uint refreshRateNumerator;
            public uint refreshRateDenominator;
            public uint scanLineOrdering;
            public int targetAvailable;
            public uint targetStatusFlags;
            public uint flags;
        }

        // İçeriği kullanılmıyor; QueryDisplayConfig yalnızca yer istiyor
        [StructLayout(LayoutKind.Sequential, Size = 64)]
        public struct DISPLAYCONFIG_MODE_INFO
        {
            public uint infoType;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_DEVICE_INFO_HEADER
        {
            public uint type;
            public uint size;
            public LUID adapterId;
            public uint id;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DISPLAYCONFIG_SOURCE_DEVICE_NAME
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string viewGdiDeviceName;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DISPLAYCONFIG_TARGET_DEVICE_NAME
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            public uint flags;
            public uint outputTechnology;
            public ushort edidManufactureId;
            public ushort edidProductCodeId;
            public uint connectorInstance;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string monitorFriendlyDeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string monitorDevicePath;
        }

        [DllImport("user32.dll")]
        public static extern int GetDisplayConfigBufferSizes(uint flags, out uint pathCount, out uint modeCount);

        [DllImport("user32.dll")]
        public static extern int QueryDisplayConfig(uint flags, ref uint pathCount, [Out] DISPLAYCONFIG_PATH_INFO[] paths,
            ref uint modeCount, [Out] DISPLAYCONFIG_MODE_INFO[] modes, IntPtr topologyId);

        [DllImport("user32.dll")]
        public static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_SOURCE_DEVICE_NAME request);

        [DllImport("user32.dll")]
        public static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_TARGET_DEVICE_NAME request);

        /// <summary>GDI aygıt adından (\\.\DISPLAY1) monitörün adına; okunamayanlar listede yok.</summary>
        public static System.Collections.Generic.Dictionary<string, string> MonitorAdlari()
        {
            var sonuc = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out uint yolSayisi, out uint kipSayisi) != 0) return sonuc;
                var yollar = new DISPLAYCONFIG_PATH_INFO[yolSayisi];
                var kipler = new DISPLAYCONFIG_MODE_INFO[kipSayisi];
                if (QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref yolSayisi, yollar, ref kipSayisi, kipler, IntPtr.Zero) != 0) return sonuc;
                for (int i = 0; i < yolSayisi; i++)
                {
                    var kaynak = new DISPLAYCONFIG_SOURCE_DEVICE_NAME();
                    kaynak.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
                    kaynak.header.size = (uint)Marshal.SizeOf(kaynak);
                    kaynak.header.adapterId = yollar[i].sourceAdapterId;
                    kaynak.header.id = yollar[i].sourceId;
                    var hedef = new DISPLAYCONFIG_TARGET_DEVICE_NAME();
                    hedef.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;
                    hedef.header.size = (uint)Marshal.SizeOf(hedef);
                    hedef.header.adapterId = yollar[i].targetAdapterId;
                    hedef.header.id = yollar[i].targetId;
                    if (DisplayConfigGetDeviceInfo(ref kaynak) == 0 && DisplayConfigGetDeviceInfo(ref hedef) == 0
                        && !string.IsNullOrWhiteSpace(hedef.monitorFriendlyDeviceName))
                        sonuc[kaynak.viewGdiDeviceName] = hedef.monitorFriendlyDeviceName.Trim();
                }
            }
            catch (Exception)
            {
                // Ad yalnızca gösterim için; okunamazsa numara yeter
            }
            return sonuc;
        }

        // ---------- İş nesnesi (PresentMon uygulamayla birlikte kapansın) ----------

        public const int JobObjectExtendedLimitInformation = 9;
        public const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;

        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IO_COUNTERS
        {
            public ulong ReadOperationCount, WriteOperationCount, OtherOperationCount;
            public ulong ReadTransferCount, WriteTransferCount, OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateJobObject(IntPtr attributes, string name);

        [DllImport("kernel32.dll")]
        public static extern bool SetInformationJobObject(IntPtr job, int infoClass, ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION info, int length);

        [DllImport("kernel32.dll")]
        public static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);
    }
}
