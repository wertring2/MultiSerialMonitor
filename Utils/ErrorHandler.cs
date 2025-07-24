using System.Diagnostics;
using System.Runtime.InteropServices;
using MultiSerialMonitor.Localization;

namespace MultiSerialMonitor.Utils
{
    public static class ErrorHandler
    {
        private static readonly object _logLock = new object();
        private static readonly string _logPath;
        
        static ErrorHandler()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MultiSerialMonitor",
                "Logs"
            );
            Directory.CreateDirectory(appDataPath);
            _logPath = Path.Combine(appDataPath, $"error_{DateTime.Now:yyyyMMdd}.log");
        }
        
        public static void LogError(Exception ex, string context = "")
        {
            try
            {
                lock (_logLock)
                {
                    var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}\n" +
                               $"Exception: {ex.GetType().Name}\n" +
                               $"Message: {ex.Message}\n" +
                               $"StackTrace: {ex.StackTrace}\n" +
                               new string('-', 80) + "\n";
                    
                    File.AppendAllText(_logPath, entry);
                }
                
                Debug.WriteLine($"Error in {context}: {ex.Message}");
            }
            catch
            {
                // Prevent error logging from causing additional errors
            }
        }
        
        public static string GetUserFriendlyMessage(Exception ex)
        {
            return ex switch
            {
                UnauthorizedAccessException => LocalizationManager.GetString("AccessDenied"),
                IOException ioe when ioe.Message.Contains("not exist") => LocalizationManager.GetString("PortDoesNotExist"),
                IOException ioe when ioe.Message.Contains("denied") => LocalizationManager.GetString("AccessDenied"),
                TimeoutException => LocalizationManager.GetString("OperationTimedOut"),
                InvalidOperationException => LocalizationManager.GetString("InvalidOperation"),
                ArgumentException => LocalizationManager.GetString("InvalidOperation"),
                OutOfMemoryException => "The application is running low on memory. Please close some connections.",
                _ => $"An error occurred: {ex.Message}"
            };
        }
        
        public static void ShowError(IWin32Window? owner, Exception ex, string title = "Error")
        {
            var message = GetUserFriendlyMessage(ex);
            MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            LogError(ex, title);
        }
        
        public static void ShowError(IWin32Window? owner, string message, string title = "Error")
        {
            MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            LogError(new Exception(message), title);
        }
        
        public static DialogResult ShowWarning(IWin32Window? owner, string message, string title = "Warning")
        {
            return MessageBox.Show(owner, message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        }
        
        public static void ShowInfo(IWin32Window? owner, string message, string title = "Information")
        {
            MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        [DllImport("kernel32.dll")]
        private static extern uint GetLastError();
        
        public static string GetLastWin32ErrorMessage()
        {
            var errorCode = GetLastError();
            return $"Win32 Error Code: {errorCode}";
        }
    }
}