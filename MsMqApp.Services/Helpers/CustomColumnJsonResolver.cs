using System.Text;
using System.Text.Json;
using MsMqApp.Models.Configuration;
using MsMqApp.Models.Domain;

namespace MsMqApp.Services.Helpers;

/// <summary>
/// Resolves the intentionally small JSON path syntax used by custom message columns.
/// Supported syntax is Property.Property[0].Property.
/// </summary>
public static class CustomColumnJsonResolver
{
    public static IReadOnlyDictionary<Guid, string> ResolveValues(
        QueueMessage message,
        IEnumerable<CustomColumnDefinition> columns)
    {
        var result = new Dictionary<Guid, string>();
        var visibleColumns = columns.Where(column => column.IsVisible).ToList();
        if (visibleColumns.Count == 0 || message is null || !message.IsBodyLoaded)
        {
            return result;
        }

        foreach (var column in visibleColumns)
        {
            result[column.Id] = string.Empty;
        }

        var content = GetRawContent(message.Body);
        if (string.IsNullOrWhiteSpace(content))
        {
            return result;
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            foreach (var column in visibleColumns)
            {
                try
                {
                    result[column.Id] = Resolve(document.RootElement, column.JsonPath);
                }
                catch (ArgumentException)
                {
                    result[column.Id] = string.Empty;
                }
            }
        }
        catch (JsonException)
        {
            // A non-JSON message simply produces blank custom values.
        }

        return result;
    }

    public static string Resolve(JsonElement root, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var current = root;
        foreach (var segment in ParsePath(path))
        {
            if (segment.PropertyName is not null)
            {
                if (current.ValueKind != JsonValueKind.Object ||
                    !current.TryGetProperty(segment.PropertyName, out current))
                {
                    return string.Empty;
                }
            }
            else
            {
                if (segment.ArrayIndex is not int arrayIndex ||
                    current.ValueKind != JsonValueKind.Array ||
                    arrayIndex < 0 ||
                    arrayIndex >= current.GetArrayLength())
                {
                    return string.Empty;
                }

                current = current[arrayIndex];
            }
        }

        return current.ValueKind switch
        {
            JsonValueKind.String => current.GetString() ?? string.Empty,
            JsonValueKind.Number => current.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            JsonValueKind.Object or JsonValueKind.Array => JsonSerializer.Serialize(current),
            _ => string.Empty
        };
    }

    private static string GetRawContent(MessageBody body)
    {
        if (!string.IsNullOrWhiteSpace(body.RawContent))
        {
            return body.RawContent;
        }

        return body.RawBytes is { Length: > 0 }
            ? Encoding.UTF8.GetString(body.RawBytes)
            : string.Empty;
    }

    private static IEnumerable<PathSegment> ParsePath(string path)
    {
        var index = 0;
        while (index < path.Length)
        {
            if (path[index] == '.')
            {
                index++;
                if (index >= path.Length)
                {
                    throw new ArgumentException("The path cannot end with a dot.", nameof(path));
                }
            }

            var propertyStart = index;
            while (index < path.Length && path[index] is not ('.' or '['))
            {
                index++;
            }

            if (propertyStart == index)
            {
                throw new ArgumentException("The path contains an empty property.", nameof(path));
            }

            yield return new PathSegment(path[propertyStart..index], null);

            while (index < path.Length && path[index] == '[')
            {
                var closeBracket = path.IndexOf(']', index + 1);
                if (closeBracket < 0 || !int.TryParse(path[(index + 1)..closeBracket], out var arrayIndex))
                {
                    throw new ArgumentException("Array indexes must be numeric and enclosed in brackets.", nameof(path));
                }

                yield return new PathSegment(null, arrayIndex);
                index = closeBracket + 1;
            }

            if (index < path.Length && path[index] != '.')
            {
                throw new ArgumentException("The path contains an invalid segment.", nameof(path));
            }
        }
    }

    private readonly record struct PathSegment(string? PropertyName, int? ArrayIndex);
}
