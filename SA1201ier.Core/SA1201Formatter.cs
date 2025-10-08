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
    /// <summary>
    /// Checks content for SA1201 violations without modifying it.
    /// </summary>
    /// <param name="filePath">The file path (for reporting purposes).</param>
    /// <param name="content">The content to check.</param>
    /// <returns>A <see cref="FormattingResult" /> containing violation information.</returns>
    public static FormattingResult CheckContent(string filePath, string content)
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
    private static void AnalyzeNode(SyntaxNode node, List<Sa1201Violation> violations)
    {
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
    /// Analyzes a type declaration for SA1201 violations while respecting
    /// preprocessor directives and region boundaries.
    /// </summary>
    /// <param name="typeDeclaration">The type declaration to analyze.</param>
    /// <param name="violations">The list to add violations to.</param>
    private static void AnalyzeTypeDeclaration(
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
    private static List<MemberOrderInfo> OrderMembers(List<MemberOrderInfo> members)
    {
        return members
            .OrderBy(m => m.MemberType)
            .ThenBy(m => m.IsConst ? 0 : 1)
            .ThenBy(m => m.IsStatic ? 0 : 1)
            .ThenBy(m => m.AccessLevel)
            .ToList();
    }

    /// <summary>
    /// Reorders a type declaration according to SA1201 rules while preserving
    /// preprocessor directives and region boundaries.
    /// </summary>
    /// <param name="typeDeclaration">The type declaration to reorder.</param>
    /// <param name="violations">The list to add violations to.</param>
    /// <returns>The reordered type declaration.</returns>
    private static TypeDeclarationSyntax ReorderTypeDeclaration(
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
                // Preserve the leading trivia of the first member in the group
                var firstOriginalMember = group.Members[0];
                var leadingTrivia = firstOriginalMember.GetLeadingTrivia();

                for (var i = 0; i < sortedMembers.Count; i++)
                {
                    var member = (MemberDeclarationSyntax)sortedMembers[i].Node;

                    // Apply the preserved leading trivia to the first reordered member
                    if (i == 0)
                    {
                        member = member.WithLeadingTrivia(leadingTrivia);
                    }

                    reorderedMembers.Add(member);
                }
            }
            else
            {
                // No reordering needed, keep original order
                reorderedMembers.AddRange(group.Members);
            }
        }

        if (!needsReordering)
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
        var groups = new List<MemberGroup>();
        var currentGroup = new MemberGroup();

        // Track the nesting level of preprocessor directives and regions
        var directiveDepth = 0;
        var regionDepth = 0;

        foreach (var member in allMembers)
        {
            var leadingTrivia = member.GetLeadingTrivia();

            // Count opening and closing directives in leading trivia
            var ifDirectives = 0;
            var endIfDirectives = 0;
            var regionDirectives = 0;
            var endRegionDirectives = 0;

            foreach (var trivia in leadingTrivia)
            {
                if (trivia.IsKind(SyntaxKind.IfDirectiveTrivia))
                    ifDirectives++;
                else if (trivia.IsKind(SyntaxKind.EndIfDirectiveTrivia))
                    endIfDirectives++;
                else if (trivia.IsKind(SyntaxKind.RegionDirectiveTrivia))
                    regionDirectives++;
                else if (trivia.IsKind(SyntaxKind.EndRegionDirectiveTrivia))
                    endRegionDirectives++;
            }

            // Check trailing trivia too
            var trailingTrivia = member.GetTrailingTrivia();
            foreach (var trivia in trailingTrivia)
            {
                if (trivia.IsKind(SyntaxKind.IfDirectiveTrivia))
                    ifDirectives++;
                else if (trivia.IsKind(SyntaxKind.EndIfDirectiveTrivia))
                    endIfDirectives++;
                else if (trivia.IsKind(SyntaxKind.RegionDirectiveTrivia))
                    regionDirectives++;
                else if (trivia.IsKind(SyntaxKind.EndRegionDirectiveTrivia))
                    endRegionDirectives++;
            }

            // If we're closing a directive/region block and have members in current group,
            // finalize this group first
            var wasInBlock = directiveDepth > 0 || regionDepth > 0;
            directiveDepth -= endIfDirectives;
            regionDepth -= endRegionDirectives;
            directiveDepth = Math.Max(0, directiveDepth);
            regionDepth = Math.Max(0, regionDepth);
            var nowInBlock = directiveDepth > 0 || regionDepth > 0;

            // If we're leaving a block, close current group
            if (wasInBlock && !nowInBlock && currentGroup.Members.Count > 0)
            {
                currentGroup.Members.Add(member);
                groups.Add(currentGroup);
                currentGroup = new MemberGroup();

                // Update depths after processing opening directives
                directiveDepth += ifDirectives;
                regionDepth += regionDirectives;
                continue;
            }

            // Update depths for opening directives
            directiveDepth += ifDirectives;
            regionDepth += regionDirectives;

            // If we're entering a block, close current group first
            if (!wasInBlock && nowInBlock && currentGroup.Members.Count > 0)
            {
                groups.Add(currentGroup);
                currentGroup = new MemberGroup();
            }

            currentGroup.Members.Add(member);
        }

        // Add any remaining members in the current group
        if (currentGroup.Members.Count > 0)
        {
            groups.Add(currentGroup);
        }

        // If no groups were created, treat all members as one group
        if (groups.Count == 0)
        {
            return new List<MemberGroup> { new MemberGroup { Members = allMembers } };
        }

        return groups;
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
}
