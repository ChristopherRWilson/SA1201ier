namespace SA1201ier.Core;

/// <summary>
/// Represents the access level of a class member according to SA1201 ordering rules.
/// </summary>
public enum AccessLevel
{
    /// <summary>
    /// Public access level.
    /// </summary>
    Public = 0,

    /// <summary>
    /// Internal access level.
    /// </summary>
    Internal = 1,

    /// <summary>
    /// Protected internal access level.
    /// </summary>
    ProtectedInternal = 2,

    /// <summary>
    /// Protected access level.
    /// </summary>
    Protected = 3,

    /// <summary>
    /// Private protected access level.
    /// </summary>
    PrivateProtected = 4,

    /// <summary>
    /// Private access level.
    /// </summary>
    Private = 5,
}
