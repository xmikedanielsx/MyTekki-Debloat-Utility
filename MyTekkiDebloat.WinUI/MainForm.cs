using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Forms;
using MyTekkiDebloat.Core.Interfaces;
using MyTekkiDebloat.Core.Models;
using MyTekkiDebloat.Core.Services;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;
using WinForms = System.Windows.Forms;
using RTControls = ReaLTaiizor.Controls;

namespace MyTekkiDebloat.WinUI
{
    public partial class MainForm : Form
    {
        private readonly IDebloatService _debloatService;
        private readonly string? _originalUserSid;
        
        // Core Controls
        private TabControl _tabControl = null!;
        private TextBox _searchBox = null!;
        private RTControls.Button _scanButton = null!;
        private RTControls.Button _applySelectedButton = null!;
        private Label _statusLabel = null!;
        private ProgressBar _progressBar = null!;
        private RTControls.Panel _statusPanel = null!;
        
        // Split panel design
        private SplitContainer _mainSplitContainer = null!;
        private CheckedListBox _allTweaksListBox = null!;
        private ListBox _tweaksToApplyListBox = null!;
        private ComboBox _presetComboBox = null!;
        private Label _leftPanelLabel = null!;
        private Label _rightPanelLabel = null!;
        
        // Menu bar
        private MenuStrip _menuStrip = null!;
        
        // ReaLTaiizor Theme Manager
        private MetroStyleManager _styleManager = null!;
        
        private TweakStateItem[] _allTweakStates = Array.Empty<TweakStateItem>();

        public MainForm(string? originalUserSid = null)
        {
            _originalUserSid = originalUserSid;
            _debloatService = new DebloatService(originalUserSid: originalUserSid);

            
            InitializeComponent();
            
            // Load tweaks after the form is fully loaded
            Load += MainForm_Load;
        }
        
        private async void MainForm_Load(object? sender, EventArgs e)
        {            
            await LoadTweaksAsync();
            
            // Update title to show admin status
            UpdateTitleWithAdminStatus();
            
            // Set the splitter distance after everything is loaded and UI is stable
            this.BeginInvoke(new Action(() => {
                try
                {
                    if (_mainSplitContainer != null && _mainSplitContainer.Width > _mainSplitContainer.Panel1MinSize + _mainSplitContainer.Panel2MinSize)
                    {
                        int desiredDistance = (int)(_mainSplitContainer.Width * 0.6);
                        int minDistance = _mainSplitContainer.Panel1MinSize;
                        int maxDistance = _mainSplitContainer.Width - _mainSplitContainer.Panel2MinSize;
                        
                        if (desiredDistance >= minDistance && desiredDistance <= maxDistance)
                        {
                            _mainSplitContainer.SplitterDistance = desiredDistance;
                        }
                    }
                }
                catch
                {
                    // Ignore any splitter distance errors - let it use default
                }
            }));
            
            // Perform initial system scan as requested
            await Task.Delay(500); // Small delay to let UI settle
            ScanButton_Click(null, EventArgs.Empty);
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            // Initialize ReaLTaiizor Theme Manager
            SetupReaLTaiizorTheme();

            // Configure form with modern dark styling
            Size = new Size(1400, 900);
            Text = "MyTekki Debloat - Professional Windows Optimization";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1200, 700);
            BackColor = Color.FromArgb(45, 45, 48);
            ForeColor = Color.White;
            
            CreateMenuBar();
            CreateMainControls();
            
            ResumeLayout();
        }

        private void SetupReaLTaiizorTheme()
        {
            // Initialize Metro Style Manager for ReaLTaiizor components
            _styleManager = new MetroStyleManager(this);
            _styleManager.Style = ReaLTaiizor.Enum.Metro.Style.Dark;
        }

