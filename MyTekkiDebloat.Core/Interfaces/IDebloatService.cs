using MyTekkiDebloat.Core.Models;

namespace MyTekkiDebloat.Core.Interfaces
{
    /// <summary>
    /// Main API facade for the MyTekkiDebloat library
    /// </summary>
    public interface IDebloatService
    {
        /// <summary>
        /// Tweak provider for getting available tweaks
        /// </summary>
        ITweakProvider TweakProvider { get; }

        /// <summary>
        /// Tweak executor for applying/reverting tweaks
        /// </summary>
        ITweakExecutor TweakExecutor { get; }

        /// <summary>
        /// Tweak detector for checking tweak status
        /// </summary>
        ITweakDetector TweakDetector { get; }

        /// <summary>
        /// Tweak state manager for managing tweak states and pending changes
        /// </summary>
        ITweakStateManager TweakStateManager { get; }

        /// <summary>
        /// Get recommended tweaks based on system analysis
        /// </summary>
        Task<IEnumerable<Tweak>> GetRecommendedTweaksAsync();

        /// <summary>
        /// Create a system restore point before applying tweaks
        /// </summary>
        Task<bool> CreateRestorePointAsync(string description = "MyTekkiDebloat Tweaks");

        /// <summary>
        /// Export current tweak configuration
        /// </summary>
        Task<string> ExportConfigurationAsync(IEnumerable<Tweak> tweaks);

        /// <summary>
        /// Import tweak configuration
        /// </summary>
        Task<IEnumerable<Tweak>> ImportConfigurationAsync(string configurationJson);

        /// <summary>
        /// Check if the current user has administrator privileges
        /// </summary>
        bool IsRunningAsAdministrator();

        /// <summary>
        /// Get system information relevant to tweaking
        /// </summary>
        Task<SystemInfo> GetSystemInfoAsync();
    }

    /// <summary>
    /// System information
    /// </summary>
    public class SystemInfo
    {
        public string WindowsVersion { get; set; } = string.Empty;
        public string WindowsBuild { get; set; } = string.Empty;
        public string WindowsEdition { get; set; } = string.Empty;
        public bool IsWindows11 { get; set; }
        public bool IsServerOS { get; set; }
        public string Architecture { get; set; } = string.Empty;
        public bool HasTPM { get; set; }
        public bool SecureBootEnabled { get; set; }
        public DateTime LastBootTime { get; set; }
    }
}