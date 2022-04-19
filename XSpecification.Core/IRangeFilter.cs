namespace XSpecification.Core;

public interface IRangeFilter : INullableFilter
{
    /// <summary>
    /// Range end, inclusive
    /// </summary>
    object? End { get; set; }

    /// <summary>
    /// Range start, inclusive
    /// </summary>
    object? Start { get; set; }

    /// <summary>
    /// Indicates the range is exclusive
    /// </summary>
    bool IsExclusive { get; set; }

    /// <summary>
    ///  If set the start value is used as constant value instead of range
    /// </summary>
    bool UseStartAsEquals { get; set; }

    Type ElementType { get; }

    /// <summary>
    ///     Checks if the filter has some filtering rules
    /// </summary>
    bool HasValue();

    void Reset();
}
