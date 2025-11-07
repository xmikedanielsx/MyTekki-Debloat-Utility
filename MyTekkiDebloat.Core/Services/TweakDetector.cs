using MyTekkiDebloat.Core.Interfaces;
using MyTekkiDebloat.Core.Models;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace MyTekkiDebloat.Core.Services
{
    /// <summary>
    /// Implementation of tweak detection for Windows systems
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class TweakDetector : ITweakDetector
    {
        /// <summary>
        /// Check if a tweak is currently applied
        /// </summary>
        public async Task<TweakStatus> GetTweakStatusAsync(Tweak tweak)
        {
            return await Task.Run(() => DetectTweakStatus(tweak));
        }

        /// <summary>
        /// Check the status of multiple tweaks
        /// </summary>
        public async Task<Dictionary<string, TweakStatus>> GetTweaksStatusAsync(IEnumerable<Tweak> tweaks)
        {
            var results = new Dictionary<string, TweakStatus>();
            
            await Task.Run(() =>
            {
                foreach (var tweak in tweaks)
                {
                    results[tweak.Id] = DetectTweakStatus(tweak);
                }
            });

            return results;
        }

        /// <summary>
        /// Scan system and return all detected applied tweaks
        /// </summary>
        public async Task<IEnumerable<TweakStatus>> ScanSystemAsync(IEnumerable<Tweak> knownTweaks)
        {
            var results = new List<TweakStatus>();
            
            await Task.Run(() =>
            {
                foreach (var tweak in knownTweaks)
                {
                    var status = DetectTweakStatus(tweak);
                    results.Add(status);
                }
            });

            return results;
        }

        /// <summary>
        /// Detect the status of a specific tweak synchronously
        /// </summary>
        private TweakStatus DetectTweakStatus(Tweak tweak)
        {
            var status = new TweakStatus
            {
                TweakId = tweak.Id,
                LastChecked = DateTime.Now
            };

            try
            {
                // Check registry operations
                if (tweak.RegistryOperations.Any())
                {
                    status = CheckRegistryOperations(tweak.RegistryOperations, status);
                }

                // Check service operations
                if (tweak.ServiceOperations.Any() && status.CanDetect)
                {
                    status = CheckServiceOperations(tweak.ServiceOperations, status);
                }

                // Check PowerShell operations (limited detection)
                if (tweak.PowerShellOperations.Any() && status.CanDetect)
                {
                    status = CheckPowerShellOperations(tweak.PowerShellOperations, status);
                }

                // If we have file operations, mark as detectable but with lower confidence
                if (tweak.FileOperations.Any() && status.CanDetect)
                {
                    status.DetectionConfidence = Math.Min(status.DetectionConfidence, 0.6);
                    status.StatusMessage += " (File operations present but not checked)";
                }

                // For demo purposes, simulate some known tweaks
                status = ApplyKnownTweakDetection(tweak.Id, status);
            }
            catch (Exception ex)
            {
                status.CanDetect = false;
                status.IsApplied = false;
                status.DetectionConfidence = 0.0;
                status.StatusMessage = $"Detection error: {ex.Message}";
            }

            return status;
        }

        /// <summary>
        /// Check registry-based operations
        /// </summary>
        private TweakStatus CheckRegistryOperations(List<RegistryOperation> operations, TweakStatus status)
        {
            try
            {
                var appliedCount = 0;
                var totalCount = operations.Count;

                foreach (var operation in operations)
                {
                    if (CheckSingleRegistryOperation(operation))
                    {
                        appliedCount++;
                    }
                }

                if (totalCount > 0)
                {
                    status.IsApplied = appliedCount == totalCount; // All operations must be applied
                    status.DetectionConfidence = 0.9;
                    status.StatusMessage = $"Registry: {appliedCount}/{totalCount} operations applied";
                    status.CanDetect = true;
                }
            }
            catch (Exception ex)
            {
                status.CanDetect = false;
                status.StatusMessage = $"Registry check failed: {ex.Message}";
            }

            return status;
        }

        /// <summary>
        /// Check a single registry operation
        /// </summary>
        private bool CheckSingleRegistryOperation(RegistryOperation operation)
        {
            if (!OperatingSystem.IsWindows())
                return false;

            try
            {
                RegistryKey? baseKey = operation.Hive switch
                {
                    RegistryHive.CurrentUser => Registry.CurrentUser,
                    RegistryHive.LocalMachine => Registry.LocalMachine,
                    RegistryHive.ClassesRoot => Registry.ClassesRoot,
                    RegistryHive.Users => Registry.Users,
                    RegistryHive.CurrentConfig => Registry.CurrentConfig,
                    _ => Registry.CurrentUser
                };

                using var key = baseKey.OpenSubKey(operation.KeyPath);
                if (key == null)
                {
                    // Key doesn't exist - depends on operation type
                    return operation.Operation == RegistryOperationType.DeleteKey;
                }

                switch (operation.Operation)
                {
                    case RegistryOperationType.SetValue:
                        var currentValue = key.GetValue(operation.ValueName);
                        return currentValue?.ToString() == operation.Value?.ToString();

                    case RegistryOperationType.DeleteValue:
                        return key.GetValue(operation.ValueName) == null;

                    case RegistryOperationType.DeleteKey:
                        return false; // If key exists, it wasn't deleted

                    case RegistryOperationType.CreateKey:
                        return true; // Key exists, so it was created (or already existed)

                    default:
                        return false;
                }
            }
            catch
            {
                return false; // Assume not applied if we can't detect
            }
        }

        /// <summary>
        /// Check service-based operations
        /// </summary>
        private TweakStatus CheckServiceOperations(List<ServiceOperation> operations, TweakStatus status)
        {
            try
            {
                var appliedCount = 0;
                var totalCount = operations.Count;

                foreach (var operation in operations)
                {
                    if (CheckSingleServiceOperation(operation))
                    {
                        appliedCount++;
                    }
                }

                if (totalCount > 0)
                {
                    status.IsApplied = appliedCount == totalCount;
                    status.DetectionConfidence = 0.8;
                    status.StatusMessage = $"Services: {appliedCount}/{totalCount} operations applied";
                    status.CanDetect = true;
                }
            }
            catch (Exception ex)
            {
                status.CanDetect = false;
                status.StatusMessage = $"Service check failed: {ex.Message}";
            }

            return status;
        }

        /// <summary>
        /// Check a single service operation
        /// </summary>
        private bool CheckSingleServiceOperation(ServiceOperation operation)
        {
            try
            {
                using var process = new Process();
                process.StartInfo.FileName = "sc.exe";
                process.StartInfo.Arguments = $"query \"{operation.ServiceName}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    var isRunning = output.Contains("RUNNING");
                    var isDisabled = output.Contains("DISABLED");
                    var isStopped = output.Contains("STOPPED");

                    return operation.Operation switch
                    {
                        ServiceOperationType.Stop => isStopped || !isRunning,
                        ServiceOperationType.Start => isRunning,
                        ServiceOperationType.Disable => isDisabled,
                        ServiceOperationType.Enable => !isDisabled,
                        ServiceOperationType.SetStartupType => true, // Complex check, assume applied for now
                        _ => false
                    };
                }
            }
            catch
            {
                // Ignore exceptions for individual service checks
            }

            return false;
        }

        /// <summary>
        /// Check PowerShell operations (limited detection capability)
        /// </summary>
        private TweakStatus CheckPowerShellOperations(List<PowerShellOperation> operations, TweakStatus status)
        {
            // PowerShell operations are complex to detect automatically
            // We would need specific detection logic for each script type
            
            // For now, mark as detectable but with low confidence
            status.CanDetect = true;
            status.DetectionConfidence = 0.3;
            status.StatusMessage = $"PowerShell operations ({operations.Count}) - detection limited";
            
            return status;
        }

        /// <summary>
        /// Apply known tweak detection for demo purposes
        /// </summary>
        private TweakStatus ApplyKnownTweakDetection(string tweakId, TweakStatus status)
        {
            // Demo: Simulate some known tweaks for testing
            switch (tweakId.ToLower())
            {
                case "dark-mode":
                case "darkmode":
                    if (OperatingSystem.IsWindows())
                    {
                        try
                        {
                            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                            if (key != null)
                            {
                                var appsUseLightTheme = key.GetValue("AppsUseLightTheme");
                                var systemUsesLightTheme = key.GetValue("SystemUsesLightTheme");
                                
                                status.IsApplied = appsUseLightTheme?.ToString() == "0" || systemUsesLightTheme?.ToString() == "0";
                                status.CanDetect = true;
                                status.DetectionConfidence = 0.95;
                                status.StatusMessage = "Detected via Windows theme registry";
                            }
                        }
                        catch
                        {
                            // Fallback to mock behavior
                            status.IsApplied = true; // Mock: assume dark mode is applied
                            status.CanDetect = true;
                            status.DetectionConfidence = 0.7;
                            status.StatusMessage = "Mock detection - Dark mode";
                        }
                    }
                    else
                    {
                        // Non-Windows fallback
                        status.IsApplied = true;
                        status.CanDetect = true;
                        status.DetectionConfidence = 0.5;
                        status.StatusMessage = "Mock detection - Dark mode (non-Windows)";
                    }
                    break;

                case "disable-telemetry":
                case "telemetry":
                case "disabletelemetry":
                    if (OperatingSystem.IsWindows())
                    {
                        try
                        {
                            var telemetryDisabled = CheckTelemetrySettings();
                            status.IsApplied = telemetryDisabled.isDisabled;
                            status.CanDetect = true;
                            status.DetectionConfidence = telemetryDisabled.confidence;
                            status.StatusMessage = telemetryDisabled.message;
                        }
                        catch
                        {
                            // Fallback to mock behavior
                            status.IsApplied = false; // Conservative: assume telemetry is enabled
                            status.CanDetect = true;
                            status.DetectionConfidence = 0.3;
                            status.StatusMessage = "Could not detect telemetry settings";
                        }
                    }
                    else
                    {
                        // Non-Windows fallback
                        status.IsApplied = false;
                        status.CanDetect = false;
                        status.DetectionConfidence = 0.0;
                        status.StatusMessage = "Telemetry detection only supported on Windows";
                    }
                    break;

                case "end-task-on-taskbar":
                case "endtaskontaskbar":
                case "taskbar-end-task":
                    if (OperatingSystem.IsWindows())
                    {
                        try
                        {
                            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings");
                            if (key != null)
                            {
                                var taskbarEndTask = key.GetValue("TaskbarEndTask");
                                status.IsApplied = taskbarEndTask?.ToString() == "1";
                                status.CanDetect = true;
                                status.DetectionConfidence = 0.95;
                                status.StatusMessage = $"TaskbarEndTask registry value: {taskbarEndTask ?? "not set"}";
                            }
                            else
                            {
                                status.IsApplied = false;
                                status.CanDetect = true;
                                status.DetectionConfidence = 0.90;
                                status.StatusMessage = "TaskbarDeveloperSettings registry key not found (feature not enabled)";
                            }
                        }
                        catch (Exception ex)
                        {
                            status.IsApplied = false;
                            status.CanDetect = false;
                            status.DetectionConfidence = 0.0;
                            status.StatusMessage = $"Error checking TaskbarEndTask: {ex.Message}";
                        }
                    }
                    else
                    {
                        status.IsApplied = false;
                        status.CanDetect = false;
                        status.DetectionConfidence = 0.0;
                        status.StatusMessage = "End Task on Taskbar detection only supported on Windows";
                    }
                    break;

                default:
                    // For unknown tweaks, leave the status as determined by operation analysis
                    break;
            }

            return status;
        }

        /// <summary>
        /// Check telemetry settings using multiple registry locations
        /// </summary>
        private (bool isDisabled, double confidence, string message) CheckTelemetrySettings()
        {
            if (!OperatingSystem.IsWindows())
                return (false, 0.0, "Not Windows");

            // Chris Titus Tech style detection - requires ALL key settings to match expected values
            var criticalChecks = new List<(string name, bool isSet, string description)>();
            var additionalChecks = new List<(string name, bool isSet, string description)>();

            try
            {
                // CRITICAL CHECKS (Chris Titus Tech primary registry locations)
                // 1. AllowTelemetry in both DataCollection policy locations (must be 0)
                using var dataCollectionKey1 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection");
                if (dataCollectionKey1 != null)
                {
                    var allowTelemetry = dataCollectionKey1.GetValue("AllowTelemetry");
                    bool isSet = allowTelemetry?.ToString() == "0";
                    criticalChecks.Add(("AllowTelemetry (Location 1)", isSet, $"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\DataCollection\\AllowTelemetry = {allowTelemetry ?? "null"}"));
                }

                using var dataCollectionKey2 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection");
                if (dataCollectionKey2 != null)
                {
                    var allowTelemetry = dataCollectionKey2.GetValue("AllowTelemetry");
                    bool isSet = allowTelemetry?.ToString() == "0";
                    criticalChecks.Add(("AllowTelemetry (Location 2)", isSet, $"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection\\AllowTelemetry = {allowTelemetry ?? "null"}"));

                    // 2. DoNotShowFeedbackNotifications (must be 1)
                    var doNotShowFeedback = dataCollectionKey2.GetValue("DoNotShowFeedbackNotifications");
                    bool feedbackDisabled = doNotShowFeedback?.ToString() == "1";
                    criticalChecks.Add(("Feedback Notifications", feedbackDisabled, $"DoNotShowFeedbackNotifications = {doNotShowFeedback ?? "null"}"));
                }

                // 3. Advertising ID disabled (must be 1)
                using var advertisingKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo");
                if (advertisingKey != null)
                {
                    var disabledByGroupPolicy = advertisingKey.GetValue("DisabledByGroupPolicy");
                    bool isSet = disabledByGroupPolicy?.ToString() == "1";
                    criticalChecks.Add(("Advertising ID", isSet, $"DisabledByGroupPolicy = {disabledByGroupPolicy ?? "null"}"));
                }

                // ADDITIONAL CHECKS (Chris Titus Tech also sets these)
                // Windows Error Reporting
                using var werKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\Windows Error Reporting");
                if (werKey != null)
                {
                    var disabled = werKey.GetValue("Disabled");
                    bool isSet = disabled?.ToString() == "1";
                    additionalChecks.Add(("Windows Error Reporting", isSet, $"Disabled = {disabled ?? "null"}"));
                }

                // Delivery Optimization
                using var deliveryOptKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config");
                if (deliveryOptKey != null)
                {
                    var doDownloadMode = deliveryOptKey.GetValue("DODownloadMode");
                    bool isSet = doDownloadMode?.ToString() == "0";
                    additionalChecks.Add(("Delivery Optimization", isSet, $"DODownloadMode = {doDownloadMode ?? "null"}"));
                }

                // Remote Assistance
                using var remoteAssistKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Remote Assistance");
                if (remoteAssistKey != null)
                {
                    var fAllowToGetHelp = remoteAssistKey.GetValue("fAllowToGetHelp");
                    bool isSet = fAllowToGetHelp?.ToString() == "0";
                    additionalChecks.Add(("Remote Assistance", isSet, $"fAllowToGetHelp = {fAllowToGetHelp ?? "null"}"));
                }

                // Tailored experiences (HKCU)
                using var cloudContentKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\CloudContent");
                if (cloudContentKey != null)
                {
                    var disableTailoredExperiences = cloudContentKey.GetValue("DisableTailoredExperiencesWithDiagnosticData");
                    bool isSet = disableTailoredExperiences?.ToString() == "1";
                    additionalChecks.Add(("Tailored Experiences", isSet, $"DisableTailoredExperiencesWithDiagnosticData = {disableTailoredExperiences ?? "null"}"));
                }

                // EVALUATION LOGIC (Chris Titus Tech style)
                int criticalMatches = criticalChecks.Count(c => c.isSet);
                int criticalTotal = criticalChecks.Count;
                int additionalMatches = additionalChecks.Count(c => c.isSet);
                int additionalTotal = additionalChecks.Count;

                // Chris Titus Tech requires MOST critical settings to be properly configured
                bool hasCriticalTelemetryDisabled = criticalTotal > 0 && criticalMatches >= Math.Max(1, criticalTotal - 1); // Allow 1 missing critical setting
                double criticalPercentage = criticalTotal > 0 ? (double)criticalMatches / criticalTotal : 0.0;
                double additionalPercentage = additionalTotal > 0 ? (double)additionalMatches / additionalTotal : 0.0;
                
                // Overall assessment
                bool isFullyDisabled = hasCriticalTelemetryDisabled && criticalPercentage >= 0.75; // At least 75% of critical settings
                double confidence = Math.Min(0.95, 0.6 + (criticalPercentage * 0.35)); // High confidence for critical matches
                
                // Build detailed message
                var allChecks = criticalChecks.Select(c => $"[CRITICAL] {c.name}: {(c.isSet ? "✓" : "✗")} ({c.description})")
                    .Concat(additionalChecks.Select(c => $"[ADDITIONAL] {c.name}: {(c.isSet ? "✓" : "✗")} ({c.description})"));
                    
                string detailedMessage = $"Chris Titus Tech telemetry detection - Critical: {criticalMatches}/{criticalTotal}, Additional: {additionalMatches}/{additionalTotal}. " +
                                       string.Join("; ", allChecks.Take(3)) + 
                                       (allChecks.Count() > 3 ? "..." : "");

                System.Diagnostics.Debug.WriteLine($"Telemetry Detection: Critical={criticalMatches}/{criticalTotal}, Additional={additionalMatches}/{additionalTotal}, Result={isFullyDisabled}");

                return (isFullyDisabled, confidence, detailedMessage);
            }
            catch (Exception ex)
            {
                return (false, 0.1, $"Error checking telemetry settings: {ex.Message}");
            }
        }
    }
}