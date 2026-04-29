namespace XSpecification.Core;

/// <summary>
/// Specification-wide runtime configuration. Registered as a singleton in DI by the
/// backend-specific <c>AddXxxSpecification</c> extensions and consumed by
/// <see cref="SpecificationBase{TModel,TFilter,TResult}"/>.
/// </summary>
/// <remarks>
/// Replaces the legacy <see cref="Options"/> class wrapped in
/// <c>Microsoft.Extensions.Options.IOptions&lt;Options&gt;</c> — the dependency on
/// <c>Microsoft.Extensions.Options.ConfigurationExtensions</c> is no longer required.
/// </remarks>
public sealed class SpecificationConfiguration
{
    /// <summary>The default configuration with all flags disabled.</summary>
    public static SpecificationConfiguration Default { get; } = new();

    /// <summary>
    /// When <see langword="true"/>, convention-based property handling is disabled and
    /// only filter properties registered via <c>HandleField</c> / <c>IgnoreField</c> are
    /// processed. The default is <see langword="false"/>.
    /// </summary>
    public bool DisablePropertyAutoHandling { get; set; }
}
