using MyTekkiDebloat.Core.Interfaces;
using MyTekkiDebloat.Core.Models;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.Json;

namespace MyTekkiDebloat.Core.Services
{
    /// <summary>
    /// Implementation of tweak execution for Windows systems
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class TweakExecutor : ITweakExecutor
    {
        private readonly ITweakDetector _tweakDetector;
        private readonly string? _originalUserSid;

        public TweakExecutor(ITweakDetector tweakDetector, string? originalUserSid = null)
        {
            _tweakDetector = tweakDetector ?? throw new ArgumentNullException(nameof(tweakDetector));
            _originalUserSid = originalUserSid;
        }

        /// <summary>
        /// Apply a tweak to the system
        /// </summary>
        public async Task<TweakResult> ApplyTweakAsync(Tweak tweak, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if already applied
                var currentStatus = await _tweakDetector.GetTweakStatusAsync(tweak);
                if (currentStatus.IsApplied && currentStatus.CanDetect)
                {
                    return new TweakResult 
                    { 
                        Success = true, 
                        AppliedOperations = { "Tweak is already applied" }
                    };
                }

                var results = new List<string>();

                // Apply registry operations
                var registryOps = tweak.ApplyOperations?.RegistryOperations ?? new List<RegistryOperation>();
                foreach (var regOp in registryOps)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return TweakResult.Failure("Operation cancelled");

                    var result = await ApplyRegistryOperationAsync(regOp);
                    results.Add(result);
                }

                // Apply service operations
                var serviceOps = tweak.ApplyOperations?.ServiceOperations ?? new List<ServiceOperation>();
                foreach (var serviceOp in serviceOps)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return TweakResult.Failure("Operation cancelled");

                    var result = await ApplyServiceOperationAsync(serviceOp);
                    results.Add(result);
                }

                // Apply PowerShell operations
                var psOps = tweak.ApplyOperations?.PowerShellOperations ?? new List<PowerShellOperation>();
                foreach (var psOp in psOps)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return TweakResult.Failure("Operation cancelled");

