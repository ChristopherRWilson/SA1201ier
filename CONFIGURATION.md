# SA1201ier Configuration Guide

## Overview

SA1201ier supports flexible configuration through `.sa1201ierrc` JSON files and command-line options. This allows you to customize formatting behavior for different parts of your codebase.

## Configuration Methods

### 1. Command-Line Options (Highest Priority)

Command-line options override all configuration files:

```bash
SA1201ier path/to/code --alphabetical-sort --sort-top-level-types
```

### 2. Configuration Files (`.sa1201ierrc`)

JSON files that can be placed in any directory to configure options for that directory and its subdirectories.

## Available Options

### `alphabeticalSort` (boolean, default: `false`)

When enabled, members with the same member type, access level, and modifiers (static/const) are sorted alphabetically by name.

**Example:**
```csharp
// With alphabeticalSort: true
public class User
{
    // Properties sorted alphabetically within public access level
    public int Age { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
}
```

### `sortTopLevelTypes` (boolean, default: `false`)

When enabled, top-level types (classes, records, interfaces, enums, structs) within a file are sorted by access level.

**Example:**
```csharp
// With sortTopLevelTypes: true
namespace MyApp;

// Public types first
public class PublicApi { }
public interface IPublicService { }

// Then internal types
internal class InternalHelper { }
internal record InternalDto { }
```

## Configuration File Format

### Basic Example

```json
{
  "alphabeticalSort": false,
  "sortTopLevelTypes": false
}
```

### With Comments (JSON5 style)

```json
{
  // Sort members alphabetically within same access level
  "alphabeticalSort": true,
  
  // Sort top-level types by access level
  "sortTopLevelTypes": false
}
```

## Hierarchical Configuration

SA1201ier searches for `.sa1201ierrc` files starting from the file being formatted, walking up the directory tree to the root. All found configurations are merged, with child directories overriding parent directories.

### Example Directory Structure

```
my-project/
├── .sa1201ierrc              # Project defaults
├── src/
│   ├── .sa1201ierrc          # Overrides for src/
│   ├── Controllers/
│   │   ├── .sa1201ierrc      # Overrides for Controllers/
│   │   └── UserController.cs
│   ├── Models/
│   │   └── User.cs
│   └── Services/
│       └── UserService.cs
└── tests/
    ├── .sa1201ierrc          # Different config for tests
    └── UserTests.cs
```

### Configuration Merge Example

**Root `.sa1201ierrc`:**
```json
{
  "alphabeticalSort": false,
  "sortTopLevelTypes": false
}
```

**`src/.sa1201ierrc`:**
```json
{
  "alphabeticalSort": true
}
```

**`src/Controllers/.sa1201ierrc`:**
```json
{
  "sortTopLevelTypes": true
}
```

**Effective Configuration:**
- `src/Models/User.cs`: `{ alphabeticalSort: true, sortTopLevelTypes: false }`
- `src/Controllers/UserController.cs`: `{ alphabeticalSort: true, sortTopLevelTypes: true }`
- `tests/UserTests.cs`: Uses `tests/.sa1201ierrc` settings

## Command-Line Interface

### Creating a Config File

```bash
# Create a sample .sa1201ierrc in current directory
SA1201ier --init-config

# Create in specific directory
cd my-project/src
SA1201ier --init-config
```

### Using CLI Options

```bash
# Enable alphabetical sorting for this run only
SA1201ier ./src --alphabetical-sort

# Enable both options
SA1201ier ./src --alphabetical-sort --sort-top-level-types

# CLI options override config files
SA1201ier ./src --alphabetical-sort  # Overrides .sa1201ierrc
```

### Checking Configuration

```bash
# Check mode respects configuration
SA1201ier ./src --check --verbose

# The verbose output will show which files have violations
# based on the effective configuration for each file
```

## Use Cases

### Use Case 1: Strict Alphabetical Ordering for DTOs

