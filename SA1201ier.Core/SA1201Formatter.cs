using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SA1201ier.Core;

/// <summary>
/// Formats C# files according to StyleCop rule SA1201.
/// SA1201: Elements should be ordered by access level.
/// </summary>
public class Sa1201IerFormatter
{
    private readonly FormatterOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="Sa1201IerFormatter"/> class.
    /// </summary>
    /// <param name="options">Formatting options.</param>
    public Sa1201IerFormatter(FormatterOptions? options = null)
    {
        _options = options ?? FormatterOptions.Default;
    }

    /// <summary>
    /// Checks content for SA1201 violations without modifying it.
    /// </summary>
    /// <param name="filePath">The file path (for reporting purposes).</param>
    /// <param name="content">The content to check.</param>
    /// <param name="options">Optional formatting options.</param>
    /// <returns>A <see cref="FormattingResult" /> containing violation information.</returns>
    public static FormattingResult CheckContent(
        string filePath,
        string content,
        FormatterOptions? options = null
    )
    {
        var formatter = new Sa1201IerFormatter(options);
        return formatter.CheckContentInternal(filePath, content);
    }

    /// <summary>
    /// Checks content for SA1201 violations without modifying it (instance method).
    /// </summary>
    private FormattingResult CheckContentInternal(string filePath, string content)
    {
        var tree = CSharpSyntaxTree.ParseText(content);

        if (tree.GetRoot() is not CompilationUnitSyntax root)
        {
            return new FormattingResult(filePath, content, null, new List<Sa1201Violation>());
        }

        var violations = new List<Sa1201Violation>();
        AnalyzeNode(root, violations);

        return new FormattingResult(filePath, content, null, violations);
    }

    /// <summary>
    /// Analyzes a syntax node for SA1201 violations.
    /// </summary>
    /// <param name="node">The node to analyze.</param>
    /// <param name="violations">The list to add violations to.</param>
    private void AnalyzeNode(SyntaxNode node, List<Sa1201Violation> violations)
    {
        // Analyze namespace declarations for top-level type ordering
        if (node is BaseNamespaceDeclarationSyntax namespaceDecl && _options.SortTopLevelTypes)
        {
            AnalyzeNamespaceDeclaration(namespaceDecl, violations);
        }

        if (node is TypeDeclarationSyntax typeDeclaration)
        {
            AnalyzeTypeDeclaration(typeDeclaration, violations);
        }

        foreach (var child in node.ChildNodes())
        {
            AnalyzeNode(child, violations);
        }
    }

    /// <summary>
    /// Analyzes a namespace declaration for top-level type ordering violations.
    /// </summary>
    /// <param name="namespaceDecl">The namespace declaration to analyze.</param>
    /// <param name="violations">The list to add violations to.</param>
    private void AnalyzeNamespaceDeclaration(
        BaseNamespaceDeclarationSyntax namespaceDecl,
        List<Sa1201Violation> violations
    )
    {
        // Get all type declarations in the namespace
        var typeMembers = namespaceDecl.Members.OfType<BaseTypeDeclarationSyntax>().ToList();

        if (typeMembers.Count <= 1)
        {
            return;
        }

        // Create order info for types
        var typeOrderInfos = typeMembers
            .Select(type => new
            {
                Type = type,
                AccessLevel = GetAccessLevel(type),
                Name = type.Identifier.Text,
                MemberType = GetTopLevelTypeOrder(type),
            })
            .ToList();

        // Sort by type order, access level, then optionally by name
        var sortedTypes = _options.AlphabeticalSort
            ? typeOrderInfos
                .OrderBy(t => t.MemberType)
                .ThenBy(t => t.AccessLevel)
                .ThenBy(t => t.Name)
                .ToList()
            : typeOrderInfos.OrderBy(t => t.MemberType).ThenBy(t => t.AccessLevel).ToList();

        // Check if reordering is needed
        for (var i = 0; i < typeOrderInfos.Count; i++)
        {
            if (typeOrderInfos[i].Type != sortedTypes[i].Type)
            {
                var lineSpan = namespaceDecl.SyntaxTree.GetLineSpan(typeOrderInfos[i].Type.Span);
                violations.Add(
                    new Sa1201Violation(
                        lineSpan.StartLinePosition.Line + 1,
                        lineSpan.StartLinePosition.Character + 1,
                        $"Type '{typeOrderInfos[i].Name}' should be ordered by access level.",
                        typeOrderInfos[i].Name
                    )
                );
                break; // Only report the first violation
            }
        }
    }

