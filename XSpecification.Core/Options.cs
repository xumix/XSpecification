namespace XSpecification.Core;

/// <summary>
/// Legacy options class kept for binary compatibility with 1.x consumers.
/// New code should depend on <see cref="SpecificationConfiguration"/> directly.
/// </summary>
[Obsolete("Use SpecificationConfiguration record instead. This type will be removed in 3.0.")]
public class Options
{
    /// <summary>Disables convention-based property handling.</summary>
    public bool DisablePropertyAutoHandling { get; set; }

    /// <summary>Convert this legacy options object to a <see cref="SpecificationConfiguration"/>.</summary>
    public SpecificationConfiguration ToConfiguration() =>
        new() { DisablePropertyAutoHandling = this.DisablePropertyAutoHandling };
}