```
project/
├── .sa1201ierrc                  # Default: alphabetical off
└── src/
    └── Dtos/
        ├── .sa1201ierrc          # Enable alphabetical for DTOs
        └── UserDto.cs
```

**`src/Dtos/.sa1201ierrc`:**
```json
{
  "alphabeticalSort": true,
  "sortTopLevelTypes": true
}
```

### Use Case 2: Different Rules for Test Files

```
project/
├── .sa1201ierrc                  # Production code: strict SA1201
└── tests/
    ├── .sa1201ierrc              # Tests: alphabetical sorting
    └── UserTests.cs
```

**Root `.sa1201ierrc`:**
```json
{
  "alphabeticalSort": false,
  "sortTopLevelTypes": false
}
```

**`tests/.sa1201ierrc`:**
```json
{
  "alphabeticalSort": true,
  "sortTopLevelTypes": false
}
```

### Use Case 3: Gradual Adoption

```
legacy-project/
├── .sa1201ierrc                  # New code only: strict
└── src/
    ├── Legacy/
    │   ├── .sa1201ierrc          # Legacy: minimal changes
    │   └── OldCode.cs
    └── New/
        └── NewCode.cs            # Uses root config
```

**Root `.sa1201ierrc`:**
```json
{
  "alphabeticalSort": true,
  "sortTopLevelTypes": true
}
```

**`src/Legacy/.sa1201ierrc`:**
```json
{
  "alphabeticalSort": false,
  "sortTopLevelTypes": false
}
```

## Priority Order

Configuration is applied in this priority order (highest to lowest):

1. **CLI Options** - `--alphabetical-sort`, `--sort-top-level-types`
2. **Nearest `.sa1201ierrc`** - In the file's directory
3. **Parent `.sa1201ierrc` files** - Walking up the directory tree
4. **Default Values** - `alphabeticalSort: false`, `sortTopLevelTypes: false`

## Best Practices

### 1. Start with Project-Wide Defaults

Place a `.sa1201ierrc` at your project root with your preferred defaults:

```json
{
  "alphabeticalSort": false,
  "sortTopLevelTypes": false
}
```

### 2. Override for Specific Areas

Create child `.sa1201ierrc` files only where you need different behavior:

```
project/
├── .sa1201ierrc              # Project defaults
└── src/
    └── Generated/
        └── .sa1201ierrc      # Don't format generated code
```

### 3. Use CLI for Experiments

Test options before committing them to config files:

```bash
# Try alphabetical sorting on a directory
SA1201ier ./src/Models --alphabetical-sort --check
```

### 4. Document Your Configuration

Add comments to your `.sa1201ierrc` files explaining why options are set:

```json
{
  // DTOs should be alphabetically sorted for easier searching
  "alphabeticalSort": true,
  
  // Multiple DTOs in one file should be ordered by visibility
  "sortTopLevelTypes": true
}
```

### 5. Version Control

Always commit `.sa1201ierrc` files to ensure consistent formatting across your team:

```bash
git add .sa1201ierrc
git commit -m "Add SA1201ier configuration"
```

## Troubleshooting

### Config File Not Being Found

- Ensure the file is named exactly `.sa1201ierrc` (with leading dot)
- Check file permissions (must be readable)
- Use `--verbose` to see which config files are being loaded

### Unexpected Behavior

- Remember CLI options override all config files
- Check for multiple `.sa1201ierrc` files in parent directories
- Verify JSON syntax (use a JSON validator)

### Config Not Applied

- Config only affects files in the same directory or subdirectories
- Check that the option names are spelled correctly (case-sensitive)
- Ensure JSON is valid (no trailing commas without `allowTrailingCommas`)

## Future Configuration Options

Planned options for future versions:

- `preserveRegionOrder`: Keep members in original order within regions
- `groupByType`: Group members by type before sorting by access level
- `customOrder`: Define custom member type ordering
- `ignorePatterns`: Exclude files/directories from formatting
