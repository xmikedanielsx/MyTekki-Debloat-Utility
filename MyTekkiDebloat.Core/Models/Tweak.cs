using System.ComponentModel;
using System.Text.Json.Serialization;

namespace MyTekkiDebloat.Core.Models
{
    /// <summary>
    /// Represents a collection of operations for applying or undoing a tweak
    /// </summary>
    public class TweakOperations
    {
        /// <summary>
        /// List of registry operations
        /// </summary>
        public List<RegistryOperation> RegistryOperations { get; set; } = new();

        /// <summary>
        /// List of service operations
        /// </summary>
        public List<ServiceOperation> ServiceOperations { get; set; } = new();

        /// <summary>
        /// List of file operations
        /// </summary>
        public List<FileOperation> FileOperations { get; set; } = new();

        /// <summary>
        /// List of PowerShell operations
        /// </summary>
        public List<PowerShellOperation> PowerShellOperations { get; set; } = new();
    }

    /// <summary>
    /// Represents a Windows system tweak that can be applied or reverted
    /// </summary>
    public class Tweak
    {
        /// <summary>
        /// Unique identifier for the tweak
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Display name for the tweak
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of what this tweak does
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Category this tweak belongs to (Privacy, Performance, etc.)
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Subcategory for more granular organization
        /// </summary>
        public string? SubCategory { get; set; }

        /// <summary>
        /// Severity level: Low, Medium, High, Critical
        /// </summary>
        public TweakSeverity Severity { get; set; } = TweakSeverity.Low;

        /// <summary>
        /// Operations to perform when applying this tweak
        /// </summary>
        public TweakOperations? ApplyOperations { get; set; }

        /// <summary>
        /// Operations to perform when undoing this tweak
        /// </summary>
        public TweakOperations? UndoOperations { get; set; }

        /// <summary>
        /// Whether this tweak requires a system restart
        /// </summary>
        public bool RequiresRestart { get; set; } = false;

        // Legacy properties for backward compatibility - these use the new structure
        [JsonIgnore]
        public List<RegistryOperation> RegistryOperations => ApplyOperations?.RegistryOperations ?? new List<RegistryOperation>();

        [JsonIgnore]
        public List<ServiceOperation> ServiceOperations => ApplyOperations?.ServiceOperations ?? new List<ServiceOperation>();

        [JsonIgnore] 
        public List<FileOperation> FileOperations => ApplyOperations?.FileOperations ?? new List<FileOperation>();

        [JsonIgnore]
        public List<PowerShellOperation> PowerShellOperations => ApplyOperations?.PowerShellOperations ?? new List<PowerShellOperation>();

        /// <summary>
        /// Whether this tweak is reversible
        /// </summary>
        public bool IsReversible { get; set; } = true;

        /// <summary>
        /// Tags for filtering and searching
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// Link to documentation or source
        /// </summary>
        public string? DocumentationUrl { get; set; }

        /// <summary>
        /// Original source (e.g., "ChrisTitusTech", "Custom")
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Version of this tweak definition
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Detection rules for determining if this tweak is currently applied
        /// </summary>
        public List<DetectionRule>? DetectionRules { get; set; }
    }

    public enum TweakSeverity
    {
        [Description("Safe - Low impact changes")]
        Low = 0,
        
        [Description("Moderate - May affect some functionality")]
        Medium = 1,
        
        [Description("High - Significant system changes")]
        High = 2,
        
        [Description("Critical - Expert users only")]
        Critical = 3
    }
}