    /// <summary>
    /// Analyzes a type declaration for SA1201 violations while respecting
    /// preprocessor directives and region boundaries.
    /// </summary>
    /// <param name="typeDeclaration">The type declaration to analyze.</param>
    /// <param name="violations">The list to add violations to.</param>
    private void AnalyzeTypeDeclaration(
        TypeDeclarationSyntax typeDeclaration,
        List<Sa1201Violation> violations
    )
    {
        var allMembers = typeDeclaration.Members.ToList();

        if (allMembers.Count == 0)
        {
            return;
        }

        // Group members by their preprocessor/region context
        var memberGroups = GroupMembersByDirectives(allMembers);

        foreach (var group in memberGroups)
        {
            if (group.Members.Count == 0)
            {
                continue;
            }

            // Get member order info for this group
            var memberInfos = group
                .Members.Select(GetMemberOrderInfo)
                .Where(m => m != null)
                .Cast<MemberOrderInfo>()
                .ToList();

            if (memberInfos.Count == 0)
            {
                continue;
            }

            var sortedMembers = OrderMembers(memberInfos);

            // Check if this group has violations
            for (var i = 0; i < memberInfos.Count; i++)
            {
                if (memberInfos[i].Node == sortedMembers[i].Node)
                    continue;

                var lineSpan = typeDeclaration.SyntaxTree.GetLineSpan(memberInfos[i].Node.Span);
                var memberName = GetMemberName(memberInfos[i].Node);

                violations.Add(
                    new Sa1201Violation(
                        lineSpan.StartLinePosition.Line + 1,
                        lineSpan.StartLinePosition.Character + 1,
                        $"{GetMemberTypeString(memberInfos[i])} '{memberName}' should be ordered by access level. "
                            + $"Expected: {GetAccessLevelString(sortedMembers[i].AccessLevel)}, "
                            + $"Actual: {GetAccessLevelString(memberInfos[i].AccessLevel)}",
                        memberName
                    )
                );
                break;
            }
        }
    }

    /// <summary>
    /// Gets the access level from a member declaration syntax.
    /// </summary>
    /// <param name="member">The member declaration.</param>
    /// <returns>The access level.</returns>
    private static AccessLevel GetAccessLevel(MemberDeclarationSyntax member)
    {
        var modifiers = member.Modifiers;

        if (modifiers.Any(SyntaxKind.PublicKeyword))
        {
            return AccessLevel.Public;
        }

        if (modifiers.Any(SyntaxKind.PrivateKeyword) && modifiers.Any(SyntaxKind.ProtectedKeyword))
        {
            return AccessLevel.PrivateProtected;
        }

        if (modifiers.Any(SyntaxKind.ProtectedKeyword) && modifiers.Any(SyntaxKind.InternalKeyword))
        {
            return AccessLevel.ProtectedInternal;
        }

        if (modifiers.Any(SyntaxKind.ProtectedKeyword))
        {
            return AccessLevel.Protected;
        }

        if (modifiers.Any(SyntaxKind.InternalKeyword))
        {
            return AccessLevel.Internal;
        }

        if (modifiers.Any(SyntaxKind.PrivateKeyword))
        {
            return AccessLevel.Private;
        }

        // Default access level
        return AccessLevel.Private;
    }

    /// <summary>
    /// Gets a string representation of an access level.
    /// </summary>
    /// <param name="accessLevel">The access level.</param>
    /// <returns>A string representation of the access level.</returns>
    private static string GetAccessLevelString(AccessLevel accessLevel)
    {
        return accessLevel switch
        {
            AccessLevel.Public => "public",
            AccessLevel.Internal => "internal",
            AccessLevel.ProtectedInternal => "protected internal",
            AccessLevel.Protected => "protected",
            AccessLevel.PrivateProtected => "private protected",
            AccessLevel.Private => "private",
            _ => "unknown",
        };
    }

