using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MsMqApp.Components.Shared;
using MsMqApp.Models.Domain;
using MsMqApp.Models.Enums;
using MsMqApp.Models.Results;
using MsMqApp.Services.Implementations;
using MsMqApp.Services.Interfaces;

namespace MsMqApp.Tests;

public class MessageExportTests
{
    public static IEnumerable<object[]> Bodies()
    {
        foreach (var content in new[] { "{\"value\":\"café\"}", "<value>café</value>", "hello\r\nworld", " \t\r\n", "" })
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            yield return new object[] { new MessageBody(content), bytes };
            yield return new object[] { new MessageBody(bytes), bytes };
        }

        // Invalid UTF-8 and NULs must never be decoded or converted to a hex/base64 display.
        byte[] binary = [0, 255, 254, 128, 1, 0];
        yield return new object[] { new MessageBody(binary) { RawContent = "display only" }, binary };

        var xml = "<?xml version=\"1.0\" encoding=\"utf-16\"?><value>café</value>";
        var utf16 = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(xml)).ToArray();
        yield return new object[] { new MessageBody(utf16), utf16 };
        yield return new object[] { new MessageBody(xml) { Encoding = "utf-16" }, Encoding.Unicode.GetBytes(xml) };

        var largeBody = new byte[4 * 1024 * 1024];
        largeBody[^1] = 255;
        yield return new object[] { new MessageBody(largeBody), largeBody };
    }

    [Theory]
    [MemberData(nameof(Bodies))]
    public async Task BodyOnlyExportPreservesCompleteContent(MessageBody body, byte[] expected)
    {
        var bytes = await ExportAsync(body, ExportFormat.Binary);
        Assert.Equal(expected, bytes);
    }

    [Fact]
    public async Task DefaultJsonExportStillContainsMetadataAndBody()
    {
        var bytes = await ExportAsync(new MessageBody("{\"value\":42}"), ExportFormat.Json);
        using var reader = new StreamReader(new MemoryStream(bytes));
        using var json = JsonDocument.Parse(await reader.ReadToEndAsync());
        Assert.Equal("message-id", json.RootElement.GetProperty("id").GetString());
        Assert.Equal("test-label", json.RootElement.GetProperty("label").GetString());
        Assert.Equal("{\"value\":42}", json.RootElement.GetProperty("body").GetProperty("rawContent").GetString());
    }

    [Theory]
    [InlineData(false, "label-messageid.json")]
    [InlineData(true, "label-messageid.data")]
    public async Task DialogShowsSelectedFileExtensionAndImportNotice(bool dataOnly, string fileName)
    {
        using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = await renderer.RenderComponentAsync<ExportMessagesDialog>(ParameterView.FromDictionary(
                new Dictionary<string, object?>
                {
                    [nameof(ExportMessagesDialog.IsOpen)] = true,
                    [nameof(ExportMessagesDialog.MessageCount)] = 2,
                    [nameof(ExportMessagesDialog.DataOnly)] = dataOnly
                }));
            return component.ToHtmlString();
        });
        Assert.Contains("type=\"checkbox\"", html);
        Assert.Contains("Data Only", html);
        Assert.Contains(fileName, html);
        Assert.Contains("Data-only exports cannot be imported.", html);
    }

    private static async Task<byte[]> ExportAsync(MessageBody body, ExportFormat format)
    {
        var msmq = DispatchProxy.Create<IMsmqService, ExportQueue>();
        ((ExportQueue)(object)msmq).Message = new QueueMessage
        {
            Id = "message-id", Label = "test-label", Body = body
        };
        var service = new MessageOperationsService(msmq, NullLogger<MessageOperationsService>.Instance);
        var path = Path.Combine(Path.GetTempPath(), $"msmq-export-test-{Guid.NewGuid():N}.data");
        try
        {
            var result = await service.ExportMessageAsync("test-queue", "message-id", path, format);
            Assert.True(result.Success, result.ErrorMessage);
            return await File.ReadAllBytesAsync(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    public class ExportQueue : DispatchProxy
    {
        public QueueMessage Message { get; set; } = new();

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            switch (method?.Name)
            {
                case nameof(IMsmqService.QueueExistsAsync):
                    return Task.FromResult(OperationResult<bool>.Successful(true));
                case nameof(IMsmqService.GetMessageByIdAsync):
                    Assert.Equal(true, args![2]); // Export must peek, never remove a message.
                    return Task.FromResult(OperationResult<QueueMessage>.Successful(Message));
                default:
                    throw new NotSupportedException(method?.Name);
            }
        }
    }
}
