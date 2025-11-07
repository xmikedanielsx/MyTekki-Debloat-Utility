using Microsoft.Win32;

namespace MyTekkiDebloat.Core.Models
{
    /// <summary>
    /// Represents a registry operation for a tweak
    /// </summary>
    public class RegistryOperation
    {
        /// <summary>
        /// Registry hive (HKEY_CURRENT_USER, HKEY_LOCAL_MACHINE, etc.)
        /// </summary>
        public RegistryHive Hive { get; set; }

        /// <summary>
        /// Registry key path
        /// </summary>
        public string KeyPath { get; set; } = string.Empty;

        /// <summary>
        /// Value name (null for default value)
        /// </summary>
        public string? ValueName { get; set; }

        /// <summary>
        /// Value to set
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// Registry value type
        /// </summary>
        public RegistryValueKind ValueType { get; set; } = RegistryValueKind.DWord;

        /// <summary>
        /// Operation type
        /// </summary>
        public RegistryOperationType Operation { get; set; } = RegistryOperationType.SetValue;

        /// <summary>
        /// Original value for reverting (populated during detection)
        /// </summary>
        public object? OriginalValue { get; set; }

        /// <summary>
        /// Whether the key/value existed before the tweak
        /// </summary>
        public bool ExistedBefore { get; set; }
    }

    public enum RegistryOperationType
    {
        /// <summary>
        /// Set a registry value
        /// </summary>
        SetValue,

        /// <summary>
        /// Delete a registry value
        /// </summary>
        DeleteValue,

        /// <summary>
        /// Delete a registry key
        /// </summary>
        DeleteKey,

        /// <summary>
        /// Create a registry key
        /// </summary>
        CreateKey
    }

    /// <summary>
    /// Represents a Windows service operation for a tweak
    /// </summary>
    public class ServiceOperation
    {
        /// <summary>
        /// Service name
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Desired service state
        /// </summary>
        public ServiceOperationType Operation { get; set; }

        /// <summary>
        /// Desired startup type
        /// </summary>
        public ServiceStartupType? StartupType { get; set; }

        /// <summary>
        /// Original startup type for reverting
        /// </summary>
        public ServiceStartupType? OriginalStartupType { get; set; }
    }

    public enum ServiceOperationType
    {
        Stop,
        Start,
        Disable,
        Enable,
        SetStartupType
    }

    public enum ServiceStartupType
    {
        Automatic = 2,
        Manual = 3,
        Disabled = 4,
        AutomaticDelayed = 2 // Special case, handled differently
    }

    /// <summary>
    /// Represents a file system operation for a tweak
    /// </summary>
    public class FileOperation
    {
        /// <summary>
        /// File or directory path
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Operation to perform
        /// </summary>
        public FileOperationType Operation { get; set; }

        /// <summary>
        /// Content to write (for CreateFile operations)
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// Whether to create parent directories if they don't exist
        /// </summary>
        public bool CreateDirectories { get; set; } = true;

        /// <summary>
        /// Backup path for reverting
        /// </summary>
        public string? BackupPath { get; set; }
    }

    public enum FileOperationType
    {
        Delete,
        CreateFile,
        CreateDirectory,
        Rename,
        SetAttributes,
        TakeOwnership
    }

    /// <summary>
    /// Represents a PowerShell command for complex operations
    /// </summary>
    public class PowerShellOperation
    {
        /// <summary>
        /// PowerShell script to execute
        /// </summary>
        public string Script { get; set; } = string.Empty;

        /// <summary>
        /// Whether to run as administrator
        /// </summary>
        public bool RunAsAdmin { get; set; } = false;

        /// <summary>
        /// Timeout in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Script to run for reverting (optional)
        /// </summary>
        public string? RevertScript { get; set; }
    }
}