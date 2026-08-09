using System.Globalization;

namespace Erp.BuildingBlocks.Application.Querying;

/// <summary>
/// Converts raw filter text into the CLR type of the field being filtered.
/// <para>
/// Conversion happens before any expression is built, so a value that cannot be
/// parsed is rejected as HTTP 400 and never reaches the database. This, plus the
/// field allow-list, is what makes user-driven filtering safe without any string
/// concatenation — the mechanism that produced 158 injection sites in the system
/// this replaces.
/// </para>
/// </summary>
internal static class QueryValueParser
{
    public static bool TryParse(Type targetType, string raw, out object? value)
    {
        var type = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var culture = CultureInfo.InvariantCulture;

        if (type == typeof(string))
        {
            value = raw;
            return true;
        }

        if (type.IsEnum)
        {
            // Accept the member name; reject the numeric form so an out-of-range
            // integer cannot become an undefined enum value.
            if (Enum.TryParse(type, raw, ignoreCase: true, out var parsedEnum)
                && Enum.IsDefined(type, parsedEnum!))
            {
                value = parsedEnum;
                return true;
            }

            value = null;
            return false;
        }

        bool ok;
        object? result = null;

        if (type == typeof(Guid))
        {
            ok = Guid.TryParse(raw, out var g);
            result = g;
        }
        else if (type == typeof(int))
        {
            ok = int.TryParse(raw, NumberStyles.Integer, culture, out var i);
            result = i;
        }
        else if (type == typeof(long))
        {
            ok = long.TryParse(raw, NumberStyles.Integer, culture, out var l);
            result = l;
        }
        else if (type == typeof(decimal))
        {
            ok = decimal.TryParse(raw, NumberStyles.Number, culture, out var d);
            result = d;
        }
        else if (type == typeof(bool))
        {
            ok = bool.TryParse(raw, out var b);
            result = b;
        }
        else if (type == typeof(DateTimeOffset))
        {
            ok = DateTimeOffset.TryParse(raw, culture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto);
            result = dto;
        }
        else if (type == typeof(DateTime))
        {
            ok = DateTime.TryParse(raw, culture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt);
            result = dt;
        }
        else if (type == typeof(DateOnly))
        {
            ok = DateOnly.TryParse(raw, culture, DateTimeStyles.None, out var d);
            result = d;
        }
        else
        {
            // Unknown types are rejected rather than coerced. Adding support is a
            // deliberate act, not an accident of Convert.ChangeType.
            value = null;
            return false;
        }

        value = ok ? result : null;
        return ok;
    }
}