        private void CreateMenuBar()
        {
            _menuStrip = new MenuStrip
            {
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                Renderer = new ToolStripProfessionalRenderer(new DarkColorTable())
            };

            // File Menu
            var fileMenu = new ToolStripMenuItem("File");
            fileMenu.DropDownItems.Add("Export Configuration", null, ExportConfig_Click);
            fileMenu.DropDownItems.Add("Import Configuration", null, ImportConfig_Click);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Exit", null, (s, e) => Close());

            // View Menu  
            var viewMenu = new ToolStripMenuItem("View");
            
            // Theme submenu
            var themeMenu = new ToolStripMenuItem("Theme");
            themeMenu.DropDownItems.Add("Dark Theme", null, (s, e) => SetDarkTheme());
            themeMenu.DropDownItems.Add("Light Theme", null, (s, e) => SetLightTheme());
            themeMenu.DropDownItems.Add(new ToolStripSeparator());
            themeMenu.DropDownItems.Add("Blue Accent", null, (s, e) => SetBlueTheme());
            themeMenu.DropDownItems.Add("Green Accent", null, (s, e) => SetGreenTheme());
            themeMenu.DropDownItems.Add("Orange Accent", null, (s, e) => SetOrangeTheme());
            
            viewMenu.DropDownItems.Add(themeMenu);
            viewMenu.DropDownItems.Add(new ToolStripSeparator());
            viewMenu.DropDownItems.Add("Refresh Tweaks", null, async (s, e) => await LoadTweaksAsync());

            // Tools Menu
            var toolsMenu = new ToolStripMenuItem("Tools");
            toolsMenu.DropDownItems.Add("Select All Tweaks", null, SelectAllTweaks_Click);
            toolsMenu.DropDownItems.Add("Deselect All Tweaks", null, DeselectAllTweaks_Click);
            toolsMenu.DropDownItems.Add(new ToolStripSeparator());
            toolsMenu.DropDownItems.Add("System Restore Point", null, CreateRestorePoint_Click);

            // Help Menu
            var helpMenu = new ToolStripMenuItem("Help");
            helpMenu.DropDownItems.Add("About", null, About_Click);
            helpMenu.DropDownItems.Add("GitHub Repository", null, GitHub_Click);
            helpMenu.DropDownItems.Add("Chris Titus Tech", null, ChrisTitus_Click);

            _menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, viewMenu, toolsMenu, helpMenu });
            Controls.Add(_menuStrip);
            MainMenuStrip = _menuStrip;
        }

        private void CreateMainControls()
        {
            // Create main tab control
            _tabControl = new TabControl
            {
                Location = new Point(10, _menuStrip.Height + 10),
                Size = new Size(ClientSize.Width - 20, ClientSize.Height - _menuStrip.Height - 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                Appearance = TabAppearance.FlatButtons,
                DrawMode = TabDrawMode.OwnerDrawFixed
            };
            _tabControl.DrawItem += TabControl_DrawItem;

            CreateMainTweaksTab();
            CreateSystemInfoTab();
            CreateAdvancedTab();
            
            Controls.Add(_tabControl);
        }

        private void CreateMainTweaksTab()
        {
            var mainTab = new WinForms.TabPage("Main Tweaks")
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            // Main container using ReaLTaiizor Panel
            var mainContainer = new RTControls.Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15),
                BackColor = Color.FromArgb(45, 45, 48)
            };

            // Top controls panel
            var topPanel = new RTControls.Panel
            {
                Height = 80,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(45, 45, 48)
            };

            // Preset selection dropdown
            var presetLabel = new Label
            {
                Text = "Load Preset:",
                Location = new Point(0, 10),
                Size = new Size(80, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F)
            };

            _presetComboBox = new ComboBox
            {
                Location = new Point(85, 8),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F)
            };
            _presetComboBox.Items.AddRange(new[] { 
                "-- Import Pre Configuration --", 
                "MyTekki Recommendations",
                "Privacy Focused",
                "Performance Optimized"
            });
            _presetComboBox.SelectedIndexChanged += PresetComboBox_SelectedIndexChanged;

            // Search box
            _searchBox = new TextBox
            {
                Location = new Point(300, 8),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            _searchBox.Text = "Search tweaks...";
            _searchBox.GotFocus += (s, e) => { if (_searchBox.Text == "Search tweaks...") _searchBox.Clear(); };
            _searchBox.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(_searchBox.Text)) _searchBox.Text = "Search tweaks..."; };

            // Buttons
            _scanButton = CreateStyledButton("Scan System", new Point(565, 5), Color.FromArgb(0, 120, 215));
            _scanButton.Size = new Size(100, 30);
            _scanButton.Click += ScanButton_Click;

            _applySelectedButton = CreateStyledButton("Apply Tweaks", new Point(675, 5), Color.FromArgb(16, 124, 16));
            _applySelectedButton.Size = new Size(100, 30);
            _applySelectedButton.Click += ApplySelectedButton_Click;
            _applySelectedButton.Enabled = false;

            topPanel.Controls.AddRange(new Control[] { presetLabel, _presetComboBox, _searchBox, _scanButton, _applySelectedButton });

            // Status panel
            _statusPanel = new RTControls.Panel
            {
                Height = 40,
                Dock = DockStyle.Bottom,
                BackColor = Color.FromArgb(60, 60, 60)
            };

            _statusLabel = new Label
            {
                Location = new Point(15, 10),
                Size = new Size(500, 20),
                Text = "Ready to load tweaks...",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.Transparent
            };

            _progressBar = new ProgressBar
            {
                Location = new Point(525, 12),
                Size = new Size(200, 16),
                Style = ProgressBarStyle.Continuous
            };

            _statusPanel.Controls.AddRange(new Control[] { _statusLabel, _progressBar });

            // Split container for two-panel design
            _mainSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48)
            };

            // Left Panel - All Tweaks
            _leftPanelLabel = new Label
            {
                Text = "Available Tweaks (✓ = Currently Applied)",
                Dock = DockStyle.Top,
                Height = 25,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(35, 35, 38),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };

            _allTweaksListBox = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                BorderStyle = BorderStyle.FixedSingle,
                CheckOnClick = true,
                IntegralHeight = false
            };
            _allTweaksListBox.ItemCheck += AllTweaksListBox_ItemCheck;

            _mainSplitContainer.Panel1.Controls.Add(_allTweaksListBox);
            _mainSplitContainer.Panel1.Controls.Add(_leftPanelLabel);

            // Right Panel - Tweaks To Apply
            _rightPanelLabel = new Label
            {
                Text = "Tweaks To Apply",
                Dock = DockStyle.Top,
                Height = 25,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(35, 35, 38),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };

            _tweaksToApplyListBox = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                BorderStyle = BorderStyle.FixedSingle,
                IntegralHeight = false
            };

            _mainSplitContainer.Panel2.Controls.Add(_tweaksToApplyListBox);
            _mainSplitContainer.Panel2.Controls.Add(_rightPanelLabel);

            // Assign TextChanged event AFTER setting initial text to avoid triggering during initialization
            _searchBox.TextChanged += SearchBox_TextChanged;

            mainContainer.Controls.Add(_mainSplitContainer);
            mainContainer.Controls.Add(_statusPanel);
            mainContainer.Controls.Add(topPanel);
            
            mainTab.Controls.Add(mainContainer);
            _tabControl.TabPages.Add(mainTab);
        }

        private void CreateSystemInfoTab()
        {
            var infoTab = new WinForms.TabPage("System Information")
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            var infoLabel = new Label
            {
                Location = new Point(20, 20),
                Size = new Size(400, 30),
                Text = "System Information and Diagnostics - Coming Soon!",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            
            infoTab.Controls.Add(infoLabel);
            _tabControl.TabPages.Add(infoTab);
        }

        private void CreateAdvancedTab()
        {
            var advancedTab = new WinForms.TabPage("Advanced Settings")
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            var advancedLabel = new Label
            {
                Location = new Point(20, 20),
                Size = new Size(400, 30),
                Text = "Advanced Configuration Options - Coming Soon!",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            
            advancedTab.Controls.Add(advancedLabel);
            _tabControl.TabPages.Add(advancedTab);
        }

        // Helper method to create styled buttons using ReaLTaiizor
        private RTControls.Button CreateStyledButton(string text, Point location, Color backColor)
        {
            return new RTControls.Button
            {
                Text = text,
                Location = location,
                Size = new Size(120, 40),
                BackColor = backColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
        }

        // Custom tab drawing
        private void TabControl_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (sender is not TabControl tabControl) return;

            var tabRect = tabControl.GetTabRect(e.Index);
            var isSelected = e.Index == tabControl.SelectedIndex;
            
            var bgColor = isSelected ? Color.FromArgb(60, 60, 60) : Color.FromArgb(45, 45, 48);
            var textColor = Color.White;

            using (var bgBrush = new SolidBrush(bgColor))
            using (var textBrush = new SolidBrush(textColor))
            {
                e.Graphics.FillRectangle(bgBrush, tabRect);
                
                var textFlags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;
                TextRenderer.DrawText(e.Graphics, tabControl.TabPages[e.Index].Text, 
                    tabControl.Font, tabRect, textColor, textFlags);
            }
        }

        // Event handlers
        private async Task LoadTweaksAsync()
        {
            try
            {
                if (_statusLabel != null)
                    _statusLabel.Text = "Loading tweaks...";
                    
                var tweakStates = await _debloatService.TweakStateManager.GetTweaksWithStatusAsync();
                _allTweakStates = tweakStates.ToArray();
                
                DisplayTweaks(_allTweakStates);
                
                if (_statusLabel != null)
                    _statusLabel.Text = $"Loaded {_allTweakStates.Length} tweaks. Ready to scan or apply.";
                
                // Enable Apply button if we have tweaks (it will be enabled based on tweaks to apply)
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading tweaks: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (_statusLabel != null)
                    _statusLabel.Text = "Error loading tweaks";
            }
        }

        private void DisplayTweaks(TweakStateItem[] tweakStates)
        {
            // Null check - UI might not be initialized yet
            if (_allTweaksListBox == null)
                return;
                
            _allTweaksListBox.Items.Clear();

            // Add tweak states to the all tweaks listbox
            foreach (var tweakState in tweakStates.OrderBy(t => t.Tweak.Category).ThenBy(t => t.Tweak.Name))
            {
                var statusIndicator = tweakState.SystemStatus.CanDetect 
                    ? (tweakState.SystemStatus.IsApplied ? "✓" : "✗") 
                    : "?";
                
                var displayText = $"[{tweakState.Tweak.Category}] {statusIndicator} {tweakState.Tweak.Name} - {tweakState.Tweak.Description}";
                
                var item = new TweakListItem 
                { 
                    TweakState = tweakState,
                    DisplayText = displayText,
                    IsAppliedOnSystem = tweakState.SystemStatus.IsApplied
                };
                
                int index = _allTweaksListBox.Items.Add(item);
                
                // Check the item if it's currently applied on the system
                _allTweaksListBox.SetItemChecked(index, item.IsAppliedOnSystem);
            }

            UpdateTweaksToApplyList();
        }

        private bool CheckIfTweakIsAppliedOnSystem(Tweak tweak)
        {
            // TODO: Implement actual system state detection
            // For now, simulate some tweaks as being applied
            if (tweak.Name.Contains("Dark Mode") || tweak.Name.Contains("Telemetry"))
            {
                return true; // Simulate these as already applied
            }
            return false;
        }

        private void AllTweaksListBox_ItemCheck(object? sender, ItemCheckEventArgs e)
        {
            if (sender is CheckedListBox listBox && e.Index < listBox.Items.Count)
            {
                var item = listBox.Items[e.Index] as TweakListItem;
                if (item != null)
                {
                    // Handle the check/uncheck logic for system state vs user changes
                    HandleTweakStateChange(item, e.NewValue == CheckState.Checked);
                }
            }
            
            // Update button states after the check state changes
            BeginInvoke(new Action(UpdateButtonStates));
        }

        private void PresetComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_presetComboBox.SelectedIndex == 0) // Import Pre Configuration
            {
                ImportPreConfiguration();
            }
            else if (_presetComboBox.SelectedIndex > 0)
            {
                var selectedPreset = _presetComboBox.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(selectedPreset))
                {
                    var result = MessageBox.Show(
                        $"Are you sure you want to load the '{selectedPreset}' preset? This will modify your current selection.",
                        "Load Preset Confirmation",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    
                    if (result == DialogResult.Yes)
                    {
                        LoadPreset(selectedPreset);
                    }
                    else
                    {
                        _presetComboBox.SelectedIndex = -1; // Reset selection
                    }
                }
            }
        }

        private void ImportPreConfiguration()
        {
            using var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Import Pre Configuration",
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // TODO: Implement JSON import logic
                    MessageBox.Show($"Import from: {openFileDialog.FileName}\n\nFeature coming soon!",
                        "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error importing configuration: {ex.Message}",
                        "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            _presetComboBox.SelectedIndex = -1; // Reset selection
        }

        private void LoadPreset(string presetName)
        {
            // TODO: Implement preset loading logic based on preset name
            _statusLabel.Text = $"Loading preset: {presetName}...";
            MessageBox.Show($"Loading preset: {presetName}\n\nFeature coming soon!",
                "Load Preset", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _statusLabel.Text = "Ready";
        }

        private async void HandleTweakStateChange(TweakListItem item, bool isChecked)
        {
            try
            {
                // Debug: Show what's happening
                string debugMessage = $"Tweak: {item.Tweak.Name}\n" +
                                    $"System State: {(item.IsAppliedOnSystem ? "Applied" : "Not Applied")}\n" +
                                    $"User Checked: {isChecked}\n" +
                                    $"Change Needed: {(isChecked != item.IsAppliedOnSystem ? "Yes" : "No")}";
                
                // Uncomment for debugging:
                // MessageBox.Show(debugMessage, "Debug Tweak State", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // Use the TweakStateManager to handle pending changes
                if (isChecked != item.IsAppliedOnSystem)
                {
                    // Determine the action needed
                    var action = isChecked ? TweakAction.Apply : TweakAction.Revert;
                    await _debloatService.TweakStateManager.AddPendingChangeAsync(item.Tweak.Id, action);
                }
                else
                {
                    // No change needed, remove from pending changes
                    await _debloatService.TweakStateManager.RemovePendingChangeAsync(item.Tweak.Id);
                }
                
                // Update the UI to reflect pending changes
                await UpdateTweaksToApplyListAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error handling tweak state change: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateTweaksToApplyList()
        {
            // Synchronous version for backward compatibility
            Task.Run(async () => await UpdateTweaksToApplyListAsync());
        }

        private async Task UpdateTweaksToApplyListAsync()
        {
            if (_tweaksToApplyListBox == null || _allTweaksListBox == null)
                return;

            try
            {
                // Get pending changes from the Core service
                var pendingChanges = await _debloatService.TweakStateManager.GetPendingChangesAsync();

                // Clear and rebuild the tweaks to apply list
                if (_tweaksToApplyListBox.InvokeRequired)
                {
                    _tweaksToApplyListBox.Invoke(() => _tweaksToApplyListBox.Items.Clear());
                }
                else
                {
                    _tweaksToApplyListBox.Items.Clear();
                }

                foreach (var change in pendingChanges)
                {
                    string actionDescription = change.Action == TweakAction.Apply ? "APPLY" : "UNDO";
                    string displayText = $"[{actionDescription}] {change.TweakName}";
                    
                    var applyItem = new TweakToApplyItem 
                    { 
                        Tweak = new Tweak { Id = change.TweakId, Name = change.TweakName }, 
                        DisplayText = displayText, 
                        Action = change.Action.ToString().ToUpper()
                    };

                    if (_tweaksToApplyListBox.InvokeRequired)
                    {
                        _tweaksToApplyListBox.Invoke(() => _tweaksToApplyListBox.Items.Add(applyItem));
                    }
                    else
                    {
                        _tweaksToApplyListBox.Items.Add(applyItem);
                    }
                }

                // Update button states
                if (InvokeRequired)
                {
                    Invoke(() => UpdateButtonStates());
                }
                else
                {
                    UpdateButtonStates();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating tweaks to apply list: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateButtonStates()
        {
            // Null check - UI might not be initialized yet  
            if (_tweaksToApplyListBox == null || _applySelectedButton == null)
                return;
                
            // Enable Apply button only when there are tweaks to apply
            _applySelectedButton.Enabled = _tweaksToApplyListBox.Items.Count > 0;
        }

        private void SearchBox_TextChanged(object? sender, EventArgs e)
        {
            // Null check - UI might not be fully initialized yet
            if (_searchBox == null || _allTweaksListBox == null)
                return;
                
            if (_searchBox.Text == "Search tweaks..." || string.IsNullOrWhiteSpace(_searchBox.Text))
            {
                DisplayTweaks(_allTweakStates);
                return;
            }

            var searchText = _searchBox.Text.ToLower();
            var filtered = _allTweakStates.Where(t => 
                t.Tweak.Name.ToLower().Contains(searchText) ||
                t.Tweak.Description.ToLower().Contains(searchText) ||
                t.Tweak.Category.ToLower().Contains(searchText)
            ).ToArray();
            
            DisplayTweaks(filtered);
        }

        // Button click handlers
        private async void ScanButton_Click(object? sender, EventArgs e)
        {
            try
            {
                _scanButton.Enabled = false;
                _statusLabel.Text = "Scanning system state...";
                _progressBar.Value = 0;
                _progressBar.Visible = true;

                // Re-scan system state for all tweaks using the service
                await _debloatService.TweakStateManager.RefreshSystemStatusAsync();
                
                // Reload tweaks with updated system status
                var updatedTweakStates = await _debloatService.TweakStateManager.GetTweaksWithStatusAsync();
                _allTweakStates = updatedTweakStates.ToArray();
                
                // Refresh the display
                DisplayTweaks(_allTweakStates);
                
                // Update progress
                _progressBar.Value = 100;
                
                var appliedCount = _allTweakStates.Count(t => t.SystemStatus.IsApplied && t.SystemStatus.CanDetect);
                _statusLabel.Text = $"✅ System scan complete. Found {appliedCount} applied tweaks.";
                _progressBar.Value = 100;
                
                // Hide progress bar after a moment
                await Task.Delay(2000);
                _progressBar.Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during scan: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _statusLabel.Text = "❌ Scan failed";
                _progressBar.Visible = false;
            }
            finally
            {
                _scanButton.Enabled = true;
            }
        }

        private async void ApplySelectedButton_Click(object? sender, EventArgs e)
        {
            var tweaksToApply = GetTweaksToApply().ToArray();
            
            if (tweaksToApply.Length == 0)
            {
                MessageBox.Show("No tweaks selected to apply.", "Information", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Show detailed confirmation dialog
            using var confirmDialog = new ConfirmationDialog(tweaksToApply, _originalUserSid);
            if (confirmDialog.ShowDialog(this) == DialogResult.OK && confirmDialog.UserConfirmed)
            {
                await ApplyTweaks(tweaksToApply, "Apply Tweaks");
            }
        }

        // This method is no longer needed as we removed the Apply All button

        private async Task ApplyTweaks(Tweak[] tweaks, string operation)
        {
            if (tweaks.Length == 0)
            {
                return; // This check is now done in the caller
            }

            try
            {
                _applySelectedButton.Enabled = false;
                _statusLabel.Text = "Applying tweaks...";
                _progressBar.Value = 0;
                _progressBar.Visible = true;

                for (int i = 0; i < tweaks.Length; i++)
                {
                    var tweak = tweaks[i];
                    _statusLabel.Text = $"Applying: {tweak.Name}";
                    _progressBar.Value = (i * 100) / tweaks.Length;
                    
                    Application.DoEvents();
                    await Task.Delay(100); // Simulate application time

                    await _debloatService.TweakExecutor.ApplyTweakAsync(tweak);
                }

                _statusLabel.Text = $"✅ Successfully applied {tweaks.Length} tweaks!";
                _progressBar.Value = 100;

                MessageBox.Show($"{operation} completed successfully!\n\n{tweaks.Length} tweaks have been applied.", 
                    "Operation Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Rescan system to update tweak states after applying changes
                _statusLabel.Text = "Rescanning system...";
                Application.DoEvents();
                
                // Re-scan system state for all tweaks using the service
                await _debloatService.TweakStateManager.RefreshSystemStatusAsync();
                
                // Reload tweaks with updated system status
                var updatedTweakStates = await _debloatService.TweakStateManager.GetTweaksWithStatusAsync();
                _allTweakStates = updatedTweakStates.ToArray();
                
                // Refresh the display
                DisplayTweaks(_allTweakStates);
                
                // Clear the tweaks to apply list since they've been applied
                _tweaksToApplyListBox?.Items.Clear();
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying tweaks: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _statusLabel.Text = "❌ Apply operation failed";
            }
            finally
            {
                UpdateButtonStates();
                _progressBar.Visible = false;
            }
        }

        private IEnumerable<Tweak> GetTweaksToApply()
        {
            var result = new List<Tweak>();
            var items = _tweaksToApplyListBox?.Items.Cast<TweakToApplyItem>() ?? Enumerable.Empty<TweakToApplyItem>();
            
            foreach (var item in items)
            {
                // Get the full tweak data from the loaded tweak states
                var tweakState = _allTweakStates.FirstOrDefault(t => t.Tweak.Id == item.Tweak.Id);
                if (tweakState?.Tweak != null)
                {
                    // For REVERT operations, create a tweak that uses UndoOperations as ApplyOperations
                    if (item.Action == "REVERT")
                    {
                        var undoTweak = CreateUndoTweakFromOperations(tweakState.Tweak);
                        result.Add(undoTweak);
                    }
                    else
                    {
                        result.Add(tweakState.Tweak);
                    }
                }
            }
            
            return result;
        }

        private Tweak CreateUndoTweakFromOperations(Tweak originalTweak)
        {
            var undoTweak = new Tweak
            {
                Id = originalTweak.Id + "_UNDO",
                Name = originalTweak.Name + " (UNDO)",
                Description = "Reverting " + originalTweak.Description,
                Category = originalTweak.Category,
                Severity = originalTweak.Severity,
                Tags = originalTweak.Tags,
                IsReversible = originalTweak.IsReversible,
                RequiresRestart = originalTweak.RequiresRestart,
                Source = originalTweak.Source,
                // Use the UndoOperations as the ApplyOperations for this undo tweak
                ApplyOperations = originalTweak.UndoOperations ?? new TweakOperations()
            };

            return undoTweak;
        }

        private void UpdateTitleWithAdminStatus()
        {
            var baseTitle = "MyTekki Debloat - Professional Windows Optimization";
            if (!string.IsNullOrEmpty(_originalUserSid))
            {
                Text = baseTitle + " (Administrator - Running for specific user)";
            }
            else
            {
                Text = baseTitle + " (Administrator)";
            }
        }

        private bool IsRunningAsAdmin()
        {
            try
            {
                using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        // Theme management using ReaLTaiizor
        private void SetDarkTheme()
        {
            _styleManager.Style = ReaLTaiizor.Enum.Metro.Style.Dark;
            
            // Update form colors
            BackColor = Color.FromArgb(45, 45, 48);
            ForeColor = Color.White;
            _tabControl.BackColor = Color.FromArgb(45, 45, 48);
            
            // Update split panel controls
            if (_allTweaksListBox != null)
            {
                _allTweaksListBox.BackColor = Color.FromArgb(50, 50, 50);
                _allTweaksListBox.ForeColor = Color.White;
            }
            if (_tweaksToApplyListBox != null)
            {
                _tweaksToApplyListBox.BackColor = Color.FromArgb(50, 50, 50);
                _tweaksToApplyListBox.ForeColor = Color.White;
            }
            
            // Update ReaLTaiizor panel colors
            _statusPanel.BackColor = Color.FromArgb(60, 60, 60);
            
            Refresh();
        }

        private void SetLightTheme()
        {
            _styleManager.Style = ReaLTaiizor.Enum.Metro.Style.Light;
            
            // Update form colors
            BackColor = Color.White;
            ForeColor = Color.Black;
            _tabControl.BackColor = Color.White;
            
            // Update split panel controls
            if (_allTweaksListBox != null)
            {
                _allTweaksListBox.BackColor = Color.WhiteSmoke;
                _allTweaksListBox.ForeColor = Color.Black;
            }
            if (_tweaksToApplyListBox != null)
            {
                _tweaksToApplyListBox.BackColor = Color.WhiteSmoke;
                _tweaksToApplyListBox.ForeColor = Color.Black;
            }
            
            // Update ReaLTaiizor panel colors
            _statusPanel.BackColor = Color.FromArgb(240, 240, 240);
            
            Refresh();
        }

        private void SetBlueTheme()
        {
            _scanButton.BackColor = Color.FromArgb(0, 120, 215);
            Refresh();
        }

        private void SetGreenTheme()
        {
            _scanButton.BackColor = Color.FromArgb(16, 124, 16);
            Refresh();
        }

        private void SetOrangeTheme()
        {
            _scanButton.BackColor = Color.FromArgb(255, 140, 0);
            Refresh();
        }

        // Menu event handlers
        private void SelectAllTweaks_Click(object? sender, EventArgs e)
        {
            if (_allTweaksListBox != null)
            {
                for (int i = 0; i < _allTweaksListBox.Items.Count; i++)
                {
                    _allTweaksListBox.SetItemChecked(i, true);
                }
                UpdateTweaksToApplyList();
            }
        }

        private void DeselectAllTweaks_Click(object? sender, EventArgs e)
        {
            if (_allTweaksListBox != null)
            {
                for (int i = 0; i < _allTweaksListBox.Items.Count; i++)
                {
                    _allTweaksListBox.SetItemChecked(i, false);
                }
                UpdateTweaksToApplyList();
            }
        }

        private void CreateRestorePoint_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("Create System Restore Point - Coming Soon!", "Feature", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExportConfig_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("Export Configuration - Coming Soon!", "Feature", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ImportConfig_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("Import Configuration - Coming Soon!", "Feature", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void About_Click(object? sender, EventArgs e)
        {
            var aboutText = @"MyTekki Debloat v1.0
Professional Windows Optimization

Built upon the incredible work of Chris Titus Tech
Modern .NET interface with ReaLTaiizor components

© 2025 - MIT License";

            MessageBox.Show(aboutText, "About MyTekki Debloat", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void GitHub_Click(object? sender, EventArgs e)
        {
            OpenUrl("https://github.com/ChrisTitusTech/winutil");
        }

        private void ChrisTitus_Click(object? sender, EventArgs e)
        {
            OpenUrl("https://christitus.com/");
        }

        private void OpenUrl(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
                MessageBox.Show($"Unable to open: {url}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    // Helper class for list items in the main tweaks list
    public class TweakListItem
    {
        public TweakStateItem TweakState { get; set; } = null!;
        public string DisplayText { get; set; } = string.Empty;
        public bool IsAppliedOnSystem { get; set; } = false;

        // Backward compatibility
        public Tweak Tweak => TweakState?.Tweak ?? new Tweak();

        public override string ToString() => DisplayText;
    }

    // Helper class for items in the "tweaks to apply" list
    public class TweakToApplyItem
    {
        public Tweak Tweak { get; set; } = null!;
        public string DisplayText { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // "APPLY" or "REMOVE"

        public override string ToString() => DisplayText;
    }

    // Dark theme for menu
    public class DarkColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected => Color.FromArgb(60, 60, 60);
        public override Color MenuItemBorder => Color.FromArgb(80, 80, 80);
        public override Color MenuBorder => Color.FromArgb(80, 80, 80);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(60, 60, 60);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(60, 60, 60);
        public override Color ToolStripDropDownBackground => Color.FromArgb(45, 45, 48);
    }
}