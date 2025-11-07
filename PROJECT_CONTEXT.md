# MyTekkiDebloat Project Context

## Project Overview
**Goal:** Create a modern .NET library and NuGet package that extracts and implements Chris Titus Tech's proven Windows tweaks in a clean, extensible API that can be consumed by other developers.

**Why This Approach:** Originally tried to use Chris's winutil.ps1 script directly, but his architecture is GUI-centric and difficult to use headlessly. Instead of reinventing the wheel, we're extracting his proven tweaks and implementing them in a modern C# library.

## Solution Architecture

### Projects Created
```
D:\Projects\Personal\MyTekkiDebloat\
├── MyTekkiDebloat.sln                    # Main solution
├── MyTekkiDebloat.Core\                  # Core library (future NuGet package)
│   ├── MyTekkiDebloat.Core.csproj       
│   ├── Models\                           # Data models
│   │   ├── Tweak.cs                     # Main tweak definition
│   │   ├── Operations.cs                # Registry/Service/File operations
│   │   └── TweakResult.cs               # Results and status tracking
│   └── Interfaces\                       # API contracts
│       ├── CoreInterfaces.cs            # ITweakProvider, ITweakExecutor, ITweakDetector
│       └── IDebloatService.cs           # Main facade API + SystemInfo
└── MyTekkiDebloat.WinUI\               # WinForms reference implementation
    ├── MyTekkiDebloat.WinUI.csproj     # References Core library
    └── ... (standard WinForms files)
```

### Core API Design

#### Key Models
- **`Tweak`** - Main tweak definition with metadata, severity, operations, tags
- **`RegistryOperation`** - Registry changes (HKEY, path, value, type)
- **`ServiceOperation`** - Windows service changes (start/stop/disable)
- **`FileOperation`** - File system changes (delete/create/rename)
- **`PowerShellOperation`** - Complex operations requiring PS (fallback)
- **`TweakResult`** - Success/failure with detailed information
- **`TweakStatus`** - Current application status on system

#### Key Interfaces
- **`ITweakProvider`** - Load/search tweaks from various sources
- **`ITweakExecutor`** - Apply/revert tweaks on system
- **`ITweakDetector`** - Check if tweaks are currently applied
- **`IDebloatService`** - Main facade combining all functionality

## Progress Status

### ✅ COMPLETED
1. **Solution Structure** - Created proper multi-project solution
2. **Core Models** - Designed comprehensive data models for tweaks
3. **API Interfaces** - Clean, extensible interface design

### 🔄 NEXT PRIORITIES

#### 1. Extract CTT Tweak Data
**Location:** `C:\ProgramData\MyTekkiDebloatUtil\winutil\config\tweaks.json`
**Task:** Parse Chris's tweak definitions and convert to our `Tweak` model format
**Key Tweaks to Start With:**
- `WPFToggleDetailedBSoD` - Enable detailed BSOD information
- `WPFTweaksEndTaskOnTaskbar` - Right-click end task on taskbar
- `WPFTweaksHiber` - Disable hibernation
- `WPFTweaksLoc` - Disable location tracking

#### 2. Build Native Tweak Executor  
**Task:** Implement `ITweakExecutor` using pure C# (no PowerShell dependencies)
**Components Needed:**
- Registry manipulation using `Microsoft.Win32.Registry`
- Service management using `System.ServiceProcess`
- File operations using `System.IO`
- Admin privilege detection

#### 3. Setup NuGet Packaging
**Task:** Configure `MyTekkiDebloat.Core.csproj` for NuGet distribution
**Requirements:**
- Package metadata (version, description, tags)
- Documentation XML generation
- Multi-targeting if needed

### 🎯 END GOAL
- **NuGet Package:** `MyTekkiDebloat.Core` - Clean API for other developers
- **Reference UI:** WinForms app showing how to consume the library
- **Proven Tweaks:** All of Chris's tested tweaks available through clean C# API
- **Extensible:** Easy for others to add custom tweaks

## Technical Context from Previous Session

### Original Problem
- Started with WinForms app trying to call Chris's winutil.ps1 directly
- Script kept launching GUI instead of running headlessly
- JSON parsing issues with tweak configuration
- PowerShell function loading problems
- Decision made to abandon direct CTT integration in favor of clean reimplementation

### Key Insights
- Chris's tweaks are proven and well-tested (don't reinvent)
- His architecture is GUI-centric (hard to use programmatically) 
- Registry/service operations can be done natively in C#
- API-first design enables broader consumption

## How to Continue

### Immediate Next Steps
1. **Build the solution** to ensure everything compiles:
   ```bash
   dotnet build
   ```

2. **Start with CTT data extraction:**
   ```bash
   # Parse Chris's tweaks.json
   Get-Content "C:\ProgramData\MyTekkiDebloatUtil\winutil\config\tweaks.json" | ConvertFrom-Json
   ```

3. **Create first implementation:**
   - Implement `ITweakProvider` to load tweaks from JSON
   - Implement basic `ITweakExecutor` for registry operations
   - Test with DetailedBSOD tweak

### Code Patterns to Follow
```csharp
// Example usage of the future API:
var debloatService = new DebloatService();
var tweaks = await debloatService.TweakProvider.GetTweaksAsync();
var detailedBsod = tweaks.First(t => t.Id == "DetailedBSOD");
var result = await debloatService.TweakExecutor.ApplyTweakAsync(detailedBsod);
```

### File Organization
- Keep models in `Models\` folder
- Keep interfaces in `Interfaces\` folder  
- Implementation classes in `Services\` or `Providers\` folders
- Data/JSON files in `Data\` folder

## Chris Titus Tech Attribution
All tweak logic and registry values derived from Chris Titus Tech's winutil project:
- GitHub: https://github.com/ChrisTitusTech/winutil
- Original tweaks tested and proven by CTT community
- This project provides a modern C# API wrapper around his work

---
*Generated: November 6, 2025 - Project started from scratch to create proper library architecture*