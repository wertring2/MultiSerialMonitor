using System.Text.RegularExpressions;

namespace MultiSerialMonitor.Utils
{
    public static class ValidationHelper
    {
        // Port name validation
        public static bool IsValidPortName(string? portName)
        {
            if (string.IsNullOrWhiteSpace(portName))
                return false;
                
            // COM ports on Windows
            if (portName.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
            {
                var portNumber = portName.Substring(3);
                return int.TryParse(portNumber, out int num) && num > 0 && num <= 256;
            }
            
            // Linux/Unix style ports
            if (portName.StartsWith("/dev/"))
            {
                return Regex.IsMatch(portName, @"^/dev/tty(USB|ACM|S)\d+$");
            }
            
            return false;
        }
        
        // Baud rate validation
        public static bool IsValidBaudRate(int baudRate)
        {
            var standardBaudRates = new[] { 
                300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 
                38400, 57600, 115200, 128000, 256000 
            };
            return standardBaudRates.Contains(baudRate);
        }
        
        // Connection name validation
        public static bool IsValidConnectionName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;
                
            if (name.Length > 100)
                return false;
                
            // Allow alphanumeric, spaces, and common punctuation
            return Regex.IsMatch(name, @"^[\w\s\-_\.\(\)]+$");
        }
        
        // Hostname validation
        public static bool IsValidHostname(string? hostname)
        {
            if (string.IsNullOrWhiteSpace(hostname))
                return false;
                
            if (hostname.Length > 255)
                return false;
                
            // IP address pattern
            var ipPattern = @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
            if (Regex.IsMatch(hostname, ipPattern))
                return true;
                
            // Hostname pattern
            var hostnamePattern = @"^(([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]*[a-zA-Z0-9])\.)*([A-Za-z0-9]|[A-Za-z0-9][A-Za-z0-9\-]*[A-Za-z0-9])$";
            return Regex.IsMatch(hostname, hostnamePattern);
        }
        
        // Port number validation
        public static bool IsValidPort(int port)
        {
            return port > 0 && port <= 65535;
        }
        
        // File path validation
        public static bool IsValidFilePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;
                
            try
            {
                var fullPath = Path.GetFullPath(path);
                var invalidChars = Path.GetInvalidPathChars();
                return !path.Any(c => invalidChars.Contains(c));
            }
            catch
            {
                return false;
            }
        }
        
        // Sanitize user input
        public static string SanitizeInput(string? input, int maxLength = 100)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;
                
            // Remove control characters
            input = Regex.Replace(input, @"[\x00-\x1F\x7F]", "");
            
            // Trim and limit length
            input = input.Trim();
            if (input.Length > maxLength)
                input = input.Substring(0, maxLength);
                
            return input;
        }
        
        // Validate detection pattern
        public static bool IsValidRegexPattern(string? pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return false;
                
            try
            {
                _ = new Regex(pattern);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}