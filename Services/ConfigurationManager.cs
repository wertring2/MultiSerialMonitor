using System.Text.Json;
using System.Text.Json.Serialization;
using MultiSerialMonitor.Models;

namespace MultiSerialMonitor.Services
{
    public class ConfigurationManager
    {
        private readonly string _configPath;
        private readonly string _profilesPath;
        private readonly JsonSerializerOptions _jsonOptions;
        
        public ConfigurationManager()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MultiSerialMonitor"
            );
            
            Directory.CreateDirectory(appDataPath);
            
            _configPath = Path.Combine(appDataPath, "default.config.json");
            _profilesPath = Path.Combine(appDataPath, "profiles");
            Directory.CreateDirectory(_profilesPath);
            
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };
        }
        
        public void SaveConfiguration(List<PortConnection> connections)
        {
            try
            {
                var configs = connections.Select(c => new PortConfigurationData
                {
                    Id = c.Id,
                    Name = c.Name,
                    Type = c.Type,
                    PortName = c.PortName,
                    BaudRate = c.BaudRate,
                    Parity = c.Parity,
                    DataBits = c.DataBits,
                    StopBits = c.StopBits,
                    HostName = c.HostName,
                    Port = c.Port,
                    Config = c.Config
                }).ToList();
                
                var json = JsonSerializer.Serialize(configs, _jsonOptions);
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                // Log error but don't throw - we don't want to interrupt the app
                System.Diagnostics.Debug.WriteLine($"Error saving configuration: {ex.Message}");
            }
        }
        
        public List<PortConnection> LoadConfiguration()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    var configs = JsonSerializer.Deserialize<List<PortConfigurationData>>(json, _jsonOptions);
                    
                    if (configs != null)
                    {
                        return configs.Select(c => new PortConnection
                        {
                            Id = c.Id,
                            Name = c.Name,
                            Type = c.Type,
                            PortName = c.PortName,
                            BaudRate = c.BaudRate,
                            Parity = c.Parity,
                            DataBits = c.DataBits,
                            StopBits = c.StopBits,
                            HostName = c.HostName,
                            Port = c.Port,
                            Config = c.Config ?? new ConnectionConfig()
                        }).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading configuration: {ex.Message}");
            }
            
            return new List<PortConnection>();
        }
        
        public void SaveProfile(string profileName, List<PortConnection> connections)
        {
            try
            {
                var profilePath = Path.Combine(_profilesPath, $"{profileName}.json");
                var configs = connections.Select(c => new PortConfigurationData
                {
                    Id = Guid.NewGuid().ToString(), // New ID for profile
                    Name = c.Name,
                    Type = c.Type,
                    PortName = c.PortName,
                    BaudRate = c.BaudRate,
                    Parity = c.Parity,
                    DataBits = c.DataBits,
                    StopBits = c.StopBits,
                    HostName = c.HostName,
                    Port = c.Port,
                    Config = c.Config
                }).ToList();
                
                var json = JsonSerializer.Serialize(configs, _jsonOptions);
                File.WriteAllText(profilePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save profile: {ex.Message}", ex);
            }
        }
        
        public List<PortConnection> LoadProfile(string profileName)
        {
            try
            {
                var profilePath = Path.Combine(_profilesPath, $"{profileName}.json");
                if (File.Exists(profilePath))
                {
                    var json = File.ReadAllText(profilePath);
                    var configs = JsonSerializer.Deserialize<List<PortConfigurationData>>(json, _jsonOptions);
                    
                    if (configs != null)
                    {
                        return configs.Select(c => new PortConnection
                        {
                            Id = Guid.NewGuid().ToString(), // New ID when loading profile
                            Name = c.Name,
                            Type = c.Type,
                            PortName = c.PortName,
                            BaudRate = c.BaudRate,
                            Parity = c.Parity,
                            DataBits = c.DataBits,
                            StopBits = c.StopBits,
                            HostName = c.HostName,
                            Port = c.Port,
                            Config = c.Config ?? new ConnectionConfig()
                        }).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load profile: {ex.Message}", ex);
            }
            
            return new List<PortConnection>();
        }
        
        public string[] GetAvailableProfiles()
        {
            try
            {
                return Directory.GetFiles(_profilesPath, "*.json")
                    .Select(Path.GetFileNameWithoutExtension)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToArray()!;
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
        
        public void DeleteProfile(string profileName)
        {
            try
            {
                var profilePath = Path.Combine(_profilesPath, $"{profileName}.json");
                if (File.Exists(profilePath))
                {
                    File.Delete(profilePath);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete profile: {ex.Message}", ex);
            }
        }
        
        public void ExportProfile(string profileName, string exportPath)
        {
            try
            {
                var sourceFile = Path.Combine(_profilesPath, $"{profileName}.json");
                if (!File.Exists(sourceFile))
                {
                    throw new FileNotFoundException($"Profile '{profileName}' not found.");
                }
                
                File.Copy(sourceFile, exportPath, overwrite: true);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to export profile: {ex.Message}", ex);
            }
        }
        
        public string ImportProfile(string importPath)
        {
            try
            {
                if (!File.Exists(importPath))
                {
                    throw new FileNotFoundException($"Import file not found: {importPath}");
                }
                
                // Validate it's a valid profile
                var json = File.ReadAllText(importPath);
                var configs = JsonSerializer.Deserialize<List<PortConfigurationData>>(json, _jsonOptions);
                if (configs == null || configs.Count == 0)
                {
                    throw new InvalidOperationException("Invalid profile file - no connections found.");
                }
                
                // Generate a unique name if needed
                string baseName = Path.GetFileNameWithoutExtension(importPath);
                string profileName = baseName;
                int counter = 1;
                
                while (File.Exists(Path.Combine(_profilesPath, $"{profileName}.json")))
                {
                    profileName = $"{baseName} ({counter})";
                    counter++;
                }
                
                // Copy to profiles directory
                string destPath = Path.Combine(_profilesPath, $"{profileName}.json");
                File.Copy(importPath, destPath);
                
                return profileName;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to import profile: {ex.Message}", ex);
            }
        }
        
        // Configuration data class for serialization
        private class PortConfigurationData
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public ConnectionType Type { get; set; }
            public string PortName { get; set; } = "";
            public int BaudRate { get; set; }
            public System.IO.Ports.Parity Parity { get; set; }
            public int DataBits { get; set; }
            public System.IO.Ports.StopBits StopBits { get; set; }
            public string HostName { get; set; } = "";
            public int Port { get; set; }
            public ConnectionConfig? Config { get; set; }
        }
    }
}