using System.Text.Json.Serialization;

namespace XSpecification.Core;

public class RangeFilter<T> : ICloneable, IRangeFilter
    where T : struct
{
    private bool isNotNull;
    private bool isNull;

    [JsonIgnore]
    public Type ElementType => typeof(T);

    /// <summary>
    ///  Range end, inclusive
    /// </summary>
    public virtual T? End { get; set; }

    /// <summary>
    ///  Indicates the range is exclusive
    /// </summary>
    public bool IsExclusive { get; set; }

    /// <summary>
    ///     Indicates that filtering must check model field for non-null value
    /// </summary>
    public bool IsNotNull
    {
        get => isNotNull;

        set
        {
            isNotNull = value;
            if (isNotNull)
            {
                isNull = false;
            }
        }
    }

    /// <summary>
    ///     Indicates that filtering must check model field for null
    /// </summary>
    public bool IsNull
    {
        get => isNull;

        set
        {
            isNull = value;
            if (isNull)
            {
                isNotNull = false;
            }
        }
    }

    /// <summary>
    ///     Range start, inclusive
    /// </summary>
    public T? Start { get; set; }

    /// <summary>
    /// If set the start value is used as constant value instead of range
    /// </summary>
    public bool UseStartAsEquals { get; set; }

    object? IRangeFilter.End
    {
        get => End;
        set => End = (T?)value;
    }

    object? IRangeFilter.Start
    {
        get => Start;
        set => Start = (T?)value;
    }

    public object Clone()
    {
        return new RangeFilter<T>
        {
            Start = Start,
            End = End,
            IsNotNull = IsNotNull,
            IsNull = IsNull,
            UseStartAsEquals = UseStartAsEquals,
        };
    }

    /// <summary>
    ///     Checks if the filter has some filtering rules
    /// </summary>
    public bool HasValue()
    {
        return Start.HasValue || End.HasValue || IsNull || IsNotNull;
    }

    /// <summary>
    ///     Сброс фильтра
    /// </summary>
    public void Reset()
    {
        Start = null;
        End = null;
        IsNotNull = false;
        IsNull = false;
        UseStartAsEquals = false;
    }

    public override string ToString()
    {
        return
            !HasValue()
                ? "Empty"
                : $"Start: {Start}, End: {End}, UseStartAsEquals: {UseStartAsEquals}, IsNull: {IsNull}, IsNotNull: {IsNotNull}, HasValue: {HasValue()}";
    }
}
