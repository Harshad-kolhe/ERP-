using System.Reflection;
using Erp.Api.Common.Paging;

namespace Erp.ArchitectureTests;

/// <summary>
/// Every list endpoint sorts and filters through a <see cref="QueryMap{T}"/>, and
/// every field on one becomes SQL. That makes the field's property type a
/// persistence concern, not a presentation one.
/// </summary>
public sealed class QueryMapConventionTests
{
    private static readonly Assembly Api = typeof(Erp.Api.Common.Paging.QueryMap<object>).Assembly;

    private static readonly Assembly Contracts = typeof(Erp.Contracts.Masters.MasterStatusDto).Assembly;

    /// <summary>
    /// A status column is stored as its name (<c>HasConversion&lt;string&gt;</c>). Put the
    /// contract enum in the projection the query map is built over and EF compiles the
    /// conversion to <c>CAST([x].[Status] AS int)</c> — valid SQL that throws
    /// "Conversion failed when converting the nvarchar value 'Approved' to data type int"
    /// the moment a page contains a row.
    /// <para>
    /// It shipped exactly that way on the supplier and customer lists, and no test caught
    /// it: the query translates, and the translation test runs against a dead connection
    /// so it never reads a row. The fix is to project the domain enum and cast to the
    /// contract enum in C# afterwards, which is what this asserts.
    /// </para>
    /// </summary>
    [Fact]
    public void No_query_map_field_is_a_contract_enum()
    {
        var offenders = QueryMapFields()
            .Where(field => field.PropertyType.IsEnum && field.PropertyType.Assembly == Contracts)
            .Select(field => $"{field.Owner}.{field.FieldName} is {field.PropertyType.Name}")
            .Order(StringComparer.Ordinal)
            .ToList();

        offenders.ShouldBeEmpty(
            "A query map field becomes SQL. Project the domain enum into the row type and "
            + "cast to the contract enum after materialisation. Offenders: "
            + string.Join("; ", offenders));
    }

    private static IEnumerable<(string Owner, string FieldName, Type PropertyType)> QueryMapFields()
    {
        var maps = Api.GetTypes()
            .Where(type => type.Namespace?.StartsWith("Erp.Api.Features", StringComparison.Ordinal) == true)
            .SelectMany(type => type
                .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(field => field.FieldType.IsGenericType
                    && field.FieldType.GetGenericTypeDefinition() == typeof(QueryMap<>))
                .Select(field => (Owner: type.Name, Map: field.GetValue(null))))
            .Where(entry => entry.Map is not null);

        foreach (var (owner, map) in maps)
        {
            var declared = map!.GetType()
                .GetField("_fields", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(map);

            foreach (var queryField in ((System.Collections.IEnumerable)declared!).Cast<object>())
            {
                // The dictionary yields KeyValuePair<string, IQueryField<T>>; the value is
                // a QueryField<T, TProp> whose second argument is the projected type.
                var value = queryField.GetType().GetProperty("Value")!.GetValue(queryField)!;
                var arguments = value.GetType().GetGenericArguments();

                if (arguments.Length == 2)
                {
                    var name = (string)value.GetType().GetProperty("Name")!.GetValue(value)!;

                    yield return (owner, name, arguments[1]);
                }
            }
        }
    }
}
