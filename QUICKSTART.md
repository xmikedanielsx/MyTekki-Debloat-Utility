# 🚀 Quick Start Guide - MyTekkiDebloat

## 🎯 **Getting Started Immediately**

### **1. Build & Verify Setup**
```powershell
# In your project directory
cd "D:\Projects\Personal\MyTekkiDebloat"
dotnet build MyTekkiDebloat.sln
# Should succeed with no errors
```

### **2. Run the Application**
```powershell
# Start the WinUI application
cd MyTekkiDebloat.WinUI\bin\Debug\net8.0-windows
.\MyTekkiDebloat.WinUI.exe
```

### **3. Explore Available Tweaks**
The application currently includes **20 carefully curated tweaks** from two sources:

- **✅ Chris Titus Tech Verified** (13 tweaks) - Proven, battle-tested optimizations with verified CTT mappings
- **🛠️ MyTekki Custom Enhanced** (7 tweaks) - Additional functionality and specialized optimizations

---

## 📋 **Understanding Tweak Files**

### **🌟 Chris Titus Tech Verified Tweaks (13)**

**🔒 Privacy & Security (4 tweaks)**
- `DisableActivityHistory.json` - Stop Windows tracking activities (CTT: WPFTweaksAH)
- `DisableConsumerFeatures.json` - Block sponsored apps (CTT: WPFTweaksConsumerFeatures)  
- `DisableLocationTracking.json` - Prevent location tracking (CTT: WPFTweaksLoc)
- `DisableTelemetry.json` - Stop telemetry collection (CTT: WPFTweaksTele)

**⚡ Performance (4 tweaks)**
- `DisableBackgroundApps.json` - Stop unnecessary background processes (CTT: WPFTweaksDisableBGapps)
- `DisableGameDVR.json` - Remove gaming DVR overhead (CTT: WPFTweaksDVR)
- `DisableHibernation.json` - Free disk space (CTT: WPFTweaksHiber)
- `OptimizeVisualEffects.json` - UI responsiveness (CTT: WPFTweaksDisplay)

**🎨 Appearance & UI (3 tweaks)**
- `DarkMode.json` - System-wide dark theme (CTT: WPFToggleDarkMode)
- `ShowFileExtensions.json` - Show file extensions (CTT: WPFToggleShowExt)
- `ShowHiddenFiles.json` - Display hidden files (CTT: WPFToggleHiddenFiles)

**🔧 System Behavior (2 tweaks)**
- `DetailedBSOD.json` - Show detailed crash info (CTT: WPFToggleDetailedBSoD)
- `EndTaskOnTaskbar.json` - Add "End Task" option (CTT: WPFTweaksEndTaskOnTaskbar)

### **🛠️ MyTekki Custom Enhanced Tweaks (7)**

### **�️ MyTekki Custom Enhanced Tweaks (7)**

**�🔒 Privacy & Security (2 tweaks)**
- `DisableAdvertisingID.json` - Block Windows advertising identifier tracking
- `DisableCortana.json` - Completely disable Cortana digital assistant

**⚡ Performance & Network (2 tweaks)**
- `DisableFastStartup.json` - Remove hybrid boot for true clean starts  
- `DisableIPv6.json` - Disable IPv6 protocol for network optimization

**🎨 UI & Interface (3 tweaks)**
- `DisableNewsAndInterests.json` - Remove taskbar news widget
- `DisableSearchHighlights.json` - Clean up search interface  
- `DisableTeamsAutostart.json` - Prevent Microsoft Teams auto-launch

### **🚧 Development Pipeline**

**Chris Titus Tech Expansion (50+ tweaks available)**
We're systematically reviewing and implementing additional CTT tweaks:
- Network optimizations (WiFi, Teredo, IPv4/6)
- Browser debloating (Edge, Brave)  
- Service management and startup control
- PowerShell 7 and development tools
- Storage cleanup and system maintenance
- Hardware-specific optimizations

---

## 🛠️ **Creating Your First Tweak**

### **Step 1: Create JSON File**
Create `MyTekkiDebloat.Core\Data\YourTweak.json`:

```json
{
  "Id": "DisableStartupSound",
  "Name": "Disable Windows Startup Sound", 
  "Description": "Removes the startup sound that plays when Windows boots",
  "Category": "System",
  "Severity": "Low",
  "Tags": ["startup", "sound", "boot"],
  "CTTKey": "",
  "ApplyOperations": {
    "RegistryOperations": [
      {
        "Hive": "LocalMachine",
        "KeyPath": "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Authentication\\LogonUI\\BootAnimation",
        "ValueName": "DisableStartupSound", 
        "Value": 1,
        "ValueType": "DWord",
        "Operation": "SetValue"
      }
    ]
  },
  "UndoOperations": {
    "RegistryOperations": [
      {
        "Hive": "LocalMachine",
        "KeyPath": "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Authentication\\LogonUI\\BootAnimation",
        "ValueName": "DisableStartupSound",
        "Value": 0,
        "ValueType": "DWord", 
        "Operation": "SetValue"
      }
    ]
  },
  "DetectionRules": [
    {
      "Type": "Registry",
      "Hive": "LocalMachine", 
      "KeyPath": "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Authentication\\LogonUI\\BootAnimation",
      "ValueName": "DisableStartupSound",
      "ExpectedValue": 1,
      "ValueType": "DWord"
    }
  ],
  "Authors": [
    {
      "Name": "Your Name",
      "Url": "https://github.com/yourusername",
      "SupportUrl": "https://yourdocumentation.com",
      "Type": "Original", 
      "Notes": "Custom tweak for disabling Windows startup sound"
    }
  ]
}
```

