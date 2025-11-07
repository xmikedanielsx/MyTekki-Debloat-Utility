using System.ComponentModel;
using System.Text.Json.Serialization;

namespace MyTekkiDebloat.Core.Models
{
    /// <summary>
    /// Defines rules for detecting if a tweak is currently applied
    /// </summary>
    public class DetectionRules
    {
        /// <summary>
        /// Type of detection (Registry, Service, File, PowerShell, Composite)
        /// </summary>
        public string Type { get; set; } = "Registry";

        /// <summary>
        /// List of detection rules to evaluate
        /// </summary>
        public List<DetectionRule> Rules { get; set; } = new();

        /// <summary>
        /// Logic for combining multiple rules (ALL, ANY, CUSTOM)
        /// </summary>
        public string Logic { get; set; } = "ALL";

        /// <summary>
        /// Custom logic expression (for CUSTOM logic type)
        /// </summary>
        public string? CustomLogic { get; set; }

        /// <summary>
        /// Behavior when detection fails or cannot be performed
        /// </summary>
        public FallbackBehavior FallbackBehavior { get; set; } = new();
    }

    /// <summary>
    /// Individual detection rule
    /// </summary>
    public class DetectionRule
    {
        /// <summary>
        /// Type of rule (RegistryValue, RegistryKey, ServiceState, FileExists, PowerShellScript)
        /// </summary>
        public string Type { get; set; } = "RegistryValue";

        /// <summary>
        /// Registry hive (for registry rules)
        /// </summary>
        public string? Hive { get; set; }

        /// <summary>
        /// Registry key path (for registry rules)
        /// </summary>
        public string? KeyPath { get; set; }

        /// <summary>
        /// Registry value name (for RegistryValue rules)
        /// </summary>
        public string? ValueName { get; set; }

        /// <summary>
        /// Expected value for the rule to be considered "applied"
        /// </summary>
        public object? ExpectedValue { get; set; }

        /// <summary>
        /// Value type (String, DWord, QWord, Binary, etc.)
        /// </summary>
        public string? ValueType { get; set; }

        /// <summary>
        /// Service name (for service rules)
        /// </summary>
        public string? ServiceName { get; set; }

        /// <summary>
        /// Expected service state (for service rules)
        /// </summary>
        public string? ExpectedServiceState { get; set; }

        /// <summary>
        /// File path (for file rules)
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// PowerShell script to execute (for PowerShell rules)
        /// </summary>
        public string? Script { get; set; }

        /// <summary>
        /// Message to show when this rule indicates the tweak is applied
        /// </summary>
        public string SuccessMessage { get; set; } = "Tweak is applied";

        /// <summary>
        /// Message to show when this rule indicates the tweak is not applied
        /// </summary>
        public string FailureMessage { get; set; } = "Tweak is not applied";

        /// <summary>
        /// Confidence level of this detection rule (0.0 to 1.0)
        /// </summary>
        public double Confidence { get; set; } = 0.8;

        /// <summary>
        /// Weight of this rule when combining with others
        /// </summary>
        public double Weight { get; set; } = 1.0;

        /// <summary>
        /// Whether this rule should be inverted (NOT condition)
        /// </summary>
        public bool Inverted { get; set; } = false;
    }

    /// <summary>
    /// Behavior when detection cannot be performed
    /// </summary>
    public class FallbackBehavior
    {
        /// <summary>
        /// Default assumption about whether the tweak is applied
        /// </summary>
        public bool IsApplied { get; set; } = false;

        /// <summary>
        /// Message to show when falling back
        /// </summary>
        public string Message { get; set; } = "Could not detect tweak status";

        /// <summary>
        /// Confidence level of the fallback (usually low)
        /// </summary>
        public double Confidence { get; set; } = 0.0;
    }
}