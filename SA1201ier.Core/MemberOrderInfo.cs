namespace SA1201ier.Core;

/// <summary>
/// Represents information about a member's position and ordering requirements.
/// </summary>
public class MemberOrderInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemberOrderInfo" /> class.
    /// </summary>
    /// <param name="node">The syntax node.</param>
    /// <param name="memberType">The member type.</param>
    /// <param name="accessLevel">The access level.</param>
    /// <param name="isStatic">Whether the member is static.</param>
    /// <param name="isConst">Whether the member is const.</param>
    public MemberOrderInfo(
        Microsoft.CodeAnalysis.SyntaxNode node,
        MemberType memberType,
        AccessLevel accessLevel,
        bool isStatic,
        bool isConst
    )
    {
        Node = node ?? throw new ArgumentNullException(nameof(node));
        MemberType = memberType;
        AccessLevel = accessLevel;
        IsStatic = isStatic;
        IsConst = isConst;
        StartPosition = node.FullSpan.Start;
        EndPosition = node.FullSpan.End;
    }

    /// <summary>
    /// Gets the access level of the member (public, private, etc.).
    /// </summary>
    public AccessLevel AccessLevel { get; init; }

    /// <summary>
    /// Gets the end position in the source file.
    /// </summary>
    public int EndPosition { get; init; }

    /// <summary>
    /// Gets a value indicating whether the member is const.
    /// </summary>
    public bool IsConst { get; init; }

    /// <summary>
    /// Gets a value indicating whether the member is static.
    /// </summary>
    public bool IsStatic { get; init; }

    /// <summary>
    /// Gets the type of member (field, constructor, property, method, etc.).
    /// </summary>
    public MemberType MemberType { get; init; }

    /// <summary>
    /// Gets the original syntax node.
    /// </summary>
    public Microsoft.CodeAnalysis.SyntaxNode Node { get; init; }

    /// <summary>
    /// Gets the start position in the source file.
    /// </summary>
    public int StartPosition { get; init; }
}
