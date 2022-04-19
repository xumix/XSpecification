using System.Collections;

namespace XSpecification.Core;

public interface IListFilter : ICloneable, IEnumerable, INullableFilter
{
    /// <summary>
    ///     Indicates that filter logic will be inverted
    /// </summary>
    bool IsInverted { get; set; }

    /// <summary>
    ///     Тип значения в фильтре
    /// </summary>
    Type ElementType { get; }

    /// <summary>
    /// Checks if the filter has some filtering rules
    /// </summary>
    bool HasValue();

    /// <summary>
    ///  Checks if the filter has some values
    /// </summary>
    bool IsEmpty();
}
