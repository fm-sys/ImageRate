using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ImageRate
{


    public class MonitorHelper
    {
        [DllImport("User32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfoEx lpmi);

        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData);

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MonitorInfoEx
        {
            public int Size;
            public Rect Monitor;
            public Rect WorkArea;
            public uint Flags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
        }

        public static List<MonitorInfoEx> GetAllMonitorsInfo()
        {
            var monitors = new List<MonitorInfoEx>();

            // Callback method as a named method instead of inline lambda
            MonitorEnumProc callback = new MonitorEnumProc((hMonitor, hdcMonitor, lprcMonitor, dwData) =>
            {
                MonitorInfoEx monitorInfo = new MonitorInfoEx();
                monitorInfo.Size = Marshal.SizeOf(typeof(MonitorInfoEx));

                if (GetMonitorInfo(hMonitor, ref monitorInfo))
                {
                    monitors.Add(monitorInfo);
                }
                else
                {
                    Console.WriteLine("Failed to get monitor info.");
                }

                return true; // Continue enumeration
            });

            // Start enumeration and check if it was successful
            bool success = EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
            if (!success)
            {
                Console.WriteLine("EnumDisplayMonitors failed.");
            }

            return monitors;
        }
    }

}
