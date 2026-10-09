using Experimental.System.Messaging;
using MsMqApp.Models.Domain;

namespace MsMqApp.Services.Helpers;

/// <summary>Bounded lookup navigation, shared by normal and journal queues.</summary>
public static class MessagePageReader
{
    public static MessagePage Read(Func<MessageLookupAction, long, QueueMessage?> peek,
        bool fromTail, int limit, long? continuationLookupId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        cancellationToken.ThrowIfCancellationRequested();
        if (continuationLookupId is long anchor && peek(MessageLookupAction.Current, anchor) == null)
            return new MessagePage { ContinuationExpired = true };

        var messages = new List<QueueMessage>();
        var position = continuationLookupId ?? 0;
        var step = fromTail ? MessageLookupAction.Previous : MessageLookupAction.Next;
        var action = continuationLookupId.HasValue ? step
            : fromTail ? MessageLookupAction.Last : MessageLookupAction.First;
        var expired = false;
        while (messages.Count < limit)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var message = peek(action, position);
            if (message == null)
            {
                // A consumer may remove the anchor between calls. Never silently restart
                // at the queue boundary, which would duplicate or skip pages.
                expired = position != 0 && peek(MessageLookupAction.Current, position) == null;
                break;
            }
            messages.Add(message);
            position = message.LookupId;
            action = step;
        }
        cancellationToken.ThrowIfCancellationRequested();
        return new MessagePage
        {
            Messages = messages,
            ContinuationLookupId = messages.Count > 0 ? position : continuationLookupId,
            HasMore = !expired && messages.Count == limit,
            ContinuationExpired = expired
        };
    }
}
