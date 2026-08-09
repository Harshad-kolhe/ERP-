using System.Text.Json;
using System.Text.Json.Serialization;

namespace Erp.IntegrationTests;

/// <summary>Shares one SQL Server container across the whole suite.</summary>
[CollectionDefinition(nameof(ErpApiCollection))]
public sealed class ErpApiCollection : ICollectionFixture<ErpApiFactory>;

public static class JsonOptions
{
    /// <summary>Mirrors the API's serializer: camelCase properties, enums as names.</summary>
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
