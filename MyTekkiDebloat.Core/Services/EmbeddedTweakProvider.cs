using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyTekkiDebloat.Core.Interfaces;
using MyTekkiDebloat.Core.Models;

namespace MyTekkiDebloat.Core.Services
{
    /// <summary>
    /// Provides tweaks from embedded JSON resources in the assembly
    /// </summary>
    public class EmbeddedTweakProvider : ITweakProvider
    {
        private readonly JsonSerializerOptions _jsonOptions;
        private List<Tweak>? _cachedTweaks;

        public EmbeddedTweakProvider()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            _jsonOptions.Converters.Add(new JsonStringEnumConverter());
        }

        /// <summary>
        /// Get all available tweaks from embedded resources
        /// </summary>
        public async Task<IEnumerable<Tweak>> GetTweaksAsync()
        {
            if (_cachedTweaks == null)
            {
                await LoadTweaksFromEmbeddedResourcesAsync();
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
                return await GetTweaksAsync();

            var tweaks = await GetTweaksAsync();
            return tweaks.Where(t => 
                t.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                t.Tags.Any(tag => tag.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
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
        /// Load all tweak files from embedded resources
        /// </summary>
        private async Task LoadTweaksFromEmbeddedResourcesAsync()
        {
            _cachedTweaks = new List<Tweak>();
            var assembly = Assembly.GetExecutingAssembly();
            
            // Get all embedded JSON resources from the Data folder
            var resourceNames = assembly.GetManifestResourceNames()
                .Where(name => name.Contains(".Data.") && name.EndsWith(".json"))
                .ToList();

            foreach (var resourceName in resourceNames)
            {
                try
                {
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream == null) continue;

                    using var reader = new StreamReader(stream);
                    var jsonContent = await reader.ReadToEndAsync();
                    
                    // Try to deserialize as single tweak (new format)
                    try
                    {
                        var singleTweak = JsonSerializer.Deserialize<Tweak>(jsonContent, _jsonOptions);
                        if (singleTweak != null && !string.IsNullOrWhiteSpace(singleTweak.Id))
                        {
                            _cachedTweaks.Add(singleTweak);
                            continue; // Successfully loaded as single tweak
                        }
                    }
                    catch
                    {
                        // Not a single tweak, try as array (old format compatibility)
                    }
                    
                    // Try to deserialize as array (old format)
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
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue loading other resources
                        Console.WriteLine($"Error loading tweaks from embedded resource {resourceName}: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accessing embedded resource {resourceName}: {ex.Message}");
                }
            }

            // Debug: uncomment below to see loaded tweaks
            // Console.WriteLine($"Loaded {_cachedTweaks.Count} tweaks from {resourceNames.Count} embedded resources");
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