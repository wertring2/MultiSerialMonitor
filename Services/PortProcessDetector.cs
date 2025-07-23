using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;

namespace MultiSerialMonitor.Services
{
    public static class PortProcessDetector
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, uint ucchMax);
        
        public static string? GetProcessUsingPort(string portName)
        {
            try
            {
                // Method 1: Try to get handle information via WMI
                var processName = GetProcessViaWmi(portName);
                if (!string.IsNullOrEmpty(processName))
                    return processName;
                
                // Method 2: Check common applications
                return CheckCommonApplications(portName);
            }
            catch
            {
                return null;
            }
        }
        
        private static string? GetProcessViaWmi(string portName)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_SerialPort WHERE DeviceID = '" + portName + "'");
                
                foreach (ManagementObject port in searcher.Get())
                {
                    var status = port["Status"]?.ToString();
                    if (status == "OK")
                    {
                        // Port is in use, try to find which process
                        return DetectProcessByHandles(portName);
                    }
                }
            }
            catch
            {
                // WMI might not be available
            }
            
            return null;
        }
        
        private static string? DetectProcessByHandles(string portName)
        {
            var knownProcesses = new Dictionary<string, string[]>
            {
                { "putty", new[] { "PuTTY" } },
                { "kitty", new[] { "KiTTY" } },
                { "arduino", new[] { "Arduino IDE" } },
                { "code", new[] { "Visual Studio Code (Serial Monitor Extension)" } },
                { "devenv", new[] { "Visual Studio" } },
                { "terminal", new[] { "Windows Terminal" } },
                { "powershell", new[] { "PowerShell" } },
                { "cmd", new[] { "Command Prompt" } },
                { "teraterm", new[] { "Tera Term" } },
                { "realterm", new[] { "RealTerm" } },
                { "coolterm", new[] { "CoolTerm" } },
                { "minicom", new[] { "Minicom" } },
                { "screen", new[] { "GNU Screen" } },
                { "python", new[] { "Python Script (pySerial)" } },
                { "java", new[] { "Java Application" } },
                { "multiserialmonitor", new[] { "Another MultiSerialMonitor instance" } }
            };
            
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    var processName = process.ProcessName.ToLower();
                    foreach (var kvp in knownProcesses)
                    {
                        if (processName.Contains(kvp.Key))
                        {
                            return kvp.Value[0];
                        }
                    }
                }
                catch
                {
                    // Process might have exited
                }
            }
            
            return null;
        }
        
        private static string? CheckCommonApplications(string portName)
        {
            // Quick check for commonly running serial port applications
            var commonApps = new[]
            {
                ("putty", "PuTTY"),
                ("kitty", "KiTTY"),
                ("arduino", "Arduino IDE"),
                ("teraterm", "Tera Term"),
                ("realterm", "RealTerm"),
                ("multiserialmonitor", "Another MultiSerialMonitor instance")
            };
            
            foreach (var (processName, displayName) in commonApps)
            {
                if (IsProcessRunning(processName))
                {
                    return displayName;
                }
            }
            
            return null;
        }
        
        private static bool IsProcessRunning(string processName)
        {
            try
            {
                return Process.GetProcessesByName(processName).Length > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}