    /// <summary>
    /// Gets the member name from a syntax node.
    /// </summary>
    /// <param name="node">The syntax node.</param>
    /// <returns>The member name.</returns>
    private static string GetMemberName(SyntaxNode node)
    {
        return node switch
        {
            FieldDeclarationSyntax field => field
                .Declaration.Variables.FirstOrDefault()
                ?.Identifier.Text
            ?? "field",
            MethodDeclarationSyntax method => method.Identifier.Text,
            PropertyDeclarationSyntax property => property.Identifier.Text,
            EventDeclarationSyntax eventDecl => eventDecl.Identifier.Text,
            EventFieldDeclarationSyntax eventField => eventField
                .Declaration.Variables.FirstOrDefault()
                ?.Identifier.Text
            ?? "event",
            ConstructorDeclarationSyntax constructor => constructor.Identifier.Text,
            DestructorDeclarationSyntax destructor => destructor.Identifier.Text,
            IndexerDeclarationSyntax => "this[]",
            BaseTypeDeclarationSyntax type => type.Identifier.Text,
            DelegateDeclarationSyntax delegateDecl => delegateDecl.Identifier.Text,
            _ => "unknown",
        };
    }

    /// <summary>
    /// Gets member order information from a member declaration syntax.
    /// </summary>
    /// <param name="member">The member declaration.</param>
    /// <returns>The member order information, or null if not applicable.</returns>
    private static MemberOrderInfo? GetMemberOrderInfo(MemberDeclarationSyntax member)
    {
        var memberType = GetMemberType(member);
        if (memberType == null)
        {
            return null;
        }

        var accessLevel = GetAccessLevel(member);
        var isStatic = IsStatic(member);
        var isConst = IsConst(member);

        return new MemberOrderInfo(member, memberType.Value, accessLevel, isStatic, isConst);
    }

    /// <summary>
    /// Gets the member type from a member declaration syntax.
    /// </summary>
    /// <param name="member">The member declaration.</param>
    /// <returns>The member type, or null if not applicable.</returns>
    private static MemberType? GetMemberType(MemberDeclarationSyntax member)
    {
        return member switch
        {
            FieldDeclarationSyntax => MemberType.Field,
            ConstructorDeclarationSyntax => MemberType.Constructor,
            DestructorDeclarationSyntax => MemberType.Destructor,
            DelegateDeclarationSyntax => MemberType.Delegate,
            EventDeclarationSyntax => MemberType.Event,
            EventFieldDeclarationSyntax => MemberType.Event,
            EnumDeclarationSyntax => MemberType.Enum,
            InterfaceDeclarationSyntax => MemberType.Interface,
            PropertyDeclarationSyntax => MemberType.Property,
            IndexerDeclarationSyntax => MemberType.Indexer,
            MethodDeclarationSyntax => MemberType.Method,
            StructDeclarationSyntax => MemberType.Struct,
            ClassDeclarationSyntax => MemberType.Class,
            RecordDeclarationSyntax => MemberType.Class,
            _ => null,
        };
    }

    /// <summary>
    /// Gets a string representation of a member type.
    /// </summary>
    /// <param name="memberInfo">The member order information.</param>
    /// <returns>A string representation of the member type.</returns>
    private static string GetMemberTypeString(MemberOrderInfo memberInfo)
    {
        var typeStr = memberInfo.MemberType.ToString();
        if (memberInfo.IsConst)
        {
            return $"Const {typeStr}";
        }
        if (memberInfo.IsStatic)
        {
            return $"Static {typeStr}";
        }
        return typeStr;
    }

    /// <summary>
    /// Determines if a member is const.
    /// </summary>
    /// <param name="member">The member declaration.</param>
    /// <returns>True if the member is const; otherwise, false.</returns>
    private static bool IsConst(MemberDeclarationSyntax member)
    {
        if (member is FieldDeclarationSyntax field)
        {
            return field.Modifiers.Any(SyntaxKind.ConstKeyword);
        }
        return false;
    }

    /// <summary>
    /// Determines if a member is static.
    /// </summary>
    /// <param name="member">The member declaration.</param>
    /// <returns>True if the member is static; otherwise, false.</returns>
    private static bool IsStatic(MemberDeclarationSyntax member)
    {
        return member.Modifiers.Any(SyntaxKind.StaticKeyword);
    }

