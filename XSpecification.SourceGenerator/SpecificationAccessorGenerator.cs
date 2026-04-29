using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XSpecification.SourceGenerator;

/// <summary>
/// Incremental Roslyn generator that walks every type derived from
/// <c>XSpecification.Core.SpecificationBase&lt;TModel, TFilter, TResult&gt;</c> and emits:
/// <list type="bullet">
///   <item>A static accessor table of <c>Func&lt;TFilter, object?&gt;</c> per filter property —
///     consumers can use it instead of <c>PropertyInfo.GetValue(...)</c> on the hot path,
///     which also keeps the code AOT-trimming friendly.</item>
///   <item>Compile-time diagnostics (<c>XSPEC001</c>) for filter properties that have no
///     same-named property on the model and are not registered via <c>HandleField</c> /
///     <c>IgnoreField</c>.</item>
/// </list>
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class SpecificationAccessorGenerator : IIncrementalGenerator
{
    private const string SpecificationBaseFullName = "XSpecification.Core.SpecificationBase`3";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidate(node),
                transform: static (ctx, _) => GetSpecificationInfo(ctx))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value);

        context.RegisterSourceOutput(candidates, Emit);
    }

    private static bool IsCandidate(SyntaxNode node) =>
        node is ClassDeclarationSyntax cls && cls.BaseList is not null;

    private static SpecificationInfo? GetSpecificationInfo(GeneratorSyntaxContext ctx)
    {
        var classDecl = (ClassDeclarationSyntax)ctx.Node;
        if (ctx.SemanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol symbol)
        {
            return null;
        }

        var specBase = FindSpecificationBase(symbol);
        if (specBase is null)
        {
            return null;
        }

        var modelType = specBase.TypeArguments[0];
        var filterType = specBase.TypeArguments[1];

        if (modelType is not INamedTypeSymbol modelNamed || filterType is not INamedTypeSymbol filterNamed)
        {
            return null;
        }

        var filterProps = filterNamed
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public
                        && !p.IsStatic
                        && !p.IsIndexer
                        && p.GetMethod is not null)
            .Select(p => p.Name)
            .ToImmutableArray();

        var modelPropNames = modelNamed
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic && !p.IsIndexer)
            .Select(p => p.Name)
            .ToImmutableHashSet();

        var unmapped = filterProps
            .Where(name => !modelPropNames.Contains(name))
            .ToImmutableArray();

        return new SpecificationInfo(
            symbol.ToDisplayString(),
            symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString(),
            symbol.Name,
            filterNamed.ToDisplayString(),
            modelNamed.ToDisplayString(),
            filterProps,
            unmapped,
            classDecl.GetLocation());
    }

    private static INamedTypeSymbol? FindSpecificationBase(INamedTypeSymbol symbol)
    {
        for (var current = symbol.BaseType; current is not null; current = current.BaseType)
        {
            if (current is { IsGenericType: true } named
                && named.ConstructedFrom?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) is { } cf
                && (cf == "global::" + SpecificationBaseFullName.Replace("`3", "<TModel, TFilter, TResult>")
                    || named.OriginalDefinition.MetadataName == "SpecificationBase`3"
                    && named.OriginalDefinition.ContainingNamespace?.ToDisplayString() == "XSpecification.Core"))
            {
                return named;
            }
        }

        return null;
    }

    private static void Emit(SourceProductionContext spc, SpecificationInfo info)
    {
        spc.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.SpecificationDetected,
            info.Location,
            info.SpecName,
            info.FilterFullName,
            info.ModelFullName));

        foreach (var prop in info.UnmappedFilterProperties)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.UnmappedFilterProperty,
                info.Location,
                info.FilterFullName,
                prop,
                info.ModelFullName));
        }

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();

        var hasNs = !string.IsNullOrEmpty(info.Namespace);
        if (hasNs)
        {
            sb.Append("namespace ").Append(info.Namespace).AppendLine(";");
            sb.AppendLine();
        }

        sb.Append("internal static class ").Append(info.SpecName).AppendLine("Accessors");
        sb.AppendLine("{");
        sb.Append("    public static readonly IReadOnlyDictionary<string, Func<")
            .Append(info.FilterFullName)
            .AppendLine(", object?>> FilterPropertyAccessors =");
        sb.Append("        new Dictionary<string, Func<")
            .Append(info.FilterFullName)
            .AppendLine(", object?>>");
        sb.AppendLine("        {");
        foreach (var prop in info.FilterProperties)
        {
            sb.Append("            [\"")
                .Append(prop)
                .Append("\"] = static f => f.")
                .Append(prop)
                .AppendLine(",");
        }

        sb.AppendLine("        };");
        sb.AppendLine("}");

        var hint = (info.Namespace is null ? string.Empty : info.Namespace + ".") + info.SpecName + "Accessors.g.cs";
        spc.AddSource(hint, sb.ToString());
    }

    private readonly struct SpecificationInfo
    {
        public SpecificationInfo(
            string fullName,
            string? @namespace,
            string specName,
            string filterFullName,
            string modelFullName,
            ImmutableArray<string> filterProperties,
            ImmutableArray<string> unmappedFilterProperties,
            Location location)
        {
            FullName = fullName;
            Namespace = @namespace;
            SpecName = specName;
            FilterFullName = filterFullName;
            ModelFullName = modelFullName;
            FilterProperties = filterProperties;
            UnmappedFilterProperties = unmappedFilterProperties;
            Location = location;
        }

        public string FullName { get; }
        public string? Namespace { get; }
        public string SpecName { get; }
        public string FilterFullName { get; }
        public string ModelFullName { get; }
        public ImmutableArray<string> FilterProperties { get; }
        public ImmutableArray<string> UnmappedFilterProperties { get; }
        public Location Location { get; }
    }
}
