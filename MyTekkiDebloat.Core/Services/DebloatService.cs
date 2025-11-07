using MyTekkiDebloat.Core.Interfaces;
using MyTekkiDebloat.Core.Models;
using MyTekkiDebloat.Core.Services;
using System.Runtime.Versioning;
using System.Security.Principal;

namespace MyTekkiDebloat.Core.Services
{
    /// <summary>
    /// Main implementation of the debloat service
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class DebloatService : IDebloatService
    {
        public ITweakProvider TweakProvider { get; }
        public ITweakExecutor TweakExecutor { get; }
        public ITweakDetector TweakDetector { get; }
        public ITweakStateManager TweakStateManager { get; }
        public string? OriginalUserSid { get; }

        public DebloatService(
            ITweakProvider? tweakProvider = null,
            ITweakExecutor? tweakExecutor = null,
            ITweakDetector? tweakDetector = null,
            string? originalUserSid = null)
        {
            OriginalUserSid = originalUserSid;
            TweakProvider = tweakProvider ?? new EmbeddedTweakProvider();
            
            if (OperatingSystem.IsWindows())
            {
                TweakDetector = tweakDetector ?? new JsonDrivenTweakDetector();
                TweakExecutor = tweakExecutor ?? new TweakExecutor(TweakDetector, OriginalUserSid);
            }
            else
            {
                // Fallback for non-Windows platforms
                TweakDetector = tweakDetector ?? new TweakDetector();
                TweakExecutor = tweakExecutor ?? new TweakExecutor(TweakDetector, OriginalUserSid);
            }
            
            TweakStateManager = new TweakStateManager(TweakProvider, TweakDetector);
        }

        /// <summary>
        /// Get recommended tweaks based on system analysis
        /// </summary>
        public async Task<IEnumerable<Tweak>> GetRecommendedTweaksAsync()
        {
            // For now, return privacy and performance tweaks
            var allTweaks = await TweakProvider.GetTweaksAsync();
            
            return allTweaks.Where(t => 
                t.Category.Equals("Privacy", StringComparison.OrdinalIgnoreCase) ||
                t.Category.Equals("Performance", StringComparison.OrdinalIgnoreCase) ||
                t.Severity <= TweakSeverity.Medium)
                .Take(10); // Limit to top 10 recommendations
        }

        /// <summary>
        /// Create a system restore point before applying tweaks
        /// </summary>
        public async Task<bool> CreateRestorePointAsync(string description = "MyTekkiDebloat Tweaks")
        {
            if (!OperatingSystem.IsWindows())
                return false;

            try
            {
                // This is a simplified implementation - in production, you'd use WMI
                await Task.Run(() =>
                {
                    using var process = new System.Diagnostics.Process();
                    process.StartInfo.FileName = "wmic.exe";
                    process.StartInfo.Arguments = $"SystemRestore Create /Description:\"{description}\" /RestorePoint:APPLICATION_INSTALL";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    process.WaitForExit(30000); // 30 second timeout
                    
                    return process.ExitCode == 0;
                });
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Export current tweak configuration
        /// </summary>
        public async Task<string> ExportConfigurationAsync(IEnumerable<Tweak> tweaks)
        {
            var config = new
            {
                ExportedAt = DateTime.UtcNow,
                Version = "1.0.0",
                Tweaks = tweaks.Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Category,
                    t.Description
                })
            };

            return await Task.FromResult(System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            }));
        }

        /// <summary>
        /// Import tweak configuration
        /// </summary>
        public async Task<IEnumerable<Tweak>> ImportConfigurationAsync(string configurationJson)
        {
            try
            {
                var config = System.Text.Json.JsonSerializer.Deserialize<dynamic>(configurationJson);
                // This is a simplified implementation - you'd need proper deserialization
                
                // For now, return empty collection
                return await Task.FromResult(Enumerable.Empty<Tweak>());
            }
            catch
            {
                return Enumerable.Empty<Tweak>();
            }
        }

        /// <summary>
        /// Check if the current user has administrator privileges
        /// </summary>
        public bool IsRunningAsAdministrator()
        {
            if (!OperatingSystem.IsWindows())
                return false;

            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get system information relevant to tweaking
        /// </summary>
        public async Task<SystemInfo> GetSystemInfoAsync()
        {
            return await Task.Run(() =>
            {
                var systemInfo = new SystemInfo
                {
                    Architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86",
                    LastBootTime = DateTime.Now.AddMilliseconds(-Environment.TickCount64)
                };

                if (OperatingSystem.IsWindows())
                {
                    try
                    {
                        var os = Environment.OSVersion;
                        systemInfo.WindowsVersion = $"{os.Version.Major}.{os.Version.Minor}";
                        systemInfo.WindowsBuild = os.Version.Build.ToString();
                        systemInfo.IsWindows11 = os.Version.Build >= 22000;
                        
                        // This would need WMI in a real implementation
                        systemInfo.WindowsEdition = "Home"; // Placeholder
                        systemInfo.HasTPM = false; // Placeholder
                        systemInfo.SecureBootEnabled = false; // Placeholder
                    }
                    catch
                    {
                        // Fallback values
                        systemInfo.WindowsVersion = "Unknown";
                        systemInfo.WindowsBuild = "Unknown";
                    }
                }

                return systemInfo;
            });
        }
    }
}