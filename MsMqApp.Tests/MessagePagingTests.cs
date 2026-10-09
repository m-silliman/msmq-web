using System.Diagnostics;
using Experimental.System.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using MsMqApp.Models.Configuration;
using MsMqApp.Models.Domain;
using MsMqApp.Services.Helpers;
using MsMqApp.Services.Implementations;
using Xunit.Abstractions;

namespace MsMqApp.Tests;

public class MessagePagingTests(ITestOutputHelper output)
{
    [Fact]
    public void MissingContinuationDoesNotRestartOrScanTheQueue()
    {
        var calls = new List<MessageLookupAction>();
        var page = MessagePageReader.Read((action, _) => { calls.Add(action); return null; },
            true, 100, 123, CancellationToken.None);
        Assert.True(page.ContinuationExpired);
        Assert.False(page.HasMore);
        Assert.Empty(page.Messages);
        Assert.Equal(new[] { MessageLookupAction.Current }, calls);
    }

    [Fact]
    public void CancellationStopsReadingBetweenMessages()
    {
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        Assert.Throws<OperationCanceledException>(() => MessagePageReader.Read((_, _) =>
        {
            calls++;
            cancellation.Cancel();
            return new QueueMessage { LookupId = 1 };
        }, true, 100, null, cancellation.Token));
        Assert.Equal(1, calls);
    }

    [MsmqIntegrationFact]
    public async Task TailAndFrontPagesAreBoundedReadOnlyAndContinueWithoutDuplicates()
    {
        foreach (var transactional in new[] { false, true })
        {
            var path = @".\private$\codex-paging-" + Guid.NewGuid().ToString("N");
            using var queue = MessageQueue.Create(path, transactional);
            try
            {
                queue.UseJournalQueue = true;
                var settings = new ApplicationSettings { MessageListPageSize = 100 };
                var service = new MsmqService(new NativeMsmqService(NullLogger<NativeMsmqService>.Instance,
                    new MsmqQueueMetricsReader(), settings), new MsmqOperationExecutor(), settings);
                async Task<MessagePage> Page(bool tail, long? anchor = null, string? address = null)
                {
                    var result = await service.GetMessagePageAsync(address ?? path, tail, continuationLookupId: anchor);
                    Assert.True(result.Success, result.ErrorMessage);
                    return result.Data!;
                }
                var empty = await Page(true);
                Assert.Empty(empty.Messages);
                Assert.False(empty.HasMore);
                Assert.False(empty.ContinuationExpired);
                void Send(int number)
                {
                    using var message = new Message { Label = number.ToString(), BodyStream = new MemoryStream(new byte[1024]) };
                    queue.Send(message, transactional ? MessageQueueTransactionType.Single : MessageQueueTransactionType.None);
                }
                for (var i = 0; i < 205; i++) Send(i);

                foreach (var tail in new[] { true, false })
                {
                    var timer = Stopwatch.StartNew();
                    var first = await Page(tail);
                    output.WriteLine($"Transactional={transactional}, tail={tail}: first 100 metadata rows {timer.Elapsed.TotalMilliseconds:F2} ms");
                    Assert.Equal(100, first.Messages.Count);
                    Assert.True(first.HasMore);
                    var second = await Page(tail, first.ContinuationLookupId);
                    var third = await Page(tail, second.ContinuationLookupId);
                    Assert.Equal(100, second.Messages.Count);
                    Assert.Equal(5, third.Messages.Count);
                    Assert.False(third.HasMore);
                    Assert.False(third.ContinuationExpired);
                    var rows = first.Messages.Concat(second.Messages).Concat(third.Messages).ToList();
                    var expected = Enumerable.Range(0, 205);
                    Assert.Equal((tail ? expected.Reverse() : expected).Select(i => i.ToString()), rows.Select(m => m.Label));
                    Assert.All(rows, row =>
                    {
                        Assert.False(row.IsBodyLoaded);
                        Assert.Null(row.Body.RawBytes);
                        Assert.Equal(1024, row.BodySizeBytes);
                    });
                    Assert.Equal(205, rows.Select(m => m.LookupId).Distinct().Count());
                }
                Send(205);
                Assert.Equal("205", (await Page(true)).Messages[0].Label);
                Assert.Equal(206, (await service.GetMessageCountAsync(path)).Data);

                var beforeDeletion = await Page(true);
                using (queue.ReceiveByLookupId(MessageLookupAction.Current, beforeDeletion.ContinuationLookupId!.Value,
                    transactional ? MessageQueueTransactionType.Single : MessageQueueTransactionType.None)) { }
                Assert.True((await Page(true, beforeDeletion.ContinuationLookupId)).ContinuationExpired);

                for (var i = 0; i < 205; i++)
                {
                    using var received = queue.Receive(TimeSpan.Zero,
                        transactional ? MessageQueueTransactionType.Single : MessageQueueTransactionType.None);
                }
                var journal = "FormatName:" + queue.FormatName + ";journal";
                var journalFirst = await Page(true, address: journal);
                Assert.Equal(100, journalFirst.Messages.Count);
                Assert.Equal("205", journalFirst.Messages[0].Label);
                var journalSecond = await Page(true, journalFirst.ContinuationLookupId, journal);
                Assert.Equal(100, journalSecond.Messages.Count);
                Assert.Empty(journalFirst.Messages.Select(m => m.LookupId).Intersect(journalSecond.Messages.Select(m => m.LookupId)));
                Assert.Empty((await Page(false)).Messages);
            }
            finally { queue.Close(); MessageQueue.Delete(path); }
        }
    }
}
