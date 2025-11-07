using MyTekkiDebloat.Core.Interfaces;
using MyTekkiDebloat.Core.Models;

namespace MyTekkiDebloat.Core.Services
{
    /// <summary>
    /// Implementation of tweak state management
    /// </summary>
    public class TweakStateManager : ITweakStateManager
    {
        private readonly ITweakProvider _tweakProvider;
        private readonly ITweakDetector _tweakDetector;
        private readonly List<PendingTweakChange> _pendingChanges = new();
        private Dictionary<string, TweakStatus> _cachedStatuses = new();
        private DateTime _lastScanTime = DateTime.MinValue;
        private readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(5);

        public TweakStateManager(ITweakProvider tweakProvider, ITweakDetector tweakDetector)
        {
            _tweakProvider = tweakProvider ?? throw new ArgumentNullException(nameof(tweakProvider));
            _tweakDetector = tweakDetector ?? throw new ArgumentNullException(nameof(tweakDetector));
        }

        /// <summary>
        /// Get tweaks with their current system status
        /// </summary>
        public async Task<IEnumerable<TweakStateItem>> GetTweaksWithStatusAsync()
        {
            // Refresh cache if needed
            if (DateTime.Now - _lastScanTime > _cacheTimeout || !_cachedStatuses.Any())
            {
                await RefreshSystemStatusAsync();
            }

            var tweaks = await _tweakProvider.GetTweaksAsync();
            var result = new List<TweakStateItem>();

            foreach (var tweak in tweaks)
            {
                var status = _cachedStatuses.GetValueOrDefault(tweak.Id, new TweakStatus
                {
                    TweakId = tweak.Id,
                    CanDetect = false,
                    IsApplied = false,
                    StatusMessage = "Not scanned"
                });

                var pendingChange = _pendingChanges.FirstOrDefault(p => p.TweakId == tweak.Id);
                
                var stateItem = new TweakStateItem
                {
                    Tweak = tweak,
                    SystemStatus = status,
                    HasPendingChange = pendingChange != null,
                    PendingAction = pendingChange?.Action
                };

                result.Add(stateItem);
            }

            return result;
        }

        /// <summary>
        /// Get pending tweak changes
        /// </summary>
        public async Task<IEnumerable<PendingTweakChange>> GetPendingChangesAsync()
        {
            await Task.CompletedTask; // Make method async for consistency
            return _pendingChanges.ToList(); // Return copy to prevent external modification
        }

        /// <summary>
        /// Add a tweak to pending changes
        /// </summary>
        public async Task<bool> AddPendingChangeAsync(string tweakId, TweakAction action)
        {
            try
            {
                // Remove existing pending change for this tweak
                await RemovePendingChangeAsync(tweakId);

                // Get tweak information
                var tweak = await _tweakProvider.GetTweakByIdAsync(tweakId);
                if (tweak == null)
                    return false;

                // Add new pending change
                var pendingChange = new PendingTweakChange
                {
                    TweakId = tweakId,
                    TweakName = tweak.Name,
                    Action = action,
                    AddedAt = DateTime.Now
                };

                _pendingChanges.Add(pendingChange);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Remove a tweak from pending changes
        /// </summary>
        public async Task<bool> RemovePendingChangeAsync(string tweakId)
        {
            await Task.CompletedTask; // Make method async for consistency
            
            var existingChange = _pendingChanges.FirstOrDefault(p => p.TweakId == tweakId);
            if (existingChange != null)
            {
                _pendingChanges.Remove(existingChange);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Clear all pending changes
        /// </summary>
        public async Task ClearPendingChangesAsync()
        {
            await Task.CompletedTask; // Make method async for consistency
            _pendingChanges.Clear();
        }

        /// <summary>
        /// Refresh system scan for all tweaks
        /// </summary>
        public async Task RefreshSystemStatusAsync()
        {
            try
            {
                var tweaks = await _tweakProvider.GetTweaksAsync();
                var statuses = await _tweakDetector.GetTweaksStatusAsync(tweaks);
                
                _cachedStatuses = statuses;
                _lastScanTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                // Log error but don't throw - fallback to existing cache
                Console.WriteLine($"Failed to refresh system status: {ex.Message}");
            }
        }

        /// <summary>
        /// Get system status for a specific tweak
        /// </summary>
        public async Task<TweakStatus> GetTweakSystemStatusAsync(string tweakId)
        {
            // Check cache first
            if (_cachedStatuses.ContainsKey(tweakId) && 
                DateTime.Now - _lastScanTime < _cacheTimeout)
            {
                return _cachedStatuses[tweakId];
            }

            // Get fresh status for this tweak
            var tweak = await _tweakProvider.GetTweakByIdAsync(tweakId);
            if (tweak == null)
            {
                return new TweakStatus
                {
                    TweakId = tweakId,
                    CanDetect = false,
                    IsApplied = false,
                    StatusMessage = "Tweak not found"
                };
            }

            var status = await _tweakDetector.GetTweakStatusAsync(tweak);
            
            // Update cache
            _cachedStatuses[tweakId] = status;
            
            return status;
        }

        /// <summary>
        /// Determine the action needed for a tweak based on its current state and user selection
        /// </summary>
        public TweakAction DetermineAction(bool isCurrentlyApplied, bool userWantsApplied)
        {
            if (userWantsApplied && !isCurrentlyApplied)
                return TweakAction.Apply;
            else if (!userWantsApplied && isCurrentlyApplied)
                return TweakAction.Revert;
            else
                throw new InvalidOperationException("No action needed - tweak is already in desired state");
        }

        /// <summary>
        /// Check if a tweak needs any action
        /// </summary>
        public bool NeedsAction(bool isCurrentlyApplied, bool userWantsApplied)
        {
            return isCurrentlyApplied != userWantsApplied;
        }
    }
}