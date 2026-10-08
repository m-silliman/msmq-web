using System.Text.Json;
using System.Xml;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MsMqApp.Components.Shared;
using MsMqApp.Models.Domain;

namespace MsMqApp.Tests;

public class MessageBodyViewerTests
{
    private sealed class Viewer : MessageBodyViewerBase
    {
        public string Format(MessageBody body)
        {
            MessageBody = body;
            OnParametersSet();
            return FormattedContent;
        }
    }

    [Fact]
    public async Task FourMiBTextRendersThroughTheLastCharacterWithoutTruncationWarning()
    {
        var content = new string('x', 4 * 1024 * 1024 - 12) + "BODY-END";
        using var services = new ServiceCollection().AddLogging().AddSingleton<IJSRuntime, UnusedJsRuntime>().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = await renderer.RenderComponentAsync<MessageBodyViewer>(ParameterView.FromDictionary(
                new Dictionary<string, object?> { [nameof(MessageBodyViewer.MessageBody)] = new MessageBody(content) }));
            return component.ToHtmlString();
        });
        Assert.Contains(content, html);
        Assert.DoesNotContain("maximum display size", html);
        Assert.DoesNotContain("Showing first", html);
    }

    [Theory]
    [InlineData("json")]
    [InlineData("xml")]
    public void LargeStructuredBodiesRemainCompleteAndValid(string format)
    {
        var value = new string('a', 1024 * 1024) + "🙂END";
        var content = format == "json" ? JsonSerializer.Serialize(new { value }) : $"<value>{value}</value>";
        var result = new Viewer().Format(new MessageBody(content));
        if (format == "json")
        {
            using var parsed = JsonDocument.Parse(result);
            Assert.Equal(value, parsed.RootElement.GetProperty("value").GetString());
        }
        else
        {
            var parsed = new XmlDocument();
            parsed.LoadXml(result);
            Assert.Equal(value, parsed.DocumentElement!.InnerText);
        }
    }

    [Fact]
    public void BinaryOnlyBodyIncludesLastByteBeyondFormerDisplayLimit()
    {
        var bytes = new byte[600 * 1024 + 1];
        bytes[^1] = 0x7E;
        var result = new Viewer().Format(new MessageBody(bytes));
        Assert.Contains("00096000  7E", result);
    }

    [Fact]
    public void WhitespaceBodyIsPreserved() => Assert.Equal(" \r\n\t ", new Viewer().Format(new MessageBody(" \r\n\t ")));

    private sealed class UnusedJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new NotSupportedException();
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken token, object?[]? args) => throw new NotSupportedException();
    }
}
