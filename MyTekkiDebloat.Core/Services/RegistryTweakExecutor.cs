using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Principal;
using Microsoft.Win32;
using MyTekkiDebloat.Core.Interfaces;
using MyTekkiDebloat.Core.Models;

namespace MyTekkiDebloat.Core.Services
{
    /// <summary>
    /// Executes tweaks by applying registry, service, and file operations using pure C#
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class RegistryTweakExecutor : ITweakExecutor
    {
        /// <summary>
        /// Apply a tweak to the system
        /// </summary>
        public async Task<TweakResult> ApplyTweakAsync(Tweak tweak, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new TweakResult();

            try
            {
                // Check for admin privileges if needed
                if (RequiresAdminPrivileges(tweak) && !IsRunningAsAdmin())
                {
                    return TweakResult.Failure("Administrator privileges required for this tweak");
                }

                // Apply registry operations
                foreach (var regOp in tweak.RegistryOperations)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await ApplyRegistryOperationAsync(regOp, result);
                }

                // Apply service operations
                foreach (var serviceOp in tweak.ServiceOperations)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await ApplyServiceOperationAsync(serviceOp, result);
                }

                // Apply file operations
                foreach (var fileOp in tweak.FileOperations)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await ApplyFileOperationAsync(fileOp, result);
                }

