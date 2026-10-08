using System.Diagnostics;
using System.Reflection;
using Experimental.System.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using MsMqApp.Models.Configuration;
using MsMqApp.Models.Domain;
using MsMqApp.Models.Results;
using MsMqApp.Services.Helpers;
using MsMqApp.Services.Implementations;
using MsMqApp.Services.Interfaces;
using Xunit.Abstractions;

namespace MsMqApp.Tests;

public class MsmqPerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task TimedOutNativeWorkRetainsCapacityAndOtherServersCanProceed()
    {
        var executor = new MsmqOperationExecutor(1, 2);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = executor.ExecuteAsync("server-a", async _ =>
        {
            entered.SetResult();
            await release.Task;
            return 1;
        }, TimeSpan.FromMilliseconds(300));
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Assert.ThrowsAsync<TimeoutException>(() => first);
            var queuedRan = false;
            await Assert.ThrowsAsync<TimeoutException>(() => executor.ExecuteAsync(@"DIRECT=OS:server-a\private$\q", _ =>
            {
                queuedRan = true;
                return Task.FromResult(2);
            }, TimeSpan.FromMilliseconds(100)));
            Assert.False(queuedRan);
            Assert.Equal(3, await executor.ExecuteAsync("server-b", _ => Task.FromResult(3), TimeSpan.FromSeconds(2)));
        }
        finally { release.TrySetResult(); }
        Assert.Equal(4, await executor.ExecuteAsync("server-a", _ => Task.FromResult(4), TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task CancellationWhileQueuedNeverStartsWork()
    {
        var executor = new MsmqOperationExecutor(1, 1);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = executor.ExecuteAsync("server-a", async _ => { entered.SetResult(); await release.Task; return 1; }, TimeSpan.FromSeconds(10));
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            using var cancellation = new CancellationTokenSource();
            var ran = false;
            var queued = executor.ExecuteAsync("server-b", _ => { ran = true; return Task.FromResult(2); }, TimeSpan.FromSeconds(10), cancellation.Token);
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queued);
            Assert.False(ran);
        }
        finally { release.TrySetResult(); await first; }
        Assert.Equal(3, await executor.ExecuteAsync("server-b", _ => Task.FromResult(3), TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public void UnknownCountsSurviveCloningAndAreNotEmpty()
    {
        var queue = new QueueInfo();
        Assert.False(queue.IsEmpty);
        Assert.Equal("Unknown", queue.Clone().MessageCountText);
        queue.ApplyMetrics(new QueueMetrics(0, null));
        Assert.True(queue.IsEmpty);
        Assert.Equal("Unknown", queue.Clone().JournalMessageCountText);
    }

    [Theory]
    [InlineData(@"FormatName:DIRECT=OS:host\private$\q", "host")]
    [InlineData(@"DIRECT=TCP:10.0.0.1\private$\q", "10.0.0.1")]
    [InlineData(@"host\private$\q", "host")]
    public void ServerKeysGroupQueuesOnSameHost(string address, string expected) => Assert.Equal(expected, MsmqAddress.Server(address));

    [Theory]
    [InlineData(@"FormatName:DIRECT=OS:host\private$\q", @"DIRECT=OS:host\private$\q")]
    [InlineData(@"formatname:DIRECT=OS:host\private$\q", @"DIRECT=OS:host\private$\q")]
    [InlineData(@"DIRECT=OS:host\private$\q", @"DIRECT=OS:host\private$\q")]
    [InlineData(@"host\private$\q", @"DIRECT=OS:host\private$\q")]
    [InlineData(@"FormatName:PRIVATE=opaque-id", @"PRIVATE=opaque-id")]
    public void DiscoveryPreservesFormatNamesWithoutDoublePrefixes(string address, string expected) =>
        Assert.Equal(expected, MsmqAddress.ToFormatName(address));

    [ExistingQueueFact]
    public async Task ExistingQueueCanBeCountedAndPeekedThroughDiscovery()
    {
        var settings = new ApplicationSettings();
        var native = new NativeMsmqService(NullLogger<NativeMsmqService>.Instance, new MsmqQueueMetricsReader(), settings);
        var service = new MsmqService(native, new MsmqOperationExecutor(), settings);
        var discovery = await service.GetQueuesAsync(".");
        Assert.True(discovery.Success, discovery.ErrorMessage);
        var name = Environment.GetEnvironmentVariable("MSMQ_READONLY_QUEUE")!;
        var queue = Assert.Single(discovery.Data!, q => q.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        var metrics = await service.GetQueueMetricsAsync(queue.ComputerName, queue.FormatName);
        Assert.True(metrics.Success, metrics.ErrorMessage);
        Assert.NotNull(metrics.Data?.MessageCount);
        var rows = await service.GetMessagesAsync(queue.FormatName);
        Assert.True(rows.Success, rows.ErrorMessage);
        output.WriteLine($"{name}: management count={metrics.Data!.MessageCount}, metadata rows={rows.Data!.Count()}");
        var expected = Environment.GetEnvironmentVariable("MSMQ_READONLY_EXPECTED_COUNT");
        if (int.TryParse(expected, out var count))
        {
            Assert.Equal(count, metrics.Data.MessageCount);
            Assert.Equal(Math.Min(count, settings.MessageListPageSize), rows.Data.Count());
        }
    }

    [Fact]
    public async Task ConnectionReturnsQueueNamesBeforeSupplementalDiscoveryOrCounts()
    {
        var supplemental = new TaskCompletionSource<OperationResult<IEnumerable<QueueInfo>>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = DispatchProxy.Create<IMsmqService, ServiceProxy>();
        ((ServiceProxy)(object)service).Handler = (method, args) =>
        {
            if (method == nameof(IMsmqService.GetQueuesAsync))
            {
                if ((bool)args![1]!) { requested.TrySetResult(); return supplemental.Task; }
                return Task.FromResult(OperationResult<IEnumerable<QueueInfo>>.Successful([
                    new QueueInfo { Id = "q", Name = "q", Path = @"host\private$\q", FormatName = @"DIRECT=OS:host\private$\q" }]));
            }
            if (method == nameof(IMsmqService.GetQueueMetricsAsync))
                return Task.FromResult(OperationResult<QueueMetrics>.Successful(new QueueMetrics(12, 3)));
            throw new InvalidOperationException("Unexpected native call: " + method);
        };
        using var manager = new QueueConnectionManager(service, NullLogger<QueueConnectionManager>.Instance);
        var result = await manager.ConnectAsync("host").WaitAsync(TimeSpan.FromSeconds(3));
        var queue = Assert.Single(result.Data!.Queues);
        Assert.False(queue.MessageCountKnown);
        await requested.Task.WaitAsync(TimeSpan.FromSeconds(3));
        var populated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        manager.ConnectionRefreshed += (_, _) => { if (queue.MessageCountKnown) populated.TrySetResult(); };
        supplemental.SetResult(OperationResult<IEnumerable<QueueInfo>>.Successful([]));
        await populated.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(12, queue.MessageCount);
        Assert.Equal(3, queue.JournalMessageCount);
    }

    public class ServiceProxy : DispatchProxy
    {
        public Func<string, object?[]?, object?> Handler { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(targetMethod!.Name, args);
    }

    [Fact]
    public async Task DisconnectDiscardsLateNativeCounts()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var result = new TaskCompletionSource<OperationResult<QueueMetrics>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var queue = new QueueInfo { Id = "q", FormatName = @"DIRECT=OS:host\private$\q" };
        var service = DispatchProxy.Create<IMsmqService, ServiceProxy>();
        ((ServiceProxy)(object)service).Handler = (method, _) => method switch
        {
            nameof(IMsmqService.GetQueuesAsync) => Task.FromResult(OperationResult<IEnumerable<QueueInfo>>.Successful([queue])),
            nameof(IMsmqService.GetQueueMetricsAsync) => GetCounts(),
            _ => throw new InvalidOperationException(method)
        };
        Task<OperationResult<QueueMetrics>> GetCounts() { entered.TrySetResult(); return result.Task; }
        using var manager = new QueueConnectionManager(service, NullLogger<QueueConnectionManager>.Instance);
        var connection = await manager.ConnectAsync("host");
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await manager.DisconnectAsync(connection.Data!.Id);
        var notifications = 0;
        manager.ConnectionRefreshed += (_, _) => notifications++;
        result.SetResult(OperationResult<QueueMetrics>.Successful(new QueueMetrics(123, 456)));
        // The continuation is asynchronous; wait for the canceled background work to settle.
        await Task.Delay(100);
        Assert.False(queue.MessageCountKnown);
        Assert.Equal(0, notifications);
    }

    [MsmqIntegrationFact]
    public async Task EmptyQueuesAndJournalsReportZeroWithoutPriorMessageTraffic()
    {
        var path = @".\private$\codex-empty-" + Guid.NewGuid().ToString("N");
        using (var created = MessageQueue.Create(path)) { }
        try
        {
            var reader = new MsmqQueueMetricsReader();
            var counts = reader.Read(".", MsmqAddress.ToFormatName(path));
            Assert.Equal(0, counts.MessageCount);
            Assert.Equal(0, counts.JournalMessageCount);
            var settings = new ApplicationSettings();
            var native = new NativeMsmqService(NullLogger<NativeMsmqService>.Instance, reader, settings);
            var service = new MsmqService(native, new MsmqOperationExecutor(), settings);
            var baseCount = await service.GetMessageCountAsync(path);
            Assert.True(baseCount.Success, baseCount.ErrorMessage);
            Assert.Equal(0, baseCount.Data);
            var journalCount = await service.GetJournalMessageCountAsync(path);
            Assert.True(journalCount.Success, journalCount.ErrorMessage);
            Assert.Equal(0, journalCount.Data);
        }
        finally { MessageQueue.Delete(path); }
    }

    [Fact]
    public void ConfirmedZeroCountsAreVisibleOnQueueAndJournalNodes()
    {
        var queue = new QueueInfo { Id = "empty", Name = "empty", QueueType = MsMqApp.Models.Enums.QueueType.Private };
        queue.ApplyMetrics(new QueueMetrics(0, 0));
        var connection = new QueueConnection { Queues = [queue] };
        var tree = MsMqApp.Models.UI.QueueTreeBuilder.BuildTreeFromConnection(connection);
        var node = MsMqApp.Models.UI.QueueTreeBuilder.FindQueueNode(tree, "empty")!;
        Assert.Equal(0, node.BadgeCount);
        Assert.Equal(0, node.SecondaryBadgeCount);
        Assert.All(node.Children, child => Assert.Equal(0, child.BadgeCount));
    }

    [MsmqIntegrationFact]
    public void MissingQueueDoesNotReportZero()
    {
        var counts = new MsmqQueueMetricsReader().Read(".", @"DIRECT=OS:.\private$\codex-missing-" + Guid.NewGuid().ToString("N"));
        Assert.Null(counts.MessageCount);
        Assert.Null(counts.JournalMessageCount);
    }

    [ExistingQueueFact]
    public async Task ExistingLocalEmptyQueuesReportZero()
    {
        var settings = new ApplicationSettings();
        var native = new NativeMsmqService(NullLogger<NativeMsmqService>.Instance, new MsmqQueueMetricsReader(), settings);
        var service = new MsmqService(native, new MsmqOperationExecutor(), settings);
        var discovery = await service.GetQueuesAsync(".");
        Assert.True(discovery.Success, discovery.ErrorMessage);
        var empty = 0;
        foreach (var queue in discovery.Data!)
        {
            var counts = await service.GetQueueMetricsAsync(queue.ComputerName, queue.FormatName);
            Assert.True(counts.Success, counts.ErrorMessage);
            var rows = await service.GetMessagesAsync(queue.FormatName, maxMessages: 1);
            Assert.True(rows.Success, rows.ErrorMessage);
            if (!rows.Data!.Any())
            {
                Assert.True(counts.Data?.MessageCount == 0, $"{queue.Name} is empty but count was {counts.Data?.MessageCount}");
                empty++;
            }
            var journal = await service.GetJournalMessagesAsync(queue.JournalPath, 1);
            Assert.True(journal.Success, journal.ErrorMessage);
            if (!journal.Data!.Any())
                Assert.True(counts.Data?.JournalMessageCount == 0, $"{queue.Name} journal is empty but count was {counts.Data?.JournalMessageCount}");
        }
        output.WriteLine($"Read-only check: {discovery.Data!.Count()} local private queues checked; {empty} empty queues correctly report zero.");
    }

    [MsmqIntegrationFact]
    public async Task SelectedMessageLoadsItsEntireNearFourMiBBody()
    {
        var path = @".\private$\codex-body-" + Guid.NewGuid().ToString("N");
        using var queue = MessageQueue.Create(path);
        try
        {
            var content = new string('x', 4 * 1024 * 1024 - 2048) + "|BODY-END|";
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            using var sent = new Message { Label = "Full selected body regression", BodyStream = new MemoryStream(bytes) };
            queue.Send(sent);
            var settings = new ApplicationSettings();
            var native = new NativeMsmqService(NullLogger<NativeMsmqService>.Instance, new MsmqQueueMetricsReader(), settings);
            var service = new MsmqService(native, new MsmqOperationExecutor(), settings);
            var rows = await service.GetMessagesAsync(path);
            Assert.True(rows.Success, rows.ErrorMessage);
            var row = Assert.Single(rows.Data!);
            Assert.False(row.IsBodyLoaded);
            Assert.Null(row.Body.RawBytes);
            var byId = await service.GetMessageByIdAsync(path, row.Id);
            Assert.True(byId.Success, byId.ErrorMessage);
            Assert.Equal(bytes, byId.Data!.Body.RawBytes);
            Assert.Equal(content, byId.Data.Body.RawContent);
            var byLookup = await service.GetMessageByLookupIdAsync(path, row.LookupId);
            Assert.True(byLookup.Success, byLookup.ErrorMessage);
            Assert.Equal(bytes, byLookup.Data!.Body.RawBytes);
            Assert.Equal(content, byLookup.Data.Body.RawContent);
            Assert.Equal(1, (await service.GetMessageCountAsync(path)).Data);
            output.WriteLine($"Complete selected body retrieved by ID and lookup ID: {bytes.Length:N0} bytes; message remains in queue.");
        }
        finally { queue.Close(); MessageQueue.Delete(path); }
    }

    [MsmqIntegrationFact]
    public async Task NativeCountsBoundedListsAndLazyBodiesWorkOnIsolatedQueue()
    {
        var path = @".\private$\codex-perf-" + Guid.NewGuid().ToString("N");
        using var queue = MessageQueue.Create(path);
        try
        {
            queue.UseJournalQueue = true;
            for (var i = 0; i < 2000; i++)
            {
                using var message = new Message { Label = $"perf-{i}", BodyStream = new MemoryStream(new byte[1024]) };
                queue.Send(message);
            }
            for (var i = 0; i < 5; i++) { using var removed = queue.Receive(TimeSpan.Zero); }
            var settings = new ApplicationSettings { MessageListPageSize = 10 };
            var native = new NativeMsmqService(NullLogger<NativeMsmqService>.Instance, new MsmqQueueMetricsReader(), settings);
            var service = new MsmqService(native, new MsmqOperationExecutor(), settings);
            var timer = Stopwatch.StartNew();
            var counts = await service.GetQueueMetricsAsync(".", queue.FormatName);
            output.WriteLine($"Management counts for 1,995 queued + 5 journal messages: {timer.Elapsed.TotalMilliseconds:F2} ms");
            Assert.True(counts.Success, counts.ErrorMessage);
            Assert.Equal(1995, counts.Data!.MessageCount);
            Assert.Equal(5, counts.Data.JournalMessageCount);
            Assert.Equal(5, (await service.GetJournalMessageCountAsync(path)).Data);
            timer.Restart();
            var rows = await service.GetMessagesAsync(path);
            output.WriteLine($"10 metadata rows: {timer.Elapsed.TotalMilliseconds:F2} ms");
            Assert.True(rows.Success, rows.ErrorMessage);
            Assert.Equal(10, rows.Data!.Count());
            Assert.All(rows.Data!, row => { Assert.False(row.IsBodyLoaded); Assert.Null(row.Body.RawBytes); });
            Assert.Equal("perf-5", rows.Data!.First().Label);
            Assert.Equal(1995, (await service.GetMessageCountAsync(path)).Data);
            var detail = await service.GetMessageByLookupIdAsync(path, rows.Data!.First().LookupId);
            Assert.True(detail.Success, detail.ErrorMessage);
            Assert.True(detail.Data!.IsBodyLoaded);
            Assert.Equal(1024, detail.Data.Body.RawBytes!.Length);
            var byId = await service.GetMessageByIdAsync(path, rows.Data!.First().Id);
            Assert.True(byId.Success, byId.ErrorMessage);
            Assert.Equal(detail.Data.Body.RawBytes, byId.Data!.Body.RawBytes);
            var journals = await service.GetJournalMessagesAsync("FormatName:" + queue.FormatName + ";journal", 2);
            Assert.True(journals.Success, journals.ErrorMessage);
            Assert.Equal(2, journals.Data!.Count());
            var missingId = Guid.NewGuid() + "\\1";
            Assert.Null((await service.GetMessageByIdAsync(path, missingId)).Data);
            Assert.False((await service.DeleteMessageAsync(path, missingId)).Success);
            var discovery = await service.GetQueuesAsync(".");
            Assert.True(discovery.Success, discovery.ErrorMessage);
            Assert.All(discovery.Data!, q => Assert.False(q.MessageCountKnown));
            var discovered = Assert.Single(discovery.Data!, q => q.Name.Equals(path.Split('\\').Last(), StringComparison.OrdinalIgnoreCase));
            var discoveredCounts = await service.GetQueueMetricsAsync(discovered.ComputerName, discovered.FormatName);
            Assert.Equal(1995, discoveredCounts.Data?.MessageCount);
            Assert.Equal(5, discoveredCounts.Data?.JournalMessageCount);
            var discoveredRows = await service.GetMessagesAsync(discovered.FormatName);
            Assert.True(discoveredRows.Success, discoveredRows.ErrorMessage);
            Assert.Equal(10, discoveredRows.Data!.Count());
            var discoveredJournal = await service.GetJournalMessagesAsync(discovered.JournalPath, 2);
            Assert.True(discoveredJournal.Success, discoveredJournal.ErrorMessage);
            Assert.Equal(2, discoveredJournal.Data!.Count());

            queue.MessageReadPropertyFilter.SetAll();
            timer.Restart();
            var baseline = queue.GetAllMessages();
            foreach (var message in baseline) message.Dispose();
            output.WriteLine($"Previous full queue read (1,995 bodies): {timer.Elapsed.TotalMilliseconds:F2} ms");
        }
        finally { queue.Close(); MessageQueue.Delete(path); }
    }
}

public sealed class MsmqIntegrationFactAttribute : FactAttribute
{
    public MsmqIntegrationFactAttribute()
    {
        if (!OperatingSystem.IsWindows() || Environment.GetEnvironmentVariable("MSMQ_INTEGRATION_TESTS") != "1")
            Skip = "Set MSMQ_INTEGRATION_TESTS=1 on Windows with MSMQ to run isolated native queue tests.";
    }
}

public sealed class ExistingQueueFactAttribute : FactAttribute
{
    public ExistingQueueFactAttribute()
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MSMQ_READONLY_QUEUE")))
            Skip = "Set MSMQ_READONLY_QUEUE to validate discovery, counts, and metadata peeking on an existing local queue.";
    }
}
