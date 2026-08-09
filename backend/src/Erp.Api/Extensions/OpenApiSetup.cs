using Microsoft.OpenApi;

namespace Erp.Api.Extensions;

/// <summary>
/// The OpenAPI document, and one correction to it.
/// </summary>
internal static class OpenApiSetup
{
    /// <summary>
    /// Registers the document and strips the string alternative from numeric schemas.
    /// <para>
    /// .NET's schema exporter describes every numeric property as
    /// <c>"type": ["integer", "string"]</c> — a decimal as
    /// <c>["null", "number", "string"]</c> — because <c>JsonNumberHandling</c>
    /// <em>could</em> be configured to read a quoted number. This application does not
    /// configure it, so the serializer neither accepts nor produces one: the document
    /// describes a shape the API never uses.
    /// </para>
    /// <para>
    /// It is not cosmetic. Every generator downstream believes it, so the TypeScript
    /// view of a page number becomes <c>number | string</c> and a weight becomes
    /// <c>number | string | null</c>. The web app would then either coerce at every
    /// numeric field or cast the union away — and a cast that exists to silence the
    /// contract is a contract that has stopped checking anything. Correcting the
    /// document once here fixes it for every client, present and future.
    /// </para>
    /// </summary>
    public static IServiceCollection AddErpOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
            options.AddSchemaTransformer((schema, _, _) =>
            {
                if (schema.Type is { } type
                    && (type.HasFlag(JsonSchemaType.Integer) || type.HasFlag(JsonSchemaType.Number))
                    && type.HasFlag(JsonSchemaType.String))
                {
                    schema.Type = type & ~JsonSchemaType.String;

                    // The pattern only ever validated the string spelling of the number.
                    // Left behind, it re-imposes a string reading on a numeric schema.
                    schema.Pattern = null;
                }

                return Task.CompletedTask;
            }));

        return services;
    }
}