    /// <summary>
    /// Orders members according to SA1201 rules.
    /// </summary>
    /// <param name="members">The members to order.</param>
    /// <returns>The ordered members.</returns>
    private List<MemberOrderInfo> OrderMembers(List<MemberOrderInfo> members)
    {
        // Build custom sort keys
        var membersWithSortKeys = members
            .Select(m => new
            {
                Member = m,
                MemberTypeOrder = GetMemberTypeSortKey(m.MemberType),
                AccessLevelOrder = GetAccessLevelSortKey(m.AccessLevel),
                ConstOrder = _options.ConstMembersFirst ? (m.IsConst ? 0 : 1) : (m.IsConst ? 1 : 0),
                StaticOrder = _options.StaticMembersFirst
                    ? (m.IsStatic ? 0 : 1)
                    : (m.IsStatic ? 1 : 0),
                Name = GetMemberName(m.Node),
            })
            .ToList();

        // Sort using custom orders
        var sorted = membersWithSortKeys
            .OrderBy(m => m.MemberTypeOrder)
            .ThenBy(m => m.ConstOrder)
            .ThenBy(m => m.StaticOrder)
            .ThenBy(m => m.AccessLevelOrder);

        // Add alphabetical sorting as the final sort key if enabled
        if (_options.AlphabeticalSort)
        {
            sorted = sorted.ThenBy(m => m.Name);
        }

        return sorted.Select(m => m.Member).ToList();
    }

    /// <summary>
    /// Gets the sort key for a member type based on custom configuration.
    /// </summary>
    /// <param name="memberType">The member type.</param>
    /// <returns>The sort key (lower values come first).</returns>
    private int GetMemberTypeSortKey(MemberType memberType)
    {
        if (_options.MemberTypeOrder == null || _options.MemberTypeOrder.Count == 0)
        {
            // Default order
            return (int)memberType;
        }

        var memberTypeName = memberType.ToString();
        var index = _options.MemberTypeOrder.IndexOf(memberTypeName);
        return index >= 0 ? index : 999; // Put unspecified types at the end
    }

    /// <summary>
    /// Gets the sort key for an access level based on custom configuration.
    /// </summary>
    /// <param name="accessLevel">The access level.</param>
    /// <returns>The sort key (lower values come first).</returns>
    private int GetAccessLevelSortKey(AccessLevel accessLevel)
    {
        if (_options.AccessLevelOrder == null || _options.AccessLevelOrder.Count == 0)
        {
            // Default order
            return (int)accessLevel;
        }

        var accessLevelName = accessLevel.ToString();
        var index = _options.AccessLevelOrder.IndexOf(accessLevelName);
        return index >= 0 ? index : 999; // Put unspecified levels at the end
    }

    /// <summary>
    /// Reorders a type declaration according to SA1201 rules while preserving
    /// preprocessor directives and region boundaries.
    /// </summary>
    /// <param name="typeDeclaration">The type declaration to reorder.</param>
    /// <param name="violations">The list to add violations to.</param>
    /// <returns>The reordered type declaration.</returns>
    private TypeDeclarationSyntax ReorderTypeDeclaration(
        TypeDeclarationSyntax typeDeclaration,
        List<Sa1201Violation> violations
    )
    {
        var allMembers = typeDeclaration.Members.ToList();

        if (allMembers.Count == 0)
        {
            return typeDeclaration;
        }

        // Group members by their preprocessor/region context
        var memberGroups = GroupMembersByDirectives(allMembers);

        var needsReordering = false;
        var reorderedMembers = new List<MemberDeclarationSyntax>();

        foreach (var group in memberGroups)
        {
            if (group.Members.Count == 0)
            {
                continue;
            }

            // Get member order info for this group
            var memberInfos = group
                .Members.Select(GetMemberOrderInfo)
                .Where(m => m != null)
                .Cast<MemberOrderInfo>()
                .ToList();

            if (memberInfos.Count == 0)
            {
                // No reorderable members in this group, keep as-is
                reorderedMembers.AddRange(group.Members);
                continue;
            }

            var sortedMembers = OrderMembers(memberInfos);

            // Check if this group needs reordering
            var groupNeedsReordering = false;
            for (var i = 0; i < memberInfos.Count; i++)
            {
                if (memberInfos[i].Node == sortedMembers[i].Node)
                    continue;

                groupNeedsReordering = true;
                needsReordering = true;
                var lineSpan = typeDeclaration.SyntaxTree.GetLineSpan(memberInfos[i].Node.Span);
                var memberName = GetMemberName(memberInfos[i].Node);

                violations.Add(
                    new Sa1201Violation(
                        lineSpan.StartLinePosition.Line + 1,
                        lineSpan.StartLinePosition.Character + 1,
                        $"{GetMemberTypeString(memberInfos[i])} '{memberName}' reordered by access level.",
                        memberName
                    )
                );
            }

            // Add members from this group (reordered if needed, or original order if not)
            if (groupNeedsReordering)
            {
                // Just add the reordered members - each member already has its own trivia
                // (comments, attributes, etc.) attached to it
                for (var i = 0; i < sortedMembers.Count; i++)
                {
                    var member = (MemberDeclarationSyntax)sortedMembers[i].Node;

                    // If insertBlankLineBetweenMembers is enabled, normalize blank lines
                    if (_options.InsertBlankLineBetweenMembers && i > 0)
                    {
                        member = EnsureSingleBlankLineBefore(member);
                    }

                    reorderedMembers.Add(member);
                }
            }
            else
            {
                // No reordering needed, but still normalize blank lines if option is enabled
                if (_options.InsertBlankLineBetweenMembers)
                {
                    for (var i = 0; i < group.Members.Count; i++)
                    {
                        var member = group.Members[i];
                        if (i > 0)
                        {
                            member = EnsureSingleBlankLineBefore(member);
                        }
                        reorderedMembers.Add(member);
                    }
                }
                else
                {
                    // Keep original order without modification
                    reorderedMembers.AddRange(group.Members);
                }
            }
        }

        var shouldSkipProcessing = !needsReordering && !_options.InsertBlankLineBetweenMembers;
        if (shouldSkipProcessing)
        {
            return typeDeclaration;
        }

        var newTypeDeclaration = typeDeclaration.WithMembers(
            new SyntaxList<MemberDeclarationSyntax>(reorderedMembers)
        );

        return newTypeDeclaration;
    }

