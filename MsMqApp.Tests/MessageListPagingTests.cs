using Microsoft.AspNetCore.Components;
using MsMqApp.Components.Shared;
using MsMqApp.Models.Domain;

namespace MsMqApp.Tests;

public class MessageListPagingTests
{
    private sealed class ListHarness : MessageListBase
    {
        public void Apply() => OnParametersSet();
        public void SetRows(List<QueueMessage> rows) => Messages = rows;
        public void SetTail(bool tail) => ReadFromTail = tail;
        public void SetQuery(string query) => SearchQuery = query;
        public void SetBodySearch(bool enabled) => SearchBody = enabled;
        public void SetHasMore(bool hasMore) => HasMore = hasMore;
        public void SetLoadMore(Func<Task> callback) => OnLoadMore = EventCallback.Factory.Create(new object(), callback);
        public List<QueueMessage> Rows => FilteredMessages;
        public string CountText => GetMessageCountText();
        public bool Paused => IsPaused;
        public Task LoadNext() => LoadMoreAsync();
    }

    [Fact]
    public void ChangingQueueEndChangesDefaultDisplayDirection()
    {
        var list = new ListHarness();
        list.SetRows([new() { Label = "older", ArrivedTime = new DateTime(2026, 1, 1) },
            new() { Label = "newer", ArrivedTime = new DateTime(2026, 1, 2) }]);
        list.Apply();
        Assert.Equal("newer", list.Rows[0].Label);
        list.SetTail(false);
        list.Apply();
        Assert.Equal("older", list.Rows[0].Label);
    }

    [Fact]
    public void BodySearchIncludesLoadedBodyAndCountStaysCompact()
    {
        var list = new ListHarness();
        list.SetRows([
            new() { Label = "first", Body = new MessageBody("needle in body"), IsBodyLoaded = true },
            new() { Label = "second", Body = new MessageBody("other text"), IsBodyLoaded = true }
        ]);
        list.Apply();
        list.SetBodySearch(true);
        list.SetQuery("needle");
        list.Apply();

        Assert.Single(list.Rows);
        Assert.Equal("2 loaded", list.CountText);
    }

    [Fact]
    public async Task RepeatedLoadMoreRequestsDoNotOverlapAndPauseAutoRefresh()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var list = new ListHarness();
        list.SetHasMore(true);
        list.SetLoadMore(async () => { calls++; await release.Task; });
        var first = list.LoadNext();
        try
        {
            await list.LoadNext();
            Assert.Equal(1, calls);
            Assert.True(list.Paused);
        }
        finally { release.SetResult(); await first; }
        list.SetHasMore(false);
        await list.LoadNext();
        Assert.Equal(1, calls);
    }
}
