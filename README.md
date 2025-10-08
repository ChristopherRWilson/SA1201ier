# SA1201ier

A command-line tool and MSBuild integration for automatically formatting C# files (including Razor files) according to StyleCop rule SA1201, which requires elements to be ordered by access level.

## Features

- **Command-line tool** for formatting individual files or entire directories
- **Razor file support** - formats C# code within `@code` blocks in Razor files
- **MSBuild integration** for automatic formatting during build
- **Check mode** for CI/CD pipelines to verify formatting without modifying files
- **Comprehensive test suite** with extensive edge case coverage
- **Preserves comments, attributes, and formatting** while reordering members
- **Respects preprocessor directives and regions** - members within `#if`/`#endif` or `#region`/`#endregion` blocks are kept together and reordered independently
- **Flexible hierarchical configuration** - use `.sa1201ierrc` files to configure options per directory
- **Fully customizable sorting** - configure access level order, member type order, and more
- **Alphabetical sorting** - optionally sort members alphabetically within same group
- **Top-level type sorting** - optionally sort classes, records, interfaces by access level
- **Static/instance control** - choose whether static or instance members come first
- **Const/non-const control** - choose whether const or non-const members come first

## What is SA1201?

StyleCop rule SA1201 requires that elements in a C# type be ordered according to their access level and member type. SA1201ier implements this rule with full customization support.

### Default Ordering

The default order follows the SA1201 standard:

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

### Customization

**All of this is customizable!** You can:
- Reverse the access level order (private → public)
- Reorder member types (properties before fields, methods before constructors, etc.)
- Choose whether static or instance members come first
- Choose whether const or non-const members come first
- Sort members alphabetically
- And much more...

