using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace GSSystemAnalyzer.Models
{
    public class SystemMemoryMetrics
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ulTotalPagePhys;
            public ulong ulAvailPagePhys;
            public ulong ullTotalVirtual;
            public ulong ulAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        public static object GetLiveMetrics()
        {
            var memStatus = new MEMORYSTATUSEX();
            memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            if (GlobalMemoryStatusEx(ref memStatus))
            {
                double totalRamGb = memStatus.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                double availRamGb = memStatus.ullAvailPhys / (1024.0 * 1024.0 * 1024.0);
                double activeRamGb = totalRamGb - availRamGb;

                double totalPageGb = memStatus.ulTotalPagePhys / (1024.0 * 1024.0 * 1024.0);
                double availPageGb = memStatus.ulAvailPagePhys / (1024.0 * 1024.0 * 1024.0);
                double swapGb = totalPageGb - availPageGb;

                return new
                {
                    activeGb = Math.Round(activeRamGb, 2),
                    cacheGb = Math.Round(availRamGb, 2),
                    swapGb = Math.Round(swapGb, 2),
                    totalGb = Math.Round(totalRamGb, 2)
                };
            }

            return null;
        }

    
    }
}
