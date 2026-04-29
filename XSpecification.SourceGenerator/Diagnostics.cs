using Microsoft.CodeAnalysis;

namespace XSpecification.SourceGenerator;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor UnmappedFilterProperty = new(
        id: "XSPEC001",
        title: "Filter property is not mapped to a model property",
        messageFormat: "Filter property '{0}.{1}' is not mapped to any property on model '{2}' — add HandleField/IgnoreField or rename it to match a model property",
        category: "XSpecification",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When XSpecification builds a specification it walks every property on the filter type and tries to find a same-named property on the model.");

    public static readonly DiagnosticDescriptor SpecificationDetected = new(
        id: "XSPEC100",
        title: "Specification class detected by XSpecification source generator",
        messageFormat: "Generated accessor delegates for specification '{0}' (filter '{1}', model '{2}')",
        category: "XSpecification",
        defaultSeverity: DiagnosticSeverity.Hidden,
        isEnabledByDefault: true,
        description: "Informational diagnostic emitted when the generator successfully processes a specification class.");
}