### **Step 2: Test Your Tweak**
1. Rebuild the solution: `dotnet build`
2. Run the application 
3. Find your tweak in the list
4. Test Apply/Undo functionality
5. Verify detection works correctly

### **Step 3: Validate Schema**
Ensure your JSON includes:
- ✅ All required fields (`Id`, `Name`, `Description`, etc.)
- ✅ Matching `DetectionRules` for every operation
- ✅ Safe `UndoOperations` to reverse changes
- ✅ Proper `Authors` attribution
- ✅ Descriptive `Tags` for searchability

---

## 🔧 **Development Workflow**

### **🏗️ Architecture Overview**

MyTekkiDebloat follows a clean, modular architecture:

```
MyTekkiDebloat.Core\
├── Interfaces\           # Service contracts
│   ├── IDebloatService.cs
│   ├── ITweakProvider.cs  
│   └── ITweakDetector.cs
├── Models\              # Data structures
│   ├── Tweak.cs
│   ├── TweakResult.cs
│   └── Operations.cs
├── Services\            # Core implementations  
│   ├── JsonTweakProvider.cs
│   ├── JsonDrivenTweakDetector.cs
│   └── DebloatService.cs
└── Data\               # Tweak definitions (JSON)
    ├── DisableTelemetry.json
    ├── EnableDarkMode.json
    └── ... (20 total tweaks)

MyTekkiDebloat.WinUI\    # User interface
├── Form1.cs            # Main application window
├── Form1.Designer.cs   # UI layout
└── Program.cs          # Application entry point
```

### **🔄 Adding New Features**

**To add a new tweak category:**
1. Create JSON files following the schema
2. Add appropriate `Category` field
3. Update UI filtering if needed

**To add new operation types:**
1. Extend `Operations.cs` models
2. Update `JsonDrivenTweakDetector.cs` 
3. Add execution logic in service layer

**To enhance the UI:**
1. Modify `Form1.Designer.cs` layout
2. Update `Form1.cs` event handlers
3. Test with existing tweaks

---

## 🧪 **Testing & Validation**

### **Manual Testing Steps**
1. **Build & Run**: Ensure application starts without errors
2. **Load Tweaks**: Verify all 20 tweaks appear in the UI
3. **Detection**: Check that current system state is detected correctly
4. **Apply Tweaks**: Test that tweaks apply successfully  
5. **Undo Operations**: Verify rollback functionality works
6. **Error Handling**: Test with invalid registry paths or permissions

### **Automated Testing**
```csharp
// Example unit test structure
[Test]
public async Task JsonTweakProvider_LoadsAllTweaks()
{
    var provider = new JsonTweakProvider();
    var tweaks = await provider.GetTweaksAsync();
    
    Assert.That(tweaks.Count(), Is.EqualTo(20));
    Assert.That(tweaks.All(t => !string.IsNullOrEmpty(t.Id)));
    Assert.That(tweaks.All(t => t.DetectionRules.Any()));
}
```

---

## 🚀 **Next Steps**

### **Immediate Opportunities**
- **Add More Tweaks**: Browse Chris Titus Tech's full collection for inspiration
- **Enhance UI**: Add progress bars, better categorization, search functionality
- **PowerShell Module**: Create cmdlets for command-line usage
- **Configuration Profiles**: Save/load tweak combinations
- **Backup/Restore**: Create system restore points before applying tweaks

### **Advanced Features**
- **Custom Repositories**: Load tweaks from external sources
- **Community Sharing**: Upload/download tweak collections
- **Scheduling**: Apply tweaks on system startup or schedule
- **Enterprise Management**: Group policy integration
- **Analytics**: Track which tweaks are most popular/effective

---

## 📚 **Additional Resources**

- **Chris Titus Tech Documentation**: https://winutil.christitus.com/dev/tweaks/
- **Windows Registry Reference**: https://docs.microsoft.com/en-us/windows/win32/sysinfo/registry
- **ReaLTaiizor UI Framework**: https://github.com/Taiizor/ReaLTaiizor  
- **Project Issues**: Create GitHub issues for bugs or feature requests

---

*Start tweaking and make Windows work the way **you** want it to! 🎯*