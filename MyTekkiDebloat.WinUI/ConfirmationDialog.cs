using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text;
using Microsoft.Win32;
using MyTekkiDebloat.Core.Models;
using ReaLTaiizor.Controls;
using RTControls = ReaLTaiizor.Controls;

namespace MyTekkiDebloat.WinUI
{
    public partial class ConfirmationDialog : Form
    {
        private readonly Tweak[] _tweaksToApply;
        private readonly string? _originalUserSid;
        private RTControls.Button _continueButton = null!;
        private RTControls.Button _cancelButton = null!;
        private RichTextBox _commandsRichTextBox = null!;
        private Label _titleLabel = null!;
        private Label _warningLabel = null!;

        public bool UserConfirmed { get; private set; } = false;

        public ConfirmationDialog(Tweak[] tweaks, string? originalUserSid = null)
        {
            _tweaksToApply = tweaks ?? throw new ArgumentNullException(nameof(tweaks));
            _originalUserSid = originalUserSid;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            // Form configuration
            Text = "Confirm System Changes";
            Size = new Size(800, 600);
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(700, 500);
            MaximizeBox = false;
            MinimizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            BackColor = Color.FromArgb(45, 45, 48);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9F);

            CreateControls();
            LayoutControls();
            
            ResumeLayout();
        }

        private void CreateControls()
        {
            // Title label
            _titleLabel = new Label
            {
                Text = $"The application is going to modify your system with {_tweaksToApply.Length} tweaks:",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Warning label
            _warningLabel = new Label
            {
                Text = "⚠️ WARNING: These changes will modify your Windows registry, services, and system files. Make sure you have a system restore point before continuing.",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.Orange,
                AutoSize = false,
                TextAlign = ContentAlignment.TopLeft
            };

            // Commands text box (scrollable)
            _commandsRichTextBox = new RichTextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.LightGray,
                Font = new Font("Consolas", 9F),
                BorderStyle = BorderStyle.FixedSingle,
                Text = GenerateCommandsPreview()
            };

            // Continue button
            _continueButton = new RTControls.Button
            {
                Text = "Execute Commands",
                Size = new Size(150, 35),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            _continueButton.Click += ContinueButton_Click;

            // Cancel button
            _cancelButton = new RTControls.Button
            {
                Text = "Cancel",
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(120, 120, 120),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F)
            };
            _cancelButton.Click += CancelButton_Click;
        }

        private void LayoutControls()
        {
            const int margin = 20;
            const int spacing = 15;
            int currentY = margin;

            // Title
            _titleLabel.Location = new Point(margin, currentY);
            _titleLabel.Size = new Size(ClientSize.Width - 2 * margin, 60);
            Controls.Add(_titleLabel);
            currentY += _titleLabel.Height + spacing;

            // Warning
            _warningLabel.Location = new Point(margin, currentY);
            _warningLabel.Size = new Size(ClientSize.Width - 2 * margin, 50);
            Controls.Add(_warningLabel);
            currentY += _warningLabel.Height + spacing;

            // Commands text box
            _commandsRichTextBox.Location = new Point(margin, currentY);
            _commandsRichTextBox.Size = new Size(ClientSize.Width - 2 * margin, ClientSize.Height - currentY - 80);
            Controls.Add(_commandsRichTextBox);

            // Buttons
            int buttonY = ClientSize.Height - 55;
            _cancelButton.Location = new Point(ClientSize.Width - margin - _cancelButton.Width, buttonY);
            _continueButton.Location = new Point(_cancelButton.Left - spacing - _continueButton.Width, buttonY);
            
            Controls.Add(_cancelButton);
            Controls.Add(_continueButton);
        }

