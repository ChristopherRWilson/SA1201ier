# SA1201ier

A command-line tool and MSBuild integration for automatically formatting C# files according to StyleCop rule SA1201, which requires elements to be ordered by access level.

## Features

- **Command-line tool** for formatting individual files or entire directories
- **MSBuild integration** for automatic formatting during build
- **Check mode** for CI/CD pipelines to verify formatting without modifying files
- **Comprehensive test suite** with extensive edge case coverage
- **Preserves comments, attributes, and formatting** while reordering members

## What is SA1201?

StyleCop rule SA1201 requires that elements in a C# type be ordered according to their access level and member type. The correct order is:

1. **Const fields** (public → internal → protected internal → protected → private protected → private)
2. **Static fields** (public → internal → protected internal → protected → private protected → private)
3. **Instance fields** (public → internal → protected internal → protected → private protected → private)
4. **Constructors** (public → internal → protected internal → protected → private protected → private)
5. **Destructors**
6. **Delegates** (public → internal → protected internal → protected → private protected → private)
7. **Events** (public → internal → protected internal → protected → private protected → private)
8. **Enums** (public → internal → protected internal → protected → private protected → private)
9. **Interfaces** (public → internal → protected internal → protected → private protected → private)
10. **Properties** (public → internal → protected internal → protected → private protected → private)
11. **Indexers** (public → internal → protected internal → protected → private protected → private)
12. **Methods** (public → internal → protected internal → protected → private protected → private)
13. **Structs** (public → internal → protected internal → protected → private protected → private)
14. **Classes** (public → internal → protected internal → protected → private protected → private)

## Installation

### As a .NET Tool (Recommended)

```bash
dotnet tool install --global SA1201ier
```

### As an MSBuild Package

Add to your project file:

```xml
<ItemGroup>
  <PackageReference Include="SA1201ier.MSBuild" Version="1.0.0" PrivateAssets="all" />
</ItemGroup>
```

## Usage

### Command Line

#### Format files

```bash
# Format a single file
SA1201ier path/to/YourFile.cs

# Format all C# files in a directory
SA1201ier path/to/your/project

# Check formatting without making changes (useful for CI/CD)
SA1201ier path/to/your/project --check

# Verbose output
SA1201ier path/to/your/project --verbose
```

#### Options

- `<path>` - Required. The file or directory path to format or check
- `-c, --check` - Check for formatting violations without making changes
- `-v, --verbose` - Show detailed output
- `-h, --help` - Show help message

### MSBuild Integration

Configure formatting in your `.csproj` file:

```xml
<PropertyGroup>
  <!-- Format files automatically on build -->
  <SA1201FormatOnBuild>true</SA1201FormatOnBuild>
  
  <!-- OR check formatting on build without modifying files -->
  <SA1201CheckOnBuild>true</SA1201CheckOnBuild>
  
  <!-- Fail the build if violations are found (only with CheckOnBuild) -->
  <SA1201FailOnViolations>true</SA1201FailOnViolations>
</PropertyGroup>
```

### CI/CD Integration

#### GitHub Actions

```yaml
name: Check Formatting

on: [push, pull_request]

jobs:
  format-check:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      
      - name: Install SA1201ier
        run: dotnet tool install --global SA1201ier
      
      - name: Check formatting
        run: SA1201ier . --check
```

#### Azure Pipelines

```yaml
- task: DotNetCoreCLI@2
  displayName: 'Install SA1201ier'
  inputs:
    command: 'custom'
    custom: 'tool'
    arguments: 'install --global SA1201ier'

- task: DotNetCoreCLI@2
  displayName: 'Check SA1201 Formatting'
  inputs:
    command: 'custom'
    custom: 'SA1201ier'
    arguments: '. --check'
```

## Building from Source

```bash
# Clone the repository
git clone https://github.com/yourusername/SA1201ier.git
cd SA1201ier

# Build the solution
dotnet build SA1201ier.sln

# Run tests
dotnet test SA1201ier.sln

# Pack for distribution
dotnet pack SA1201ier.sln
```

## Project Structure

- **SA1201ier** - Command-line interface
- **SA1201ier.Core** - Core formatting logic using Roslyn
- **SA1201ier.MSBuild** - MSBuild task integration
- **SA1201ier.Tests** - Comprehensive test suite

## Examples

### Before Formatting

```csharp
public class MyClass
{
    private void PrivateMethod() { }
    public int PublicProperty { get; set; }
    private int PrivateField;
    public static int PublicStaticField;
    public MyClass() { }
    public void PublicMethod() { }
}
```

### After Formatting

```csharp
public class MyClass
{
    public static int PublicStaticField;
    private int PrivateField;
    
    public MyClass() { }
    
    public int PublicProperty { get; set; }
    
    public void PublicMethod() { }
    private void PrivateMethod() { }
}
```

## Future Enhancements

- Visual Studio extension for real-time formatting
- EditorConfig support for customization
- Integration with popular formatters like CSharpier
- Support for additional StyleCop ordering rules

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- Based on [StyleCop Analyzers](https://github.com/DotNetAnalyzers/StyleCopAnalyzers) SA1201 rule
- Inspired by [CSharpier](https://csharpier.com/)

## Related Projects

- [StyleCop Analyzers](https://github.com/DotNetAnalyzers/StyleCopAnalyzers) - Comprehensive StyleCop rule analyzers
- [CSharpier](https://csharpier.com/) - Opinionated C# code formatter
- [dotnet-format](https://github.com/dotnet/format) - Official .NET code formatter
