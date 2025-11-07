namespace MyTekkiDebloat.Core.Models
{
    /// <summary>
    /// Result of a tweak operation
    /// </summary>
    public class TweakResult
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if operation failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Detailed error information
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Operations that were successfully applied
        /// </summary>
        public List<string> AppliedOperations { get; set; } = new();

        /// <summary>
        /// Operations that failed
        /// </summary>
        public List<string> FailedOperations { get; set; } = new();

        /// <summary>
        /// Whether a system restart is required
        /// </summary>
        public bool RequiresRestart { get; set; }

        /// <summary>
        /// Time taken to execute
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }

        /// <summary>
        /// Additional information or warnings
        /// </summary>
        public List<string> Messages { get; set; } = new();

        /// <summary>
        /// Create a successful result
        /// </summary>
        public static TweakResult CreateSuccess(TimeSpan executionTime, bool requiresRestart = false)
        {
            return new TweakResult 
            { 
                Success = true, 
                ExecutionTime = executionTime,
                RequiresRestart = requiresRestart
            };
        }

        /// <summary>
        /// Create a failed result
        /// </summary>
        public static TweakResult Failure(string errorMessage, Exception? exception = null)
        {
            return new TweakResult 
            { 
                Success = false, 
                ErrorMessage = errorMessage,
                Exception = exception
            };
        }
    }

    /// <summary>
    /// Status of a tweak on the current system
    /// </summary>
    public class TweakStatus
    {
        /// <summary>
        /// Tweak identifier
        /// </summary>
        public string TweakId { get; set; } = string.Empty;

        /// <summary>
        /// Whether the tweak is currently applied
        /// </summary>
        public bool IsApplied { get; set; }

        /// <summary>
        /// Whether the tweak can be detected reliably
        /// </summary>
        public bool CanDetect { get; set; } = true;

        /// <summary>
        /// Confidence level of the detection (0.0 to 1.0)
        /// </summary>
        public double DetectionConfidence { get; set; } = 1.0;

        /// <summary>
        /// Additional status information
        /// </summary>
        public string? StatusMessage { get; set; }

        /// <summary>
        /// When this status was last checked
        /// </summary>
        public DateTime LastChecked { get; set; } = DateTime.Now;
    }
}