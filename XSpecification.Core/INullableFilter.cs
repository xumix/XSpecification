namespace XSpecification.Core;

public interface INullableFilter
{
    /// <summary>
    ///     Indicates that filtering must check model field for non-null value
    /// </summary>
    bool IsNotNull { get; set; }

    /// <summary>
    ///     Indicates that filtering must check model field for null
    /// </summary>
    bool IsNull { get; set; }
}
