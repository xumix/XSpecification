namespace XSpecification.Core;

public class StringFilter : ICloneable, INullableFilter
{
    private bool isNotNull;
    private bool isNull;

    public StringFilter(string value)
    {
        Value = value;
    }

    public StringFilter()
    {
    }

    /// <summary>
    ///     Indicates that filtering must be inverted, eg != "someValue"
    /// </summary>
    public bool IsInverted { get; set; }

    public bool Contains { get; set; }

    public bool StartsWith { get; set; }

    public bool EndsWith { get; set; }

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
    ///     The value to use in filtering
    /// </summary>
    public string? Value { get; set; }

    public static implicit operator StringFilter(string value)
    {
        return new StringFilter(value);
    }

    public StringFilter Clone()
    {
        return new StringFilter
        {
            IsNotNull = IsNotNull,
            IsNull = IsNull,
            Value = Value,
            Contains = Contains,
            EndsWith = EndsWith,
            StartsWith = StartsWith,
            IsInverted = IsInverted,
        };
    }

    /// <summary>
    ///     Checks if the filter has some filtering rules
    /// </summary>
    public bool HasValue()
    {
        return IsNull || IsNotNull || !IsEmpty();
    }

    /// <summary>
    ///     Checks if the filter contains a string value
    /// </summary>
    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(Value);
    }

    public void Reset()
    {
        IsNotNull = false;
        IsNull = false;
        Value = null;
        Contains = false;
        EndsWith = false;
        StartsWith = false;
        IsInverted = false;
    }

    object ICloneable.Clone()
    {
        return Clone();
    }

    public override string ToString()
    {
        return !HasValue()
            ? "Empty"
            : $"{nameof(IsInverted)}: {IsInverted}, {nameof(Contains)}: {Contains}, {nameof(StartsWith)}: {StartsWith}, {nameof(EndsWith)}: {EndsWith}, {nameof(IsNotNull)}: {IsNotNull}, {nameof(IsNull)}: {IsNull}, {nameof(Value)}: {Value}";
    }
}
