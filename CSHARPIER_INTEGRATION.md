# SA1201ier and CSharpier Integration Guide

## Overview

SA1201ier and CSharpier complement each other perfectly:
- **SA1201ier** - Reorders class members according to StyleCop SA1201 rules
- **CSharpier** - Formats code according to opinionated style rules (indentation, spacing, line breaks, etc.)

When used together, you get both consistent member ordering AND consistent code formatting.

## How They Work Together

### Execution Order

SA1201ier is configured to run **before** CSharpier in the MSBuild pipeline:

1. **SA1201ier** runs first (target: `FormatSA1201`, before: `CSharpierFormat`)
   - Reorders members by access level and type
   - Preserves existing formatting, comments, and attributes
   
2. **CSharpier** runs second (target: `CSharpierFormat`)
   - Formats the reordered code
   - Applies consistent indentation, spacing, line breaks

### Why This Order Matters

Running SA1201ier before CSharpier ensures:
- Member reordering happens first
- CSharpier can then apply its formatting to the final structure
- No conflicts or repeated reformatting
- Optimal performance (single pass through each file)

## Installation

### Option 1: Both Packages in Your Project

```xml
<ItemGroup>
  <PackageReference Include="SA1201ier.MSBuild" Version="*" PrivateAssets="all" />
  <PackageReference Include="CSharpier.MSBuild" Version="*" PrivateAssets="all" />
</ItemGroup>
```

### Option 2: Directory.Build.props (Recommended for Solutions)

Create `Directory.Build.props` in your solution root:

```xml
<Project>
  <ItemGroup>
    <PackageReference Include="SA1201ier.MSBuild" Version="*" PrivateAssets="all" />
    <PackageReference Include="CSharpier.MSBuild" Version="*" PrivateAssets="all" />
  </ItemGroup>
  
  <PropertyGroup>
    <!-- SA1201ier: Format on build -->
    <SA1201FormatOnBuild>true</SA1201FormatOnBuild>
    
    <!-- CSharpier: Format on build (not check) -->
    <CSharpier_Check>false</CSharpier_Check>
  </PropertyGroup>
</Project>
```

This applies to all projects in your solution automatically.

## Configuration

### Basic Setup (Format on Build)

```xml
<PropertyGroup>
  <!-- SA1201ier: Reorder members on build -->
  <SA1201FormatOnBuild>true</SA1201FormatOnBuild>
  
  <!-- CSharpier: Format on build -->
  <CSharpier_Check>false</CSharpier_Check>
</PropertyGroup>
```

### CI/CD Setup (Check Mode)

```xml
<PropertyGroup Condition="'$(CI)' == 'true'">
  <!-- SA1201ier: Check without modifying -->
  <SA1201CheckOnBuild>true</SA1201CheckOnBuild>
  <SA1201FailOnViolations>true</SA1201FailOnViolations>
  
  <!-- CSharpier: Check without modifying -->
  <CSharpier_Check>true</CSharpier_Check>
</PropertyGroup>
```

### Development Setup (Format Locally, Check in CI)

```xml
<!-- Format automatically during local development -->
<PropertyGroup Condition="'$(CI)' != 'true'">
  <SA1201FormatOnBuild>true</SA1201FormatOnBuild>
  <CSharpier_Check>false</CSharpier_Check>
</PropertyGroup>

<!-- Check without modifying in CI -->
<PropertyGroup Condition="'$(CI)' == 'true'">
  <SA1201CheckOnBuild>true</SA1201CheckOnBuild>
  <SA1201FailOnViolations>true</SA1201FailOnViolations>
  <CSharpier_Check>true</CSharpier_Check>
</PropertyGroup>
```

## Configuration Files

Both tools support configuration files that work together:

### SA1201ier Configuration (`.sa1201ierrc`)

```json
{
  "alphabeticalSort": true,
  "memberTypeOrder": ["Constructor", "Property", "Method", "Field"]
}
```

### CSharpier Configuration (`.csharpierrc`)

```json
{
  "printWidth": 100,
  "useTabs": false,
  "tabWidth": 4,
  "endOfLine": "lf"
}
```

Both files can coexist in the same directory and will be respected by their respective tools.

## Usage Examples

### Example 1: Simple Integration

**Input file:**
```csharp
public class Example
{
    private void PrivateMethod() { }
    public int PublicField;
    private int PrivateField;
    public void PublicMethod() { }
}
```

**After SA1201ier (member reordering):**
```csharp
public class Example
{
    public int PublicField;
    private int PrivateField;
    public void PublicMethod() { }
    private void PrivateMethod() { }
}
```

**After CSharpier (code formatting):**
```csharp
public class Example
{
    public int PublicField;
    private int PrivateField;

    public void PublicMethod() { }

    private void PrivateMethod() { }
}
```

### Example 2: With Custom SA1201ier Configuration

**`.sa1201ierrc`:**
```json
{
  "alphabeticalSort": true,
  "memberTypeOrder": ["Property", "Constructor", "Method"]
}
```

