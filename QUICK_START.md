# Quick Start Guide - SA1201ier

## Installation

### Install as Global Tool (Recommended)

```bash
dotnet tool install --global SA1201ier.Cli --version 1.0.0
```

### Install as MSBuild Package

Add to your `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="SA1201ier.MSBuild" Version="1.0.0" PrivateAssets="all" />
</ItemGroup>
```

## Basic Usage

### Format a Single File

```bash
sa1201ier MyClass.cs
```

### Format an Entire Project

```bash
sa1201ier ./src
```

### Check Without Formatting (CI/CD)

```bash
sa1201ier ./src --check
```

Exit code 0 = properly formatted, 1 = violations found

### Verbose Output

```bash
sa1201ier ./src --verbose
```

## MSBuild Integration

### Option 1: Format on Every Build

```xml
<PropertyGroup>
  <SA1201FormatOnBuild>true</SA1201FormatOnBuild>
</PropertyGroup>
```

### Option 2: Check on Build (Warnings)

```xml
<PropertyGroup>
  <SA1201CheckOnBuild>true</SA1201CheckOnBuild>
</PropertyGroup>
```

### Option 3: Check on Build (Fail Build)

```xml
<PropertyGroup>
  <SA1201CheckOnBuild>true</SA1201CheckOnBuild>
  <SA1201FailOnViolations>true</SA1201FailOnViolations>
</PropertyGroup>
```

## CI/CD Integration

### GitHub Actions

```yaml
name: Format Check

on: [push, pull_request]

jobs:
  check-format:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      
      - name: Install SA1201 Formatter
        run: dotnet tool install --global SA1201.Cli --version 1.0.0
      
      - name: Check Formatting
        run: sa1201ier . --check
```

### Azure Pipelines

```yaml
steps:
- task: DotNetCoreCLI@2
  displayName: 'Install SA1201 Formatter'
  inputs:
    command: 'custom'
    custom: 'tool'
    arguments: 'install --global SA1201.Cli --version 1.0.0'

- script: sa1201ier . --check
  displayName: 'Check SA1201 Formatting'
```

### GitLab CI

```yaml
format-check:
  image: mcr.microsoft.com/dotnet/sdk:9.0
  script:
    - dotnet tool install --global SA1201.Cli --version 1.0.0
    - export PATH="$PATH:$HOME/.dotnet/tools"
    - sa1201ier . --check
```

## What Gets Reordered?

### Before SA1201 Formatting
```csharp
public class MyClass
{
    private void PrivateMethod() { }      // ❌ Methods before fields
    public int PublicProperty { get; set; } // ❌ Property before constructor
    private int _field;                     // ❌ Field after method
    public MyClass() { }                    // ❌ Constructor after property
    public void PublicMethod() { }          // ❌ Public after private
}
```

### After SA1201 Formatting
```csharp
public class MyClass
{
    private int _field;                     // ✅ Fields first
    
    public MyClass() { }                    // ✅ Constructor after fields
    
    public int PublicProperty { get; set; } // ✅ Properties after constructor
    
    public void PublicMethod() { }          // ✅ Public methods before private
    private void PrivateMethod() { }        // ✅ Private methods last
}
```

## Ordering Rules

Members are ordered by:

1. **Member Type**: Fields → Constructors → Properties → Methods → etc.
2. **Modifiers**: Const → Static → Instance
3. **Access Level**: Public → Internal → Protected → Private

### Complete Order

1. Const fields (public → private)
2. Static fields (public → private)
3. Instance fields (public → private)
4. Constructors (public → private)
5. Destructors
6. Delegates (public → private)
7. Events (public → private)
8. Enums (public → private)
9. Interfaces (public → private)
10. Properties (public → private)
11. Indexers (public → private)
12. Methods (public → private)
13. Structs (public → private)
14. Classes (public → private)

## Examples

### Example 1: Basic Reordering

```bash
# Create a test file
cat > Test.cs << 'EOF'
public class Test {
    private int _b;
    public int A;
}
EOF

# Format it
sa1201ier Test.cs

# Result: public field A now comes before private field _b
```

### Example 2: CI/CD Check

```bash
# In your CI pipeline
if sa1201ier ./src --check; then
    echo "✅ All files are properly formatted"
else
    echo "❌ Formatting violations found"
    exit 1
fi
```

### Example 3: Pre-commit Hook

```bash
# .git/hooks/pre-commit
#!/bin/bash
dotnet tool restore
staged_cs_files=$(git diff --cached --name-only --diff-filter=ACM | grep '\.cs$')

if [ -n "$staged_cs_files" ]; then
    echo "Checking SA1201 formatting..."
    if ! sa1201ier $staged_cs_files --check; then
        echo "❌ Please fix SA1201 violations before committing"
        echo "Run: sa1201ier . to auto-fix"
        exit 1
    fi
fi
```

## Troubleshooting

### Issue: Tool not found after installation

```bash
# Add tools to PATH
export PATH="$PATH:$HOME/.dotnet/tools"  # Linux/Mac
$env:PATH += ";$HOME\.dotnet\tools"      # Windows PowerShell
```

### Issue: MSBuild task not running

Check that the package is properly installed:
```bash
dotnet list package | grep SA1201
```

Ensure properties are set in `.csproj`:
```xml
<SA1201FormatOnBuild>true</SA1201FormatOnBuild>
```

### Issue: Files not being formatted

1. Check file is a `.cs` file
2. Verify file contains actual violations
3. Run with `--verbose` flag for details

## Getting Help

```bash
# Show help
sa1201ier --help

# Get version
dotnet tool list --global | grep SA1201
```

## Uninstall

```bash
# Remove global tool
dotnet tool uninstall --global SA1201.Cli

# Remove from project
dotnet remove package SA1201ier.MSBuild
```

## Additional Resources

- **Documentation**: See README-NEW.md
- **Examples**: See examples/ directory
- **Project Summary**: See PROJECT_SUMMARY.md
- **Source Code**: https://github.com/ChristopherRWilson/SA1201ier

## Support

For issues or questions, please open an issue on GitHub:
https://github.com/ChristopherRWilson/SA1201ier/issues
