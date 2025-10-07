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
    /// Analyzes a type declaration for SA1201 violations.
    /// </summary>
    /// <param name="typeDeclaration">The type declaration to analyze.</param>
    /// <param name="violations">The list to add violations to.</param>
    private static void AnalyzeTypeDeclaration(
        TypeDeclarationSyntax typeDeclaration,
        List<Sa1201Violation> violations
    )
    {
        var members = typeDeclaration
            .Members.Select(GetMemberOrderInfo)
            .Where(m => m != null)
            .Cast<MemberOrderInfo>()
            .ToList();

        var sortedMembers = OrderMembers(members);

        for (var i = 0; i < members.Count; i++)
        {
            if (members[i].Node == sortedMembers[i].Node)
                continue;

            var lineSpan = typeDeclaration.SyntaxTree.GetLineSpan(members[i].Node.Span);
            var memberName = GetMemberName(members[i].Node);

            violations.Add(
                new Sa1201Violation(
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.StartLinePosition.Character + 1,
                    $"{GetMemberTypeString(members[i])} '{memberName}' should be ordered by access level. "
                        + $"Expected: {GetAccessLevelString(sortedMembers[i].AccessLevel)}, "
                        + $"Actual: {GetAccessLevelString(members[i].AccessLevel)}",
                    memberName
                )
            );
            break;
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
    /// Reorders a type declaration according to SA1201 rules.
    /// </summary>
    /// <param name="typeDeclaration">The type declaration to reorder.</param>
    /// <param name="violations">The list to add violations to.</param>
    /// <returns>The reordered type declaration.</returns>
    private static TypeDeclarationSyntax ReorderTypeDeclaration(
        TypeDeclarationSyntax typeDeclaration,
        List<Sa1201Violation> violations
    )
    {
        var members = typeDeclaration
            .Members.Select(GetMemberOrderInfo)
            .Where(m => m != null)
            .Cast<MemberOrderInfo>()
            .ToList();

        if (members.Count == 0)
        {
            return typeDeclaration;
        }

        var sortedMembers = OrderMembers(members);

        var needsReordering = false;
        for (var i = 0; i < members.Count; i++)
        {
            if (members[i].Node == sortedMembers[i].Node)
                continue;

            needsReordering = true;
            var lineSpan = typeDeclaration.SyntaxTree.GetLineSpan(members[i].Node.Span);
            var memberName = GetMemberName(members[i].Node);

            violations.Add(
                new Sa1201Violation(
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.StartLinePosition.Character + 1,
                    $"{GetMemberTypeString(members[i])} '{memberName}' reordered by access level.",
                    memberName
                )
            );
        }

        if (!needsReordering)
        {
            return typeDeclaration;
        }

        var newMembers = sortedMembers.Select(m => (MemberDeclarationSyntax)m.Node).ToList();
        var newTypeDeclaration = typeDeclaration.WithMembers(
            new SyntaxList<MemberDeclarationSyntax>(newMembers)
        );

        return newTypeDeclaration;
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
