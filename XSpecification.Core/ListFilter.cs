using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace XSpecification.Core;

public sealed class ListFilter<T> : IListFilter, IReadOnlyCollection<T>
{
    private bool isNotNull;
    private bool isNull;

    public ListFilter()
        : this(null)
    {
    }

    public ListFilter(params T[]? values)
        : this(values?.AsEnumerable())
    {
    }

    public ListFilter(IEnumerable<T>? values)
    {
        Values = values?.ToList();
    }

    [JsonIgnore]
    public Type ElementType => typeof(T);

    /// <summary>
    ///     Gets the number of elements in the collection.
    /// </summary>
    /// <returns>
    ///     The number of elements in the collection.
    /// </returns>
    [JsonIgnore]
    public int Count => Values?.Count ?? 0;

    /// <summary>
    /// Indicates that filter logic will be inverted, eg !.Contains()
    /// </summary>
    public bool IsInverted { get; set; }

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
    ///  Filter values
    /// </summary>
    internal List<T>? Values { get; set; }

    public static implicit operator ListFilter<T>(T value)
    {
        return new ListFilter<T>(value);
    }

    public static implicit operator ListFilter<T>(T[]? values)
    {
        return new ListFilter<T>(values);
    }

    public static implicit operator ListFilter<T>(List<T>? values)
    {
        return new ListFilter<T>(values);
    }

    public void Add([NotNull] params T[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        Values ??= new List<T>();
        Values.AddRange(values);
    }

    public void Remove([NotNull] params T[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        Values ??= new List<T>();
        foreach (var value in values)
        {
            Values.Remove(value);
        }
    }

    public void ClearValues()
    {
        Values?.Clear();
    }

    public void Reset()
    {
        IsNotNull = false;
        IsNull = false;
        Values = null;
    }

    public ListFilter<T> Clone()
    {
        return new ListFilter<T>
        {
            IsNotNull = IsNotNull,
            IsNull = IsNull,
            Values = Values,
        };
    }

    /// <summary>
    ///     Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
    /// </returns>
    public IEnumerator<T> GetEnumerator()
    {
        return Values != null
            ? Values.AsEnumerable().GetEnumerator()
            : Array.Empty<T>().AsEnumerable().GetEnumerator();
    }

    /// <summary>
    ///     Checks if the filter has some filtering rules
    /// </summary>
    public bool HasValue()
    {
        return IsNull || IsNotNull || !IsEmpty();
    }

    /// <summary>
    ///     Проверка на наличие значения фильтрации
    /// </summary>
    public bool IsEmpty()
    {
        return Values == null;
    }

    /// <summary>
    ///     Returns a string that represents the current object.
    /// </summary>
    /// <returns>
    ///     A string that represents the current object.
    /// </returns>
    public override string ToString()
    {
        return !HasValue()
            ? "Empty"
            : $"IsInverted: {IsInverted}, IsNull: {IsNull}, IsNotNull: {IsNotNull}, HasValue: {HasValue()}, Count: {Count}," +
              $" Values: {(Values != null ? string.Join(",", Values) : null)}";
    }

    /// <summary>The clone.</summary>
    /// <returns></returns>
    object ICloneable.Clone()
    {
        return Clone();
    }

    /// <summary>
    ///     Returns an enumerator that iterates through a collection.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
    /// </returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
