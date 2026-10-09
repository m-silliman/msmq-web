namespace MsMqApp.Models.Domain;

/// <summary>A metadata-only batch in queue traversal order.</summary>
public sealed class MessagePage
{
    public List<QueueMessage> Messages { get; init; } = new();
    public long? ContinuationLookupId { get; init; }
    public bool HasMore { get; init; }
    public bool ContinuationExpired { get; init; }
}
