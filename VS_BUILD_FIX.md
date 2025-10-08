# Fixing Visual Studio Build Errors After Multi-Targeting Changes

If you're getting errors about missing target frameworks in Visual Studio after the multi-targeting changes, follow these steps:

## Solution

### Option 1: Using the Cleanup Script (Recommended)

**Windows:**
1. **Close Visual Studio completely**
2. Run `clean-vs-cache.cmd` (double-click or run from command prompt)
3. Reopen the solution in Visual Studio

**Linux/Mac:**
```bash
./clean-vs-cache.sh
```

### Option 2: Manual Cleanup

**Close Visual Studio completely** before running these commands:

```bash
# Windows PowerShell
Remove-Item -Recurse -Force .vs, */obj, */bin
dotnet restore

# Linux/Mac or Git Bash
rm -rf .vs */obj */bin
dotnet restore
```

### Option 3: Within Visual Studio

1. Close the solution
2. Go to Tools → NuGet Package Manager → Package Manager Settings
3. Click "Clear All NuGet Cache(s)"
4. Close Visual Studio
5. Delete the `.vs` folder in the solution directory
6. Reopen Visual Studio and the solution
7. Right-click the solution → Restore NuGet Packages
8. Rebuild the solution

## Why This Happens

When switching from single-targeting (`net9.0`) to multi-targeting (`net6.0;net8.0;net9.0`), both Visual Studio and the .NET CLI cache asset files (`project.assets.json`) that become invalid. Visual Studio maintains its own cache in the `.vs` folder, which needs to be cleared separately from the command-line `obj` and `bin` folders.

## Prevention

After making project file changes (especially to `TargetFramework` or `TargetFrameworks`):
1. Close Visual Studio before building from command line
2. OR do a "Clean Solution" in Visual Studio before rebuilding
3. Keep the cleanup scripts handy for future use
