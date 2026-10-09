using MsMqApp.Models.Configuration;
using MsMqApp.Models.Domain;
using MsMqApp.Models.Results;
using MsMqApp.Services.Interfaces;

namespace MsMqApp.Services.Implementations;

/// <summary>Async boundary around the synchronous MSMQ and WMI implementation.</summary>
public sealed class MsmqService(NativeMsmqService native, MsmqOperationExecutor executor,
    ApplicationSettings settings) : IMsmqService
{
    private async Task<T> Run<T>(string address, Func<CancellationToken, Task<T>> operation,
        CancellationToken token, int? timeoutSeconds = null) where T : OperationResult, new()
    {
        try
        {
            return await executor.ExecuteAsync(address, operation,
                TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds ?? settings.RemoteConnectionTimeoutSeconds)), token);
        }
        catch (TimeoutException ex)
        {
            return new T { Success = false, ErrorMessage = ex.Message, Exception = ex };
        }
    }

    public Task<OperationResult<IEnumerable<QueueInfo>>> GetQueuesAsync(string computerName,
        bool includeSystemQueues = false, CancellationToken cancellationToken = default) =>
        Run(computerName, ct => native.GetQueuesAsync(computerName, includeSystemQueues, ct), cancellationToken);
    public Task<OperationResult<QueueInfo>> GetQueueInfoAsync(string queuePath, CancellationToken cancellationToken = default) =>
        Run(queuePath, ct => native.GetQueueInfoAsync(queuePath, ct), cancellationToken);
    public Task<OperationResult<QueueMetrics>> GetQueueMetricsAsync(string computerName, string formatName,
        CancellationToken cancellationToken = default) =>
        Run(computerName, ct => native.GetQueueMetricsAsync(computerName, formatName, ct), cancellationToken);
    public Task<OperationResult<int>> GetMessageCountAsync(string queuePath, CancellationToken cancellationToken = default) =>
        Run(queuePath, ct => native.GetMessageCountAsync(queuePath, ct), cancellationToken);
    public Task<OperationResult<int>> GetJournalMessageCountAsync(string queuePath, CancellationToken cancellationToken = default) =>
        Run(queuePath, ct => native.GetJournalMessageCountAsync(queuePath, ct), cancellationToken);
    public Task<OperationResult<MessagePage>> GetMessagePageAsync(string queuePath, bool fromTail = false,
        int maxMessages = 0, long? continuationLookupId = null, CancellationToken cancellationToken = default) =>
        Run(queuePath, ct => native.GetMessagePageAsync(queuePath, fromTail, maxMessages, continuationLookupId, ct), cancellationToken);
    public Task<OperationResult<IEnumerable<QueueMessage>>> GetMessagesAsync(string queuePath, bool peekOnly = true,
        int maxMessages = 0, CancellationToken cancellationToken = default) =>
        Run(queuePath, ct => native.GetMessagesAsync(queuePath, peekOnly, maxMessages, ct), cancellationToken);
    public Task<OperationResult<IEnumerable<QueueMessage>>> GetJournalMessagesAsync(string journalQueuePath,
        int maxMessages = 0, CancellationToken cancellationToken = default) =>
        Run(journalQueuePath, ct => native.GetJournalMessagesAsync(journalQueuePath, maxMessages, ct), cancellationToken);
    public Task<OperationResult<QueueMessage?>> GetMessageByIdAsync(string queuePath, string messageId,
        bool peekOnly = true, CancellationToken cancellationToken = default) =>
        Run(queuePath, ct => native.GetMessageByIdAsync(queuePath, messageId, peekOnly, ct), cancellationToken);
    public Task<OperationResult<QueueMessage?>> GetMessageByLookupIdAsync(string queuePath, long lookupId,
        bool peekOnly = true, CancellationToken cancellationToken = default) =>
        Run(queuePath, ct => native.GetMessageByLookupIdAsync(queuePath, lookupId, peekOnly, ct), cancellationToken);
    public Task<OperationResult> DeleteMessageAsync(string queuePath, string messageId, CancellationToken cancellationToken = default) =>
        Run(queuePath, ct => native.DeleteMessageAsync(queuePath, messageId, ct), cancellationToken);
    public Task<OperationResult> MoveMessageAsync(string sourceQueuePath, string destinationQueuePath, string messageId,
        CancellationToken cancellationToken = default) =>
        Run(sourceQueuePath, ct => native.MoveMessageAsync(sourceQueuePath, destinationQueuePath, messageId, ct), cancellationToken);
    public Task<OperationResult> PurgeQueueAsync(string queuePath, CancellationToken cancellationToken = default) =>
        Run(queuePath, ct => native.PurgeQueueAsync(queuePath, ct), cancellationToken);
    public Task<OperationResult> SendMessageAsync(string queuePath, QueueMessage message, CancellationToken cancellationToken = default) =>
        Run(queuePath, ct => native.SendMessageAsync(queuePath, message, ct), cancellationToken);
    public Task<OperationResult> TestConnectionAsync(string computerName, int timeoutSeconds = 30,
        CancellationToken cancellationToken = default) =>
        Run(computerName, ct => native.TestConnectionAsync(computerName, timeoutSeconds, ct), cancellationToken, timeoutSeconds);
    public Task<OperationResult<bool>> QueueExistsAsync(string queuePath, CancellationToken cancellationToken = default) =>
        Run(queuePath, ct => native.QueueExistsAsync(queuePath, ct), cancellationToken);
}
