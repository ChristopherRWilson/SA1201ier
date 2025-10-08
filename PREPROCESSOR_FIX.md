# Preprocessor Directive and Region Handling Fix

## Problem
The original SA1201ier implementation would reorder all members in a type without considering preprocessor directives (#if, #endif, etc.) and regions (#region, #endregion). This caused issues where:

1. Members inside `#if DEBUG` blocks would be moved outside those blocks
2. Region boundaries would be lost
3. The preprocessor context of code would be broken

## Solution
The fix introduces a grouping mechanism that:

1. **Detects preprocessor and region boundaries**: The code scans through member trivia (whitespace, comments, directives) to identify when members are inside preprocessor blocks or regions.

2. **Groups members by context**: Members are grouped together if they share the same preprocessor/region context. Each group is treated independently.

3. **Reorders within groups**: SA1201 ordering rules are applied within each group, but members are not moved across group boundaries.

4. **Preserves trivia**: Leading trivia (including preprocessor directives) is preserved when reordering.

## Implementation Details

### New Method: `GroupMembersByDirectives`
This method:
- Tracks nesting depth of preprocessor directives and regions
- Creates separate groups for members inside vs. outside blocks
- Ensures that preprocessor/region context is never broken

### Updated Methods
Both `AnalyzeTypeDeclaration` and `ReorderTypeDeclaration` now:
- Call `GroupMembersByDirectives` to get member groups
- Process each group independently
- Only compare and reorder members within the same group

### New Class: `MemberGroup`
A simple container class to hold members that should be reordered together.

## Examples

### Example 1: Region Preservation
**Before Fix:**
```csharp
// Input
public class TestClass
{
#region Private Methods
    private void Method1() { }
#endregion
    public void PublicMethod() { }
}

// Old output (BROKEN)
public class TestClass
{
    public void PublicMethod() { }
    private void Method1() { }  // Region lost!
}
```

**After Fix:**
```csharp
// New output (CORRECT)
public class TestClass
{
#region Private Methods
    private void Method1() { }
#endregion
    public void PublicMethod() { }
}
```

### Example 2: Preprocessor Preservation
**Before Fix:**
```csharp
// Input
public class TestClass
{
#if DEBUG
    private void DebugMethod() { }
#endif
    private void PrivateMethod() { }
    public void PublicMethod() { }
}

// Old output (BROKEN)
public class TestClass
{
    public void PublicMethod() { }
    private void DebugMethod() { }  // No longer in #if DEBUG!
    private void PrivateMethod() { }
}
```

**After Fix:**
```csharp
// New output (CORRECT)
public class TestClass
{
#if DEBUG
    private void DebugMethod() { }
#endif
    public void PublicMethod() { }
    private void PrivateMethod() { }
}
```

### Example 3: Mixed Scenario
```csharp
// Input
public class TestClass
{
#region Constants
    private const int PRIVATE_CONST = 1;
    public const int PUBLIC_CONST = 2;
#endregion

#region Fields  
    private int privateField;
    public int publicField;
#endregion

    // Methods outside regions get reordered
    private void PrivateMethod() { }
    public void PublicMethod() { }
}

// Output
public class TestClass
{
#region Constants
    public const int PUBLIC_CONST = 2;
    private const int PRIVATE_CONST = 1;
#endregion

#region Fields  
    public int publicField;
    private int privateField;
#endregion

    // Methods reordered by access level
    public void PublicMethod() { }
    private void PrivateMethod() { }
}
```

## Testing
New test methods added to `SA1201ierEdgeCaseTests.cs`:
- `FormatContent_WithRegions_PreservesRegionsAndReordersWithin`
- `FormatContent_WithPreprocessorDirectives_PreservesDirectives`
- `FormatContent_WithRegionsCorrectOrder_NoChanges`
- `FormatContent_WithNestedPreprocessorDirectives_PreservesStructure`

## Limitations
- Members are only reordered within their group. If you have all private members in one region and all public members in another, they won't be reordered relative to each other (the regions stay in their original positions).
- This is intentional - it respects the developer's explicit grouping decisions via regions and preprocessor directives.