**Result:** Members are ordered by type (Property → Constructor → Method), then by access level, then alphabetically.

## Command-Line Usage

You can also use both tools from the command line:

```bash
# Run SA1201ier first
SA1201ier ./src

# Then run CSharpier
dotnet csharpier ./src
```

Or create a script:

```bash
#!/bin/bash
# format.sh
echo "Reordering members..."
SA1201ier ./src

echo "Formatting code..."
dotnet csharpier ./src

echo "Done!"
```

## Troubleshooting

### Issue: CSharpier Runs Before SA1201ier

**Symptom:** Code formatting is correct but member ordering is wrong.

**Solution:** Ensure SA1201ier.MSBuild package is referenced before CSharpier.MSBuild in your `.csproj`:

```xml
<ItemGroup>
  <!-- This order doesn't matter, but keep them together for clarity -->
  <PackageReference Include="SA1201ier.MSBuild" Version="*" PrivateAssets="all" />
  <PackageReference Include="CSharpier.MSBuild" Version="*" PrivateAssets="all" />
</ItemGroup>
```

The target ordering is handled automatically by the `BeforeTargets="CSharpierFormat"` attribute in SA1201ier's targets file.

### Issue: Both Tools Run on Every Build (Slow)

**Solution:** Use conditional formatting based on configuration:

```xml
<!-- Only format in Debug builds -->
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <SA1201FormatOnBuild>true</SA1201FormatOnBuild>
  <CSharpier_Check>false</CSharpier_Check>
</PropertyGroup>

<!-- Check (don't format) in Release builds -->
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <SA1201CheckOnBuild>true</SA1201CheckOnBuild>
  <CSharpier_Check>true</CSharpier_Check>
</PropertyGroup>
```

### Issue: Files Keep Changing After Build

**Symptom:** Files are modified on every build even though they appear correct.

**Possible causes:**
1. Conflicting configurations between tools
2. Line ending differences
3. Indentation conflicts

**Solution:**
1. Ensure `.editorconfig` settings are compatible with both tools
2. Use consistent line endings (configure in `.gitattributes`)
3. Verify both tools are using the same indentation settings

## Best Practices

### 1. Use Directory.Build.props

Apply to all projects in your solution:

```xml
<!-- Directory.Build.props in solution root -->
<Project>
  <ItemGroup>
    <PackageReference Include="SA1201ier.MSBuild" Version="*" PrivateAssets="all" />
    <PackageReference Include="CSharpier.MSBuild" Version="*" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

### 2. Format Locally, Check in CI

```xml
<PropertyGroup Condition="'$(CI)' != 'true'">
  <SA1201FormatOnBuild>true</SA1201FormatOnBuild>
  <CSharpier_Check>false</CSharpier_Check>
</PropertyGroup>

<PropertyGroup Condition="'$(CI)' == 'true'">
  <SA1201CheckOnBuild>true</SA1201CheckOnBuild>
  <SA1201FailOnViolations>true</SA1201FailOnViolations>
  <CSharpier_Check>true</CSharpier_Check>
</PropertyGroup>
```

### 3. Commit Configuration Files

Always commit both `.sa1201ierrc` and `.csharpierrc` to version control for team consistency.

### 4. Run Once Before Committing Configuration

When first adding these tools to a project:

```bash
# Initial format
SA1201ier ./src
dotnet csharpier ./src

# Review changes
git diff

# Commit in separate commit
git add .
git commit -m "Apply SA1201ier and CSharpier formatting"
```

### 5. Document in README

Add a note to your project's README:

```markdown
## Code Formatting

This project uses:
- **SA1201ier** for member ordering (SA1201 compliance)
- **CSharpier** for code formatting

Both tools run automatically on build in Debug mode.
```

## Advanced Integration

### Pre-commit Hook

Use with husky or other git hooks:

```bash
#!/bin/sh
# .husky/pre-commit

# Format files being committed
git diff --cached --name-only --diff-filter=ACM | grep "\.cs$" | xargs SA1201ier
git diff --cached --name-only --diff-filter=ACM | grep "\.cs$" | xargs dotnet csharpier

# Re-add formatted files
git diff --cached --name-only --diff-filter=ACM | grep "\.cs$" | xargs git add
```

### GitHub Actions

```yaml
name: Code Format Check

on: [push, pull_request]

jobs:
  format:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      
      - name: Install SA1201ier
        run: dotnet tool install --global SA1201ier
      
      - name: Install CSharpier
        run: dotnet tool install --global csharpier
      
      - name: Check SA1201 compliance
        run: SA1201ier ./src --check
      
      - name: Check CSharpier formatting
        run: dotnet csharpier ./src --check
```

## Summary

✅ SA1201ier and CSharpier work together seamlessly  
✅ SA1201ier runs first to reorder members  
✅ CSharpier runs second to format code  
✅ Both respect their own configuration files  
✅ Both support check mode for CI/CD  
✅ No conflicts or repeated formatting  

The combination gives you complete control over both code structure (member ordering) and code style (formatting).
