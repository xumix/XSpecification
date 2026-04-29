using System.Collections;

namespace XSpecification.Core;

public interface IListFilter : ICloneable, IEnumerable, INullableFilter
{
    /// <summary>
    ///     Indicates that filter logic will be inverted
    /// </summary>
    bool IsInverted { get; set; }

    /// <summary>
    ///     Element type stored in the filter (e.g. <see cref="int"/> for <c>ListFilter&lt;int&gt;</c>).
    /// </summary>
    Type ElementType { get; }

    /// <summary>
    ///  Filter values
    /// </summary>
    IEnumerable? Values { get; set; }

    /// <summary>
    /// Checks if the filter has some filtering rules
    /// </summary>
    bool HasValue();

    /// <summary>
    ///  Checks if the filter has some values
    /// </summary>
    bool IsEmpty();
}
