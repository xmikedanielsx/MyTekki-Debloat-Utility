using MyTekkiDebloat.Core.Models;

namespace MyTekkiDebloat.Core.Interfaces
{
    /// <summary>
    /// Interface for providing tweaks to the system
    /// </summary>
    public interface ITweakProvider
    {
        /// <summary>
        /// Get all available tweaks
        /// </summary>
        Task<IEnumerable<Tweak>> GetTweaksAsync();

        /// <summary>
        /// Get tweaks by category
        /// </summary>
        Task<IEnumerable<Tweak>> GetTweaksByCategoryAsync(string category);

        /// <summary>
        /// Get a specific tweak by ID
        /// </summary>
        Task<Tweak?> GetTweakByIdAsync(string id);

        /// <summary>
        /// Search tweaks by name, description, or tags
        /// </summary>
        Task<IEnumerable<Tweak>> SearchTweaksAsync(string searchTerm);

        /// <summary>
        /// Get available categories
        /// </summary>
        Task<IEnumerable<string>> GetCategoriesAsync();
    }

    /// <summary>
    /// Interface for executing tweaks on the system
    /// </summary>
    public interface ITweakExecutor
    {
        /// <summary>
        /// Apply a tweak to the system
        /// </summary>
        Task<TweakResult> ApplyTweakAsync(Tweak tweak, CancellationToken cancellationToken = default);

        /// <summary>
        /// Revert a tweak from the system
        /// </summary>
        Task<TweakResult> RevertTweakAsync(Tweak tweak, CancellationToken cancellationToken = default);

        /// <summary>
        /// Apply multiple tweaks in batch
        /// </summary>
        Task<Dictionary<string, TweakResult>> ApplyTweaksAsync(
            IEnumerable<Tweak> tweaks, 
            IProgress<TweakExecutionProgress>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Revert multiple tweaks in batch
        /// </summary>
        Task<Dictionary<string, TweakResult>> RevertTweaksAsync(
            IEnumerable<Tweak> tweaks,
            IProgress<TweakExecutionProgress>? progress = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface for detecting the status of tweaks on the system
    /// </summary>
    public interface ITweakDetector
    {
        /// <summary>
        /// Check if a tweak is currently applied
        /// </summary>
        Task<TweakStatus> GetTweakStatusAsync(Tweak tweak);

        /// <summary>
        /// Check the status of multiple tweaks
        /// </summary>
        Task<Dictionary<string, TweakStatus>> GetTweaksStatusAsync(IEnumerable<Tweak> tweaks);

        /// <summary>
        /// Scan system and return all detected applied tweaks
        /// </summary>
        Task<IEnumerable<TweakStatus>> ScanSystemAsync(IEnumerable<Tweak> knownTweaks);
    }

    /// <summary>
    /// Interface for managing tweak state and changes
    /// </summary>
    public interface ITweakStateManager
    {
        /// <summary>
        /// Get tweaks with their current system status
        /// </summary>
        Task<IEnumerable<TweakStateItem>> GetTweaksWithStatusAsync();

        /// <summary>
        /// Get pending tweak changes
        /// </summary>
        Task<IEnumerable<PendingTweakChange>> GetPendingChangesAsync();

        /// <summary>
        /// Add a tweak to pending changes
        /// </summary>
        Task<bool> AddPendingChangeAsync(string tweakId, TweakAction action);

        /// <summary>
        /// Remove a tweak from pending changes
        /// </summary>
        Task<bool> RemovePendingChangeAsync(string tweakId);

        /// <summary>
        /// Clear all pending changes
        /// </summary>
        Task ClearPendingChangesAsync();

        /// <summary>
        /// Refresh system scan for all tweaks
        /// </summary>
        Task RefreshSystemStatusAsync();

        /// <summary>
        /// Get system status for a specific tweak
        /// </summary>
        Task<TweakStatus> GetTweakSystemStatusAsync(string tweakId);
    }

    /// <summary>
    /// Tweak with system status information
    /// </summary>
    public class TweakStateItem
    {
        public Tweak Tweak { get; set; } = new();
        public TweakStatus SystemStatus { get; set; } = new();
        public bool HasPendingChange { get; set; }
        public TweakAction? PendingAction { get; set; }
    }

    /// <summary>
    /// Represents a pending tweak change
    /// </summary>
    public class PendingTweakChange
    {
        public string TweakId { get; set; } = string.Empty;
        public string TweakName { get; set; } = string.Empty;
        public TweakAction Action { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Action to be performed on a tweak
    /// </summary>
    public enum TweakAction
    {
        Apply,
        Revert
    }

    /// <summary>
    /// Progress information for tweak execution
    /// </summary>
    public class TweakExecutionProgress
    {
        public int TotalTweaks { get; set; }
        public int CompletedTweaks { get; set; }
        public string CurrentTweakName { get; set; } = string.Empty;
        public string CurrentOperation { get; set; } = string.Empty;
        public double PercentComplete => TotalTweaks > 0 ? (double)CompletedTweaks / TotalTweaks * 100 : 0;
    }
}