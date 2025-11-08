using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Management;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using MyTekkiDebloat.Core.Interfaces;
using MyTekkiDebloat.Core.Models;
using MyTekkiDebloat.Core.Services;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;
using WinForms = System.Windows.Forms;
using RTControls = ReaLTaiizor.Controls;

namespace MyTekkiDebloat.WinUI
{
    public partial class MainForm : ReaLTaiizor.Forms.MetroForm
    {
        private readonly IDebloatService _debloatService;
        private readonly string? _originalUserSid;
        
        // Core Controls
        private RTControls.MetroTabControl _tabControl = null!;
        private RTControls.MetroTextBox _searchBox = null!;
        private RTControls.Button _scanButton = null!;
        private RTControls.Button _applySelectedButton = null!;
        private Label _statusLabel = null!;
        private ProgressBar _progressBar = null!;
        private RTControls.Panel _statusPanel = null!;
        
        // Main Tweaks Tab panels - need references for theming
        private RTControls.Panel _mainContainer = null!;
        private RTControls.Panel _topPanel = null!;
        private Label _presetLabel = null!;
        
        // Split panel design
        private SplitContainer _mainSplitContainer = null!;
        private CheckedListBox _allTweaksListBox = null!;
        private ListBox _tweaksToApplyListBox = null!;
        private RTControls.MetroComboBox _presetComboBox = null!;
        private Label _leftPanelLabel = null!;
        private Label _rightPanelLabel = null!;
        
        // Menu bar
        private MenuStrip _menuStrip = null!;  // Back to standard MenuStrip
        
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

            // Configure form with modern dark styling and proper MetroForm properties
            Size = new Size(1400, 900);
            Text = GetDynamicWindowTitle(); // Set dynamic title with user info
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1200, 700);
            BackColor = Color.FromArgb(45, 45, 48);
            ForeColor = Color.White;
            
            // Important: Add menu first (bottom in z-order), then tab control
            CreateMenuBar();
            CreateMainControls();
            
            // Apply initial theme to ensure menu colors are correct
            SetDarkTheme();
            
