# Razor File Support Implementation

## Overview

Added comprehensive support for formatting C# code within Blazor Razor files (`.razor`). SA1201ier now automatically detects and formats `@code` blocks while preserving all Razor markup.

## Files Created

### 1. `SA1201ier.Core/RazorFileProcessor.cs`
New helper class that handles Razor file-specific operations:

**Key Methods:**
- `IsRazorFile()` - Detects if a file is a Razor file
- `ExtractCodeBlocks()` - Extracts all `@code` blocks from Razor content
- `ReinsertCodeBlocks()` - Reinserts formatted code back into Razor template
- `WrapCodeForParsing()` - Wraps extracted code in a class for parsing
- `UnwrapFormattedCode()` - Unwraps formatted code after processing

**Key Classes:**
- `RazorExtractionResult` - Holds extracted code blocks and template
- `CodeBlock` - Represents a single `@code` block with original and formatted code

**Features:**
- Handles nested braces correctly
- Properly ignores braces in strings, chars, and comments
- Supports single-line (`//`) and multi-line (`/* */`) comments
- Handles escape sequences in strings

### 2. `SA1201ier.Tests/RazorFileProcessingTests.cs`
Comprehensive test suite with 8 tests covering:
- Single code block extraction
- Multiple code block extraction
- Nested braces handling
- Braces in strings handling
- Wrap/unwrap round-trip
- Full Razor file formatting
- Files without code blocks
- Check mode (detect violations without modifying)

## Files Modified

### 1. `SA1201ier.Core/FileProcessor.cs`

**Changes:**
- Updated class documentation to mention Razor files
- Modified `ProcessDirectoryAsync()` to find both `.cs` and `.razor` files
- Updated `ProcessFileAsync()` to detect and route Razor files
- Added new `ProcessRazorFileAsync()` method that:
  - Extracts `@code` blocks
  - Formats each block independently
  - Collects violations from all blocks
  - Reinserts formatted code
  - Supports both check and format modes

### 2. `SA1201ier.Cli/Program.cs`

**Changes:**
- Updated help text to mention "C# and Razor files"
- Updated path argument description to note `.cs and .razor files`

### 3. `README.md`

**Changes:**
- Updated title to mention Razor file support
- Added Razor file support to features list
- Added new "Razor File Support" section with:
  - Explanation of how it works
  - Before/after example
  - Usage examples
  - Important notes about what gets processed

## How It Works

### Processing Flow

1. **Detection**: FileProcessor detects `.razor` extension
2. **Extraction**: RazorFileProcessor extracts all `@code` blocks using regex
3. **Parsing**: Each code block is wrapped in a class structure for Roslyn parsing
4. **Formatting**: SA1201Formatter processes the wrapped code
5. **Unwrapping**: The class wrapper is removed from formatted code
6. **Reinsertion**: Formatted code is reinserted into the Razor template
7. **Writing**: The complete Razor file is written back to disk (if not in check mode)

### Code Block Extraction Algorithm

The extraction algorithm properly handles:

1. **Nested Braces**: Counts brace depth to find matching closing brace
2. **Strings**: Ignores braces inside double-quoted strings
3. **Characters**: Ignores braces inside single-quoted characters
4. **Escape Sequences**: Handles `\"`, `\'`, `\\` correctly
5. **Comments**: 
   - Single-line comments (`//`)
   - Multi-line comments (`/* */`)
6. **Edge Cases**: Empty code blocks, multiple code blocks, etc.

### Example Processing

**Input Razor File:**
```razor
@page "/counter"
<h1>Counter</h1>
@code {
    private void Method2() { }
    public int Field1;
    private int Field2;
    public void Method1() { }
}
```

**Step 1 - Extract:**
```
Template: "@page \"/counter\"\n<h1>Counter</h1>\n__RAZOR_CODE_BLOCK_0__"
CodeBlock[0].OriginalCode: "private void Method2() { }\n    public int Field1;\n..."
```

**Step 2 - Wrap:**
```csharp
public class __RazorCodeBlock
{
    private void Method2() { }
    public int Field1;
    private int Field2;
    public void Method1() { }
}
```

**Step 3 - Format (by SA1201Formatter):**
```csharp
public class __RazorCodeBlock
{
    public int Field1;
    private int Field2;
    public void Method1() { }
    private void Method2() { }
}
```

**Step 4 - Unwrap:**
```csharp
public int Field1;
private int Field2;
public void Method1() { }
private void Method2() { }
```

**Step 5 - Reinsert:**
```razor
@page "/counter"
<h1>Counter</h1>
@code {
    public int Field1;
    private int Field2;
    public void Method1() { }
    private void Method2() { }
}
```

## Configuration Support

Razor files respect all configuration options:
- Hierarchical `.sa1201ierrc` configuration works
- CLI options override config files
- All custom ordering options apply to `@code` blocks
- Alphabetical sorting works within Razor files

## Limitations & Design Decisions

### What Gets Formatted
✅ C# code within `@code` blocks  
❌ Inline C# expressions (`@someVariable`)  
❌ Inline C# statements (`@{ var x = 1; }`)  
❌ Razor directives (`@page`, `@inject`, etc.)  
❌ HTML/Razor markup  

### Why Only @code Blocks?
- `@code` blocks contain class members (fields, properties, methods)
- SA1201 is specifically about class member ordering
- Inline expressions/statements don't have member declarations
- This design keeps the formatter focused and predictable

### Multiple @code Blocks
- Each `@code` block is processed independently
- Blocks can have different effective configurations if in different files
- This matches how Blazor compiles Razor files

## Testing

Comprehensive test coverage includes:
- ✅ Single code block extraction
- ✅ Multiple code block extraction
- ✅ Nested braces (methods with if/for/while)
- ✅ Braces in strings and JSON
- ✅ Comments with braces
- ✅ Wrap/unwrap round-trip
- ✅ Full formatting integration
- ✅ Files without code blocks
- ✅ Check mode (no modifications)

## Usage Examples

### Format a Blazor Component
```bash
SA1201ier Pages/Counter.razor
```

### Format All Razor Files in a Project
```bash
SA1201ier ./Pages
SA1201ier ./Components
```

### Check Razor Files in CI/CD
```bash
SA1201ier ./Pages --check
```

### Use with Custom Configuration
```bash
# Create config
echo '{"alphabeticalSort": true}' > Pages/.sa1201ierrc

# Format with config
SA1201ier ./Pages
```

## Future Enhancements

Possible future improvements:
- Support for `@functions` blocks (legacy syntax)
- Support for `@{ }` code blocks (if there's demand)
- Better preservation of indentation in `@code` blocks
- Support for `.cshtml` files (Razor Pages)
- Integration with Razor formatter for consistent styling

## Backward Compatibility

✅ **Fully backward compatible**
- Existing C# file processing unchanged
- All existing tests pass
- No breaking changes to API
- CLI interface unchanged (just handles more file types)

## Performance

- Minimal overhead for Razor file detection (simple extension check)
- Regex extraction is efficient for typical Razor files
- Each code block is formatted independently
- Configuration caching still applies
- No performance impact on `.cs` file processing