                    var result = await ApplyPowerShellOperationAsync(psOp);
                    results.Add(result);
                }

                // Apply file operations
                var fileOps = tweak.ApplyOperations?.FileOperations ?? new List<FileOperation>();
                foreach (var fileOp in fileOps)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return TweakResult.Failure("Operation cancelled");

                    var result = await ApplyFileOperationAsync(fileOp);
                    results.Add(result);
                }

                return new TweakResult 
                { 
                    Success = true, 
                    AppliedOperations = results
                };
            }
            catch (Exception ex)
            {
                return TweakResult.Failure(ex.Message, ex);
            }
        }

        /// <summary>
        /// Revert a tweak from the system
        /// </summary>
        public async Task<TweakResult> RevertTweakAsync(Tweak tweak, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!tweak.IsReversible)
                {
                    return TweakResult.Failure("This tweak is not reversible");
                }

                var results = new List<string>();

                // Revert operations in reverse order
                // Revert PowerShell operations with revert scripts
                var psOps = tweak.ApplyOperations?.PowerShellOperations ?? new List<PowerShellOperation>();
                foreach (var psOp in psOps.AsEnumerable().Reverse())
                {
                    if (cancellationToken.IsCancellationRequested)
                        return TweakResult.Failure("Operation cancelled");

                    if (!string.IsNullOrEmpty(psOp.RevertScript))
                    {
                        var revertOp = new PowerShellOperation
                        {
                            Script = psOp.RevertScript,
                            RunAsAdmin = psOp.RunAsAdmin,
                            TimeoutSeconds = psOp.TimeoutSeconds
                        };
                        var result = await ApplyPowerShellOperationAsync(revertOp);
                        results.Add($"Reverted PS: {result}");
                    }
                }

                // Note: Registry and service reverts would need the original values stored
                // This is a simplified implementation
                
                return new TweakResult 
                { 
                    Success = true, 
                    AppliedOperations = { $"Reverted: {string.Join("; ", results)}" }
                };
            }
            catch (Exception ex)
            {
                return TweakResult.Failure(ex.Message, ex);
            }
        }

        /// <summary>
        /// Apply multiple tweaks in batch
        /// </summary>
        public async Task<Dictionary<string, TweakResult>> ApplyTweaksAsync(
            IEnumerable<Tweak> tweaks,
            IProgress<TweakExecutionProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var results = new Dictionary<string, TweakResult>();
            var tweakList = tweaks.ToList();
            var totalTweaks = tweakList.Count;
            var completedTweaks = 0;

            foreach (var tweak in tweakList)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                progress?.Report(new TweakExecutionProgress
                {
                    TotalTweaks = totalTweaks,
                    CompletedTweaks = completedTweaks,
                    CurrentTweakName = tweak.Name,
                    CurrentOperation = "Applying"
                });

                var result = await ApplyTweakAsync(tweak, cancellationToken);
                results[tweak.Id] = result;
                completedTweaks++;

                progress?.Report(new TweakExecutionProgress
                {
                    TotalTweaks = totalTweaks,
                    CompletedTweaks = completedTweaks,
                    CurrentTweakName = tweak.Name,
                    CurrentOperation = result.Success ? "Completed" : "Failed"
                });
            }

            return results;
        }

        /// <summary>
        /// Revert multiple tweaks in batch
        /// </summary>
        public async Task<Dictionary<string, TweakResult>> RevertTweaksAsync(
            IEnumerable<Tweak> tweaks,
            IProgress<TweakExecutionProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var results = new Dictionary<string, TweakResult>();
            var tweakList = tweaks.ToList();
            var totalTweaks = tweakList.Count;
            var completedTweaks = 0;

            foreach (var tweak in tweakList)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                progress?.Report(new TweakExecutionProgress
                {
                    TotalTweaks = totalTweaks,
                    CompletedTweaks = completedTweaks,
                    CurrentTweakName = tweak.Name,
                    CurrentOperation = "Reverting"
                });

                var result = await RevertTweakAsync(tweak, cancellationToken);
                results[tweak.Id] = result;
                completedTweaks++;

                progress?.Report(new TweakExecutionProgress
                {
                    TotalTweaks = totalTweaks,
                    CompletedTweaks = completedTweaks,
                    CurrentTweakName = tweak.Name,
                    CurrentOperation = result.Success ? "Reverted" : "Failed"
                });
            }

            return results;
        }

        #region Private Helper Methods

        private async Task<string> ApplyRegistryOperationAsync(RegistryOperation operation)
        {
            if (!OperatingSystem.IsWindows())
                return "Skipped: Not Windows";

            return await Task.Run(() =>
            {
                try
                {
                    RegistryKey? baseKey;
                    
                    // Handle HKCU impersonation when running as admin for a different user
                    if (operation.Hive == RegistryHive.CurrentUser && !string.IsNullOrEmpty(_originalUserSid))
                    {
                        // Access the original user's registry hive via HKU\{SID}
                        baseKey = Registry.Users.OpenSubKey(_originalUserSid, writable: true);
                        if (baseKey == null)
                        {
                            // If the user's hive isn't loaded, we need to load it first
                            return $"Failed: Cannot access original user's registry hive. User SID: {_originalUserSid}";
                        }
                    }
                    else
                    {
                        baseKey = operation.Hive switch
                        {
                            RegistryHive.CurrentUser => Registry.CurrentUser,
                            RegistryHive.LocalMachine => Registry.LocalMachine,
                            RegistryHive.ClassesRoot => Registry.ClassesRoot,
                            RegistryHive.Users => Registry.Users,
                            RegistryHive.CurrentConfig => Registry.CurrentConfig,
                            _ => Registry.CurrentUser
                        };
                    }

                    using (baseKey)
                    {
                        switch (operation.Operation)
                        {
                            case RegistryOperationType.SetValue:
                            using (var key = baseKey.CreateSubKey(operation.KeyPath))
                            {
                                // Handle JsonElement values from JSON deserialization
                                object? actualValue = operation.Value;
                                if (operation.Value is System.Text.Json.JsonElement jsonElement)
                                {
                                    actualValue = operation.ValueType switch
                                    {
                                        RegistryValueKind.DWord => jsonElement.GetInt32(),
                                        RegistryValueKind.QWord => jsonElement.GetInt64(),
                                        RegistryValueKind.String => jsonElement.GetString(),
                                        RegistryValueKind.ExpandString => jsonElement.GetString(),
                                        RegistryValueKind.MultiString => JsonSerializer.Deserialize<string[]>(jsonElement.GetRawText()) ?? new string[0],
                                        RegistryValueKind.Binary => Convert.FromBase64String(jsonElement.GetString() ?? ""),
                                        _ => jsonElement.GetString()
                                    };
                                }

                                key.SetValue(operation.ValueName, actualValue ?? "", operation.ValueType);
                                return $"Set registry value: {operation.KeyPath}\\{operation.ValueName} = {actualValue}";
                            }

                        case RegistryOperationType.DeleteValue:
                            using (var key = baseKey.OpenSubKey(operation.KeyPath, writable: true))
                            {
                                if (key != null)
                                {
                                    key.DeleteValue(operation.ValueName ?? "", throwOnMissingValue: false);
                                    return $"Deleted registry value: {operation.KeyPath}\\{operation.ValueName}";
                                }
                                return "Registry key not found";
                            }

                        case RegistryOperationType.DeleteKey:
                            baseKey.DeleteSubKeyTree(operation.KeyPath, throwOnMissingSubKey: false);
                            return $"Deleted registry key: {operation.KeyPath}";

                        case RegistryOperationType.CreateKey:
                            using (var key = baseKey.CreateSubKey(operation.KeyPath))
                            {
                                return $"Created registry key: {operation.KeyPath}";
                            }

                            default:
                                return "Unknown registry operation";
                        }
                    }
                }
                catch (Exception ex)
                {
                    return $"Registry error: {ex.Message}";
                }
            });
        }

        private async Task<string> ApplyServiceOperationAsync(ServiceOperation operation)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var process = new Process();
                    process.StartInfo.FileName = "sc.exe";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.CreateNoWindow = true;

                    switch (operation.Operation)
                    {
                        case ServiceOperationType.Stop:
                            process.StartInfo.Arguments = $"stop \"{operation.ServiceName}\"";
                            break;
                        case ServiceOperationType.Start:
                            process.StartInfo.Arguments = $"start \"{operation.ServiceName}\"";
                            break;
                        case ServiceOperationType.Disable:
                            process.StartInfo.Arguments = $"config \"{operation.ServiceName}\" start=disabled";
                            break;
                        case ServiceOperationType.Enable:
                            process.StartInfo.Arguments = $"config \"{operation.ServiceName}\" start=auto";
                            break;
                        case ServiceOperationType.SetStartupType:
                            var startType = operation.StartupType switch
                            {
                                ServiceStartupType.Automatic => "auto",
                                ServiceStartupType.Manual => "demand",
                                ServiceStartupType.Disabled => "disabled",
                                _ => "demand"
                            };
                            process.StartInfo.Arguments = $"config \"{operation.ServiceName}\" start={startType}";
                            break;
                        default:
                            return "Unknown service operation";
                    }

                    process.Start();
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(10000); // 10 second timeout

                    return process.ExitCode == 0 
                        ? $"Service operation successful: {operation.Operation} on {operation.ServiceName}"
                        : $"Service operation failed: {output}";
                }
                catch (Exception ex)
                {
                    return $"Service error: {ex.Message}";
                }
            });
        }

        private async Task<string> ApplyPowerShellOperationAsync(PowerShellOperation operation)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var process = new Process();
                    process.StartInfo.FileName = "powershell.exe";
                    process.StartInfo.Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{operation.Script}\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;

                    if (operation.RunAsAdmin)
                    {
                        process.StartInfo.Verb = "runas";
                        process.StartInfo.UseShellExecute = true;
                        process.StartInfo.RedirectStandardOutput = false;
                        process.StartInfo.RedirectStandardError = false;
                    }

                    process.Start();

                    string output = "";
                    string error = "";

                    if (!operation.RunAsAdmin)
                    {
                        output = process.StandardOutput.ReadToEnd();
                        error = process.StandardError.ReadToEnd();
                    }

                    var timeoutMs = operation.TimeoutSeconds * 1000;
                    process.WaitForExit(timeoutMs);

                    if (!process.HasExited)
                    {
                        process.Kill();
                        return "PowerShell operation timed out";
                    }

                    return process.ExitCode == 0
                        ? $"PowerShell executed successfully: {output}"
                        : $"PowerShell failed: {error}";
                }
                catch (Exception ex)
                {
                    return $"PowerShell error: {ex.Message}";
                }
            });
        }

        private async Task<string> ApplyFileOperationAsync(FileOperation operation)
        {
            return await Task.Run(() =>
            {
                try
                {
                    switch (operation.Operation)
                    {
                        case FileOperationType.Delete:
                            if (File.Exists(operation.Path))
                            {
                                File.Delete(operation.Path);
                                return $"Deleted file: {operation.Path}";
                            }
                            else if (Directory.Exists(operation.Path))
                            {
                                Directory.Delete(operation.Path, recursive: true);
                                return $"Deleted directory: {operation.Path}";
                            }
                            return "File/directory not found";

                        case FileOperationType.CreateFile:
                            if (operation.CreateDirectories)
                            {
                                var dir = Path.GetDirectoryName(operation.Path);
                                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                                {
                                    Directory.CreateDirectory(dir);
                                }
                            }
                            File.WriteAllText(operation.Path, operation.Content ?? "");
                            return $"Created file: {operation.Path}";

                        case FileOperationType.CreateDirectory:
                            Directory.CreateDirectory(operation.Path);
                            return $"Created directory: {operation.Path}";

                        default:
                            return "File operation not implemented";
                    }
                }
                catch (Exception ex)
                {
                    return $"File operation error: {ex.Message}";
                }
            });
        }

        #endregion
    }
}