using MsMqApp.Models.Configuration;
using MsMqApp.Models.Domain;
using MsMqApp.Services.Helpers;

namespace MsMqApp.Tests;

public sealed class CustomColumnJsonResolverTests
{
    [Fact]
    public void ResolveValues_reads_nested_properties_and_array_elements()
    {
        var message = CreateMessage("""
            {
              "Header": { "MessageName": "OrderCreated" },
              "MessageBody": { "Elements": [ { "ItemIdent": "ABC-123" } ] }
            }
            """);
        var name = new CustomColumnDefinition { Header = "Name", JsonPath = "Header.MessageName" };
        var item = new CustomColumnDefinition { Header = "Item", JsonPath = "MessageBody.Elements[0].ItemIdent" };

        var values = CustomColumnJsonResolver.ResolveValues(message, [name, item]);

        Assert.Equal("OrderCreated", values[name.Id]);
        Assert.Equal("ABC-123", values[item.Id]);
    }

    [Fact]
    public void Resolve_returns_blank_for_missing_or_out_of_range_paths()
    {
        using var document = System.Text.Json.JsonDocument.Parse("""{ "items": [] }""");

        Assert.Equal(string.Empty, CustomColumnJsonResolver.Resolve(document.RootElement, "Missing.Value"));
        Assert.Equal(string.Empty, CustomColumnJsonResolver.Resolve(document.RootElement, "items[0].id"));
    }

    [Fact]
    public void ResolveValues_returns_blank_for_invalid_json_and_invalid_paths()
    {
        var message = CreateMessage("not json");
        var invalidPath = new CustomColumnDefinition { Header = "Invalid", JsonPath = "items[x].id" };

        var values = CustomColumnJsonResolver.ResolveValues(message, [invalidPath]);

        Assert.Equal(string.Empty, values[invalidPath.Id]);
    }

    [Fact]
    public void ResolveValues_formats_scalar_values_and_keeps_objects_as_json()
    {
        var message = CreateMessage("""{ "count": 3, "enabled": true, "data": { "x": 1 } }""");
        var count = new CustomColumnDefinition { Header = "Count", JsonPath = "count" };
        var enabled = new CustomColumnDefinition { Header = "Enabled", JsonPath = "enabled" };
        var data = new CustomColumnDefinition { Header = "Data", JsonPath = "data" };

        var values = CustomColumnJsonResolver.ResolveValues(message, [count, enabled, data]);

        Assert.Equal("3", values[count.Id]);
        Assert.Equal("true", values[enabled.Id]);
        Assert.Equal("""{"x":1}""", values[data.Id]);
    }

    private static QueueMessage CreateMessage(string body) => new()
    {
        Id = Guid.NewGuid().ToString(),
        Body = new MessageBody(body),
        IsBodyLoaded = true
    };
}

