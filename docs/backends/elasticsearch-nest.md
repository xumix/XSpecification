# Elasticsearch — NEST 7.x backend

`XSpecification.Elasticsearch.Nest` (PackageId `XSpecification.Elasticsearch.Nest`) targets the
legacy [NEST 7.17.x](https://www.nuget.org/packages/NEST/) client. It is the historical
implementation; new projects should evaluate the V8 backend instead.

## Registering

```csharp
services.AddElasticSpecification(cfg =>
{
    cfg.AddSpecification<CustomerSpec>();
});
```

## Building a query

```csharp
public sealed class CustomerSearch
{
    private readonly CustomerSpec _spec;
    private readonly IElasticClient _client;

    public CustomerSearch(CustomerSpec spec, IElasticClient client)
    {
        _spec = spec;
        _client = client;
    }

    public async Task<IReadOnlyCollection<Customer>> Search(CustomerFilter filter, CancellationToken ct)
    {
        var query = await _spec.CreateFilterQueryAsync(filter, ct);

        var response = await _client.SearchAsync<Customer>(s => s
            .Query(_ => query)
            .Size(100), ct);

        return response.Documents;
    }
}
```

## Field-name strategy

`SpecificationBase.NamingStrategy` defaults to `CamelCaseNamingStrategy`. Override it on a derived
specification if your indices use a different convention.

## Backend-agnostic translation

Both NEST 7.x and V8 backends consume the same `IQueryNode` AST defined in
`XSpecification.Elasticsearch.Abstractions`. The class
`XSpecification.Elasticsearch.NestQueryBackend` exposes a singleton `IQueryBackend<QueryContainer>`
that you can use to translate AST nodes you produce yourself.

```csharp
QueryContainer query = NestQueryBackend.Instance.Translate(
    new TermNode("status", "active"));
```
