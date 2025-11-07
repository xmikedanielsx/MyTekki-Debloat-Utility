using System.Text.Json;
using System.Text.Json.Serialization;
using MyTekkiDebloat.Core.Interfaces;
using MyTekkiDebloat.Core.Models;

namespace MyTekkiDebloat.Core.Services
{
    /// <summary>
    /// Provides tweaks from JSON files in the Data folder (for testing)
    /// </summary>
    public class FileTweakProvider : ITweakProvider
    {
        private readonly JsonSerializerOptions _jsonOptions;
        private List<Tweak>? _cachedTweaks;
        private string _dataPath;

        public FileTweakProvider(string? dataPath = null)
        {
            // Try to find the Data folder in several locations
            if (dataPath == null)
            {
                // First try relative to the executable
                var appDir = AppContext.BaseDirectory;
                var possiblePaths = new[]
                {
                    Path.Combine(appDir, "Data"),
                    Path.Combine(appDir, "..", "..", "..", "..", "MyTekkiDebloat.Core", "Data"),
                    Path.Combine(appDir, "..", "..", "..", "MyTekkiDebloat.Core", "Data"),
                    @"D:\Projects\Personal\MyTekkiDebloat\MyTekkiDebloat.Core\Data" // Absolute fallback
                };

                foreach (var path in possiblePaths)
                {
                    if (Directory.Exists(path))
                    {
                        _dataPath = path;
                        break;
                    }
                }

                _dataPath = _dataPath ?? possiblePaths[0]; // Default to first option if none found
            }
            else
            {
                _dataPath = dataPath;
            }
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            _jsonOptions.Converters.Add(new JsonStringEnumConverter());
        }

        public async Task<IEnumerable<Tweak>> GetTweaksAsync()
        {
            if (_cachedTweaks == null)
            {
                await LoadTweaksFromFilesAsync();
            }
            return _cachedTweaks ?? Enumerable.Empty<Tweak>();
        }

        public async Task<IEnumerable<Tweak>> GetTweaksByCategoryAsync(string category)
        {
            var tweaks = await GetTweaksAsync();
            return tweaks.Where(t => string.Equals(t.Category, category, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<Tweak?> GetTweakByIdAsync(string id)
        {
            var tweaks = await GetTweaksAsync();
            return tweaks.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<IEnumerable<Tweak>> SearchTweaksAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetTweaksAsync();

            var tweaks = await GetTweaksAsync();
            return tweaks.Where(t => 
                t.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                t.Tags.Any(tag => tag.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
        }

        public async Task<IEnumerable<string>> GetCategoriesAsync()
        {
            var tweaks = await GetTweaksAsync();
            return tweaks.Select(t => t.Category).Distinct().OrderBy(c => c);
        }

        private async Task LoadTweaksFromFilesAsync()
        {
            _cachedTweaks = new List<Tweak>();

            var logPath = Path.Combine(Path.GetTempPath(), "MyTekkiDebloat_TweakLoading.log");
            var logMessages = new List<string>();

            void Log(string message)
            {
                Console.WriteLine(message);
                logMessages.Add($"{DateTime.Now:HH:mm:ss}: {message}");
            }

            Log($"Current working directory: {Directory.GetCurrentDirectory()}");
            Log($"AppContext.BaseDirectory: {AppContext.BaseDirectory}");
            Log($"Trying to load from: {_dataPath}");

            if (!Directory.Exists(_dataPath))
            {
                Log($"ERROR: Data directory not found: {_dataPath}");
                
                // Try to find it in other locations
                var searchPaths = new[]
                {
                    @"D:\Projects\Personal\MyTekkiDebloat\MyTekkiDebloat.Core\Data",
                    Path.Combine(Environment.CurrentDirectory, "MyTekkiDebloat.Core", "Data"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Data")
                };

                foreach (var searchPath in searchPaths)
                {
                    Log($"Checking: {searchPath} - Exists: {Directory.Exists(searchPath)}");
                    if (Directory.Exists(searchPath))
                    {
                        _dataPath = searchPath;
                        Log($"Found data at: {_dataPath}");
                        break;
                    }
                }

                if (!Directory.Exists(_dataPath))
                {
                    Log("ERROR: Could not find Data directory in any location");
                    try
                    {
                        await File.WriteAllLinesAsync(logPath, logMessages);
                    }
                    catch { }
                    return;
                }
            }

            var jsonFiles = Directory.GetFiles(_dataPath, "*.json")
                .Where(f => !Path.GetFileName(f).Equals("tweaks.json", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Log($"Found {jsonFiles.Count} JSON files in {_dataPath}:");
            foreach (var file in jsonFiles)
            {
                Log($"  - {Path.GetFileName(file)}");
            }

            foreach (var filePath in jsonFiles)
            {
                try
                {
                    var jsonContent = await File.ReadAllTextAsync(filePath);
                    
                    try
                    {
                        var tweak = JsonSerializer.Deserialize<Tweak>(jsonContent, _jsonOptions);
                        if (tweak != null && !string.IsNullOrWhiteSpace(tweak.Id))
                        {
                            _cachedTweaks.Add(tweak);
                            Log($"Loaded tweak: {tweak.Id} - {tweak.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Error deserializing {Path.GetFileName(filePath)}: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error reading file {Path.GetFileName(filePath)}: {ex.Message}");
                }
            }

            Log($"Total loaded tweaks: {_cachedTweaks.Count}");

            // Write log to file
            try
            {
                await File.WriteAllLinesAsync(logPath, logMessages);
                Log($"Log written to: {logPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write log: {ex.Message}");
            }
        }

        public void ClearCache()
        {
            _cachedTweaks = null;
        }
    }
}