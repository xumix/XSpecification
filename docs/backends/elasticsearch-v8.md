# Elasticsearch — Elastic.Clients.Elasticsearch 8.x backend

`XSpecification.Elasticsearch.V8` targets the modern
[Elastic.Clients.Elasticsearch](https://www.nuget.org/packages/Elastic.Clients.Elasticsearch) 8.x
client (officially recommended by Elastic for ES 8 / 9 servers).

The V8 backend translates the same backend-agnostic `IQueryNode` AST as the NEST 7.x backend, so
you can swap one backend for the other without rewriting your specifications.

## Registering

```csharp
services.AddElasticSpecificationV8(cfg =>
{
    cfg.AddSpecification<CustomerSpec>();
});
```

(Coming as part of the V8 module — the abstractions and translator are already shipped; the DI
extension follows the same pattern as the NEST 7.x backend.)

## Translating an AST node directly

```csharp
using Elastic.Clients.Elasticsearch.QueryDsl;
using XSpecification.Elasticsearch.Abstractions;
using XSpecification.Elasticsearch.V8;

Query query = ElasticV8QueryBackend.Instance.Translate(new TermNode("status", "active"));
```

## Why two backends?

NEST 7.x is feature-frozen and only compatible with Elasticsearch 7.x servers (and the 7.x
compatibility mode of 8.x clusters). New code should use the V8 backend; the NEST 7.x backend is
provided so existing 1.x deployments can upgrade to XSpecification 2.0 without forcing a client
upgrade in the same step.

The package `XSpecification.Elasticsearch` (PackageId for the legacy artifact) maps to the NEST
7.x backend so 1.x consumers continue to receive updates without changing their `dotnet add
package` line.