    /// <summary>
    /// Groups members by their preprocessor directive and region context.
    /// Members within the same #if/#endif or #region/#endregion block are kept together.
    /// </summary>
    /// <param name="allMembers">All member declarations from the type.</param>
    /// <returns>Groups of members that should be reordered together.</returns>
    private static List<MemberGroup> GroupMembersByDirectives(
        List<MemberDeclarationSyntax> allMembers
    )
    {
        if (allMembers.Count == 0)
        {
            return new List<MemberGroup>();
        }

        // Check if any members have preprocessor or region directives
        var hasAnyDirectives = allMembers.Any(member =>
            member
                .GetLeadingTrivia()
                .Any(t =>
                    t.IsKind(SyntaxKind.IfDirectiveTrivia)
                    || t.IsKind(SyntaxKind.EndIfDirectiveTrivia)
                    || t.IsKind(SyntaxKind.RegionDirectiveTrivia)
                    || t.IsKind(SyntaxKind.EndRegionDirectiveTrivia)
                )
            || member
                .GetTrailingTrivia()
                .Any(t =>
                    t.IsKind(SyntaxKind.IfDirectiveTrivia)
                    || t.IsKind(SyntaxKind.EndIfDirectiveTrivia)
                    || t.IsKind(SyntaxKind.RegionDirectiveTrivia)
                    || t.IsKind(SyntaxKind.EndRegionDirectiveTrivia)
                )
        );

        // If no directives, treat all members as one group
        if (!hasAnyDirectives)
        {
            return new List<MemberGroup> { new MemberGroup { Members = allMembers } };
        }

        var groups = new List<MemberGroup>();
        var currentGroup = new MemberGroup();

        // Track the nesting level of preprocessor directives and regions
        var directiveDepth = 0;
        var regionDepth = 0;

        foreach (var member in allMembers)
        {
            var leadingTrivia = member.GetLeadingTrivia();

            // Process leading trivia to see if we're entering/leaving blocks
            var leadingHasOpening = leadingTrivia.Any(t =>
                t.IsKind(SyntaxKind.IfDirectiveTrivia) || t.IsKind(SyntaxKind.RegionDirectiveTrivia)
            );
            var leadingHasClosing = leadingTrivia.Any(t =>
                t.IsKind(SyntaxKind.EndIfDirectiveTrivia)
                || t.IsKind(SyntaxKind.EndRegionDirectiveTrivia)
            );

            // Count directives in leading trivia
            foreach (var trivia in leadingTrivia)
            {
                // Process closing directives first
                if (trivia.IsKind(SyntaxKind.EndIfDirectiveTrivia))
                {
                    directiveDepth = Math.Max(0, directiveDepth - 1);
                }
                else if (trivia.IsKind(SyntaxKind.EndRegionDirectiveTrivia))
                {
                    regionDepth = Math.Max(0, regionDepth - 1);
                }
            }

            // If we just closed a block and have accumulated members, finalize the group
            var wasInBlock = directiveDepth > 0 || regionDepth > 0;
            if (leadingHasClosing && !wasInBlock && currentGroup.Members.Count > 0)
            {
                groups.Add(currentGroup);
                currentGroup = new MemberGroup();
            }

            // Now process opening directives
            foreach (var trivia in leadingTrivia)
            {
                if (trivia.IsKind(SyntaxKind.IfDirectiveTrivia))
                {
                    // If we're starting a new block and have members outside, finalize that group
                    if (directiveDepth == 0 && regionDepth == 0 && currentGroup.Members.Count > 0)
                    {
                        groups.Add(currentGroup);
                        currentGroup = new MemberGroup();
                    }
                    directiveDepth++;
                }
                else if (trivia.IsKind(SyntaxKind.RegionDirectiveTrivia))
                {
                    // If we're starting a new region and have members outside, finalize that group
                    if (directiveDepth == 0 && regionDepth == 0 && currentGroup.Members.Count > 0)
                    {
                        groups.Add(currentGroup);
                        currentGroup = new MemberGroup();
                    }
                    regionDepth++;
                }
            }

            // Add member to current group
            currentGroup.Members.Add(member);

            // Check trailing trivia
            var trailingTrivia = member.GetTrailingTrivia();
            foreach (var trivia in trailingTrivia)
            {
                if (trivia.IsKind(SyntaxKind.EndIfDirectiveTrivia))
                {
                    directiveDepth = Math.Max(0, directiveDepth - 1);
                }
                else if (trivia.IsKind(SyntaxKind.EndRegionDirectiveTrivia))
                {
                    regionDepth = Math.Max(0, regionDepth - 1);
                }
            }

            // If we're now outside all blocks after adding this member, close the group
            if (
                (directiveDepth == 0 && regionDepth == 0)
                && (
                    trailingTrivia.Any(t =>
                        t.IsKind(SyntaxKind.EndIfDirectiveTrivia)
                        || t.IsKind(SyntaxKind.EndRegionDirectiveTrivia)
                    )
                )
            )
            {
                groups.Add(currentGroup);
                currentGroup = new MemberGroup();
            }
        }

        // Add any remaining members in the current group
        if (currentGroup.Members.Count > 0)
        {
            groups.Add(currentGroup);
        }

        return groups;
    }