See the [Configuration](#configuration) section below for details.

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

# Sort members alphabetically within same access level
SA1201ier path/to/your/project --alphabetical-sort

# Sort top-level types by access level
SA1201ier path/to/your/project --sort-top-level-types

# Create a sample .sa1201ierrc config file
SA1201ier --init-config
```

#### Options

- `<path>` - Required. The file or directory path to format or check
- `-c, --check` - Check for formatting violations without making changes
- `-v, --verbose` - Show detailed output
- `--alphabetical-sort` - Sort members alphabetically within same group
- `--sort-top-level-types` - Sort top-level types (classes, records, etc.) by access level
- `--init-config` - Create a sample .sa1201ierrc configuration file with all available options
- `-h, --help` - Show help message

**Note:** For advanced customization like custom access level order, custom member type order, and static/const preferences, use a `.sa1201ierrc` configuration file rather than CLI options.

## Configuration

SA1201ier supports powerful, flexible configuration through `.sa1201ierrc` JSON files. You can customize sorting behavior for different parts of your codebase with hierarchical configuration that merges from parent to child directories.

### Quick Start

Create a configuration file in your project root:

```bash
# Create a sample .sa1201ierrc with all available options
SA1201ier --init-config
```

This creates a `.sa1201ierrc` file with comprehensive examples and documentation.

### Available Configuration Options

All options are optional and have sensible defaults. You can configure:

#### Boolean Options

| Option | Default | Description |
|--------|---------|-------------|
| `alphabeticalSort` | `false` | Sort members alphabetically within same group |
| `sortTopLevelTypes` | `false` | Sort classes, interfaces, etc. by access level |
| `staticMembersFirst` | `true` | Put static members before instance members |
| `constMembersFirst` | `true` | Put const members before non-const members |

#### Custom Ordering Arrays

| Option | Description | Valid Values |
|--------|-------------|--------------|
| `accessLevelOrder` | Custom order for access modifiers | `"Public"`, `"Internal"`, `"ProtectedInternal"`, `"Protected"`, `"PrivateProtected"`, `"Private"` |
| `memberTypeOrder` | Custom order for member types | `"Field"`, `"Constructor"`, `"Destructor"`, `"Delegate"`, `"Event"`, `"Enum"`, `"Interface"`, `"Property"`, `"Indexer"`, `"Method"`, `"Struct"`, `"Class"` |
| `topLevelTypeOrder` | Custom order for top-level types | `"Enum"`, `"Interface"`, `"Struct"`, `"Class"` |

### Configuration Examples

#### Basic Configuration

```json
{
  "alphabeticalSort": true,
  "sortTopLevelTypes": false
}
```

#### Custom Access Level Order (Private-First Style)

Some teams prefer private implementation details at the top:

```json
{
  "accessLevelOrder": ["Private", "Protected", "Internal", "Public"],
  "staticMembersFirst": false,
  "constMembersFirst": false
}
```

#### Custom Member Type Order (Properties-First Style)

Useful for DTOs and POCOs:

```json
{
  "alphabeticalSort": true,
  "memberTypeOrder": ["Property", "Constructor", "Method", "Field"]
}
```

#### Complete Custom Configuration

```json
{
  "alphabeticalSort": true,
  "sortTopLevelTypes": true,
  "staticMembersFirst": false,
  "constMembersFirst": true,
  "accessLevelOrder": ["Private", "Protected", "Internal", "Public"],
  "memberTypeOrder": ["Constructor", "Property", "Method", "Field"],
  "topLevelTypeOrder": ["Class", "Interface", "Enum", "Struct"]
}
```

### Hierarchical Configuration

SA1201ier searches for `.sa1201ierrc` files from the file being formatted up to the root directory, merging all configurations it finds. Child directories override parent settings.

**Example Directory Structure:**

```
my-project/
├── .sa1201ierrc                    # Project defaults
├── src/
│   ├── .sa1201ierrc                # Override for src/
│   ├── Dtos/
│   │   ├── .sa1201ierrc            # Custom order for DTOs
│   │   └── UserDto.cs              # Uses Dtos config
│   └── Controllers/
│       ├── .sa1201ierrc            # Custom order for Controllers
│       └── UserController.cs       # Uses Controllers config
└── tests/
    ├── .sa1201ierrc                # Different rules for tests
    └── UserTests.cs
```

**Root `.sa1201ierrc`:**
```json
{
  "alphabeticalSort": false,
  "sortTopLevelTypes": false
}
```

**`src/Dtos/.sa1201ierrc`:**
```json
{
  "alphabeticalSort": true,
  "memberTypeOrder": ["Property", "Constructor", "Method"]
}
```

**Effective configuration for `UserDto.cs`:**
- `alphabeticalSort`: `true` (from Dtos config)
- `sortTopLevelTypes`: `false` (from root config)
- `memberTypeOrder`: `["Property", "Constructor", "Method"]` (from Dtos config)

### Configuration Priority

Settings are applied in this order (highest priority first):

1. **Command-line options** - Override everything
2. **Nearest `.sa1201ierrc`** - In the file's directory
3. **Parent `.sa1201ierrc` files** - Walking up the directory tree
4. **Default values** - Built-in defaults

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
git clone https://github.com/ChristopherRWilson/SA1201ier.git
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

### Preprocessor Directives and Regions

SA1201ier respects preprocessor directives and region boundaries. Members within these blocks are treated as separate groups and reordered independently:

```csharp
// Before
public class MyClass
{
#region Private Fields
    private int field2;
    private int field1;
#endregion

#region Public Fields
    public int publicField2;
    public int publicField1;
#endregion

#if DEBUG
    private void DebugMethod() { }
#endif

    private void PrivateMethod() { }
    public void PublicMethod() { }
}

// After
public class MyClass
{
#region Private Fields
    private int field2;
    private int field1;
#endregion

#region Public Fields
    public int publicField2;
    public int publicField1;
#endregion

#if DEBUG
    private void DebugMethod() { }
#endif

    public void PublicMethod() { }
    private void PrivateMethod() { }
}
```

Note: Members within regions and preprocessor blocks maintain their relative order, while members outside these blocks are reordered according to SA1201 rules.

## Razor File Support

SA1201ier automatically detects and formats C# code within `@code` blocks in Razor files (`.razor`). The formatter:

- Extracts all `@code` blocks from the Razor file
- Formats the C# code according to SA1201 rules
- Preserves all Razor markup and directives
- Reinserts the formatted code back into the Razor file

**Example:**

```razor
@page "/counter"

<PageTitle>Counter</PageTitle>

<h1>Counter</h1>

<p role="status">Current count: @currentCount</p>

<button class="btn btn-primary" @onclick="IncrementCount">Click me</button>

@code {
    // Before formatting
    private void IncrementCount()
    {
        currentCount++;
    }
    public int currentCount = 0;
    private void Reset()
    {
        currentCount = 0;
    }
}
```

After formatting:

```razor
@page "/counter"

<PageTitle>Counter</PageTitle>

<h1>Counter</h1>

<p role="status">Current count: @currentCount</p>

<button class="btn btn-primary" @onclick="IncrementCount">Click me</button>

@code {
    // After formatting - fields before methods, public before private
    public int currentCount = 0;
    
    public void IncrementCount()
    {
        currentCount++;
    }
    private void Reset()
    {
        currentCount = 0;
    }
}
```

**Usage:**

```bash
# Format a single Razor file
SA1201ier MyComponent.razor

# Format all C# and Razor files in a directory
SA1201ier ./Pages

# Check Razor files without formatting
SA1201ier ./Pages --check
```

**Note:** SA1201ier processes only the C# code within `@code` blocks. All other Razor syntax (HTML, directives, inline C# expressions) is preserved unchanged.

### Alphabetical Sorting

Enable alphabetical sorting within the same group of members:

**Configuration:**
```json
{
  "alphabeticalSort": true
}
```

**Or use CLI:**
```bash
SA1201ier path/to/your/project --alphabetical-sort
```

**Example:**
```csharp
// Before
public class MyClass
{
    public string Name { get; set; }
    public int Age { get; set; }
    public string Email { get; set; }
}

// After (with alphabetical sorting)
public class MyClass
{
    public int Age { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
}
```

**Use Cases:**
- DTOs with many properties become easier to navigate
- Reduces merge conflicts when multiple developers add properties
- Makes finding specific members easier in large classes

### Top-Level Type Sorting

Sort classes, interfaces, enums, and structs by access level:

**Configuration:**
```json
{
  "sortTopLevelTypes": true
}
```

**Or use CLI:**
```bash
SA1201ier path/to/your/project --sort-top-level-types
```

**Example:**
```csharp
// Before
namespace MyApp;

internal class InternalHelper { }
public class PublicApi { }
public record PublicRecord { }
internal record InternalRecord { }

// After (with top-level type sorting)
namespace MyApp;

public class PublicApi { }
public record PublicRecord { }
internal record InternalRecord { }
internal class InternalHelper { }
```

**Use Cases:**
- Files with multiple type definitions
- Ensures public API appears first
- Makes file structure more predictable

### Static vs Instance Members

Control whether static members appear before or after instance members:

**Configuration:**
```json
{
  "staticMembersFirst": false
}
```

**Example:**
```csharp
// With staticMembersFirst: true (default)
public class MyClass
{
    public static int StaticField;
    public int InstanceField;
}

// With staticMembersFirst: false
public class MyClass
{
    public int InstanceField;
    public static int StaticField;
}
```

### Const vs Non-Const Members

Control whether const members appear before or after non-const members:

**Configuration:**
```json
{
  "constMembersFirst": false
}
```

**Example:**
```csharp
// With constMembersFirst: true (default)
public class MyClass
{
    public const int MaxValue = 100;
    public int Value;
}

// With constMembersFirst: false
public class MyClass
{
    public int Value;
    public const int MaxValue = 100;
}
```

### Custom Access Level Order

Define your own access level ordering. The default follows SA1201 (public → private), but you can reverse it or use any custom order:

**Configuration:**
```json
{
  "accessLevelOrder": ["Private", "Protected", "Internal", "Public"]
}
```

**Example:**
```csharp
// With custom order above
public class MyClass
{
    private int _privateField;
    protected int ProtectedField;
    internal int InternalField;
    public int PublicField;
}
```

**Use Cases:**
- Private-first development style
- Implementation-before-interface coding
- Custom team conventions

### Custom Member Type Order

Define your own member type ordering. The default follows SA1201 (fields → constructors → properties → methods), but you can customize it:

**Configuration:**
```json
{
  "memberTypeOrder": ["Constructor", "Property", "Method", "Field"]
}
```

**Example:**
```csharp
// With custom order above
public class MyClass
{
    // Constructors first
    public MyClass() { }
    
    // Then properties
    public int Id { get; set; }
    public string Name { get; set; }
    
    // Then methods
    public void DoSomething() { }
    
    // Then fields
    private int _field;
}
```

**Use Cases:**
- DTOs where properties should be prominent
- Test classes where setup methods should be first
- Custom team conventions

### Custom Top-Level Type Order

Define your own top-level type ordering:

**Configuration:**
```json
{
  "sortTopLevelTypes": true,
  "topLevelTypeOrder": ["Class", "Interface", "Enum", "Struct"]
}
```

**Example:**
```csharp
// With custom order above
namespace MyApp;

// Classes first
public class MyClass { }

// Then interfaces
public interface IMyInterface { }

// Then enums
public enum MyEnum { }

// Then structs
public struct MyStruct { }
```

### Real-World Configuration Examples

#### Example 1: DTO Configuration

For Data Transfer Objects, prioritize properties and enable alphabetical sorting:

```json
{
  "alphabeticalSort": true,
  "memberTypeOrder": ["Property", "Constructor", "Method", "Field"],
  "staticMembersFirst": false
}
```

#### Example 2: Private-First Development Style

Some teams prefer seeing private implementation first:

```json
{
  "accessLevelOrder": [
    "Private",
    "PrivateProtected",
    "Protected",
    "ProtectedInternal",
    "Internal",
    "Public"
  ],
  "constMembersFirst": false,
  "staticMembersFirst": false
}
```

#### Example 3: Test Class Configuration

Test classes often benefit from different organization:

```json
{
  "alphabeticalSort": true,
  "memberTypeOrder": ["Field", "Constructor", "Method", "Property"]
}
```

#### Example 4: Controller Configuration

For MVC/API controllers:

```json
{
  "alphabeticalSort": false,
  "memberTypeOrder": ["Constructor", "Method", "Field", "Property"],
  "accessLevelOrder": ["Public", "Private", "Protected", "Internal"]
}
```

### Using Configuration in Your Workflow

#### For New Projects

Start with a project-wide configuration:

```bash
cd my-project
SA1201ier --init-config
# Edit .sa1201ierrc to your preferences
git add .sa1201ierrc
git commit -m "Add SA1201ier configuration"
```

#### For Existing Projects

Use hierarchical configuration for gradual adoption:

```
my-project/
├── .sa1201ierrc              # Minimal changes by default
└── src/
    └── NewFeature/
        └── .sa1201ierrc      # Strict formatting for new code
```

#### For Mixed Codebases

Different rules for different areas:

```
my-project/
├── .sa1201ierrc              # Project defaults
├── src/
│   ├── Dtos/
│   │   └── .sa1201ierrc      # Alphabetical, properties-first
│   ├── Controllers/
│   │   └── .sa1201ierrc      # Public methods first
│   └── Models/
│       └── .sa1201ierrc      # Standard SA1201
└── tests/
    └── .sa1201ierrc          # Alphabetical for tests
```

### Command-Line Overrides

Command-line options override all configuration files:

```bash
# Override config files with CLI options
SA1201ier ./src --alphabetical-sort

# Test configuration before committing
SA1201ier ./src/Models --alphabetical-sort --check

# Apply multiple options
SA1201ier ./src --alphabetical-sort --sort-top-level-types
```

### Best Practices

1. **Start Simple** - Begin with default settings and add customization as needed
2. **Document Your Choices** - Use JSON comments to explain why you chose specific settings
3. **Use Hierarchical Config** - Define project defaults at root, override for specific areas
4. **Test Before Committing** - Use `--check` to preview changes before applying
5. **Version Control** - Always commit `.sa1201ierrc` files to ensure team consistency

## Troubleshooting

### Configuration Not Working

- Ensure the file is named exactly `.sa1201ierrc` (with leading dot)
- Verify JSON syntax is valid (use a JSON validator or check for common errors)
- Use `--verbose` to see which config files are being loaded
- Remember: CLI options override all config files

### Finding the Right Configuration

If you're unsure what configuration to use:

1. Create a sample config: `SA1201ier --init-config`
2. Test different options: `SA1201ier ./src --alphabetical-sort --check`
3. Apply what works: Copy successful CLI options to your `.sa1201ierrc`

### Unexpected Ordering

- Check for multiple `.sa1201ierrc` files in parent directories
- Verify custom order arrays have correct values (case-sensitive)
- Use `--verbose` to see which configuration is being applied

## Future Enhancements

- Integration with popular formatters like CSharpier
- Support for additional StyleCop ordering rules  
- Visual Studio extension
- `preserveRegionOrder` option to prevent reordering within regions
- `ignorePatterns` for excluding files/directories

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the GPL3 License - see the LICENSE file for details.

## Acknowledgments

- Based on [StyleCop Analyzers](https://github.com/DotNetAnalyzers/StyleCopAnalyzers) SA1201 rule
- Inspired by [CSharpier](https://csharpier.com/)

## Related Projects

- [StyleCop Analyzers](https://github.com/DotNetAnalyzers/StyleCopAnalyzers) - Comprehensive StyleCop rule analyzers
- [CSharpier](https://csharpier.com/) - Opinionated C# code formatter
- [dotnet-format](https://github.com/dotnet/format) - Official .NET code formatter
