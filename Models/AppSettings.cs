using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MultiSerialMonitor.Services;

namespace MultiSerialMonitor.Models
{
    public class AppSettings
    {
        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MultiSerialMonitor"
        );
        
        private static readonly string SettingsFile = Path.Combine(SettingsDirectory, "appsettings.json");
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Theme Theme { get; set; } = Theme.Light;
        
        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    var json = File.ReadAllText(SettingsFile);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading app settings: {ex.Message}");
            }
            
            return new AppSettings();
        }
        
        public void Save()
        {
            try
            {
                if (!Directory.Exists(SettingsDirectory))
                {
                    Directory.CreateDirectory(SettingsDirectory);
                }
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                
                var json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(SettingsFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving app settings: {ex.Message}");
            }
        }
    }
}