    /// <summary>
    /// Ensures that a member has exactly one blank line before it by normalizing its leading trivia.
    /// Preserves comments, attributes, and other important trivia while ensuring proper spacing.
    /// </summary>
    /// <param name="member">The member to process.</param>
    /// <returns>The member with normalized leading trivia.</returns>
    private static MemberDeclarationSyntax EnsureSingleBlankLineBefore(
        MemberDeclarationSyntax member
    )
    {
        var leadingTrivia = member.GetLeadingTrivia();
        var newTrivia = new List<SyntaxTrivia>();

        // Find the newline style used in the original trivia (for consistency)
        var newlineText = "\r\n"; // Default to CRLF
        foreach (var trivia in leadingTrivia)
        {
            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                newlineText = trivia.ToFullString();
                break;
            }
        }

        // Start with exactly one blank line
        // The previous member's trailing newline + this newline = one blank line
        newTrivia.Add(SyntaxFactory.EndOfLine(newlineText));

        // Collect non-whitespace trivia (comments, attributes, directives) and the final indentation
        var importantTrivia = new List<SyntaxTrivia>();
        SyntaxTrivia? finalIndentation = null;

        var foundImportantTrivia = false;
        for (var i = 0; i < leadingTrivia.Count; i++)
        {
            var trivia = leadingTrivia[i];

            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                // Skip newlines at the start, but once we've found important trivia, keep them
                if (foundImportantTrivia)
                {
                    importantTrivia.Add(trivia);
                }
                continue;
            }

