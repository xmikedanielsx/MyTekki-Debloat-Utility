using System.Runtime.Versioning;
using Microsoft.Win32;
using MyTekkiDebloat.Core.Interfaces;
using MyTekkiDebloat.Core.Models;

namespace MyTekkiDebloat.Core.Services
{
    /// <summary>
    /// JSON-driven tweak detection service that executes detection rules from JSON
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class JsonDrivenTweakDetector : ITweakDetector
    {
        public async Task<Dictionary<string, TweakStatus>> GetTweaksStatusAsync(IEnumerable<Tweak> tweaks)
        {
            var results = new Dictionary<string, TweakStatus>();

            foreach (var tweak in tweaks)
            {
                results[tweak.Id] = await GetTweakStatusAsync(tweak);
            }

            return results;
        }

        public async Task<IEnumerable<TweakStatus>> ScanSystemAsync(IEnumerable<Tweak> knownTweaks)
        {
            var results = new List<TweakStatus>();

            foreach (var tweak in knownTweaks)
            {
                var status = await GetTweakStatusAsync(tweak);
                results.Add(status);
            }

            return results;
        }

        public async Task<TweakStatus> GetTweakStatusAsync(Tweak tweak)
        {
            var status = new TweakStatus
            {
                TweakId = tweak.Id,
                IsApplied = false,
                CanDetect = false,
                DetectionConfidence = 0.0,
                StatusMessage = "No detection rules defined",
                LastChecked = DateTime.UtcNow
            };

            // If no detection rules are defined, fall back to basic operation analysis
            if (tweak.DetectionRules == null || !tweak.DetectionRules.Rules.Any())
            {
                return await FallbackToOperationAnalysis(tweak, status);
            }

            try
            {
                return await ExecuteDetectionRules(tweak.DetectionRules, status);
            }
            catch (Exception ex)
            {
                // Use fallback behavior from JSON
                var fallback = tweak.DetectionRules.FallbackBehavior;
                status.IsApplied = fallback.IsApplied;
                status.CanDetect = false;
                status.DetectionConfidence = fallback.Confidence;
                status.StatusMessage = $"{fallback.Message}: {ex.Message}";
                return status;
            }
        }

        private async Task<TweakStatus> ExecuteDetectionRules(DetectionRules rules, TweakStatus status)
        {
            var ruleResults = new List<(bool success, double confidence, string message)>();

            foreach (var rule in rules.Rules)
            {
                var result = await ExecuteDetectionRule(rule);
                ruleResults.Add(result);
            }

            // Combine results based on logic
            var combinedResult = CombineRuleResults(ruleResults, rules.Logic, rules.CustomLogic);
            
            status.IsApplied = combinedResult.success;
            status.CanDetect = true;
            status.DetectionConfidence = combinedResult.confidence;
            status.StatusMessage = combinedResult.message;

            return status;
        }

        private async Task<(bool success, double confidence, string message)> ExecuteDetectionRule(DetectionRule rule)
        {
            return rule.Type.ToLower() switch
            {
                "registryvalue" => await ExecuteRegistryValueRule(rule),
                "registrykey" => await ExecuteRegistryKeyRule(rule),
                "servicestate" => await ExecuteServiceRule(rule),
                "fileexists" => await ExecuteFileRule(rule),
                "powershellscript" => await ExecutePowerShellRule(rule),
                _ => (false, 0.0, $"Unknown rule type: {rule.Type}")
            };
        }

        private async Task<(bool success, double confidence, string message)> ExecuteRegistryValueRule(DetectionRule rule)
        {
            await Task.CompletedTask; // Make async

            if (string.IsNullOrEmpty(rule.Hive) || string.IsNullOrEmpty(rule.KeyPath) || string.IsNullOrEmpty(rule.ValueName))
            {
                return (false, 0.0, "Invalid registry rule configuration");
            }

            try
            {
                var hive = rule.Hive.ToLower() switch
                {
                    "currentuser" => Registry.CurrentUser,
                    "localmachine" => Registry.LocalMachine,
                    "hkcu" => Registry.CurrentUser,
                    "hklm" => Registry.LocalMachine,
                    _ => throw new ArgumentException($"Unknown registry hive: {rule.Hive}")
                };

                using var key = hive.OpenSubKey(rule.KeyPath);
                if (key == null)
                {
                    var keyMessage = rule.Inverted ? rule.SuccessMessage : rule.FailureMessage;
                    return (rule.Inverted, rule.Confidence, $"{keyMessage} (Key not found)");
                }

                var actualValue = key.GetValue(rule.ValueName);
                if (actualValue == null)
                {
                    var valueMessage = rule.Inverted ? rule.SuccessMessage : rule.FailureMessage;
                    return (rule.Inverted, rule.Confidence, $"{valueMessage} (Value not found)");
                }

                // Compare values
                bool matches = CompareValues(actualValue, rule.ExpectedValue, rule.ValueType);
                
                // Apply inversion if needed
                bool success = rule.Inverted ? !matches : matches;
                string message = success ? rule.SuccessMessage : rule.FailureMessage;
                
                return (success, rule.Confidence, $"{message} (Actual: {actualValue})");
            }
            catch (Exception ex)
            {
                return (false, 0.0, $"Registry check failed: {ex.Message}");
            }
        }

        private async Task<(bool success, double confidence, string message)> ExecuteRegistryKeyRule(DetectionRule rule)
        {
            await Task.CompletedTask; // Make async

            if (string.IsNullOrEmpty(rule.Hive) || string.IsNullOrEmpty(rule.KeyPath))
            {
                return (false, 0.0, "Invalid registry key rule configuration");
            }

            try
            {
                var hive = rule.Hive.ToLower() switch
                {
                    "currentuser" => Registry.CurrentUser,
                    "localmachine" => Registry.LocalMachine,
                    "hkcu" => Registry.CurrentUser,
                    "hklm" => Registry.LocalMachine,
                    _ => throw new ArgumentException($"Unknown registry hive: {rule.Hive}")
                };

                using var key = hive.OpenSubKey(rule.KeyPath);
                bool keyExists = key != null;
                
                // Apply inversion if needed
                bool success = rule.Inverted ? !keyExists : keyExists;
                string message = success ? rule.SuccessMessage : rule.FailureMessage;
                
                return (success, rule.Confidence, $"{message} (Key exists: {keyExists})");
            }
            catch (Exception ex)
            {
                return (false, 0.0, $"Registry key check failed: {ex.Message}");
            }
        }

        private async Task<(bool success, double confidence, string message)> ExecuteServiceRule(DetectionRule rule)
        {
            // TODO: Implement service state detection
            await Task.CompletedTask;
            return (false, 0.5, "Service detection not yet implemented");
        }

        private async Task<(bool success, double confidence, string message)> ExecuteFileRule(DetectionRule rule)
        {
            // TODO: Implement file existence detection
            await Task.CompletedTask;
            return (false, 0.5, "File detection not yet implemented");
        }

        private async Task<(bool success, double confidence, string message)> ExecutePowerShellRule(DetectionRule rule)
        {
            // TODO: Implement PowerShell script execution
            await Task.CompletedTask;
            return (false, 0.5, "PowerShell detection not yet implemented");
        }

        private bool CompareValues(object? actual, object? expected, string? valueType)
        {
            if (actual == null && expected == null) return true;
            if (actual == null || expected == null) return false;

            // Convert both values to strings for comparison, handling numeric types
            string actualStr = actual.ToString() ?? "";
            string expectedStr = expected.ToString() ?? "";

            // For numeric comparisons
            if (valueType?.ToLower() == "dword" || valueType?.ToLower() == "qword")
            {
                if (long.TryParse(actualStr, out var actualNum) && long.TryParse(expectedStr, out var expectedNum))
                {
                    return actualNum == expectedNum;
                }
            }

            return string.Equals(actualStr, expectedStr, StringComparison.OrdinalIgnoreCase);
        }

        private (bool success, double confidence, string message) CombineRuleResults(
            List<(bool success, double confidence, string message)> results,
            string logic,
            string? customLogic)
        {
            if (!results.Any())
            {
                return (false, 0.0, "No rules to evaluate");
            }

            return logic.ToUpper() switch
            {
                "ALL" => CombineWithAllLogic(results),
                "ANY" => CombineWithAnyLogic(results),
                "CUSTOM" => CombineWithCustomLogic(results, customLogic),
                _ => CombineWithAllLogic(results) // Default to ALL
            };
        }

        private (bool success, double confidence, string message) CombineWithAllLogic(
            List<(bool success, double confidence, string message)> results)
        {
            bool allSuccess = results.All(r => r.success);
            double avgConfidence = results.Average(r => r.confidence);
            var messages = results.Select(r => r.message);
            
            return (allSuccess, avgConfidence, string.Join("; ", messages));
        }

        private (bool success, double confidence, string message) CombineWithAnyLogic(
            List<(bool success, double confidence, string message)> results)
        {
            bool anySuccess = results.Any(r => r.success);
            double maxConfidence = results.Max(r => r.confidence);
            var successMessages = results.Where(r => r.success).Select(r => r.message);
            var failMessages = results.Where(r => !r.success).Select(r => r.message);
            
            string combinedMessage = anySuccess 
                ? string.Join("; ", successMessages)
                : string.Join("; ", failMessages);
            
            return (anySuccess, maxConfidence, combinedMessage);
        }

        private (bool success, double confidence, string message) CombineWithCustomLogic(
            List<(bool success, double confidence, string message)> results,
            string? customLogic)
        {
            // TODO: Implement custom logic parsing and evaluation
            // For now, fall back to ALL logic
            return CombineWithAllLogic(results);
        }

        private async Task<TweakStatus> FallbackToOperationAnalysis(Tweak tweak, TweakStatus status)
        {
            // Fall back to analyzing operations (similar to old logic)
            if (tweak.RegistryOperations.Any())
            {
                status.CanDetect = true;
                status.DetectionConfidence = 0.7;
                status.StatusMessage = "Detection based on registry operations (basic analysis)";
            }
            
            return await Task.FromResult(status);
        }
    }
}