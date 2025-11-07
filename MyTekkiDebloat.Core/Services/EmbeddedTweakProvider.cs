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
            
            // Debug: Print all manifest resource names first
            var allResources = assembly.GetManifestResourceNames();
            var logPath = Path.Combine(Path.GetTempPath(), "MyTekkiDebloat_EmbeddedResources.log");
            var logMessages = new List<string>
            {
                $"Assembly: {assembly.FullName}",
                $"Total embedded resources: {allResources.Length}",
                "All embedded resources:"
            };
            
            foreach (var resource in allResources)
            {
                logMessages.Add($"  - {resource}");
            }
            
            // Get all embedded JSON resources from the Data folder
            var resourceNames = allResources
                .Where(name => name.Contains(".Data.") && name.EndsWith(".json"))
                .ToList();

            logMessages.Add($"Found {resourceNames.Count} JSON resources matching pattern:");
            foreach (var name in resourceNames)
            {
                logMessages.Add($"  - {name}");
            }

            foreach (var resourceName in resourceNames)
            {
                try
                {
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream == null) 
                    {
                        logMessages.Add($"Stream is null for resource: {resourceName}");
                        continue;
                    }

                    using var reader = new StreamReader(stream);
                    var jsonContent = await reader.ReadToEndAsync();
                    
                    logMessages.Add($"Read {jsonContent.Length} characters from {resourceName}");
                    
                    // Try to deserialize as single tweak (new format)
                    try
                    {
                        var singleTweak = JsonSerializer.Deserialize<Tweak>(jsonContent, _jsonOptions);
                        if (singleTweak != null && !string.IsNullOrWhiteSpace(singleTweak.Id))
                        {
                            _cachedTweaks.Add(singleTweak);
                            logMessages.Add($"Successfully loaded single tweak: {singleTweak.Id} - {singleTweak.Name}");
                            continue; // Successfully loaded as single tweak
                        }
                        else
                        {
                            logMessages.Add($"Single tweak deserialization returned null or invalid tweak for {resourceName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        logMessages.Add($"Single tweak deserialization failed for {resourceName}: {ex.Message}");
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
                                    logMessages.Add($"Successfully loaded array tweak: {tweak.Id} - {tweak.Name}");
                                }
                            }
                        }
                        else
                        {
                            logMessages.Add($"Array deserialization returned null for {resourceName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        logMessages.Add($"Array deserialization failed for {resourceName}: {ex.Message}");
                        // Log error but continue loading other resources
                    }
                }
                catch (Exception ex)
                {
                    logMessages.Add($"Error accessing embedded resource {resourceName}: {ex.Message}");
                }
            }

            logMessages.Add($"Total loaded tweaks: {_cachedTweaks.Count}");
            
            // Write debug log to temp file
            try
            {
                await File.WriteAllLinesAsync(logPath, logMessages);
            }
            catch 
            {
                // Ignore file write errors
            }
            
            // Also print to console
            foreach (var message in logMessages.TakeLast(5)) // Last few messages
            {
                Console.WriteLine(message);
            }
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