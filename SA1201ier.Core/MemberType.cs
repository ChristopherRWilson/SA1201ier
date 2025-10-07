namespace SA1201ier.Core;

/// <summary>
/// Represents the type of a class member according to SA1201 ordering rules.
/// </summary>
public enum MemberType
{
    /// <summary>
    /// A field member.
    /// </summary>
    Field = 0,

    /// <summary>
    /// A constructor member.
    /// </summary>
    Constructor = 1,

    /// <summary>
    /// A destructor/finalizer member.
    /// </summary>
    Destructor = 2,

    /// <summary>
    /// A delegate member.
    /// </summary>
    Delegate = 3,

    /// <summary>
    /// An event member.
    /// </summary>
    Event = 4,

    /// <summary>
    /// An enum member.
    /// </summary>
    Enum = 5,

    /// <summary>
    /// An interface member.
    /// </summary>
    Interface = 6,

    /// <summary>
    /// A property member.
    /// </summary>
    Property = 7,

    /// <summary>
    /// An indexer member.
    /// </summary>
    Indexer = 8,

    /// <summary>
    /// A method member.
    /// </summary>
    Method = 9,

    /// <summary>
    /// A struct member.
    /// </summary>
    Struct = 10,

    /// <summary>
    /// A class member.
    /// </summary>
    Class = 11,
}
