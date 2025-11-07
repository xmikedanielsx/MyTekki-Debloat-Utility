using System.Reflection;
using System.Text.Json;
using MyTekkiDebloat.Core.Interfaces;
using MyTekkiDebloat.Core.Models;

namespace MyTekkiDebloat.Core.Services
{
    /// <summary>
    /// Provides tweaks from JSON files in the Data directory
    /// </summary>
    public class JsonTweakProvider : ITweakProvider
    {
        private readonly string _dataDirectory;
        private readonly JsonSerializerOptions _jsonOptions;
        private List<Tweak>? _cachedTweaks;

        public JsonTweakProvider(string? dataDirectory = null)
        {
            // Default to Data directory relative to assembly location
            _dataDirectory = dataDirectory ?? GetDefaultDataDirectory();
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
        }

        /// <summary>
        /// Get all available tweaks
        /// </summary>
        public async Task<IEnumerable<Tweak>> GetTweaksAsync()
        {
            if (_cachedTweaks == null)
            {
                await LoadTweaksAsync();
            }
            return _cachedTweaks ?? Enumerable.Empty<Tweak>();
        }

        /// <summary>
        /// Get tweaks by category
        /// </summary>
        public async Task<IEnumerable<Tweak>> GetTweaksByCategoryAsync(string category)
        {
            var tweaks = await GetTweaksAsync();
            return tweaks.Where(t => string.Equals(t.Category, category, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get a specific tweak by ID
        /// </summary>
        public async Task<Tweak?> GetTweakByIdAsync(string id)
        {
            var tweaks = await GetTweaksAsync();
            return tweaks.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Search tweaks by name, description, or tags
        /// </summary>
        public async Task<IEnumerable<Tweak>> SearchTweaksAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<Tweak>();

            var tweaks = await GetTweaksAsync();
            var lowerSearchTerm = searchTerm.ToLowerInvariant();

            return tweaks.Where(t => 
                t.Name.ToLowerInvariant().Contains(lowerSearchTerm) ||
                t.Description.ToLowerInvariant().Contains(lowerSearchTerm) ||
                t.Tags.Any(tag => tag.ToLowerInvariant().Contains(lowerSearchTerm)));
        }

        /// <summary>
        /// Get available categories
        /// </summary>
        public async Task<IEnumerable<string>> GetCategoriesAsync()
        {
            var tweaks = await GetTweaksAsync();
            return tweaks.Select(t => t.Category).Distinct().OrderBy(c => c);
        }

        /// <summary>
        /// Load all tweak files from the data directory
        /// </summary>
        private async Task LoadTweaksAsync()
        {
            _cachedTweaks = new List<Tweak>();

            if (!Directory.Exists(_dataDirectory))
            {
                throw new DirectoryNotFoundException($"Data directory not found: {_dataDirectory}");
            }

            var jsonFiles = Directory.GetFiles(_dataDirectory, "*.json", SearchOption.TopDirectoryOnly);

            foreach (var filePath in jsonFiles)
            {
                try
                {
                    var jsonContent = await File.ReadAllTextAsync(filePath);
                    
                    // Try to deserialize as array first (old format)
                    try
                    {
                        var tweakArray = JsonSerializer.Deserialize<Tweak[]>(jsonContent, _jsonOptions);
                        if (tweakArray != null)
                        {
                            foreach (var tweak in tweakArray)
                            {
                                if (tweak != null && !string.IsNullOrWhiteSpace(tweak.Id))
                                {
                                    _cachedTweaks.Add(tweak);
                                }
                            }
                            continue; // Successfully loaded as array
                        }
                    }
                    catch
                    {
                        // Not an array, try as single tweak (new format)
                    }
                    
                    // Try to deserialize as single tweak (new format)
                    var singleTweak = JsonSerializer.Deserialize<Tweak>(jsonContent, _jsonOptions);
                    if (singleTweak != null && !string.IsNullOrWhiteSpace(singleTweak.Id))
                    {
                        _cachedTweaks.Add(singleTweak);
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue loading other files
                    Console.WriteLine($"Error loading tweaks from {filePath}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Get the default data directory path
        /// </summary>
        private static string GetDefaultDataDirectory()
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation) ?? throw new InvalidOperationException("Unable to determine assembly directory");
            return Path.Combine(assemblyDirectory, "Data");
        }

        /// <summary>
        /// Clear the cached tweaks to force reload on next access
        /// </summary>
        public void ClearCache()
        {
            _cachedTweaks = null;
        }
    }
}