            if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                // Always track the last whitespace as potential indentation
                finalIndentation = trivia;
                // If we've found important trivia, preserve whitespace
                if (foundImportantTrivia)
                {
                    importantTrivia.Add(trivia);
                }
                continue;
            }

            // This is important trivia (comments, attributes, directives, etc.)
            foundImportantTrivia = true;
            importantTrivia.Add(trivia);
        }

        // Add any important trivia (comments, attributes, etc.)
        newTrivia.AddRange(importantTrivia);

        // Add final indentation if we have one
        if (finalIndentation.HasValue)
        {
            newTrivia.Add(finalIndentation.Value);
        }

        return member.WithLeadingTrivia(newTrivia);
    }

    /// <summary>
    /// Represents a group of members that should be reordered together.
    /// </summary>
    private class MemberGroup
    {
        public List<MemberDeclarationSyntax> Members { get; set; } =
            new List<MemberDeclarationSyntax>();
    }

    /// <summary>
    /// Checks if a C# file has SA1201 violations without modifying it.
    /// </summary>
    /// <param name="filePath">The path to the C# file.</param>
    /// <returns>A <see cref="FormattingResult" /> containing violation information.</returns>
    public async Task<FormattingResult> CheckFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        var content = await File.ReadAllTextAsync(filePath);
        return CheckContent(filePath, content);
    }

    /// <summary>
    /// Formats content according to SA1201 rules.
    /// </summary>
    /// <param name="filePath">The file path (for reporting purposes).</param>
    /// <param name="content">The content to format.</param>
    /// <returns>A <see cref="FormattingResult" /> containing the formatted content.</returns>
    public FormattingResult FormatContent(string filePath, string content)
    {
        var tree = CSharpSyntaxTree.ParseText(content);

        if (tree.GetRoot() is not CompilationUnitSyntax root)
        {
            return new FormattingResult(filePath, content, null, new List<Sa1201Violation>());
        }

        var violations = new List<Sa1201Violation>();
        var newRoot = ReorderNode(root, violations);

        var formattedContent = newRoot.ToFullString();

        return new FormattingResult(filePath, content, formattedContent, violations);
    }

    /// <summary>
    /// Formats a C# file according to SA1201 rules.
    /// </summary>
    /// <param name="filePath">The path to the C# file.</param>
    /// <returns>A <see cref="FormattingResult" /> containing the formatted content.</returns>
    public async Task<FormattingResult> FormatFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        var content = await File.ReadAllTextAsync(filePath);
        return FormatContent(filePath, content);
    }

    /// <summary>
    /// Reorders a syntax node according to SA1201 rules.
    /// </summary>
    /// <param name="node">The node to reorder.</param>
    /// <param name="violations">The list to add violations to.</param>
    /// <returns>The reordered node.</returns>
    private SyntaxNode ReorderNode(SyntaxNode node, List<Sa1201Violation> violations)
    {
        // Handle namespace declarations (both traditional and file-scoped)
        if (node is BaseNamespaceDeclarationSyntax namespaceDecl && _options.SortTopLevelTypes)
        {
            return ReorderNamespaceDeclaration(namespaceDecl, violations);
        }

        if (node is TypeDeclarationSyntax typeDeclaration)
        {
            var reordered = ReorderTypeDeclaration(typeDeclaration, violations);
            return reordered;
        }

        var children = node.ChildNodes().Select(child => ReorderNode(child, violations)).ToArray();
        return node.ReplaceNodes(
            node.ChildNodes(),
            (oldNode, _) =>
            {
                var index = Array.IndexOf(node.ChildNodes().ToArray(), oldNode);
                return index >= 0 && index < children.Length ? children[index] : oldNode;
            }
        );
    }

    /// <summary>
    /// Reorders types within a namespace declaration.
    /// </summary>
    /// <param name="namespaceDecl">The namespace declaration.</param>
    /// <param name="violations">The list to add violations to.</param>
    /// <returns>The reordered namespace declaration.</returns>
    private BaseNamespaceDeclarationSyntax ReorderNamespaceDeclaration(
        BaseNamespaceDeclarationSyntax namespaceDecl,
        List<Sa1201Violation> violations
    )
    {
        // Get all type declarations in the namespace
        var typeMembers = namespaceDecl.Members.OfType<BaseTypeDeclarationSyntax>().ToList();

        if (typeMembers.Count <= 1)
        {
            // No reordering needed, but still process children
            var children = namespaceDecl
                .ChildNodes()
                .Select(child => ReorderNode(child, violations))
                .ToArray();
            return (BaseNamespaceDeclarationSyntax)
                namespaceDecl.ReplaceNodes(
                    namespaceDecl.ChildNodes(),
                    (oldNode, _) =>
                    {
                        var index = Array.IndexOf(namespaceDecl.ChildNodes().ToArray(), oldNode);
                        return index >= 0 && index < children.Length ? children[index] : oldNode;
                    }
                );
        }

        // Create order info for types
        var typeOrderInfos = typeMembers
            .Select(type => new
            {
                Type = type,
                AccessLevel = GetAccessLevel(type),
                Name = type.Identifier.Text,
                MemberType = GetTopLevelTypeOrder(type),
            })
            .ToList();

        // Sort by type order, access level, then optionally by name
        var sortedTypes = _options.AlphabeticalSort
            ? typeOrderInfos
                .OrderBy(t => t.MemberType)
                .ThenBy(t => t.AccessLevel)
                .ThenBy(t => t.Name)
                .ToList()
            : typeOrderInfos.OrderBy(t => t.MemberType).ThenBy(t => t.AccessLevel).ToList();

        // Check if reordering is needed
        var needsReordering = false;
        for (var i = 0; i < typeOrderInfos.Count; i++)
        {
            if (typeOrderInfos[i].Type != sortedTypes[i].Type)
            {
                needsReordering = true;
                var lineSpan = namespaceDecl.SyntaxTree.GetLineSpan(typeOrderInfos[i].Type.Span);
                violations.Add(
                    new Sa1201Violation(
                        lineSpan.StartLinePosition.Line + 1,
                        lineSpan.StartLinePosition.Character + 1,
                        $"Type '{typeOrderInfos[i].Name}' reordered by access level.",
                        typeOrderInfos[i].Name
                    )
                );
            }
        }

        if (!needsReordering)
        {
            // Still need to process children
            var children = namespaceDecl
                .ChildNodes()
                .Select(child => ReorderNode(child, violations))
                .ToArray();
            return (BaseNamespaceDeclarationSyntax)
                namespaceDecl.ReplaceNodes(
                    namespaceDecl.ChildNodes(),
                    (oldNode, _) =>
                    {
                        var index = Array.IndexOf(namespaceDecl.ChildNodes().ToArray(), oldNode);
                        return index >= 0 && index < children.Length ? children[index] : oldNode;
                    }
                );
        }

        // Reorder the types
        var newMembers = new List<MemberDeclarationSyntax>();

        // First add all non-type members in original order
        foreach (var member in namespaceDecl.Members)
        {
            if (member is not BaseTypeDeclarationSyntax)
            {
                newMembers.Add(member);
            }
        }

        // Then add sorted types (after recursively processing them)
        foreach (var sortedType in sortedTypes)
        {
            var reorderedType = ReorderNode(sortedType.Type, violations);
            newMembers.Add((MemberDeclarationSyntax)reorderedType);
        }

        return namespaceDecl.WithMembers(new SyntaxList<MemberDeclarationSyntax>(newMembers));
    }

    /// <summary>
    /// Gets the sort order for top-level types based on custom configuration.
    /// </summary>
    private int GetTopLevelTypeOrder(BaseTypeDeclarationSyntax type)
    {
        if (_options.TopLevelTypeOrder == null || _options.TopLevelTypeOrder.Count == 0)
        {
            // Default order
            return type switch
            {
                EnumDeclarationSyntax => 0,
                InterfaceDeclarationSyntax => 1,
                StructDeclarationSyntax => 2,
                ClassDeclarationSyntax => 3,
                RecordDeclarationSyntax => 3, // Same as class
                _ => 4,
            };
        }

        // Use custom order
        var typeName = type switch
        {
            EnumDeclarationSyntax => "Enum",
            InterfaceDeclarationSyntax => "Interface",
            StructDeclarationSyntax => "Struct",
            ClassDeclarationSyntax => "Class",
            RecordDeclarationSyntax => "Class", // Records are treated as classes
            _ => null,
        };

        if (typeName == null)
        {
            return 999; // Unknown types at the end
        }

        var index = _options.TopLevelTypeOrder.IndexOf(typeName);
        return index >= 0 ? index : 999;
    }

    /// <summary>
    /// Gets the access level from a base type declaration.
    /// </summary>
    private static AccessLevel GetAccessLevel(BaseTypeDeclarationSyntax type)
    {
        var modifiers = type.Modifiers;

        if (modifiers.Any(SyntaxKind.PublicKeyword))
        {
            return AccessLevel.Public;
        }

        if (modifiers.Any(SyntaxKind.PrivateKeyword) && modifiers.Any(SyntaxKind.ProtectedKeyword))
        {
            return AccessLevel.PrivateProtected;
        }

        if (modifiers.Any(SyntaxKind.ProtectedKeyword) && modifiers.Any(SyntaxKind.InternalKeyword))
        {
            return AccessLevel.ProtectedInternal;
        }

        if (modifiers.Any(SyntaxKind.ProtectedKeyword))
        {
            return AccessLevel.Protected;
        }

        if (modifiers.Any(SyntaxKind.PrivateKeyword))
        {
            return AccessLevel.Private;
        }

        // Default is internal for top-level types
        return AccessLevel.Internal;
    }
}
