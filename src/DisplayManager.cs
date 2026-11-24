using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace ScreenProfileSwitcher
{
    public class ResolutionOption
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public List<int> RefreshRates { get; set; }

        public ResolutionOption()
        {
            RefreshRates = new List<int>();
        }

        public string DisplayText
        {
            get
            {
                string tag = GetFriendlyTag(Width, Height);
                return string.IsNullOrEmpty(tag)
                    ? string.Format("{0} × {1}", Width, Height)
                    : string.Format("{0} × {1} ({2})", Width, Height, tag);
            }
        }

        public static string GetFriendlyTag(int w, int h)
        {
            if (w == 3840 && h == 2160) return "4K / UHD";
            if (w == 2560 && h == 1440) return "2K / QHD";
            if (w == 2560 && h == 1080) return "UW-FHD";
            if (w == 3440 && h == 1440) return "UW-QHD";
            if (w == 1920 && h == 1080) return "Full HD";
            if (w == 1920 && h == 1200) return "WUXGA";
            if (w == 1600 && h == 900) return "HD+";
            if (w == 1280 && h == 720) return "HD";
            return string.Empty;
        }
    }

    public class DisplayInfo
    {
        public string DeviceName { get; set; }
        public string FriendlyName { get; set; }
        public bool IsPrimary { get; set; }
        public int CurrentWidth { get; set; }
        public int CurrentHeight { get; set; }
        public int CurrentRefreshRate { get; set; }
        public int CurrentScalePercent { get; set; }
        public int RecommendedScalePercent { get; set; }
        public List<int> SupportedScales { get; set; }
        public List<ResolutionOption> SupportedResolutions { get; set; }

        public DisplayInfo()
        {
            SupportedScales = new List<int>();
            SupportedResolutions = new List<ResolutionOption>();
        }

        public string DisplayLabel
        {
            get
            {
                string shortDev = DeviceName.Replace(@"\\.\", "");
                string mon = string.IsNullOrWhiteSpace(FriendlyName) ? shortDev : string.Format("{0} ({1})", FriendlyName, shortDev);
                return IsPrimary ? mon + " - Chính" : mon;
            }
        }

        public string CurrentStatusText
        {
            get
            {
                return string.Format("Hiện tại: {0} × {1}  •  Scale {2}%  •  {3} Hz",
                    CurrentWidth, CurrentHeight, CurrentScalePercent, CurrentRefreshRate);
            }
        }
    }

    public static class DisplayManager
    {
        private static readonly int[] DpiSteps = { 100, 125, 150, 175, 200, 225, 250, 300, 350, 400, 450, 500 };

        private const int ENUM_CURRENT_SETTINGS = -1;
        private const int DISPLAY_DEVICE_ACTIVE = 0x00000001;
        private const int DISPLAY_DEVICE_PRIMARY_DEVICE = 0x00000004;

        private const int CDS_UPDATEREGISTRY = 0x00000001;
        private const int DISP_CHANGE_SUCCESSFUL = 0;
        private const int DISP_CHANGE_RESTART = 1;

        private const uint DM_BITSPERPEL = 0x00040000;
        private const uint DM_PELSWIDTH = 0x00080000;
        private const uint DM_PELSHEIGHT = 0x00100000;
        private const uint DM_DISPLAYFREQUENCY = 0x00400000;

        private const uint QDC_ONLY_ACTIVE_PATHS = 0x00000002;
        private const int DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1;
        private const int DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME = 2;
        private const int DISPLAYCONFIG_DEVICE_INFO_GET_DPI_SCALE = -3;
        private const int DISPLAYCONFIG_DEVICE_INFO_SET_DPI_SCALE = -4;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;
            public ushort dmSpecVersion;
            public ushort dmDriverVersion;
            public ushort dmSize;
            public ushort dmDriverExtra;
            public uint dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public uint dmDisplayOrientation;
            public uint dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;
            public ushort dmLogPixels;
            public uint dmBitsPerPel;
            public uint dmPelsWidth;
            public uint dmPelsHeight;
            public uint dmDisplayFlags;
            public uint dmDisplayFrequency;
            public uint dmICMMethod;
            public uint dmICMIntent;
            public uint dmMediaType;
            public uint dmDitherType;
            public uint dmReserved1;
            public uint dmReserved2;
            public uint dmPanningWidth;
            public uint dmPanningHeight;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_RATIONAL
        {
            public uint Numerator;
            public uint Denominator;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_PATH_SOURCE_INFO
        {
            public LUID adapterId;
            public uint id;
            public uint modeInfoIdx;
            public uint statusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_PATH_TARGET_INFO
        {
            public LUID adapterId;
            public uint id;
            public uint modeInfoIdx;
            public uint outputTechnology;
            public uint rotation;
            public uint scaling;
            public DISPLAYCONFIG_RATIONAL refreshRate;
            public uint scanLineOrdering;
            [MarshalAs(UnmanagedType.Bool)]
            public bool targetAvailable;
            public uint statusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_PATH_INFO
        {
            public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
            public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
            public uint flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_MODE_INFO
        {
            public uint infoType;
            public uint id;
            public LUID adapterId;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)]
            public byte[] data;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_DEVICE_INFO_HEADER
        {
            public int type;
            public uint size;
            public LUID adapterId;
            public uint id;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DISPLAYCONFIG_SOURCE_DEVICE_NAME
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string viewGdiDeviceName;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DISPLAYCONFIG_TARGET_DEVICE_NAME
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

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_SOURCE_DPI_SCALE_GET
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            public int minScaleRel;
            public int curScaleRel;
            public int maxScaleRel;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_SOURCE_DPI_SCALE_SET
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            public int scaleRel;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int ChangeDisplaySettingsEx(string lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd, uint dwflags, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetDisplayConfigBufferSizes(uint flags, out uint numPathArrayElements, out uint numModeInfoArrayElements);

        [DllImport("user32.dll")]
        private static extern int QueryDisplayConfig(
            uint flags,
            ref uint numPathArrayElements,
            [Out] DISPLAYCONFIG_PATH_INFO[] pathArray,
            ref uint numModeInfoArrayElements,
            [Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray,
            IntPtr currentTopologyId);

        [DllImport("user32.dll")]
        private static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_SOURCE_DEVICE_NAME requestPacket);

        [DllImport("user32.dll")]
        private static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_TARGET_DEVICE_NAME requestPacket);

        [DllImport("user32.dll")]
        private static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_SOURCE_DPI_SCALE_GET requestPacket);

        [DllImport("user32.dll")]
        private static extern int DisplayConfigSetDeviceInfo(ref DISPLAYCONFIG_SOURCE_DPI_SCALE_SET setPacket);

        private class DpiPathState
        {
            public LUID AdapterId;
            public uint SourceId;
            public string MonitorFriendlyName;
            public int CurrentScalePercent;
            public int RecommendedScalePercent;
            public int MinScaleRel;
            public int CurScaleRel;
            public int MaxScaleRel;
            public List<int> SupportedScales = new List<int>();
        }

        public static List<DisplayInfo> GetActiveDisplays()
        {
            var result = new List<DisplayInfo>();
            var dpiMap = QueryDpiStatesByGdiName();

            var dd = new DISPLAY_DEVICE();
            dd.cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE));

            for (uint devNum = 0; EnumDisplayDevices(null, devNum, ref dd, 0); devNum++)
            {
                if ((dd.StateFlags & DISPLAY_DEVICE_ACTIVE) == 0)
                {
                    dd.cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE));
                    continue;
                }

                var currentDm = CreateDevMode();
                if (!EnumDisplaySettings(dd.DeviceName, ENUM_CURRENT_SETTINGS, ref currentDm))
                {
                    dd.cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE));
                    continue;
                }

                var info = new DisplayInfo
                {
                    DeviceName = dd.DeviceName,
                    IsPrimary = (dd.StateFlags & DISPLAY_DEVICE_PRIMARY_DEVICE) != 0,
                    CurrentWidth = (int)currentDm.dmPelsWidth,
                    CurrentHeight = (int)currentDm.dmPelsHeight,
                    CurrentRefreshRate = (int)currentDm.dmDisplayFrequency
                };

                // Try getting friendly monitor name from EnumDisplayDevices child if DisplayConfig doesn't have one
                var monDd = new DISPLAY_DEVICE();
                monDd.cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE));
                string fallbackMonitorName = EnumDisplayDevices(dd.DeviceName, 0, ref monDd, 0) ? monDd.DeviceString : string.Empty;

                DpiPathState dpiState;
                if (dpiMap.TryGetValue(dd.DeviceName, out dpiState))
                {
                    info.FriendlyName = !string.IsNullOrWhiteSpace(dpiState.MonitorFriendlyName)
                        ? dpiState.MonitorFriendlyName
                        : fallbackMonitorName;
                    info.CurrentScalePercent = dpiState.CurrentScalePercent;
                    info.RecommendedScalePercent = dpiState.RecommendedScalePercent;
                    info.SupportedScales = dpiState.SupportedScales;
                }
                else
                {
                    info.FriendlyName = fallbackMonitorName;
                    info.CurrentScalePercent = 100;
                    info.RecommendedScalePercent = 100;
                    info.SupportedScales = new List<int> { 100, 125, 150, 175, 200 };
                }

                info.SupportedResolutions = GetSupportedResolutions(dd.DeviceName, currentDm.dmDisplayOrientation);
                result.Add(info);

                dd = new DISPLAY_DEVICE();
                dd.cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE));
            }

            // Primary display first
            return result.OrderByDescending(d => d.IsPrimary).ThenBy(d => d.DeviceName).ToList();
        }

        public static bool ApplyProfile(ScreenProfile profile, out string message)
        {
            if (profile == null)
            {
                message = "Profile không hợp lệ.";
                return false;
            }

            var displays = GetActiveDisplays();
            if (displays.Count == 0)
            {
                message = "Không tìm thấy màn hình đang hoạt động.";
                return false;
            }

            var targetDisplay = displays.FirstOrDefault(d =>
                string.Equals(d.DeviceName, profile.DeviceName, StringComparison.OrdinalIgnoreCase))
                ?? displays.FirstOrDefault(d => d.IsPrimary)
                ?? displays[0];

            string deviceName = targetDisplay.DeviceName;
            bool resolutionChanged = false;

            // Step 1: Change resolution / refresh rate if needed
            var currentDm = CreateDevMode();
            if (!EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref currentDm))
            {
                message = "Không thể đọc thông số hiện tại của màn hình " + deviceName;
                return false;
            }

            int desiredRefresh = profile.RefreshRate;
            if (desiredRefresh <= 0)
            {
                var matchingRes = targetDisplay.SupportedResolutions
                    .FirstOrDefault(r => r.Width == profile.Width && r.Height == profile.Height);
                if (matchingRes != null && matchingRes.RefreshRates.Count > 0)
                {
                    desiredRefresh = matchingRes.RefreshRates.Max();
                }
            }

            bool needsResChange = (int)currentDm.dmPelsWidth != profile.Width
                || (int)currentDm.dmPelsHeight != profile.Height
                || (desiredRefresh > 0 && (int)currentDm.dmDisplayFrequency != desiredRefresh);

            if (needsResChange)
            {
                DEVMODE bestMode;
                if (!TryFindMode(deviceName, profile.Width, profile.Height, desiredRefresh, currentDm.dmDisplayOrientation, out bestMode))
                {
                    message = string.Format("Màn hình {0} không hỗ trợ độ phân giải {1} × {2}.",
                        deviceName.Replace(@"\\.\", ""), profile.Width, profile.Height);
                    return false;
                }

                // Preserve desktop coordinates and required flags
                bestMode.dmPositionX = currentDm.dmPositionX;
                bestMode.dmPositionY = currentDm.dmPositionY;
                bestMode.dmFields |= DM_PELSWIDTH | DM_PELSHEIGHT | DM_BITSPERPEL | DM_DISPLAYFREQUENCY;

                int res = ChangeDisplaySettingsEx(deviceName, ref bestMode, IntPtr.Zero, CDS_UPDATEREGISTRY, IntPtr.Zero);
                if (res == DISP_CHANGE_RESTART)
                {
                    message = "Hệ thống yêu cầu khởi động lại để áp dụng độ phân giải mới.";
                    return false;
                }
                if (res != DISP_CHANGE_SUCCESSFUL)
                {
                    message = string.Format("Không thể đổi độ phân giải (mã lỗi Win32: {0}).", res);
                    return false;
                }

                resolutionChanged = true;
                // Allow Windows Display Manager to recalculate DPI scale bounds for the new resolution
                Thread.Sleep(200);
            }

            // Step 2: Apply DPI scale
            string dpiError;
            bool dpiApplied = ApplyDpiScale(deviceName, profile.ScalePercent, out dpiError);
            if (!dpiApplied)
            {
                message = resolutionChanged
                    ? string.Format("Đã đổi độ phân giải {0}×{1}, nhưng lỗi đổi Scale: {2}", profile.Width, profile.Height, dpiError)
                    : string.Format("Lỗi đổi Scale: {0}", dpiError);
                return false;
            }

            string refreshLabel = desiredRefresh > 0 ? desiredRefresh + " Hz" : "Auto";
            message = string.Format("Đã thiết lập \"{0}\": {1} × {2}, Scale {3}%, {4}",
                profile.Name, profile.Width, profile.Height, profile.ScalePercent, refreshLabel);
            return true;
        }

        private static bool ApplyDpiScale(string deviceName, int targetScalePercent, out string error)
        {
            var dpiMap = QueryDpiStatesByGdiName();
            DpiPathState state;
            if (!dpiMap.TryGetValue(deviceName, out state))
            {
                error = "Không tìm thấy thông tin DPI của màn hình " + deviceName;
                return false;
            }

            int targetIdx = Array.IndexOf(DpiSteps, targetScalePercent);
            if (targetIdx < 0)
            {
                // Pick nearest valid step
                targetIdx = 0;
                int bestDiff = int.MaxValue;
                for (int i = 0; i < DpiSteps.Length; i++)
                {
                    int diff = Math.Abs(DpiSteps[i] - targetScalePercent);
                    if (diff < bestDiff)
                    {
                        bestDiff = diff;
                        targetIdx = i;
                    }
                }
            }

            int recommendedIdx = Clamp(-state.MinScaleRel, 0, DpiSteps.Length - 1);
            int targetRel = Clamp(targetIdx - recommendedIdx, state.MinScaleRel, state.MaxScaleRel);

            if (targetRel == state.CurScaleRel)
            {
                error = null;
                return true;
            }

            var setPacket = new DISPLAYCONFIG_SOURCE_DPI_SCALE_SET();
            setPacket.header.type = DISPLAYCONFIG_DEVICE_INFO_SET_DPI_SCALE;
            setPacket.header.size = (uint)Marshal.SizeOf(typeof(DISPLAYCONFIG_SOURCE_DPI_SCALE_SET));
            setPacket.header.adapterId = state.AdapterId;
            setPacket.header.id = state.SourceId;
            setPacket.scaleRel = targetRel;

            int res = DisplayConfigSetDeviceInfo(ref setPacket);
            if (res != 0)
            {
                error = "DisplayConfigSetDeviceInfo thất bại (mã lỗi " + res + ").";
                return false;
            }

            error = null;
            return true;
        }

        private static bool TryFindMode(string deviceName, int width, int height, int refreshRate, uint orientation, out DEVMODE matchedMode)
        {
            matchedMode = CreateDevMode();
            var candidates = new List<DEVMODE>();

            var dm = CreateDevMode();
            int i = 0;
            while (EnumDisplaySettings(deviceName, i++, ref dm))
            {
                if (dm.dmPelsWidth == width && dm.dmPelsHeight == height && dm.dmBitsPerPel == 32 && dm.dmDisplayOrientation == orientation)
                {
                    candidates.Add(dm);
                }
                dm = CreateDevMode();
            }

            if (candidates.Count == 0)
            {
                // Fallback ignoring orientation if needed
                i = 0;
                dm = CreateDevMode();
                while (EnumDisplaySettings(deviceName, i++, ref dm))
                {
                    if (dm.dmPelsWidth == width && dm.dmPelsHeight == height && dm.dmBitsPerPel == 32)
                    {
                        candidates.Add(dm);
                    }
                    dm = CreateDevMode();
                }
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            if (refreshRate > 0)
            {
                var exact = candidates.Where(c => c.dmDisplayFrequency == refreshRate).ToList();
                if (exact.Count > 0)
                {
                    matchedMode = exact[0];
                    return true;
                }
            }

            matchedMode = candidates.OrderByDescending(c => c.dmDisplayFrequency).First();
            return true;
        }

        private static List<ResolutionOption> GetSupportedResolutions(string deviceName, uint currentOrientation)
        {
            var grouped = new Dictionary<string, ResolutionOption>();
            var dm = CreateDevMode();
            int i = 0;

            while (EnumDisplaySettings(deviceName, i++, ref dm))
            {
                if (dm.dmBitsPerPel == 32 && dm.dmDisplayOrientation == currentOrientation && dm.dmPelsWidth >= 800 && dm.dmPelsHeight >= 600)
                {
                    string key = dm.dmPelsWidth + "x" + dm.dmPelsHeight;
                    ResolutionOption opt;
                    if (!grouped.TryGetValue(key, out opt))
                    {
                        opt = new ResolutionOption
                        {
                            Width = (int)dm.dmPelsWidth,
                            Height = (int)dm.dmPelsHeight
                        };
                        grouped[key] = opt;
                    }

                    int hz = (int)dm.dmDisplayFrequency;
                    if (hz > 0 && !opt.RefreshRates.Contains(hz))
                    {
                        opt.RefreshRates.Add(hz);
                    }
                }
                dm = CreateDevMode();
            }

            foreach (var opt in grouped.Values)
            {
                opt.RefreshRates.Sort((a, b) => b.CompareTo(a));
            }

            return grouped.Values
                .OrderByDescending(r => (long)r.Width * r.Height)
                .ThenByDescending(r => r.Width)
                .ToList();
        }

        private static Dictionary<string, DpiPathState> QueryDpiStatesByGdiName()
        {
            var map = new Dictionary<string, DpiPathState>(StringComparer.OrdinalIgnoreCase);

            uint pathCount, modeCount;
            if (GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out pathCount, out modeCount) != 0 || pathCount == 0)
            {
                return map;
            }

            var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
            var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
            if (QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero) != 0)
            {
                return map;
            }

            for (int i = 0; i < pathCount; i++)
            {
                var srcName = new DISPLAYCONFIG_SOURCE_DEVICE_NAME();
                srcName.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
                srcName.header.size = (uint)Marshal.SizeOf(typeof(DISPLAYCONFIG_SOURCE_DEVICE_NAME));
                srcName.header.adapterId = paths[i].sourceInfo.adapterId;
                srcName.header.id = paths[i].sourceInfo.id;

                if (DisplayConfigGetDeviceInfo(ref srcName) != 0 || string.IsNullOrEmpty(srcName.viewGdiDeviceName))
                {
                    continue;
                }

                if (map.ContainsKey(srcName.viewGdiDeviceName))
                {
                    continue;
                }

                var tgtName = new DISPLAYCONFIG_TARGET_DEVICE_NAME();
                tgtName.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;
                tgtName.header.size = (uint)Marshal.SizeOf(typeof(DISPLAYCONFIG_TARGET_DEVICE_NAME));
                tgtName.header.adapterId = paths[i].targetInfo.adapterId;
                tgtName.header.id = paths[i].targetInfo.id;
                DisplayConfigGetDeviceInfo(ref tgtName);

                var dpiGet = new DISPLAYCONFIG_SOURCE_DPI_SCALE_GET();
                dpiGet.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_DPI_SCALE;
                dpiGet.header.size = (uint)Marshal.SizeOf(typeof(DISPLAYCONFIG_SOURCE_DPI_SCALE_GET));
                dpiGet.header.adapterId = paths[i].sourceInfo.adapterId;
                dpiGet.header.id = paths[i].sourceInfo.id;

                var state = new DpiPathState
                {
                    AdapterId = paths[i].sourceInfo.adapterId,
                    SourceId = paths[i].sourceInfo.id,
                    MonitorFriendlyName = tgtName.monitorFriendlyDeviceName
                };

                if (DisplayConfigGetDeviceInfo(ref dpiGet) == 0)
                {
                    int recIdx = Clamp(-dpiGet.minScaleRel, 0, DpiSteps.Length - 1);
                    int curIdx = Clamp(recIdx + dpiGet.curScaleRel, 0, DpiSteps.Length - 1);
                    int minIdx = Clamp(recIdx + dpiGet.minScaleRel, 0, DpiSteps.Length - 1);
                    int maxIdx = Clamp(recIdx + dpiGet.maxScaleRel, minIdx, DpiSteps.Length - 1);

                    state.MinScaleRel = dpiGet.minScaleRel;
                    state.CurScaleRel = dpiGet.curScaleRel;
                    state.MaxScaleRel = dpiGet.maxScaleRel;
                    state.RecommendedScalePercent = DpiSteps[recIdx];
                    state.CurrentScalePercent = DpiSteps[curIdx];

                    for (int idx = minIdx; idx <= maxIdx; idx++)
                    {
                        state.SupportedScales.Add(DpiSteps[idx]);
                    }
                }
                else
                {
                    state.RecommendedScalePercent = 100;
                    state.CurrentScalePercent = 100;
                    state.SupportedScales.AddRange(new[] { 100, 125, 150, 175, 200 });
                }

                map[srcName.viewGdiDeviceName] = state;
            }

            return map;
        }

        private static DEVMODE CreateDevMode()
        {
            var dm = new DEVMODE();
            dm.dmSize = (ushort)Marshal.SizeOf(typeof(DEVMODE));
            return dm;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
