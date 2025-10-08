namespace SA1201ier.Core;

/// <summary>
/// Configuration options for SA1201 formatting.
/// </summary>
public class FormatterOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to alphabetically sort members
    /// within the same access level and member type.
    /// </summary>
    public bool AlphabeticalSort { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to sort top-level types
    /// (classes, records, interfaces, etc.) within a file by access level.
    /// </summary>
    public bool SortTopLevelTypes { get; set; }

    /// <summary>
    /// Gets or sets the custom order for access levels.
    /// If null or empty, uses default order: Public, Internal, ProtectedInternal, Protected, PrivateProtected, Private.
    /// Valid values: "Public", "Internal", "ProtectedInternal", "Protected", "PrivateProtected", "Private".
    /// </summary>
    public List<string>? AccessLevelOrder { get; set; }

    /// <summary>
    /// Gets or sets the custom order for member types.
    /// If null or empty, uses default order: Field, Constructor, Destructor, Delegate, Event, Enum, Interface, Property, Indexer, Method, Struct, Class.
    /// Valid values: "Field", "Constructor", "Destructor", "Delegate", "Event", "Enum", "Interface", "Property", "Indexer", "Method", "Struct", "Class".
    /// </summary>
    public List<string>? MemberTypeOrder { get; set; }

    /// <summary>
    /// Gets or sets the custom order for top-level types.
    /// If null or empty, uses default order: Enum, Interface, Struct, Class.
    /// Valid values: "Enum", "Interface", "Struct", "Class".
    /// </summary>
    public List<string>? TopLevelTypeOrder { get; set; }

    /// <summary>
    /// Gets or sets whether static members should come before instance members.
    /// Default: true (static first).
    /// </summary>
    public bool StaticMembersFirst { get; set; } = true;

    /// <summary>
    /// Gets or sets whether const members should come before non-const members.
    /// Default: true (const first).
    /// </summary>
    public bool ConstMembersFirst { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to insert a single blank line between sorted members.
    /// When enabled, ensures exactly one blank line exists between members.
    /// Default: false.
    /// </summary>
    public bool InsertBlankLineBetweenMembers { get; set; } = false;

    /// <summary>
    /// Gets a cached instance with default options.
    /// </summary>
    public static readonly FormatterOptions Default = new FormatterOptions
    {
        AlphabeticalSort = false,
        SortTopLevelTypes = false,
        StaticMembersFirst = true,
        ConstMembersFirst = true,
        InsertBlankLineBetweenMembers = false,
    };

    /// <summary>
    /// Merges this options instance with another, with the other taking precedence for non-null values.
    /// </summary>
    /// <param name="other">The options to merge with.</param>
    /// <returns>A new options instance with merged values.</returns>
    public FormatterOptions MergeWith(FormatterOptions? other)
    {
        if (other == null)
        {
            return this;
        }

        // For boolean properties, only override if the other value is different from the default
        // This allows option sources to selectively override other settings
        var defaultOptions = Default;

        return new FormatterOptions
        {
            AlphabeticalSort =
                other.AlphabeticalSort != defaultOptions.AlphabeticalSort
                    ? other.AlphabeticalSort
                    : AlphabeticalSort,
            SortTopLevelTypes =
                other.SortTopLevelTypes != defaultOptions.SortTopLevelTypes
                    ? other.SortTopLevelTypes
                    : SortTopLevelTypes,
            AccessLevelOrder = other.AccessLevelOrder ?? AccessLevelOrder,
            MemberTypeOrder = other.MemberTypeOrder ?? MemberTypeOrder,
            TopLevelTypeOrder = other.TopLevelTypeOrder ?? TopLevelTypeOrder,
            StaticMembersFirst =
                other.StaticMembersFirst != defaultOptions.StaticMembersFirst
                    ? other.StaticMembersFirst
                    : StaticMembersFirst,
            ConstMembersFirst =
                other.ConstMembersFirst != defaultOptions.ConstMembersFirst
                    ? other.ConstMembersFirst
                    : ConstMembersFirst,
            InsertBlankLineBetweenMembers =
                other.InsertBlankLineBetweenMembers != defaultOptions.InsertBlankLineBetweenMembers
                    ? other.InsertBlankLineBetweenMembers
                    : InsertBlankLineBetweenMembers,
        };
    }
}
