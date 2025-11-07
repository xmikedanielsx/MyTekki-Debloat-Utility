# 🤝 Contributing to MyTekkiDebloat

Thank you for your interest in contributing to MyTekkiDebloat! This project aims to make Windows optimization accessible to everyone while maintaining the highest standards of safety and reliability.

## 🎯 **How You Can Contribute**

### **🔧 Adding New Tweaks**
- Create JSON-based Windows optimizations
- Port tweaks from other sources (with proper attribution)
- Enhance existing tweaks with additional functionality
- Improve detection rules for better accuracy

### **🐛 Bug Reports & Fixes**
- Report issues with existing tweaks
- Fix broken registry paths or operations  
- Improve error handling and edge cases
- Enhance compatibility across Windows versions

### **📚 Documentation**
- Improve tweak descriptions and explanations
- Add usage examples and screenshots
- Translate documentation to other languages
- Create video tutorials or guides

### **🎨 UI/UX Improvements**
- Enhance the WinUI application interface
- Add new features like search, filtering, or categorization
- Improve accessibility and usability
- Create themes or customization options

---

## 📋 **Tweak Contribution Guidelines**

### **JSON Schema Requirements**

Every tweak **MUST** include these fields:

```json
{
  "Id": "UniqueCamelCaseIdentifier",
  "Name": "Human-Readable Display Name",
  "Description": "Comprehensive description explaining what this tweak does, why it's beneficial, and any potential risks or considerations users should know about.",
  "Category": "Privacy|Performance|Appearance|System|Security|Gaming|Development",
  "Severity": "Low|Medium|High",
  "Tags": ["keyword1", "keyword2", "descriptive-tags"],
  "CTTKey": "CorrespondingChrisTitusTechKey"
}
```

### **Operation Types**

#### **Registry Operations (Preferred)**
```json
"ApplyOperations": {
  "RegistryOperations": [
    {
      "Hive": "LocalMachine|CurrentUser|Users|ClassesRoot|CurrentConfig",
      "KeyPath": "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Example",
      "ValueName": "SettingName",
      "Value": 1,
      "ValueType": "String|DWord|QWord|Binary|MultiString|ExpandString",
      "Operation": "SetValue|DeleteValue|DeleteKey|CreateKey"
    }
  ]
}
```

#### **Service Operations**
```json
"ApplyOperations": {
  "ServiceOperations": [
    {
      "ServiceName": "ServiceName",
      "Action": "Stop|Start|Disable|Enable",
      "StartupType": "Disabled|Manual|Automatic"
    }
  ]
}
```

#### **PowerShell Operations (Use Sparingly)**
```json
"ApplyOperations": {
  "PowerShellOperations": [
    {
      "Script": "Your-PowerShell-Command -Parameter Value",
      "Description": "Clear explanation of what this script does",
      "RequiresAdmin": true,
      "ExpectedExitCode": 0
    }
  ]
}
```

### **Required Sections**

#### **1. Detection Rules**
Every operation **MUST** have corresponding detection rules:

```json
"DetectionRules": [
  {
    "Type": "Registry",
    "Hive": "LocalMachine",
    "KeyPath": "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Example", 
    "ValueName": "SettingName",
    "ExpectedValue": 1,
    "ValueType": "DWord"
  },
  {
    "Type": "Service",
    "ServiceName": "ServiceName",
    "ExpectedStatus": "Stopped",
    "ExpectedStartupType": "Disabled"
  }
]
```

#### **2. Undo Operations**
Provide safe rollback functionality:

```json
"UndoOperations": {
  "RegistryOperations": [
    {
      "Hive": "LocalMachine",
      "KeyPath": "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Example",
      "ValueName": "SettingName", 
      "Value": 0,
      "ValueType": "DWord",
      "Operation": "SetValue"
    }
  ]
}
```

#### **3. Author Attribution**
Always credit sources and contributors:

```json
"Authors": [
  {
    "Name": "Original Author Name",
    "Url": "https://github.com/originalauthor",
    "SupportUrl": "https://documentation-url.com",
    "Type": "Original",
    "Notes": "Source of this tweak and any relevant context"
  },
  {
    "Name": "Your Name", 
    "Url": "https://github.com/yourusername",
    "SupportUrl": "https://your-website.com",
    "Type": "Contributor",
    "Notes": "Description of your contributions or enhancements"
  }
]
```

---

## ✅ **Contribution Checklist**

Before submitting a tweak, ensure:

### **🔍 Research & Validation**
- [ ] Tweak is safe and reversible
- [ ] Registry paths and values are correct
- [ ] Tested on multiple Windows versions if possible
- [ ] No conflicts with existing tweaks
- [ ] Performance impact is minimal

### **📝 Documentation Quality**
- [ ] Clear, comprehensive description
- [ ] Accurate category and severity
- [ ] Relevant tags for searchability  
- [ ] Proper attribution to original sources
- [ ] Notes about any limitations or risks

### **🧪 Technical Requirements**
- [ ] Valid JSON syntax (use a JSON validator)
- [ ] Unique `Id` that doesn't conflict with existing tweaks
- [ ] All registry operations have matching detection rules
- [ ] Undo operations safely reverse all changes
- [ ] PowerShell scripts are minimal and well-documented

### **🛡️ Safety Standards**
- [ ] No operations that could brick the system
- [ ] No deletion of critical system files
- [ ] Registry operations target appropriate hives
- [ ] Service operations won't break essential functionality
- [ ] Clear severity rating based on risk level

---

## 🚀 **Submission Process**

### **1. Fork & Clone**
```bash
git clone https://github.com/YourUsername/MyTekkiDebloat.git
cd MyTekkiDebloat
git checkout -b feature/your-tweak-name
```

### **2. Create Your Tweak**
- Add JSON file to `MyTekkiDebloat.Core\Data\`
- Follow naming convention: `PascalCaseTweakName.json`
- Validate against schema requirements

### **3. Test Thoroughly**
```powershell
# Build the solution
dotnet build MyTekkiDebloat.sln

# Run the application
cd MyTekkiDebloat.WinUI\bin\Debug\net8.0-windows
.\MyTekkiDebloat.WinUI.exe

# Test your tweak:
# 1. Verify it appears in the list
# 2. Check detection works correctly
# 3. Apply the tweak successfully
# 4. Verify undo functionality
# 5. Test detection after both apply and undo
```

### **4. Submit Pull Request**
- Provide clear title and description
- Reference any issues your PR addresses
- Include testing details and Windows versions tested
- Be responsive to review feedback

---

## 🎯 **Priority Contributions**

We especially welcome contributions in these areas:

### **🔒 Privacy & Security**
- Telemetry and data collection controls
- Windows Update configuration
- Microsoft account integration settings
- Advertising and tracking prevention

### **⚡ Performance Optimizations**
- Startup and boot optimizations
- Memory and CPU usage improvements
- Storage and disk performance tweaks
- Background service management

### **🎨 User Interface Enhancements**
- Visual effects and animations
- Taskbar and Start menu customizations
- File Explorer improvements
- Desktop and window management

### **🛠️ Developer Tools**
- WSL and Linux subsystem tweaks
- PowerShell and terminal enhancements
- Development environment optimizations
- Version control and Git integration

---

## ❌ **What We Don't Accept**

### **Unsafe Operations**
- Tweaks that could corrupt the system
- Operations requiring unsigned drivers
- Registry modifications to critical system areas
- File system operations outside user directories

### **Malicious or Questionable Content**
- Tweaks that disable security features without clear benefit
- Operations that could enable malware or exploits
- Privacy-violating modifications
- Unlicensed software distribution

### **Low-Quality Submissions**
- Incomplete or untested tweaks
- Duplicates of existing functionality
- Poor documentation or unclear descriptions
- Missing attribution or incorrect sources

---

## 📞 **Getting Help**

### **Questions & Support**
- **GitHub Issues**: For bugs, feature requests, or general questions
- **GitHub Discussions**: For brainstorming and community chat
- **Pull Request Reviews**: We'll provide detailed feedback on submissions

### **Resources**
- **Windows Registry Documentation**: https://docs.microsoft.com/en-us/windows/win32/sysinfo/registry
- **Chris Titus Tech Tweaks**: https://winutil.christitus.com/dev/tweaks/
- **JSON Schema Validators**: https://jsonlint.com/ or https://jsonschemavalidator.net/

---

## 🏆 **Recognition**

Contributors will be:
- Listed in the project README
- Credited in tweak attribution
- Recognized in release notes
- Invited to become maintainers based on contribution quality

---

## 📜 **Code of Conduct**

By contributing, you agree to:
- Be respectful and inclusive to all community members
- Provide constructive feedback and accept criticism gracefully
- Focus on what's best for the community and project
- Maintain high standards of technical quality and safety

---

**Thank you for helping make Windows optimization safer, easier, and more accessible! 🚀**