            ResumeLayout();
        }

        private void SetupReaLTaiizorTheme()
        {
            // Initialize Metro Style Manager properly for MetroForm
            _styleManager = new MetroStyleManager(this);
            _styleManager.Style = ReaLTaiizor.Enum.Metro.Style.Dark;
            
            // Configure MetroForm properties for proper title bar and window controls
            this.AllowResize = true;
            this.ShowBorder = true;
            this.ShowHeader = true;
            this.HeaderColor = Color.FromArgb(45, 45, 48); // Dark header to match form
            this.HeaderHeight = 32;
            this.TextColor = Color.FromArgb(180, 180, 180); // Darker grey text for better visibility
            this.ShowLeftRect = false; // No left accent rectangle
            this.ShowTitle = true; // Ensure title is shown
            
            // Add MetroControlBox for window controls (minimize, maximize, close)
            var controlBox = new RTControls.MetroControlBox
            {
                StyleManager = _styleManager,
                Style = ReaLTaiizor.Enum.Metro.Style.Dark,
                MinimizeBox = true,
                MaximizeBox = true,
                DefaultLocation = ReaLTaiizor.Enum.Metro.LocationType.Edge,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Size = new Size(100, 25),
                Visible = true
            };
            Controls.Add(controlBox);
            controlBox.BringToFront(); // Ensure it's on top
        }

        private string GetDynamicWindowTitle()
        {
            try
            {
                string userName = System.Environment.UserName;
                bool isAdmin = IsRunningAsAdministrator();
                string adminText = isAdmin ? "Administrator" : "Standard User";
                
                return $"MyTekki Debloat - Professional Windows Optimization ({adminText} - Running for {userName})";
            }
            catch
            {
                return "MyTekki Debloat - Professional Windows Optimization";
            }
        }

        private bool IsRunningAsAdministrator()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private void CreateMenuBar()
        {
            // Use standard MenuStrip with manual positioning for MetroForm compatibility
            _menuStrip = new MenuStrip
            {
                BackColor = Color.FromArgb(45, 45, 48), // Dark grey background
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                Dock = DockStyle.None, // Manual positioning for MetroForm
                Location = new Point(2, this.HeaderHeight + 1), // Account for MetroForm padding
                Size = new Size(this.ClientSize.Width - 4, 24), // Use ClientSize and account for padding
                Anchor = AnchorStyles.None, // Remove anchor, we'll handle manually
                Renderer = new ToolStripProfessionalRenderer(new DarkMenuColorTable()),
                ImageScalingSize = new Size(0, 0), // Remove image placeholders
                ShowItemToolTips = false // Remove tooltips that might cause white boxes
            };

            // File Menu
            var fileMenu = new ToolStripMenuItem("File") 
            { 
                ForeColor = Color.White,
                DisplayStyle = ToolStripItemDisplayStyle.Text // Text only, no images
            };
            fileMenu.DropDownItems.Add("Export Configuration", null, ExportConfig_Click);
            fileMenu.DropDownItems.Add("Import Configuration", null, ImportConfig_Click);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Exit", null, (s, e) => Close());

            // View Menu  
            var viewMenu = new ToolStripMenuItem("View") 
            { 
                ForeColor = Color.White,
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            
            // Theme submenu
            var themeMenu = new ToolStripMenuItem("Theme") 
            { 
                ForeColor = Color.White,
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            themeMenu.DropDownItems.Add("Dark Theme", null, (s, e) => SetDarkTheme());
            themeMenu.DropDownItems.Add("Light Theme", null, (s, e) => SetLightTheme());
            
            viewMenu.DropDownItems.Add(themeMenu);
            viewMenu.DropDownItems.Add(new ToolStripSeparator());
            viewMenu.DropDownItems.Add("Refresh Tweaks", null, async (s, e) => await LoadTweaksAsync());

            // Tools Menu
            var toolsMenu = new ToolStripMenuItem("Tools") 
            { 
                ForeColor = Color.White,
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            toolsMenu.DropDownItems.Add("Select All Tweaks", null, SelectAllTweaks_Click);
            toolsMenu.DropDownItems.Add("Deselect All Tweaks", null, DeselectAllTweaks_Click);
            toolsMenu.DropDownItems.Add(new ToolStripSeparator());
            toolsMenu.DropDownItems.Add("System Restore Point", null, CreateRestorePoint_Click);

            // Help Menu
            var helpMenu = new ToolStripMenuItem("Help") 
            { 
                ForeColor = Color.White,
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            helpMenu.DropDownItems.Add("About", null, About_Click);
            helpMenu.DropDownItems.Add("GitHub Repository", null, GitHub_Click);
            helpMenu.DropDownItems.Add("Chris Titus Tech", null, ChrisTitus_Click);

            _menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, viewMenu, toolsMenu, helpMenu });
            
            // Set all dropdown items to text only
            SetAllMenuItemsTextOnly(_menuStrip);
            
            Controls.Add(_menuStrip);
            _menuStrip.BringToFront(); // Ensure menu is at the front
            MainMenuStrip = _menuStrip;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            
            // Update menu width when form is resized
            if (_menuStrip != null)
            {
                _menuStrip.Width = this.ClientSize.Width - 4; // Account for MetroForm padding
                _menuStrip.Location = new Point(2, this.HeaderHeight + 1); // Maintain proper position
            }
        }

        private void SetAllMenuItemsTextOnly(MenuStrip menuStrip)
        {
            foreach (ToolStripItem item in menuStrip.Items)
            {
                if (item is ToolStripMenuItem menuItem)
                {
                    SetMenuItemTextOnly(menuItem);
                }
            }
        }

        private void SetMenuItemTextOnly(ToolStripMenuItem menuItem)
        {
            menuItem.DisplayStyle = ToolStripItemDisplayStyle.Text;
            menuItem.Image = null;
            
            // Recursively set for all dropdown items
            foreach (ToolStripItem dropDownItem in menuItem.DropDownItems)
            {
                if (dropDownItem is ToolStripMenuItem subMenuItem)
                {
                    SetMenuItemTextOnly(subMenuItem);
                }
                else if (dropDownItem is ToolStripItem item)
                {
                    item.DisplayStyle = ToolStripItemDisplayStyle.Text;
                    item.Image = null;
                }
            }
        }

        private void CreateMainControls()
        {
            // Create main tab control using ReaLTaiizor
            // Position below the manually positioned menu
            int menuBottom = this.HeaderHeight + 25; // Header + menu height + small margin
            
            _tabControl = new RTControls.MetroTabControl
            {
                Location = new Point(10, menuBottom),
                Size = new Size(this.ClientSize.Width - 20, this.ClientSize.Height - menuBottom - 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular)
            };

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
            _mainContainer = new RTControls.Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15),
                BackColor = Color.FromArgb(45, 45, 48)
            };

            // Top controls panel
            _topPanel = new RTControls.Panel
            {
                Height = 80,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(45, 45, 48)
            };

            // Preset selection dropdown
            _presetLabel = new Label
            {
                Text = "Load Preset:",
                Location = new Point(0, 10),
                Size = new Size(80, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F)
            };

            _presetComboBox = new RTControls.MetroComboBox
            {
                Location = new Point(85, 8),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
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
            _searchBox = new RTControls.MetroTextBox
            {
                Location = new Point(300, 8),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 9F),
                Text = "Search tweaks..."
            };
            _searchBox.GotFocus += (s, e) => { if (_searchBox.Text == "Search tweaks...") _searchBox.Text = ""; };
            _searchBox.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(_searchBox.Text)) _searchBox.Text = "Search tweaks..."; };

            // Buttons
            _scanButton = CreateStyledButton("Scan System", new Point(565, 5), Color.FromArgb(0, 120, 215));
            _scanButton.Size = new Size(100, 30);
            _scanButton.Click += ScanButton_Click;

            _applySelectedButton = CreateStyledButton("Apply Tweaks", new Point(675, 5), Color.FromArgb(16, 124, 16));
            _applySelectedButton.Size = new Size(100, 30);
            _applySelectedButton.Click += ApplySelectedButton_Click;
            _applySelectedButton.Enabled = false;

            _topPanel.Controls.AddRange(new Control[] { _presetLabel, _presetComboBox, _searchBox, _scanButton, _applySelectedButton });

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

            _mainContainer.Controls.Add(_mainSplitContainer);
            _mainContainer.Controls.Add(_statusPanel);
            _mainContainer.Controls.Add(_topPanel);
            
            mainTab.Controls.Add(_mainContainer);
            _tabControl.TabPages.Add(mainTab);
        }

        private void CreateSystemInfoTab()
        {
            var infoTab = new WinForms.TabPage("System Information")
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            // Create the main container for system info
            var infoContainer = new RTControls.Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                Padding = new Padding(10)
            };

            // Title
            var titleLabel = new Label
            {
                Text = "💻 System Information & Hardware Details",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 174, 219),
                AutoSize = true,
                Location = new Point(10, 10)
            };
            infoContainer.Controls.Add(titleLabel);

            // Create CPU-Z style sub-tabs using MetroTabControl
            var systemTabControl = new RTControls.MetroTabControl
            {
                Location = new Point(10, 45),
                Size = new Size(800, 500),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Style = ReaLTaiizor.Enum.Metro.Style.Dark,
                DrawMode = TabDrawMode.OwnerDrawFixed
            };

            // Create CPU-Z style tabs
            CreateCPUTab(systemTabControl);
            CreateMotherboardTab(systemTabControl);
            CreateMemoryTab(systemTabControl);
            CreateGraphicsTab(systemTabControl);

            infoContainer.Controls.Add(systemTabControl);
            infoTab.Controls.Add(infoContainer);
            _tabControl.TabPages.Add(infoTab);
        }

        private void CreateCPUTab(RTControls.MetroTabControl tabControl)
        {
            var cpuTab = new WinForms.TabPage("CPU")
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            var cpuPanel = new RTControls.Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                Padding = new Padding(15)
            };

            var cpuInfo = GetProcessorInfo();
            int yPos = 10;

            // CPU Section Header
            var cpuHeaderLabel = new Label
            {
                Text = "Processor",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 200, 255),
                Location = new Point(15, yPos),
                AutoSize = true
            };
            cpuPanel.Controls.Add(cpuHeaderLabel);
            yPos += 30;

            // Create CPU info display similar to CPU-Z
            foreach (var info in cpuInfo)
            {
                yPos = CreateInfoRow(cpuPanel, info.key, info.value, yPos, 150);
            }

            yPos += 20;

            // Clocks Section
            var clocksHeaderLabel = new Label
            {
                Text = "Clocks",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 200, 255),
                Location = new Point(15, yPos),
                AutoSize = true
            };
            cpuPanel.Controls.Add(clocksHeaderLabel);
            yPos += 30;

            // Add clock information
            var clockInfo = GetClockInfo();
            foreach (var info in clockInfo)
            {
                yPos = CreateInfoRow(cpuPanel, info.key, info.value, yPos, 150);
            }

            yPos += 20;

            // Cache Section
            var cacheHeaderLabel = new Label
            {
                Text = "Cache",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 200, 255),
                Location = new Point(15, yPos),
                AutoSize = true
            };
            cpuPanel.Controls.Add(cacheHeaderLabel);
            yPos += 30;

            var cacheInfo = GetCacheInfo();
            foreach (var info in cacheInfo)
            {
                yPos = CreateInfoRow(cpuPanel, info.key, info.value, yPos, 150);
            }

            cpuTab.Controls.Add(cpuPanel);
            tabControl.TabPages.Add(cpuTab);
        }

        private void CreateMotherboardTab(RTControls.MetroTabControl tabControl)
        {
            var motherboardTab = new WinForms.TabPage("Motherboard")
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            var motherboardPanel = new RTControls.Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                Padding = new Padding(15)
            };

            var motherboardInfo = GetMotherboardInfo();
            int yPos = 10;

            // Motherboard Section Header
            var motherboardHeaderLabel = new Label
            {
                Text = "Motherboard",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 200, 255),
                Location = new Point(15, yPos),
                AutoSize = true
            };
            motherboardPanel.Controls.Add(motherboardHeaderLabel);
            yPos += 30;

            foreach (var info in motherboardInfo)
            {
                yPos = CreateInfoRow(motherboardPanel, info.key, info.value, yPos, 150);
            }

            yPos += 20;

            // BIOS Section
            var biosHeaderLabel = new Label
            {
                Text = "BIOS",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 200, 255),
                Location = new Point(15, yPos),
                AutoSize = true
            };
            motherboardPanel.Controls.Add(biosHeaderLabel);
            yPos += 30;

            var biosInfo = GetBIOSInfo();
            foreach (var info in biosInfo)
            {
                yPos = CreateInfoRow(motherboardPanel, info.key, info.value, yPos, 150);
            }

            motherboardTab.Controls.Add(motherboardPanel);
            tabControl.TabPages.Add(motherboardTab);
        }

        private void CreateMemoryTab(RTControls.MetroTabControl tabControl)
        {
            var memoryTab = new WinForms.TabPage("Memory")
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            var memoryPanel = new RTControls.Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                Padding = new Padding(15)
            };

            int yPos = 10;

            // General Memory Information
            var memoryHeaderLabel = new Label
            {
                Text = "General",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 200, 255),
                Location = new Point(15, yPos),
                AutoSize = true
            };
            memoryPanel.Controls.Add(memoryHeaderLabel);
            yPos += 30;

            var memoryInfo = GetMemoryInfo();
            foreach (var info in memoryInfo)
            {
                yPos = CreateInfoRow(memoryPanel, info.key, info.value, yPos, 150);
            }

            yPos += 20;

            // Memory Slot Selection (like SPD tab)
            var slotHeaderLabel = new Label
            {
                Text = "Memory Slot Selection",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 200, 255),
                Location = new Point(15, yPos),
                AutoSize = true
            };
            memoryPanel.Controls.Add(slotHeaderLabel);
            yPos += 30;

            // Create dropdown for memory slots
            var slotComboBox = new RTControls.MetroComboBox
            {
                Location = new Point(15, yPos),
                Size = new Size(200, 23),
                Style = ReaLTaiizor.Enum.Metro.Style.Dark
            };

            // Add memory slots
            var memorySlots = GetMemorySlots();
            foreach (var slot in memorySlots)
            {
                slotComboBox.Items.Add(slot);
            }

            if (slotComboBox.Items.Count > 0)
                slotComboBox.SelectedIndex = 0;

            memoryPanel.Controls.Add(slotComboBox);
            yPos += 40;

            // Memory details panel that updates based on selection
            var memoryDetailsPanel = new RTControls.Panel
            {
                Location = new Point(15, yPos),
                Size = new Size(750, 200),
                BackColor = Color.FromArgb(50, 50, 53)
            };

            // Event handler to update memory details when selection changes
            slotComboBox.SelectedIndexChanged += (s, e) =>
            {
                UpdateMemorySlotDetails(memoryDetailsPanel, slotComboBox.SelectedIndex);
            };

            memoryPanel.Controls.Add(memoryDetailsPanel);

            // Initialize with first slot
            if (slotComboBox.Items.Count > 0)
            {
                UpdateMemorySlotDetails(memoryDetailsPanel, 0);
            }

            memoryTab.Controls.Add(memoryPanel);
            tabControl.TabPages.Add(memoryTab);
        }

        private void CreateGraphicsTab(RTControls.MetroTabControl tabControl)
        {
            var graphicsTab = new WinForms.TabPage("Graphics")
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            var graphicsPanel = new RTControls.Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                Padding = new Padding(15)
            };

            int yPos = 10;

            // Display Device Selection Header
            var deviceHeaderLabel = new Label
            {
                Text = "Display Device Selection",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 200, 255),
                Location = new Point(15, yPos),
                AutoSize = true
            };
            graphicsPanel.Controls.Add(deviceHeaderLabel);
            yPos += 30;

            // Create dropdown for graphics adapters
            var gpuComboBox = new RTControls.MetroComboBox
            {
                Location = new Point(15, yPos),
                Size = new Size(400, 23),
                Style = ReaLTaiizor.Enum.Metro.Style.Dark
            };

            // Get all graphics adapters and prioritize dedicated GPUs
            var graphicsAdapters = GetGraphicsAdapters();
            int defaultIndex = 0;
            
            for (int i = 0; i < graphicsAdapters.Count; i++)
            {
                gpuComboBox.Items.Add(graphicsAdapters[i].displayName);
                
                // Set dedicated GPU as default (look for NVIDIA, AMD, Intel Arc)
                if (graphicsAdapters[i].isDedicated)
                {
                    defaultIndex = i;
                }
            }

            if (gpuComboBox.Items.Count > 0)
                gpuComboBox.SelectedIndex = defaultIndex;

            graphicsPanel.Controls.Add(gpuComboBox);
            yPos += 40;

            // Graphics details panel that updates based on selection
            var gpuDetailsPanel = new RTControls.Panel
            {
                Location = new Point(15, yPos),
                Size = new Size(750, 300),
                BackColor = Color.FromArgb(50, 50, 53)
            };

            // Event handler to update GPU details when selection changes
            gpuComboBox.SelectedIndexChanged += (s, e) =>
            {
                UpdateGPUDetails(gpuDetailsPanel, gpuComboBox.SelectedIndex, graphicsAdapters);
            };

            graphicsPanel.Controls.Add(gpuDetailsPanel);

            // Initialize with selected GPU
            if (gpuComboBox.Items.Count > 0)
            {
                UpdateGPUDetails(gpuDetailsPanel, defaultIndex, graphicsAdapters);
            }

            graphicsTab.Controls.Add(graphicsPanel);
            tabControl.TabPages.Add(graphicsTab);
        }

        private int CreateInfoRow(Control parent, string label, string value, int yPos, int labelWidth)
        {
            var keyLabel = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.White,
                Location = new Point(15, yPos),
                Size = new Size(labelWidth, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };
            parent.Controls.Add(keyLabel);

            var valueLabel = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(220, 220, 220),
                Location = new Point(labelWidth + 20, yPos),
                Size = new Size(400, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };
            parent.Controls.Add(valueLabel);

            return yPos + 25;
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
            // Update MetroStyleManager for dark theme (handles title bar automatically)
            _styleManager.Style = ReaLTaiizor.Enum.Metro.Style.Dark;
            
            // Update form colors
            BackColor = Color.FromArgb(45, 45, 48);
            ForeColor = Color.White;
            
            // Update standard MenuStrip for dark theme
            if (_menuStrip != null)
            {
                _menuStrip.BackColor = Color.FromArgb(45, 45, 48);
                _menuStrip.ForeColor = Color.White;
                _menuStrip.Renderer = new ToolStripProfessionalRenderer(new DarkMenuColorTable());
                
                // Update menu item colors
                foreach (ToolStripMenuItem item in _menuStrip.Items)
                {
                    item.ForeColor = Color.White;
                    UpdateMenuItemColors(item, Color.White);
                }
            }
            
            // Update tab pages background for dark theme
            if (_tabControl != null)
            {
                foreach (TabPage tabPage in _tabControl.TabPages)
                {
                    tabPage.BackColor = Color.FromArgb(45, 45, 48);
                    tabPage.ForeColor = Color.White;
                }
            }
            
            // Update ReaLTaiizor panels for dark theme
            if (_mainContainer != null)
            {
                _mainContainer.BackColor = Color.FromArgb(45, 45, 48);
            }
            if (_topPanel != null)
            {
                _topPanel.BackColor = Color.FromArgb(45, 45, 48);
            }
            if (_statusPanel != null)
            {
                _statusPanel.BackColor = Color.FromArgb(60, 60, 60);
            }
            
            // Update main container controls
            if (_presetLabel != null)
            {
                _presetLabel.ForeColor = Color.White;
            }
            // ReaLTaiizor controls handle their own theming via StyleManager
            // _presetComboBox and _searchBox colors managed by ReaLTaiizor
            if (_statusLabel != null)
            {
                _statusLabel.ForeColor = Color.White;
            }
            
            // Update ReaLTaiizor buttons for dark theme
            if (_scanButton != null)
            {
                _scanButton.BackColor = Color.FromArgb(0, 120, 215);
                _scanButton.ForeColor = Color.White;
            }
            if (_applySelectedButton != null)
            {
                _applySelectedButton.BackColor = Color.FromArgb(16, 124, 16);
                _applySelectedButton.ForeColor = Color.White;
            }
            
            if (_leftPanelLabel != null)
            {
                _leftPanelLabel.BackColor = Color.FromArgb(35, 35, 38);
                _leftPanelLabel.ForeColor = Color.White;
            }
            if (_rightPanelLabel != null)
            {
                _rightPanelLabel.BackColor = Color.FromArgb(35, 35, 38);
                _rightPanelLabel.ForeColor = Color.White;
            }
            if (_mainSplitContainer != null)
            {
                _mainSplitContainer.BackColor = Color.FromArgb(45, 45, 48);
            }
            
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
            if (_statusPanel != null)
            {
                _statusPanel.BackColor = Color.FromArgb(60, 60, 60);
            }
            
            Refresh();
        }

        private void SetLightTheme()
        {
            // Update MetroStyleManager for light theme (handles title bar automatically)
            _styleManager.Style = ReaLTaiizor.Enum.Metro.Style.Light;
            
            // Update form colors
            BackColor = Color.White;
            ForeColor = Color.Black;
            
            // Update standard MenuStrip for light theme
            if (_menuStrip != null)
            {
                _menuStrip.BackColor = Color.FromArgb(240, 240, 240);
                _menuStrip.ForeColor = Color.Black;
                _menuStrip.Renderer = new ToolStripProfessionalRenderer(new LightMenuColorTable());
                
                // Update menu item colors
                foreach (ToolStripMenuItem item in _menuStrip.Items)
                {
                    item.ForeColor = Color.Black;
                    UpdateMenuItemColors(item, Color.Black);
                }
            }
            
            // Update tab pages background for light theme
            if (_tabControl != null)
            {
                foreach (TabPage tabPage in _tabControl.TabPages)
                {
                    tabPage.BackColor = Color.White;
                    tabPage.ForeColor = Color.Black;
                }
            }
            
            // Update ReaLTaiizor panels for light theme
            if (_mainContainer != null)
            {
                _mainContainer.BackColor = Color.White;
            }
            if (_topPanel != null)
            {
                _topPanel.BackColor = Color.White;
            }
            if (_statusPanel != null)
            {
                _statusPanel.BackColor = Color.FromArgb(240, 240, 240);
            }
            
            // Update main container controls
            if (_presetLabel != null)
            {
                _presetLabel.ForeColor = Color.Black;
            }
            // ReaLTaiizor controls handle their own theming via StyleManager
            // _presetComboBox and _searchBox colors managed by ReaLTaiizor
            if (_statusLabel != null)
            {
                _statusLabel.ForeColor = Color.Black;
            }
            
            // Update ReaLTaiizor buttons for light theme
            if (_scanButton != null)
            {
                _scanButton.BackColor = Color.FromArgb(0, 120, 215);
                _scanButton.ForeColor = Color.White;
            }
            if (_applySelectedButton != null)
            {
                _applySelectedButton.BackColor = Color.FromArgb(16, 124, 16);
                _applySelectedButton.ForeColor = Color.White;
            }
            
            if (_leftPanelLabel != null)
            {
                _leftPanelLabel.BackColor = Color.FromArgb(230, 230, 230);
                _leftPanelLabel.ForeColor = Color.Black;
            }
            if (_rightPanelLabel != null)
            {
                _rightPanelLabel.BackColor = Color.FromArgb(230, 230, 230);
                _rightPanelLabel.ForeColor = Color.Black;
            }
            if (_mainSplitContainer != null)
            {
                _mainSplitContainer.BackColor = Color.White;
            }
            
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
            if (_statusPanel != null)
            {
                _statusPanel.BackColor = Color.FromArgb(240, 240, 240);
            }
            
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
            ShowThemedAboutDialog();
        }

        private void ShowThemedAboutDialog()
        {
            // Create a themed about dialog using standard Form with dark theming
            using var aboutForm = new Form
            {
                Text = "About MyTekki Debloat",
                Size = new Size(500, 350),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.None, // Remove title bar
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            // Create content panel
            var contentPanel = new Panel
            {
                BackColor = Color.FromArgb(45, 45, 48),
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BorderStyle = BorderStyle.FixedSingle // Add subtle border since no title bar
            };

            // Make the form draggable by clicking and dragging the content panel
            bool isDragging = false;
            Point lastCursor = Point.Empty;
            Point lastForm = Point.Empty;
            
            contentPanel.MouseDown += (s, e) => {
                isDragging = true;
                lastCursor = Cursor.Position;
                lastForm = aboutForm.Location;
            };
            
            contentPanel.MouseMove += (s, e) => {
                if (isDragging)
                {
                    var currentCursor = Cursor.Position;
                    var offset = new Point(currentCursor.X - lastCursor.X, currentCursor.Y - lastCursor.Y);
                    aboutForm.Location = new Point(lastForm.X + offset.X, lastForm.Y + offset.Y);
                }
            };
            
            contentPanel.MouseUp += (s, e) => {
                isDragging = false;
            };

            // Title label
            var titleLabel = new Label
            {
                Text = "MyTekki Debloat Utility v1.0",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 174, 219), // Metro blue
                AutoSize = true,
                Location = new Point(20, 20)
            };

            // Subtitle label
            var subtitleLabel = new Label
            {
                Text = "Professional Windows Optimization",
                Font = new Font("Segoe UI", 12F, FontStyle.Regular),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 55)
            };

            // Description label
            var descriptionLabel = new Label
            {
                Text = "Built to enhance the amazing work done by Chris Titus Tech\nusing Modern .NET and C# with ReaLTaiizor components",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(200, 200, 200),
                AutoSize = false,
                Size = new Size(440, 50),
                Location = new Point(20, 90)
            };

            // Special thanks label
            var thanksLabel = new Label
            {
                Text = "💝 Special thanks from the original author (Mike Daniels)\nto his two lovely kids Maksim and Melanie Daniels 💖✨",
                Font = new Font("Segoe UI", 10F, FontStyle.Italic),
                ForeColor = Color.FromArgb(255, 182, 193), // Light pink
                AutoSize = false,
                Size = new Size(440, 50),
                Location = new Point(20, 160),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Copyright label
            var copyrightLabel = new Label
            {
                Text = "© 2025 - MIT License",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(150, 150, 150),
                AutoSize = true,
                Location = new Point(20, 250)
            };

            // OK button - use standard WinForms Button
            var okButton = new Button
            {
                Text = "OK",
                Size = new Size(80, 30),
                Location = new Point(400, 290),
                UseVisualStyleBackColor = false,
                BackColor = Color.FromArgb(0, 174, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            okButton.FlatAppearance.BorderSize = 0;
            okButton.Click += (s, e) => aboutForm.Close();

            // Add controls to content panel
            contentPanel.Controls.Add(titleLabel);
            contentPanel.Controls.Add(subtitleLabel);
            contentPanel.Controls.Add(descriptionLabel);
            contentPanel.Controls.Add(thanksLabel);
            contentPanel.Controls.Add(copyrightLabel);
            contentPanel.Controls.Add(okButton);

            // Add content panel to form
            aboutForm.Controls.Add(contentPanel);

            // Show dialog
            aboutForm.ShowDialog(this);
        }

        private void GitHub_Click(object? sender, EventArgs e)
        {
            OpenUrl("https://github.com/xmikedanielsx/MyTekki-Debloat-Utility");
        }

        private void ChrisTitus_Click(object? sender, EventArgs e)
        {
            OpenUrl("https://github.com/ChrisTitusTech/winutil");
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

        // Helper method for updating menu item colors
        private void UpdateMenuItemColors(ToolStripMenuItem item, Color color)
        {
            item.ForeColor = color;
            foreach (ToolStripItem subItem in item.DropDownItems)
            {
                if (subItem is ToolStripMenuItem menuItem)
                {
                    UpdateMenuItemColors(menuItem, color);
                }
                else
                {
                    subItem.ForeColor = color;
                }
            }
        }

        // System Information Gathering Methods

        private List<(string key, string value)> GetClockInfo()
        {
            var info = new List<(string key, string value)>();
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                foreach (ManagementObject obj in searcher.Get())
                {
                    info.Add(("Core Speed", $"{obj["CurrentClockSpeed"]} MHz"));
                    info.Add(("Multiplier", $"x{(Convert.ToDouble(obj["CurrentClockSpeed"]) / 100):F1}"));
                    info.Add(("Bus Speed", "100.0 MHz")); // Typical modern bus speed
                    break;
                }
            }
            catch (Exception ex)
            {
                info.Add(("Error", $"Unable to retrieve clock information: {ex.Message}"));
            }
            return info;
        }

        private List<(string key, string value)> GetCacheInfo()
        {
            var info = new List<(string key, string value)>();
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var l1Cache = obj["L1InstructionCacheSize"] ?? 0;
                    var l2Cache = obj["L2CacheSize"] ?? 0;
                    var l3Cache = obj["L3CacheSize"] ?? 0;
                    
                    info.Add(("L1 Data", $"{l1Cache} KBytes"));
                    info.Add(("L1 Inst.", $"{l1Cache} KBytes"));
                    info.Add(("Level 2", $"{l2Cache} KBytes"));
                    info.Add(("Level 3", $"{l3Cache} KBytes"));
                    break;
                }
            }
            catch (Exception ex)
            {
                info.Add(("Error", $"Unable to retrieve cache information: {ex.Message}"));
            }
            return info;
        }

        private List<(string key, string value)> GetMotherboardInfo()
        {
            var info = new List<(string key, string value)>();
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
                foreach (ManagementObject obj in searcher.Get())
                {
                    info.Add(("Manufacturer", obj["Manufacturer"]?.ToString() ?? "Unknown"));
                    info.Add(("Model", obj["Product"]?.ToString() ?? "Unknown"));
                    info.Add(("Revision", obj["Version"]?.ToString() ?? "Unknown"));
                    break;
                }

                using var busSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_SystemBus");
                foreach (ManagementObject obj in busSearcher.Get())
                {
                    info.Add(("Bus Specs.", obj["Name"]?.ToString() ?? "PCI-Express"));
                    break;
                }

                info.Add(("Chipset", "Intel/AMD Chipset")); // Generic placeholder
                info.Add(("Southbridge", "Intel/AMD Southbridge")); // Generic placeholder
            }
            catch (Exception ex)
            {
                info.Add(("Error", $"Unable to retrieve motherboard information: {ex.Message}"));
            }
            return info;
        }

        private List<(string key, string value)> GetBIOSInfo()
        {
            var info = new List<(string key, string value)>();
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS");
                foreach (ManagementObject obj in searcher.Get())
                {
                    info.Add(("Brand", obj["Manufacturer"]?.ToString() ?? "Unknown"));
                    info.Add(("Version", obj["SMBIOSBIOSVersion"]?.ToString() ?? "Unknown"));
                    info.Add(("Date", obj["ReleaseDate"]?.ToString()?.Substring(0, 8) ?? "Unknown"));
                    break;
                }
            }
            catch (Exception ex)
            {
                info.Add(("Error", $"Unable to retrieve BIOS information: {ex.Message}"));
            }
            return info;
        }

        private List<string> GetMemorySlots()
        {
            var slots = new List<string>();
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
                int slotNumber = 1;
                foreach (ManagementObject obj in searcher.Get())
                {
                    var capacity = Convert.ToUInt64(obj["Capacity"]) / (1024 * 1024 * 1024);
                    var manufacturer = obj["Manufacturer"]?.ToString() ?? "Unknown";
                    slots.Add($"Slot #{slotNumber} - {capacity} GB {manufacturer}");
                    slotNumber++;
                }

                if (slots.Count == 0)
                {
                    slots.Add("No memory modules detected");
                }
            }
            catch (Exception ex)
            {
                slots.Add($"Error: {ex.Message}");
            }
            return slots;
        }

        private void UpdateMemorySlotDetails(RTControls.Panel panel, int slotIndex)
        {
            panel.Controls.Clear();
            
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
                var memories = searcher.Get().Cast<ManagementObject>().ToArray();
                
                if (slotIndex >= 0 && slotIndex < memories.Length)
                {
                    var memory = memories[slotIndex];
                    int yPos = 10;

                    // Module details
                    yPos = CreateInfoRow(panel, "Module Size", $"{Convert.ToUInt64(memory["Capacity"]) / (1024 * 1024 * 1024)} GB", yPos, 120);
                    yPos = CreateInfoRow(panel, "Module Type", memory["MemoryType"]?.ToString() == "26" ? "DDR4" : "DDR3/Other", yPos, 120);
                    yPos = CreateInfoRow(panel, "Module Manuf.", memory["Manufacturer"]?.ToString() ?? "Unknown", yPos, 120);
                    yPos = CreateInfoRow(panel, "DRAM Manuf.", memory["Manufacturer"]?.ToString() ?? "Unknown", yPos, 120);
                    yPos = CreateInfoRow(panel, "Part Number", memory["PartNumber"]?.ToString()?.Trim() ?? "Unknown", yPos, 120);
                    yPos = CreateInfoRow(panel, "Serial Number", memory["SerialNumber"]?.ToString() ?? "Unknown", yPos, 120);
                    yPos = CreateInfoRow(panel, "Week/Year", "Unknown", yPos, 120);
                    yPos = CreateInfoRow(panel, "Correction", "None", yPos, 120);
                }
            }
            catch (Exception ex)
            {
                CreateInfoRow(panel, "Error", $"Unable to load slot details: {ex.Message}", 10, 120);
            }
        }

        private List<(string key, string value)> GetGraphicsInfo()
        {
            var info = new List<(string key, string value)>();
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
                foreach (ManagementObject obj in searcher.Get())
                {
                    if (obj["Name"]?.ToString()?.Contains("Microsoft") == false) // Skip basic Microsoft adapters
                    {
                        info.Add(("Name", obj["Name"]?.ToString() ?? "Unknown"));
                        info.Add(("Board Manuf.", obj["AdapterCompatibility"]?.ToString() ?? "Unknown"));
                        
                        var adapterRAM = obj["AdapterRAM"];
                        if (adapterRAM != null && UInt32.TryParse(adapterRAM.ToString(), out uint ramBytes))
                        {
                            info.Add(("Memory Size", $"{ramBytes / (1024 * 1024)} MB"));
                        }
                        else
                        {
                            info.Add(("Memory Size", "Unknown"));
                        }
                        
                        info.Add(("Memory Type", "GDDR6")); // Generic placeholder
                        info.Add(("GPU", obj["VideoProcessor"]?.ToString() ?? "Unknown"));
                        info.Add(("Technology", "8 nm")); // Generic placeholder
                        info.Add(("Memory", $"{obj["AdapterRAM"] ?? 0} MB"));
                        info.Add(("Cores", "Unknown"));
                        info.Add(("Bus Width", "Unknown"));
                        break; // Just get the first real GPU
                    }
                }
                
                if (info.Count == 0)
                {
                    info.Add(("Graphics", "No discrete graphics card detected"));
                }
            }
            catch (Exception ex)
            {
                info.Add(("Error", $"Unable to retrieve graphics information: {ex.Message}"));
            }
            return info;
        }

        private List<(string displayName, bool isDedicated, ManagementObject adapter)> GetGraphicsAdapters()
        {
            var adapters = new List<(string displayName, bool isDedicated, ManagementObject adapter)>();
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var name = obj["Name"]?.ToString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        // Determine if it's a dedicated GPU
                        bool isDedicated = IsDedicatedGPU(name);
                        
                        // Create display name with some basic info
                        var displayName = name;
                        var adapterRAM = obj["AdapterRAM"];
                        if (adapterRAM != null && UInt32.TryParse(adapterRAM.ToString(), out uint ramBytes) && ramBytes > 0)
                        {
                            displayName += $" ({ramBytes / (1024 * 1024)} MB)";
                        }
                        
                        adapters.Add((displayName, isDedicated, obj));
                    }
                }
                
                // Sort so dedicated GPUs come first
                adapters = adapters.OrderByDescending(a => a.isDedicated).ToList();
                
                if (adapters.Count == 0)
                {
                    adapters.Add(("No graphics adapters found", false, null!));
                }
            }
            catch (Exception ex)
            {
                adapters.Add(($"Error loading graphics adapters: {ex.Message}", false, null!));
            }
            
            return adapters;
        }

        private bool IsDedicatedGPU(string gpuName)
        {
            if (string.IsNullOrEmpty(gpuName))
                return false;
                
            var name = gpuName.ToUpperInvariant();
            
            // Check for dedicated GPU indicators
            return name.Contains("NVIDIA") || 
                   name.Contains("GEFORCE") || 
                   name.Contains("QUADRO") || 
                   name.Contains("TESLA") ||
                   name.Contains("AMD") || 
                   name.Contains("RADEON") || 
                   name.Contains("RX ") || 
                   name.Contains("VEGA") ||
                   name.Contains("INTEL ARC") ||
                   (name.Contains("INTEL") && (name.Contains("ARC") || name.Contains("DG"))) ||
                   // Exclude integrated graphics
                   (!name.Contains("MICROSOFT") && 
                    !name.Contains("BASIC") && 
                    !name.Contains("STANDARD") &&
                    !name.Contains("VGA") &&
                    !name.Contains("UHD GRAPHICS") &&
                    !name.Contains("HD GRAPHICS") &&
                    !name.Contains("IRIS XE"));
        }

        private void UpdateGPUDetails(RTControls.Panel panel, int selectedIndex, List<(string displayName, bool isDedicated, ManagementObject adapter)> adapters)
        {
            panel.Controls.Clear();
            
            if (selectedIndex < 0 || selectedIndex >= adapters.Count || adapters[selectedIndex].adapter == null)
            {
                CreateInfoRow(panel, "Error", "No graphics adapter selected or adapter data unavailable", 10, 120);
                return;
            }
            
            try
            {
                var adapter = adapters[selectedIndex].adapter;
                int yPos = 10;

                // GPU Section
                var gpuHeaderLabel = new Label
                {
                    Text = "GPU",
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(100, 200, 255),
                    Location = new Point(15, yPos),
                    AutoSize = true
                };
                panel.Controls.Add(gpuHeaderLabel);
                yPos += 25;

                // GPU Details
                yPos = CreateInfoRow(panel, "Name", adapter["Name"]?.ToString() ?? "Unknown", yPos, 120);
                yPos = CreateInfoRow(panel, "Board Manuf.", adapter["AdapterCompatibility"]?.ToString() ?? "Unknown", yPos, 120);
                
                // Code Name (try to determine from GPU name)
                var codeName = GetGPUCodeName(adapter["Name"]?.ToString());
                yPos = CreateInfoRow(panel, "Code Name", codeName, yPos, 120);
                
                yPos = CreateInfoRow(panel, "Revision", adapter["DriverVersion"]?.ToString() ?? "Unknown", yPos, 120);
                yPos = CreateInfoRow(panel, "Technology", GetGPUTechnology(adapter["Name"]?.ToString()), yPos, 120);
                yPos = CreateInfoRow(panel, "Die Size", "Unknown", yPos, 120);

                yPos += 15;

                // Memory Section
                var memoryHeaderLabel = new Label
                {
                    Text = "Memory",
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(100, 200, 255),
                    Location = new Point(15, yPos),
                    AutoSize = true
                };
                panel.Controls.Add(memoryHeaderLabel);
                yPos += 25;

                var adapterRAM = adapter["AdapterRAM"];
                var memorySize = "Unknown";
                if (adapterRAM != null && UInt32.TryParse(adapterRAM.ToString(), out uint ramBytes) && ramBytes > 0)
                {
                    memorySize = $"{ramBytes / (1024 * 1024)} MB";
                }

                yPos = CreateInfoRow(panel, "Size", memorySize, yPos, 120);
                yPos = CreateInfoRow(panel, "Type", GetGPUMemoryType(adapter["Name"]?.ToString()), yPos, 120);
                yPos = CreateInfoRow(panel, "Bus Width", GetGPUBusWidth(adapter["Name"]?.ToString()), yPos, 120);
                yPos = CreateInfoRow(panel, "Bandwidth", "Unknown", yPos, 120);

                yPos += 15;

                // Additional Info
                var infoHeaderLabel = new Label
                {
                    Text = "Additional Information",
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(100, 200, 255),
                    Location = new Point(15, yPos),
                    AutoSize = true
                };
                panel.Controls.Add(infoHeaderLabel);
                yPos += 25;

                yPos = CreateInfoRow(panel, "DirectX Support", adapter["VideoModeDescription"]?.ToString() ?? "Unknown", yPos, 120);
                yPos = CreateInfoRow(panel, "OpenGL Support", "Unknown", yPos, 120);
                yPos = CreateInfoRow(panel, "Driver Version", adapter["DriverVersion"]?.ToString() ?? "Unknown", yPos, 120);
                yPos = CreateInfoRow(panel, "Driver Date", adapter["DriverDate"]?.ToString() ?? "Unknown", yPos, 120);
            }
            catch (Exception ex)
            {
                CreateInfoRow(panel, "Error", $"Unable to load GPU details: {ex.Message}", 10, 120);
            }
        }

        private string GetGPUCodeName(string? gpuName)
        {
            if (string.IsNullOrEmpty(gpuName))
                return "Unknown";
                
            var name = gpuName.ToUpperInvariant();
            
            // NVIDIA RTX 30/40 series
            if (name.Contains("RTX 4090")) return "AD102";
            if (name.Contains("RTX 4080")) return "AD103";
            if (name.Contains("RTX 4070")) return "AD104";
            if (name.Contains("RTX 4060")) return "AD107";
            if (name.Contains("RTX 3090")) return "GA102";
            if (name.Contains("RTX 3080")) return "GA102";
            if (name.Contains("RTX 3070")) return "GA104";
            if (name.Contains("RTX 3060")) return "GA106";
            
            // AMD RDNA series
            if (name.Contains("RX 7900")) return "Navi 31";
            if (name.Contains("RX 7800")) return "Navi 32";
            if (name.Contains("RX 7700")) return "Navi 32";
            if (name.Contains("RX 7600")) return "Navi 33";
            if (name.Contains("RX 6900")) return "Navi 21";
            if (name.Contains("RX 6800")) return "Navi 21";
            if (name.Contains("RX 6700")) return "Navi 22";
            if (name.Contains("RX 6600")) return "Navi 23";
            
            return "Unknown";
        }

        private string GetGPUTechnology(string? gpuName)
        {
            if (string.IsNullOrEmpty(gpuName))
                return "Unknown";
                
            var name = gpuName.ToUpperInvariant();
            
            // NVIDIA
            if (name.Contains("RTX 40")) return "5 nm";
            if (name.Contains("RTX 30")) return "8 nm";
            if (name.Contains("RTX 20")) return "12 nm";
            if (name.Contains("GTX 16")) return "12 nm";
            
            // AMD
            if (name.Contains("RX 7")) return "5 nm";
            if (name.Contains("RX 6")) return "7 nm";
            if (name.Contains("RX 5")) return "7 nm";
            
            return "Unknown";
        }

        private string GetGPUMemoryType(string? gpuName)
        {
            if (string.IsNullOrEmpty(gpuName))
                return "Unknown";
                
            var name = gpuName.ToUpperInvariant();
            
            // Modern GPUs typically use GDDR6/6X
            if (name.Contains("RTX 4") || name.Contains("RX 7")) return "GDDR6X";
            if (name.Contains("RTX 3") || name.Contains("RX 6")) return "GDDR6";
            if (name.Contains("RTX 2") || name.Contains("RX 5")) return "GDDR6";
            if (name.Contains("GTX 16")) return "GDDR6";
            
            // Older/integrated graphics
            if (name.Contains("UHD") || name.Contains("HD GRAPHICS")) return "System Memory";
            
            return "GDDR6";
        }

        private string GetGPUBusWidth(string? gpuName)
        {
            if (string.IsNullOrEmpty(gpuName))
                return "Unknown";
                
            var name = gpuName.ToUpperInvariant();
            
            // High-end GPUs
            if (name.Contains("4090") || name.Contains("7900")) return "384-bit";
            if (name.Contains("4080") || name.Contains("7800")) return "256-bit";
            if (name.Contains("4070") || name.Contains("7700")) return "192-bit";
            if (name.Contains("4060") || name.Contains("7600")) return "128-bit";
            
            // RTX 30 series
            if (name.Contains("3090")) return "384-bit";
            if (name.Contains("3080")) return "320-bit";
            if (name.Contains("3070")) return "256-bit";
            if (name.Contains("3060")) return "192-bit";
            
            return "Unknown";
        }
        private List<(string key, string value)> GetProcessorInfo()
        {
            var info = new List<(string key, string value)>();
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                foreach (ManagementObject obj in searcher.Get())
                {
                    info.Add(("Processor Name", obj["Name"]?.ToString() ?? "Unknown"));
                    info.Add(("Manufacturer", obj["Manufacturer"]?.ToString() ?? "Unknown"));
                    info.Add(("Architecture", GetArchitectureString(obj["Architecture"]?.ToString())));
                    info.Add(("Cores", obj["NumberOfCores"]?.ToString() ?? "Unknown"));
                    info.Add(("Logical Processors", obj["NumberOfLogicalProcessors"]?.ToString() ?? "Unknown"));
                    info.Add(("Max Clock Speed", $"{obj["MaxClockSpeed"]} MHz"));
                    info.Add(("Current Clock Speed", $"{obj["CurrentClockSpeed"]} MHz"));
                    info.Add(("L2 Cache Size", $"{obj["L2CacheSize"]} KB"));
                    info.Add(("L3 Cache Size", $"{obj["L3CacheSize"]} KB"));
                    break; // Just get the first processor
                }
            }
            catch (Exception ex)
            {
                info.Add(("Error", $"Unable to retrieve processor information: {ex.Message}"));
            }
            return info;
        }

        private List<(string key, string value)> GetMemoryInfo()
        {
            var info = new List<(string key, string value)>();
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var totalMemory = Convert.ToUInt64(obj["TotalPhysicalMemory"]);
                    info.Add(("Total Physical Memory", $"{totalMemory / (1024 * 1024 * 1024):F1} GB"));
                    break;
                }

                var availableMemory = new PerformanceCounter("Memory", "Available MBytes");
                var availableMB = availableMemory.NextValue();
                info.Add(("Available Memory", $"{availableMB / 1024:F1} GB"));
                
                // Calculate usage percentage
                var totalMemoryGB = Convert.ToUInt64(new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem").Get().Cast<ManagementObject>().First()["TotalPhysicalMemory"]) / (1024.0 * 1024 * 1024);
                var usagePercent = ((totalMemoryGB - (availableMB / 1024)) / totalMemoryGB) * 100;
                info.Add(("Memory Usage", $"{usagePercent:F1}%"));
            }
            catch (Exception ex)
            {
                info.Add(("Error", $"Unable to retrieve memory information: {ex.Message}"));
            }
            return info;
        }

        private List<(string key, string value)> GetStorageInfo()
        {
            var info = new List<(string key, string value)>();
            try
            {
                var drives = DriveInfo.GetDrives().Where(d => d.IsReady);
                foreach (var drive in drives)
                {
                    var driveType = GetDriveTypeString(drive.Name);
                    var totalGB = drive.TotalSize / (1024.0 * 1024 * 1024);
                    var freeGB = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
                    var usedGB = totalGB - freeGB;
                    var usagePercent = (usedGB / totalGB) * 100;

                    info.Add(($"Drive {drive.Name}", $"{drive.DriveType} - {totalGB:F1} GB total, {freeGB:F1} GB free ({usagePercent:F1}% used)"));
                }
            }
            catch (Exception ex)
            {
                info.Add(("Error", $"Unable to retrieve storage information: {ex.Message}"));
            }
            return info;
        }

        private List<(string key, string value)> GetWindowsInfo()
        {
            var info = new List<(string key, string value)>();
            try
            {
                var version = Environment.OSVersion;
                info.Add(("Operating System", $"{version.Platform} {version.Version}"));
                info.Add(("Version String", version.VersionString));
                info.Add(("Architecture", Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit"));
                info.Add(("Computer Name", Environment.MachineName));
                info.Add(("User Domain", Environment.UserDomainName));
                info.Add(("System Directory", Environment.SystemDirectory));
                
                // Get Windows edition from registry
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                if (key != null)
                {
                    info.Add(("Product Name", key.GetValue("ProductName")?.ToString() ?? "Unknown"));
                    info.Add(("Edition", key.GetValue("EditionID")?.ToString() ?? "Unknown"));
                    info.Add(("Build Number", key.GetValue("CurrentBuild")?.ToString() ?? "Unknown"));
                    info.Add(("Release ID", key.GetValue("ReleaseId")?.ToString() ?? "Unknown"));
                }
            }
            catch (Exception ex)
            {
                info.Add(("Error", $"Unable to retrieve Windows information: {ex.Message}"));
            }
            return info;
        }

        private List<(string key, string value)> GetPerformanceInfo()
        {
            var info = new List<(string key, string value)>();
            try
            {
                var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
                info.Add(("System Uptime", $"{uptime.Days} days, {uptime.Hours} hours, {uptime.Minutes} minutes"));
                
                var processes = Process.GetProcesses();
                info.Add(("Running Processes", processes.Length.ToString()));
                
                var workingSet = Environment.WorkingSet / (1024 * 1024);
                info.Add(("Current Process Memory", $"{workingSet} MB"));
                
                info.Add(("Processor Count", Environment.ProcessorCount.ToString()));
                info.Add((".NET Runtime Version", Environment.Version.ToString()));
            }
            catch (Exception ex)
            {
                info.Add(("Error", $"Unable to retrieve performance information: {ex.Message}"));
            }
            return info;
        }

        private List<(string key, string value)> GetNetworkInfo()
        {
            var info = new List<(string key, string value)>();
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = True");
                var adapters = 0;
                foreach (ManagementObject obj in searcher.Get())
                {
                    adapters++;
                    var description = obj["Description"]?.ToString() ?? "Unknown Adapter";
                    var ipAddresses = obj["IPAddress"] as string[];
                    var ipv4 = ipAddresses?.FirstOrDefault(ip => !ip.Contains(":")) ?? "Not assigned";
                    var ipv6 = ipAddresses?.FirstOrDefault(ip => ip.Contains(":")) ?? "Not assigned";
                    
                    info.Add(($"Network Adapter {adapters}", description));
                    info.Add(($"IPv4 Address", ipv4));
                    if (ipv6 != "Not assigned")
                    {
                        info.Add(($"IPv6 Address", ipv6.Length > 30 ? ipv6.Substring(0, 30) + "..." : ipv6));
                    }
                }
                
                if (adapters == 0)
                {
                    info.Add(("Network Status", "No active network adapters found"));
                }
            }
            catch (Exception ex)
            {
                info.Add(("Error", $"Unable to retrieve network information: {ex.Message}"));
            }
            return info;
        }

        private List<(string key, string value)> GetSecurityInfo()
        {
            var info = new List<(string key, string value)>();
            try
            {
                // Check if running as administrator
                info.Add(("Administrator Rights", IsRunningAsAdministrator() ? "Yes" : "No"));
                
                // Check UAC status
                using var uacKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
                var uacEnabled = uacKey?.GetValue("EnableLUA")?.ToString() == "1";
                info.Add(("User Account Control", uacEnabled ? "Enabled" : "Disabled"));
                
                // Check Windows Defender status (basic check)
                try
                {
                    using var defenderKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender");
                    var defenderExists = defenderKey != null;
                    info.Add(("Windows Defender", defenderExists ? "Installed" : "Not found"));
                }
                catch
                {
                    info.Add(("Windows Defender", "Status unknown"));
                }
                
                info.Add(("Security Provider", "Windows Security"));
            }
            catch (Exception ex)
            {
                info.Add(("Error", $"Unable to retrieve security information: {ex.Message}"));
            }
            return info;
        }

        private string GetArchitectureString(string? arch)
        {
            return arch switch
            {
                "0" => "x86 (32-bit)",
                "1" => "MIPS",
                "2" => "Alpha",
                "3" => "PowerPC", 
                "5" => "ARM",
                "6" => "Itanium (IA-64)",
                "9" => "x64 (64-bit)",
                "12" => "ARM64",
                _ => arch ?? "Unknown"
            };
        }

        private string GetDriveTypeString(string driveName)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_LogicalDisk WHERE DeviceID = '{driveName.TrimEnd('\\')}:'");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var mediaType = obj["MediaType"]?.ToString();
                    return mediaType switch
                    {
                        "12" => "SSD", // Fixed hard disk media
                        _ => "HDD"
                    };
                }
            }
            catch { }
            return "Unknown";
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

    // Custom color table for dark theme menus
    public class DarkMenuColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected => Color.FromArgb(65, 177, 225);
        public override Color MenuItemBorder => Color.FromArgb(100, 100, 100);
        public override Color MenuBorder => Color.FromArgb(100, 100, 100);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(65, 177, 225);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(65, 177, 225);
        public override Color ToolStripDropDownBackground => Color.FromArgb(45, 45, 48);
        public override Color MenuItemPressedGradientBegin => Color.FromArgb(65, 177, 225);
        public override Color MenuItemPressedGradientEnd => Color.FromArgb(65, 177, 225);
        public override Color ImageMarginGradientBegin => Color.FromArgb(45, 45, 48); // Remove image margin
        public override Color ImageMarginGradientEnd => Color.FromArgb(45, 45, 48); // Remove image margin
        public override Color ImageMarginGradientMiddle => Color.FromArgb(45, 45, 48); // Remove image margin
        public override Color MenuStripGradientBegin => Color.FromArgb(45, 45, 48);
        public override Color MenuStripGradientEnd => Color.FromArgb(45, 45, 48);
    }

    // Custom color table for light theme menus
    public class LightMenuColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected => Color.FromArgb(65, 177, 225);
        public override Color MenuItemBorder => Color.FromArgb(200, 200, 200);
        public override Color MenuBorder => Color.FromArgb(200, 200, 200);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(65, 177, 225);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(65, 177, 225);
        public override Color ToolStripDropDownBackground => Color.FromArgb(240, 240, 240);
        public override Color MenuItemPressedGradientBegin => Color.FromArgb(65, 177, 225);
        public override Color MenuItemPressedGradientEnd => Color.FromArgb(65, 177, 225);
        public override Color ImageMarginGradientBegin => Color.FromArgb(240, 240, 240); // Remove image margin
        public override Color ImageMarginGradientEnd => Color.FromArgb(240, 240, 240); // Remove image margin
        public override Color ImageMarginGradientMiddle => Color.FromArgb(240, 240, 240); // Remove image margin
        public override Color MenuStripGradientBegin => Color.FromArgb(240, 240, 240);
        public override Color MenuStripGradientEnd => Color.FromArgb(240, 240, 240);
    }
}