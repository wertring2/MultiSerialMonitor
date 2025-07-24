using System.Globalization;

namespace MultiSerialMonitor.Localization
{
    public static class LocalizationManager
    {
        private static Language _currentLanguage = Language.English;
        private static readonly Dictionary<string, Dictionary<Language, string>> _translations = new();
        
        public static Language CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                _currentLanguage = value;
                var cultureCode = value == Language.Thai ? "th-TH" : "en-US";
                Thread.CurrentThread.CurrentCulture = new CultureInfo(cultureCode);
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(cultureCode);
                LanguageChanged?.Invoke(null, EventArgs.Empty);
            }
        }
        
        public static event EventHandler? LanguageChanged;
        
        static LocalizationManager()
        {
            InitializeTranslations();
        }
        
        public static string GetString(string key)
        {
            if (_translations.TryGetValue(key, out var translations))
            {
                if (translations.TryGetValue(_currentLanguage, out var translation))
                {
                    return translation;
                }
            }
            return key; // Return key if translation not found
        }
        
        private static void InitializeTranslations()
        {
            // Main Window
            _translations["AppTitle"] = new Dictionary<Language, string>
            {
                { Language.English, "Multi Serial Monitor" },
                { Language.Thai, "โปรแกรมมอนิเตอร์พอร์ตอนุกรมหลายพอร์ต" }
            };
            
            // Toolbar buttons
            _translations["AddPort"] = new Dictionary<Language, string>
            {
                { Language.English, "Add Port" },
                { Language.Thai, "เพิ่มพอร์ต" }
            };
            
            _translations["Refresh"] = new Dictionary<Language, string>
            {
                { Language.English, "Refresh" },
                { Language.Thai, "รีเฟรช" }
            };
            
            _translations["RemoveAll"] = new Dictionary<Language, string>
            {
                { Language.English, "Remove All" },
                { Language.Thai, "ลบทั้งหมด" }
            };
            
            _translations["ClearAllData"] = new Dictionary<Language, string>
            {
                { Language.English, "Clear All Data" },
                { Language.Thai, "ล้างข้อมูลทั้งหมด" }
            };
            
            _translations["Profiles"] = new Dictionary<Language, string>
            {
                { Language.English, "Profiles" },
                { Language.Thai, "โปรไฟล์" }
            };
            
            // Profile menu items
            _translations["SaveProfile"] = new Dictionary<Language, string>
            {
                { Language.English, "Save Profile..." },
                { Language.Thai, "บันทึกโปรไฟล์..." }
            };
            
            _translations["LoadProfile"] = new Dictionary<Language, string>
            {
                { Language.English, "Load Profile" },
                { Language.Thai, "โหลดโปรไฟล์" }
            };
            
            _translations["ExportProfile"] = new Dictionary<Language, string>
            {
                { Language.English, "Export Profile..." },
                { Language.Thai, "ส่งออกโปรไฟล์..." }
            };
            
            _translations["ImportProfile"] = new Dictionary<Language, string>
            {
                { Language.English, "Import Profile..." },
                { Language.Thai, "นำเข้าโปรไฟล์..." }
            };
            
            _translations["ManageProfiles"] = new Dictionary<Language, string>
            {
                { Language.English, "Manage Profiles..." },
                { Language.Thai, "จัดการโปรไฟล์..." }
            };
            
            // Status messages
            _translations["Ready"] = new Dictionary<Language, string>
            {
                { Language.English, "Ready" },
                { Language.Thai, "พร้อม" }
            };
            
            _translations["Connected"] = new Dictionary<Language, string>
            {
                { Language.English, "Connected" },
                { Language.Thai, "เชื่อมต่อแล้ว" }
            };
            
            _translations["Disconnected"] = new Dictionary<Language, string>
            {
                { Language.English, "Disconnected" },
                { Language.Thai, "ตัดการเชื่อมต่อ" }
            };
            
            _translations["Connecting"] = new Dictionary<Language, string>
            {
                { Language.English, "Connecting" },
                { Language.Thai, "กำลังเชื่อมต่อ" }
            };
            
            _translations["Error"] = new Dictionary<Language, string>
            {
                { Language.English, "Error" },
                { Language.Thai, "ข้อผิดพลาด" }
            };
            
            // Context menu
            _translations["Connect"] = new Dictionary<Language, string>
            {
                { Language.English, "Connect" },
                { Language.Thai, "เชื่อมต่อ" }
            };
            
            _translations["Disconnect"] = new Dictionary<Language, string>
            {
                { Language.English, "Disconnect" },
                { Language.Thai, "ตัดการเชื่อมต่อ" }
            };
            
            _translations["ClearData"] = new Dictionary<Language, string>
            {
                { Language.English, "Clear Data" },
                { Language.Thai, "ล้างข้อมูล" }
            };
            
            _translations["ExportData"] = new Dictionary<Language, string>
            {
                { Language.English, "Export Data..." },
                { Language.Thai, "ส่งออกข้อมูล..." }
            };
            
            _translations["GridView"] = new Dictionary<Language, string>
            {
                { Language.English, "Grid View" },
                { Language.Thai, "มุมมองตาราง" }
            };
            
            _translations["ListView"] = new Dictionary<Language, string>
            {
                { Language.English, "List View" },
                { Language.Thai, "มุมมองรายการ" }
            };
            
            _translations["Remove"] = new Dictionary<Language, string>
            {
                { Language.English, "Remove" },
                { Language.Thai, "ลบ" }
            };
            
            // Port Panel buttons
            _translations["Expand"] = new Dictionary<Language, string>
            {
                { Language.English, "Expand" },
                { Language.Thai, "ขยาย" }
            };
            
            _translations["Clear"] = new Dictionary<Language, string>
            {
                { Language.English, "Clear" },
                { Language.Thai, "ล้าง" }
            };
            
            // Add Port Form
            _translations["AddPortConnection"] = new Dictionary<Language, string>
            {
                { Language.English, "Add Port Connection" },
                { Language.Thai, "เพิ่มการเชื่อมต่อพอร์ต" }
            };
            
            _translations["Name"] = new Dictionary<Language, string>
            {
                { Language.English, "Name:" },
                { Language.Thai, "ชื่อ:" }
            };
            
            _translations["SerialPort"] = new Dictionary<Language, string>
            {
                { Language.English, "Serial Port" },
                { Language.Thai, "พอร์ตอนุกรม" }
            };
            
            _translations["Telnet"] = new Dictionary<Language, string>
            {
                { Language.English, "Telnet" },
                { Language.Thai, "เทลเน็ต" }
            };
            
            _translations["Port"] = new Dictionary<Language, string>
            {
                { Language.English, "Port:" },
                { Language.Thai, "พอร์ต:" }
            };
            
            _translations["BaudRate"] = new Dictionary<Language, string>
            {
                { Language.English, "Baud Rate:" },
                { Language.Thai, "อัตราบอด:" }
            };
            
            _translations["Parity"] = new Dictionary<Language, string>
            {
                { Language.English, "Parity:" },
                { Language.Thai, "พาริตี้:" }
            };
            
            _translations["DataBits"] = new Dictionary<Language, string>
            {
                { Language.English, "Data Bits:" },
                { Language.Thai, "บิตข้อมูล:" }
            };
            
            _translations["StopBits"] = new Dictionary<Language, string>
            {
                { Language.English, "Stop Bits:" },
                { Language.Thai, "บิตหยุด:" }
            };
            
            _translations["Host"] = new Dictionary<Language, string>
            {
                { Language.English, "Host:" },
                { Language.Thai, "โฮสต์:" }
            };
            
            _translations["OK"] = new Dictionary<Language, string>
            {
                { Language.English, "OK" },
                { Language.Thai, "ตกลง" }
            };
            
            _translations["Cancel"] = new Dictionary<Language, string>
            {
                { Language.English, "Cancel" },
                { Language.Thai, "ยกเลิก" }
            };
            
            // Console Form
            _translations["Console"] = new Dictionary<Language, string>
            {
                { Language.English, "Console" },
                { Language.Thai, "คอนโซล" }
            };
            
            _translations["Send"] = new Dictionary<Language, string>
            {
                { Language.English, "Send" },
                { Language.Thai, "ส่ง" }
            };
            
            _translations["LineNumbers"] = new Dictionary<Language, string>
            {
                { Language.English, "Line #" },
                { Language.Thai, "บรรทัด" }
            };
            
            _translations["Status"] = new Dictionary<Language, string>
            {
                { Language.English, "Status" },
                { Language.Thai, "สถานะ" }
            };
            
            // Error messages
            _translations["ValidationError"] = new Dictionary<Language, string>
            {
                { Language.English, "Validation Error" },
                { Language.Thai, "ข้อผิดพลาดการตรวจสอบ" }
            };
            
            _translations["PleaseEnterName"] = new Dictionary<Language, string>
            {
                { Language.English, "Please enter a name for this connection." },
                { Language.Thai, "กรุณาใส่ชื่อสำหรับการเชื่อมต่อนี้" }
            };
            
            _translations["PleaseSelectPort"] = new Dictionary<Language, string>
            {
                { Language.English, "Please select a serial port." },
                { Language.Thai, "กรุณาเลือกพอร์ตอนุกรม" }
            };
            
            _translations["PleaseEnterHost"] = new Dictionary<Language, string>
            {
                { Language.English, "Please enter a host name or IP address." },
                { Language.Thai, "กรุณาใส่ชื่อโฮสต์หรือที่อยู่ IP" }
            };
            
            _translations["InvalidConnectionName"] = new Dictionary<Language, string>
            {
                { Language.English, "Connection name contains invalid characters or is too long.\nUse only letters, numbers, spaces, and basic punctuation." },
                { Language.Thai, "ชื่อการเชื่อมต่อมีอักขระที่ไม่ถูกต้องหรือยาวเกินไป\nใช้เฉพาะตัวอักษร ตัวเลข ช่องว่าง และเครื่องหมายวรรคตอนพื้นฐาน" }
            };
            
            _translations["InvalidHostname"] = new Dictionary<Language, string>
            {
                { Language.English, "Invalid hostname or IP address.\nPlease enter a valid hostname (e.g., example.com) or IP address (e.g., 192.168.1.1)." },
                { Language.Thai, "ชื่อโฮสต์หรือที่อยู่ IP ไม่ถูกต้อง\nกรุณาใส่ชื่อโฮสต์ที่ถูกต้อง (เช่น example.com) หรือที่อยู่ IP (เช่น 192.168.1.1)" }
            };
            
            _translations["ConnectionFailed"] = new Dictionary<Language, string>
            {
                { Language.English, "Connection Failed" },
                { Language.Thai, "การเชื่อมต่อล้มเหลว" }
            };
            
            _translations["ConnectionError"] = new Dictionary<Language, string>
            {
                { Language.English, "Connection Error" },
                { Language.Thai, "ข้อผิดพลาดการเชื่อมต่อ" }
            };
            
            _translations["PortNotFound"] = new Dictionary<Language, string>
            {
                { Language.English, "Port not found" },
                { Language.Thai, "ไม่พบพอร์ต" }
            };
            
            _translations["PortInUse"] = new Dictionary<Language, string>
            {
                { Language.English, "Port Already In Use" },
                { Language.Thai, "พอร์ตถูกใช้งานอยู่" }
            };
            
            _translations["ConfirmRemove"] = new Dictionary<Language, string>
            {
                { Language.English, "Confirm Remove" },
                { Language.Thai, "ยืนยันการลบ" }
            };
            
            _translations["RemoveQuestion"] = new Dictionary<Language, string>
            {
                { Language.English, "Remove {0}?" },
                { Language.Thai, "ลบ {0} หรือไม่?" }
            };
            
            _translations["RemoveAllQuestion"] = new Dictionary<Language, string>
            {
                { Language.English, "Remove all {0} port(s)?" },
                { Language.Thai, "ลบพอร์ตทั้งหมด {0} พอร์ตหรือไม่?" }
            };
            
            _translations["ConfirmRemoveAll"] = new Dictionary<Language, string>
            {
                { Language.English, "Confirm Remove All" },
                { Language.Thai, "ยืนยันการลบทั้งหมด" }
            };
            
            _translations["NoPortsToRemove"] = new Dictionary<Language, string>
            {
                { Language.English, "No ports to remove." },
                { Language.Thai, "ไม่มีพอร์ตที่จะลบ" }
            };
            
            _translations["Information"] = new Dictionary<Language, string>
            {
                { Language.English, "Information" },
                { Language.Thai, "ข้อมูล" }
            };
            
            _translations["Warning"] = new Dictionary<Language, string>
            {
                { Language.English, "Warning" },
                { Language.Thai, "คำเตือน" }
            };
            
            _translations["ConfirmClearAll"] = new Dictionary<Language, string>
            {
                { Language.English, "Confirm Clear All" },
                { Language.Thai, "ยืนยันการล้างทั้งหมด" }
            };
            
            _translations["ClearAllQuestion"] = new Dictionary<Language, string>
            {
                { Language.English, "Are you sure you want to clear all data for all ports?" },
                { Language.Thai, "คุณแน่ใจหรือไม่ที่จะล้างข้อมูลทั้งหมดสำหรับทุกพอร์ต?" }
            };
            
            _translations["NoPortsToClear"] = new Dictionary<Language, string>
            {
                { Language.English, "No ports to clear." },
                { Language.Thai, "ไม่มีพอร์ตที่จะล้าง" }
            };
            
            // Stats display
            _translations["Lines"] = new Dictionary<Language, string>
            {
                { Language.English, "Lines" },
                { Language.Thai, "บรรทัด" }
            };
            
            _translations["Packages"] = new Dictionary<Language, string>
            {
                { Language.English, "Packages" },
                { Language.Thai, "แพ็กเกจ" }
            };
            
            _translations["Time"] = new Dictionary<Language, string>
            {
                { Language.English, "Time" },
                { Language.Thai, "เวลา" }
            };
            
            _translations["NoData"] = new Dictionary<Language, string>
            {
                { Language.English, "No data" },
                { Language.Thai, "ไม่มีข้อมูล" }
            };
            
            _translations["Detections"] = new Dictionary<Language, string>
            {
                { Language.English, "Detections" },
                { Language.Thai, "การตรวจพบ" }
            };
            
            // File dialogs
            _translations["ProfileFiles"] = new Dictionary<Language, string>
            {
                { Language.English, "Profile files (*.json)|*.json|All files (*.*)|*.*" },
                { Language.Thai, "ไฟล์โปรไฟล์ (*.json)|*.json|ไฟล์ทั้งหมด (*.*)|*.*" }
            };
            
            _translations["TextFiles"] = new Dictionary<Language, string>
            {
                { Language.English, "Text files (*.txt)|*.txt|CSV files (*.csv)|*.csv|Log files (*.log)|*.log|All files (*.*)|*.*" },
                { Language.Thai, "ไฟล์ข้อความ (*.txt)|*.txt|ไฟล์ CSV (*.csv)|*.csv|ไฟล์บันทึก (*.log)|*.log|ไฟล์ทั้งหมด (*.*)|*.*" }
            };
            
            _translations["ExportSuccess"] = new Dictionary<Language, string>
            {
                { Language.English, "Export Success" },
                { Language.Thai, "ส่งออกสำเร็จ" }
            };
            
            _translations["ExportError"] = new Dictionary<Language, string>
            {
                { Language.English, "Export Error" },
                { Language.Thai, "ข้อผิดพลาดการส่งออก" }
            };
            
            _translations["ImportSuccess"] = new Dictionary<Language, string>
            {
                { Language.English, "Import Success" },
                { Language.Thai, "นำเข้าสำเร็จ" }
            };
            
            _translations["ImportError"] = new Dictionary<Language, string>
            {
                { Language.English, "Import Error" },
                { Language.Thai, "ข้อผิดพลาดการนำเข้า" }
            };
            
            _translations["NoDataToExport"] = new Dictionary<Language, string>
            {
                { Language.English, "No data to export for {0}." },
                { Language.Thai, "ไม่มีข้อมูลที่จะส่งออกสำหรับ {0}" }
            };
            
            _translations["DataExportedSuccessfully"] = new Dictionary<Language, string>
            {
                { Language.English, "Data exported successfully to:\n{0}\n\nTotal lines: {1}" },
                { Language.Thai, "ส่งออกข้อมูลสำเร็จไปยัง:\n{0}\n\nจำนวนบรรทัดทั้งหมด: {1}" }
            };
            
            _translations["LoadProfileNow"] = new Dictionary<Language, string>
            {
                { Language.English, "Would you like to load this profile now?" },
                { Language.Thai, "คุณต้องการโหลดโปรไฟล์นี้เลยหรือไม่?" }
            };
            
            _translations["NoProfiles"] = new Dictionary<Language, string>
            {
                { Language.English, "(No profiles)" },
                { Language.Thai, "(ไม่มีโปรไฟล์)" }
            };
            
            _translations["SelectProfile"] = new Dictionary<Language, string>
            {
                { Language.English, "Select Profile" },
                { Language.Thai, "เลือกโปรไฟล์" }
            };
            
            _translations["AvailableProfiles"] = new Dictionary<Language, string>
            {
                { Language.English, "Available Profiles:" },
                { Language.Thai, "โปรไฟล์ที่มี:" }
            };
            
            _translations["ProfileImported"] = new Dictionary<Language, string>
            {
                { Language.English, "Profile '{0}' imported successfully!" },
                { Language.Thai, "นำเข้าโปรไฟล์ '{0}' สำเร็จ!" }
            };
            
            _translations["SomePortsNotAvailable"] = new Dictionary<Language, string>
            {
                { Language.English, "Some Ports Not Available" },
                { Language.Thai, "บางพอร์ตไม่พร้อมใช้งาน" }
            };
            
            _translations["PortsNotAvailableMessage"] = new Dictionary<Language, string>
            {
                { Language.English, "The following ports from the profile could not be loaded:\n\n{0}\n\nThese ports are not currently available. They may be unplugged or in use by another application." },
                { Language.Thai, "ไม่สามารถโหลดพอร์ตต่อไปนี้จากโปรไฟล์:\n\n{0}\n\nพอร์ตเหล่านี้ไม่พร้อมใช้งานในขณะนี้ อาจถูกถอดออกหรือถูกใช้งานโดยแอปพลิเคชันอื่น" }
            };
            
            // Tooltips
            _translations["OpenConsoleView"] = new Dictionary<Language, string>
            {
                { Language.English, "Open console view" },
                { Language.Thai, "เปิดมุมมองคอนโซล" }
            };
            
            _translations["RemoveThisPort"] = new Dictionary<Language, string>
            {
                { Language.English, "Remove this port" },
                { Language.Thai, "ลบพอร์ตนี้" }
            };
            
            _translations["ConfigureDetectionPatterns"] = new Dictionary<Language, string>
            {
                { Language.English, "Configure Detection Patterns" },
                { Language.Thai, "ตั้งค่ารูปแบบการตรวจจับ" }
            };
            
            _translations["ClearAllDataForThisPort"] = new Dictionary<Language, string>
            {
                { Language.English, "Clear all data for this port" },
                { Language.Thai, "ล้างข้อมูลทั้งหมดสำหรับพอร์ตนี้" }
            };
            
            _translations["ClickToViewAll"] = new Dictionary<Language, string>
            {
                { Language.English, "Click to view all" },
                { Language.Thai, "คลิกเพื่อดูทั้งหมด" }
            };
            
            // Error descriptions
            _translations["AccessDenied"] = new Dictionary<Language, string>
            {
                { Language.English, "Access denied. The port may be in use by another application." },
                { Language.Thai, "การเข้าถึงถูกปฏิเสธ พอร์ตอาจถูกใช้งานโดยแอปพลิเคชันอื่น" }
            };
            
            _translations["PortDoesNotExist"] = new Dictionary<Language, string>
            {
                { Language.English, "The specified port does not exist." },
                { Language.Thai, "ไม่พบพอร์ตที่ระบุ" }
            };
            
            _translations["OperationTimedOut"] = new Dictionary<Language, string>
            {
                { Language.English, "The operation timed out. The device may not be responding." },
                { Language.Thai, "การดำเนินการหมดเวลา อุปกรณ์อาจไม่ตอบสนอง" }
            };
            
            _translations["InvalidOperation"] = new Dictionary<Language, string>
            {
                { Language.English, "Invalid operation. Please check your settings." },
                { Language.Thai, "การดำเนินการไม่ถูกต้อง กรุณาตรวจสอบการตั้งค่าของคุณ" }
            };
            
            _translations["CommandTooLong"] = new Dictionary<Language, string>
            {
                { Language.English, "Command is too long. Maximum 1000 characters allowed." },
                { Language.Thai, "คำสั่งยาวเกินไป อนุญาตสูงสุด 1000 ตัวอักษร" }
            };
            
            _translations["InvalidCommand"] = new Dictionary<Language, string>
            {
                { Language.English, "Invalid Command" },
                { Language.Thai, "คำสั่งไม่ถูกต้อง" }
            };
            
            _translations["CannotSendCommand"] = new Dictionary<Language, string>
            {
                { Language.English, "Cannot send command: {0}" },
                { Language.Thai, "ไม่สามารถส่งคำสั่ง: {0}" }
            };
            
            _translations["SendCommandError"] = new Dictionary<Language, string>
            {
                { Language.English, "Send Command Error" },
                { Language.Thai, "ข้อผิดพลาดการส่งคำสั่ง" }
            };
            
            // Language menu
            _translations["Language"] = new Dictionary<Language, string>
            {
                { Language.English, "Language" },
                { Language.Thai, "ภาษา" }
            };
            
            // Device boot messages
            _translations["DeviceBooting"] = new Dictionary<Language, string>
            {
                { Language.English, "Device booting..." },
                { Language.Thai, "อุปกรณ์กำลังเริ่มทำงาน..." }
            };
            
            _translations["DeviceResetting"] = new Dictionary<Language, string>
            {
                { Language.English, "Device is resetting - temporary data corruption expected" },
                { Language.Thai, "อุปกรณ์กำลังรีเซ็ต - คาดว่าข้อมูลจะเสียหายชั่วคราว" }
            };
            
            _translations["FramingErrorInfo"] = new Dictionary<Language, string>
            {
                { Language.English, "Framing error detected - this is common during device boot/reset" },
                { Language.Thai, "ตรวจพบข้อผิดพลาดเฟรม - เป็นเรื่องปกติระหว่างการบูต/รีเซ็ตอุปกรณ์" }
            };
            
            _translations["ConnectionWillContinue"] = new Dictionary<Language, string>
            {
                { Language.English, "The connection will continue - data may be garbled temporarily" },
                { Language.Thai, "การเชื่อมต่อจะดำเนินต่อ - ข้อมูลอาจอ่านไม่ออกชั่วคราว" }
            };
        }
    }
}