namespace XSpecification.Core;

/// <summary>
/// Tri-state boolean filter that distinguishes between "not set" (no filtering),
/// <see langword="true"/>, and <see langword="false"/>. Use this instead of <c>bool?</c>
/// when the filtered field itself is non-nullable but the user may opt out of filtering it.
/// </summary>
public sealed class BoolFilter : INullableFilter, ICloneable
{
    private bool _isNotNull;
    private bool _isNull;

    /// <summary>The boolean value to match. <see langword="null"/> means "do not filter".</summary>
    public bool? Value { get; set; }

    /// <inheritdoc />
    public bool IsNotNull
    {
        get => _isNotNull;
        set
        {
            _isNotNull = value;
            if (_isNotNull)
            {
                _isNull = false;
            }
        }
    }

    /// <inheritdoc />
    public bool IsNull
    {
        get => _isNull;
        set
        {
            _isNull = value;
            if (_isNull)
            {
                _isNotNull = false;
            }
        }
    }

    /// <summary>Returns <see langword="true"/> when this filter has any active rule.</summary>
    public bool HasValue() => Value.HasValue || IsNull || IsNotNull;

    /// <summary>Reset the filter to its empty (no-op) state.</summary>
    public void Reset()
    {
        Value = null;
        IsNotNull = false;
        IsNull = false;
    }

    /// <inheritdoc />
    public object Clone() => new BoolFilter
    {
        Value = Value,
        IsNotNull = IsNotNull,
        IsNull = IsNull,
    };

    /// <summary>Implicit conversion from <see cref="bool"/> to <see cref="BoolFilter"/>.</summary>
    public static implicit operator BoolFilter(bool value) => new() { Value = value };

    /// <inheritdoc />
    public override string ToString() =>
        !HasValue() ? "Empty" : $"Value: {Value}, IsNull: {IsNull}, IsNotNull: {IsNotNull}";
}