        private string GenerateCommandsPreview()
        {
            var commands = new List<string>();
            
            commands.Add("=== EXECUTABLE COMMANDS ===\n");

            for (int i = 0; i < _tweaksToApply.Length; i++)
            {
                var tweak = _tweaksToApply[i];
                commands.Add($"{i + 1}. {tweak.Name}");
                commands.Add("");
                commands.Add("");

                // Registry Operations - Show actual reg commands
                var registryOps = tweak.ApplyOperations?.RegistryOperations ?? new List<RegistryOperation>();
                if (registryOps.Any())
                {
                    foreach (var regOp in registryOps)
                    {
                        // Handle HKCU impersonation when running as admin
                        var hiveKey = GetRegistryHiveKey(regOp.Hive);
                        
                        var fullKeyPath = $"{hiveKey}\\{regOp.KeyPath}";
                        
                        if (regOp.Operation == RegistryOperationType.SetValue)
                        {
                            var regType = regOp.ValueType.ToString() == "DWord" ? "REG_DWORD" :
                                         regOp.ValueType.ToString() == "String" ? "REG_SZ" :
                                         regOp.ValueType.ToString() == "ExpandString" ? "REG_EXPAND_SZ" :
                                         regOp.ValueType.ToString() == "Binary" ? "REG_BINARY" :
                                         regOp.ValueType.ToString() == "MultiString" ? "REG_MULTI_SZ" : "REG_SZ";
                            
                            // For the new system, the tweak already has the correct operations
                            commands.Add($"   reg add \"{fullKeyPath}\" /v \"{regOp.ValueName}\" /t {regType} /d \"{regOp.Value}\" /f");
                        }
                        else if (regOp.Operation == RegistryOperationType.DeleteValue)
                        {
                            commands.Add($"   reg delete \"{fullKeyPath}\" /v \"{regOp.ValueName}\" /f");
                        }
                        else if (regOp.Operation == RegistryOperationType.DeleteKey)
                        {
                            commands.Add($"   reg delete \"{fullKeyPath}\" /f");
                        }
                    }
                    commands.Add("");
                }

                // Service Operations - Show actual sc commands
                var serviceOps = tweak.ApplyOperations?.ServiceOperations ?? new List<ServiceOperation>();
                if (serviceOps.Any())
                {
                    foreach (var svcOp in serviceOps)
                    {
                        if (svcOp.Operation == ServiceOperationType.Stop)
                        {
                            commands.Add($"   sc stop \"{svcOp.ServiceName}\"");
                        }
                        else if (svcOp.Operation == ServiceOperationType.Start)
                        {
                            commands.Add($"   sc start \"{svcOp.ServiceName}\"");
                        }
                        else if (svcOp.Operation == ServiceOperationType.Disable)
                        {
                            commands.Add($"   sc config \"{svcOp.ServiceName}\" start= disabled");
                        }
                        else if (svcOp.Operation == ServiceOperationType.Enable)
                        {
                            commands.Add($"   sc config \"{svcOp.ServiceName}\" start= auto");
                        }
                        
                        if (svcOp.StartupType.HasValue)
                        {
                            var startType = svcOp.StartupType.Value.ToString().ToLower();
                            if (startType == "automatic") startType = "auto";
                            commands.Add($"   sc config \"{svcOp.ServiceName}\" start= {startType}");
                        }
                    }
                    commands.Add("");
                }

                // File Operations - Show actual file commands
                var fileOps = tweak.ApplyOperations?.FileOperations ?? new List<FileOperation>();
                if (fileOps.Any())
                {
                    foreach (var fileOp in fileOps)
                    {
                        if (fileOp.Operation == FileOperationType.Delete)
                        {
                            commands.Add($"   del /f /q \"{fileOp.Path}\"");
                        }
                        else if (fileOp.Operation == FileOperationType.CreateFile)
                        {
                            commands.Add($"   echo. > \"{fileOp.Path}\"");
                        }
                        else if (fileOp.Operation == FileOperationType.CreateDirectory)
                        {
                            commands.Add($"   mkdir \"{fileOp.Path}\"");
                        }
                        else if (fileOp.Operation == FileOperationType.Rename)
                        {
                            commands.Add($"   ren \"{fileOp.Path}\" \"{fileOp.BackupPath}\"");
                        }
                        else if (fileOp.Operation == FileOperationType.TakeOwnership)
                        {
                            commands.Add($"   takeown /f \"{fileOp.Path}\" /r /d y");
                            commands.Add($"   icacls \"{fileOp.Path}\" /grant administrators:F /t");
                        }
                    }
                    commands.Add("");
                }

                // PowerShell Operations - Show actual PowerShell commands
                var psOps = tweak.ApplyOperations?.PowerShellOperations ?? new List<PowerShellOperation>();
                if (psOps.Any())
                {
                    foreach (var psOp in psOps)
                    {
                        if (psOp.RunAsAdmin)
                        {
                            var escapedScript = psOp.Script.Replace("\"", "'");
                            commands.Add($"   powershell -Command \"Start-Process powershell -ArgumentList '-Command {escapedScript}' -Verb RunAs\"");
                        }
                        else
                        {
                            var escapedScript = psOp.Script.Replace("\"", "'");
                            commands.Add($"   powershell -Command \"{escapedScript}\"");
                        }
                    }
                    commands.Add("");
                }

                if (tweak.RequiresRestart)
                {
                    commands.Add("   ⚠️  This tweak requires a system restart to take effect.");
                    commands.Add("");
                }

                commands.Add("".PadRight(80, '-'));
                commands.Add("");
            }

            commands.Add($"\nTotal operations: {_tweaksToApply.Length} tweaks");
            commands.Add($"Reversible tweaks: {_tweaksToApply.Count(t => t.IsReversible)}");
            commands.Add($"Tweaks requiring restart: {_tweaksToApply.Count(t => t.RequiresRestart)}");

            return string.Join("\n", commands);
        }

        private string GetRegistryHiveKey(RegistryHive hive)
        {
            // When running as Administrator and working with CurrentUser, 
            // we need to use the original user's SID to target their registry hive
            if (hive == RegistryHive.CurrentUser && !string.IsNullOrEmpty(_originalUserSid))
            {
                return $"HKU\\{_originalUserSid}";
            }
            
            return hive.ToString() switch
            {
                "CurrentUser" => "HKCU",
                "LocalMachine" => "HKLM", 
                "ClassesRoot" => "HKCR",
                "Users" => "HKU",
                "CurrentConfig" => "HKCC",
                _ => hive.ToString()
            };
        }

        private string GetRegistryHiveDisplayName(string hive)
        {
            return hive switch
            {
                "CurrentUser" => "HKEY_CURRENT_USER",
                "LocalMachine" => "HKEY_LOCAL_MACHINE",
                "ClassesRoot" => "HKEY_CLASSES_ROOT",
                "Users" => "HKEY_USERS",
                "CurrentConfig" => "HKEY_CURRENT_CONFIG",
                _ => hive
            };
        }

        private void ContinueButton_Click(object? sender, EventArgs e)
        {
            // Since we're already running as admin, just proceed
            UserConfirmed = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void CancelButton_Click(object? sender, EventArgs e)
        {
            UserConfirmed = false;
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}