                // Apply PowerShell operations (if any)
                foreach (var psOp in tweak.PowerShellOperations)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await ApplyPowerShellOperationAsync(psOp, result);
                }

                result.Success = true;
                result.ExecutionTime = stopwatch.Elapsed;
                result.Messages.Add("Tweak applied successfully");
                return result;
            }
            catch (OperationCanceledException)
            {
                return TweakResult.Failure("Operation was cancelled");
            }
            catch (Exception ex)
            {
                return TweakResult.Failure($"Error applying tweak: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Revert a tweak from the system
        /// </summary>
        public async Task<TweakResult> RevertTweakAsync(Tweak tweak, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new TweakResult();

            try
            {
                // Check for admin privileges if needed
                if (RequiresAdminPrivileges(tweak) && !IsRunningAsAdmin())
                {
                    return TweakResult.Failure("Administrator privileges required to revert this tweak");
                }

                // Revert registry operations in reverse order
                foreach (var regOp in tweak.RegistryOperations.AsEnumerable().Reverse())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await RevertRegistryOperationAsync(regOp, result);
                }

                // Revert service operations
                foreach (var serviceOp in tweak.ServiceOperations.AsEnumerable().Reverse())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await RevertServiceOperationAsync(serviceOp, result);
                }

                // Revert file operations
                foreach (var fileOp in tweak.FileOperations.AsEnumerable().Reverse())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await RevertFileOperationAsync(fileOp, result);
                }

                // Revert PowerShell operations
                foreach (var psOp in tweak.PowerShellOperations.AsEnumerable().Reverse())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await RevertPowerShellOperationAsync(psOp, result);
                }

                result.Success = true;
                result.ExecutionTime = stopwatch.Elapsed;
                result.Messages.Add("Tweak reverted successfully");
            }
            catch (OperationCanceledException)
            {
                return TweakResult.Failure("Revert operation was cancelled");
            }
            catch (Exception ex)
            {
                return TweakResult.Failure($"Error reverting tweak: {ex.Message}", ex);
            }

            return result;
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
            var tweaksList = tweaks.ToList();
            int totalTweaks = tweaksList.Count;
            int completedTweaks = 0;

            foreach (var tweak in tweaksList)
            {
                progress?.Report(new TweakExecutionProgress
                {
                    TotalTweaks = totalTweaks,
                    CompletedTweaks = completedTweaks,
                    CurrentTweakName = tweak.Name,
                    CurrentOperation = "Applying tweak"
                });

                var result = await ApplyTweakAsync(tweak, cancellationToken);
                results[tweak.Id] = result;
                completedTweaks++;

                if (cancellationToken.IsCancellationRequested)
                    break;
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
            var tweaksList = tweaks.ToList();
            int totalTweaks = tweaksList.Count;
            int completedTweaks = 0;

            foreach (var tweak in tweaksList)
            {
                progress?.Report(new TweakExecutionProgress
                {
                    TotalTweaks = totalTweaks,
                    CompletedTweaks = completedTweaks,
                    CurrentTweakName = tweak.Name,
                    CurrentOperation = "Reverting tweak"
                });

                var result = await RevertTweakAsync(tweak, cancellationToken);
                results[tweak.Id] = result;
                completedTweaks++;

                if (cancellationToken.IsCancellationRequested)
                    break;
            }

            return results;
        }

        #region Private Helper Methods

        /// <summary>
        /// Apply a single registry operation
        /// </summary>
        private async Task ApplyRegistryOperationAsync(RegistryOperation operation, TweakResult result)
        {
            await Task.Run(() =>
            {
                try
                {
                    using var baseKey = RegistryKey.OpenBaseKey(operation.Hive, RegistryView.Default);
                    using var key = baseKey.CreateSubKey(operation.KeyPath, true);

                    if (key == null)
                    {
                        throw new InvalidOperationException($"Could not create/open registry key: {operation.KeyPath}");
                    }

                    switch (operation.Operation)
                    {
                        case RegistryOperationType.SetValue:
                            key.SetValue(operation.ValueName ?? "", operation.Value, operation.ValueType);
                            result.AppliedOperations.Add($"Set registry value: {operation.Hive}\\{operation.KeyPath}\\{operation.ValueName} = {operation.Value}");
                            break;

                        case RegistryOperationType.DeleteValue:
                            if (operation.ValueName != null && key.GetValue(operation.ValueName) != null)
                            {
                                key.DeleteValue(operation.ValueName);
                                result.AppliedOperations.Add($"Deleted registry value: {operation.Hive}\\{operation.KeyPath}\\{operation.ValueName}");
                            }
                            break;

                        case RegistryOperationType.DeleteKey:
                            baseKey.DeleteSubKeyTree(operation.KeyPath, false);
                            result.AppliedOperations.Add($"Deleted registry key: {operation.Hive}\\{operation.KeyPath}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Registry operation failed for {operation.KeyPath}: {ex.Message}", ex);
                }
            });
        }

        /// <summary>
        /// Revert a single registry operation
        /// </summary>
        private async Task RevertRegistryOperationAsync(RegistryOperation operation, TweakResult result)
        {
            await Task.Run(() =>
            {
                try
                {
                    using var baseKey = RegistryKey.OpenBaseKey(operation.Hive, RegistryView.Default);

                    switch (operation.Operation)
                    {
                        case RegistryOperationType.SetValue:
                            if (operation.OriginalValue != null)
                            {
                                using var key = baseKey.CreateSubKey(operation.KeyPath, true);
                                key?.SetValue(operation.ValueName ?? "", operation.OriginalValue, operation.ValueType);
                                result.AppliedOperations.Add($"Restored registry value: {operation.Hive}\\{operation.KeyPath}\\{operation.ValueName} = {operation.OriginalValue}");
                            }
                            else if (!operation.ExistedBefore)
                            {
                                using var key = baseKey.OpenSubKey(operation.KeyPath, true);
                                if (key != null && operation.ValueName != null)
                                {
                                    key.DeleteValue(operation.ValueName, false);
                                    result.AppliedOperations.Add($"Removed registry value: {operation.Hive}\\{operation.KeyPath}\\{operation.ValueName}");
                                }
                            }
                            break;

                        case RegistryOperationType.DeleteValue:
                            if (operation.ExistedBefore && operation.OriginalValue != null)
                            {
                                using var key = baseKey.CreateSubKey(operation.KeyPath, true);
                                key?.SetValue(operation.ValueName ?? "", operation.OriginalValue, operation.ValueType);
                                result.AppliedOperations.Add($"Restored deleted registry value: {operation.Hive}\\{operation.KeyPath}\\{operation.ValueName}");
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Registry revert failed for {operation.KeyPath}: {ex.Message}", ex);
                }
            });
        }

        /// <summary>
        /// Apply a service operation (placeholder - will be implemented)
        /// </summary>
        private async Task ApplyServiceOperationAsync(ServiceOperation operation, TweakResult result)
        {
            await Task.Run(() =>
            {
                // TODO: Implement service operations using ServiceController
                result.AppliedOperations.Add($"Service operation: {operation.ServiceName} - {operation.Operation} (not yet implemented)");
            });
        }

        /// <summary>
        /// Revert a service operation (placeholder - will be implemented)
        /// </summary>
        private async Task RevertServiceOperationAsync(ServiceOperation operation, TweakResult result)
        {
            await Task.Run(() =>
            {
                // TODO: Implement service revert operations
                result.AppliedOperations.Add($"Reverted service operation: {operation.ServiceName} (not yet implemented)");
            });
        }

        /// <summary>
        /// Apply a file operation (placeholder - will be implemented)
        /// </summary>
        private async Task ApplyFileOperationAsync(FileOperation operation, TweakResult result)
        {
            await Task.Run(() =>
            {
                // TODO: Implement file operations
                result.AppliedOperations.Add($"File operation: {operation.Path} - {operation.Operation} (not yet implemented)");
            });
        }

        /// <summary>
        /// Revert a file operation (placeholder - will be implemented)
        /// </summary>
        private async Task RevertFileOperationAsync(FileOperation operation, TweakResult result)
        {
            await Task.Run(() =>
            {
                // TODO: Implement file revert operations
                result.AppliedOperations.Add($"Reverted file operation: {operation.Path} (not yet implemented)");
            });
        }

        /// <summary>
        /// Apply a PowerShell operation (placeholder - will be implemented)
        /// </summary>
        private async Task ApplyPowerShellOperationAsync(PowerShellOperation operation, TweakResult result)
        {
            await Task.Run(() =>
            {
                // TODO: Implement PowerShell operations
                result.AppliedOperations.Add($"PowerShell operation: {operation.Script} (not yet implemented)");
            });
        }

        /// <summary>
        /// Revert a PowerShell operation (placeholder - will be implemented)
        /// </summary>
        private async Task RevertPowerShellOperationAsync(PowerShellOperation operation, TweakResult result)
        {
            await Task.Run(() =>
            {
                // TODO: Implement PowerShell revert operations  
                string revertScript = operation.RevertScript ?? "No revert script provided";
                result.AppliedOperations.Add($"Reverted PowerShell operation: {revertScript} (not yet implemented)");
            });
        }

        /// <summary>
        /// Check if the tweak requires admin privileges
        /// </summary>
        private static bool RequiresAdminPrivileges(Tweak tweak)
        {
            return tweak.RegistryOperations.Any(op => op.Hive == RegistryHive.LocalMachine) ||
                   tweak.ServiceOperations.Any() ||
                   tweak.PowerShellOperations.Any();
        }

        /// <summary>
        /// Check if running as administrator
        /// </summary>
        private static bool IsRunningAsAdmin()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        #endregion
    }
}