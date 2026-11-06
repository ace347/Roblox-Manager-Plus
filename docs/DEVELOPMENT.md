# Developer Setup & Workflow Guide

## 1. Prerequisites

Before developing or building this project, install:

1. **Windows 10 Version 19041+ or Windows 11 (x64)**
2. **.NET 10 SDK (x64):**
   - Download from [dot.net](https://dotnet.microsoft.com/download/dotnet/10.0)
   - Or install via winget: `winget install Microsoft.DotNet.SDK.10`
3. **Git:**
   - `winget install Git.Git`
4. **Visual Studio 2026 / Visual Studio Code:**
   - For VS Code: C# Dev Kit extension recommended.

---

## 2. Setting Up the Repository

Clone and initialize:

```powershell
git clone <repo-url>
cd REEA
./scripts/setup.ps1
```

---

## 3. Build & Test Commands

### Restore Dependencies
```powershell
dotnet restore
```

### Build Solution (Debug)
```powershell
./scripts/build.ps1
```

### Build Solution (Release)
```powershell
./scripts/build.ps1 -Configuration Release
```

### Run Tests
```powershell
./scripts/test.ps1
```

### Code Formatting Check
```powershell
dotnet format --verify-no-changes
```

