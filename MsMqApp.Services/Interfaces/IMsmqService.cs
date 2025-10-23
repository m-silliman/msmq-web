using MsMqApp.Models.Domain;
using MsMqApp.Models.Results;

namespace MsMqApp.Services.Interfaces;

/// <summary>
/// Core MSMQ service interface for queue discovery, message retrieval, and queue operations.
/// Provides methods for interacting with local and remote MSMQ queues.
/// </summary>
/// <remarks>
/// This service uses System.Messaging internally to communicate with MSMQ.
/// All operations support cancellation and provide detailed error information through OperationResult.
/// </remarks>
public interface IMsmqService
{
    #region Queue Discovery

    /// <summary>
    /// Discovers and retrieves all MSMQ queues from the specified computer.
    /// </summary>
    /// <param name="computerName">
    /// The computer name or IP address to query. Use "." or "localhost" for local computer.
    /// For remote computers, ensure the caller has appropriate permissions.
    /// </param>
    /// <param name="includeSystemQueues">
    /// If true, includes system queues (dead letter, journal, etc.) in the results.
    /// Default is false.
    /// </param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result containing a collection of QueueInfo objects representing discovered queues.
    /// Returns empty collection if no queues found. Returns failure result if discovery fails.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when computerName is null or empty</exception>
    /// <remarks>
    /// This method queries both private and public queues. Private queues are stored locally,
    /// while public queues are registered in Active Directory.
    /// </remarks>
    Task<OperationResult<IEnumerable<QueueInfo>>> GetQueuesAsync(
        string computerName,
        bool includeSystemQueues = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about a specific queue.
    /// </summary>
    /// <param name="queuePath">
    /// The full queue path (e.g., ".\private$\myqueue" or "MACHINE\private$\myqueue")
    /// </param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result containing the QueueInfo object with current queue state,
    /// or failure result if queue doesn't exist or is inaccessible.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when queuePath is null or empty</exception>
    Task<OperationResult<QueueInfo>> GetQueueInfoAsync(
        string queuePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the message count for a specific queue.
    /// </summary>
    /// <param name="queuePath">The full queue path</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result containing the current message count,
    /// or failure result if the operation fails.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when queuePath is null or empty</exception>
    Task<OperationResult<int>> GetMessageCountAsync(
        string queuePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the message count for a queue's journal.
    /// </summary>
    /// <param name="queuePath">The full queue path (journal path will be derived)</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result containing the current journal message count,
    /// or zero if journaling is not enabled, or failure result if the operation fails.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when queuePath is null or empty</exception>
    /// <remarks>
    /// This method accesses the journal queue by appending ";journal" to the queue path.
    /// If journaling is not enabled on the queue, returns zero count.
    /// </remarks>
    Task<OperationResult<int>> GetJournalMessageCountAsync(
        string queuePath,
        CancellationToken cancellationToken = default);

    #endregion

    #region Message Retrieval

    /// <summary>
    /// Retrieves all messages from the specified queue without removing them.
    /// </summary>
    /// <param name="queuePath">The full queue path</param>
    /// <param name="peekOnly">
    /// If true, messages are only peeked (not removed). If false, messages are received and removed.
    /// Default is true.
    /// </param>
    /// <param name="maxMessages">
    /// Maximum number of messages to retrieve. Use 0 or negative value for unlimited.
    /// Default is 0 (all messages).
    /// </param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result containing a collection of QueueMessage objects.
    /// Returns empty collection if queue is empty.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when queuePath is null or empty</exception>
    /// <remarks>
    /// For large queues, consider using maxMessages parameter to limit results.
    /// Messages are returned in queue order (FIFO for non-priority queues).
    /// </remarks>
    Task<OperationResult<IEnumerable<QueueMessage>>> GetMessagesAsync(
        string queuePath,
        bool peekOnly = true,
        int maxMessages = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all messages from the specified journal queue without removing them.
    /// </summary>
    /// <param name="journalQueuePath">The full journal queue path (should already include ;journal suffix)</param>
    /// <param name="maxMessages">
    /// Maximum number of messages to retrieve. Use 0 or negative value for unlimited.
    /// Default is 0 (all messages).
    /// </param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result containing a collection of QueueMessage objects from the journal.
    /// Returns empty collection if journal is empty or journaling is not enabled.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when journalQueuePath is null or empty</exception>
    /// <remarks>
    /// The journalQueuePath parameter should already have ";journal" appended to it.
    /// For example: "FormatName:DIRECT=OS:.\private$\myqueue;journal"
    /// Journal messages are read-only and cannot be removed from the journal via this method.
    /// If journaling is not enabled on the queue, returns failure result.
    /// </remarks>
    Task<OperationResult<IEnumerable<QueueMessage>>> GetJournalMessagesAsync(
        string journalQueuePath,
        int maxMessages = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single message by its unique identifier.
    /// </summary>
    /// <param name="queuePath">The full queue path</param>
    /// <param name="messageId">The unique message ID to retrieve</param>
    /// <param name="peekOnly">
    /// If true, message is only peeked (not removed). If false, message is received and removed.
    /// Default is true.
    /// </param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result containing the QueueMessage object if found,
    /// or null if message doesn't exist, or failure result on error.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when queuePath or messageId is null or empty</exception>
    Task<OperationResult<QueueMessage?>> GetMessageByIdAsync(
        string queuePath,
        string messageId,
        bool peekOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single message by its lookup identifier (numeric ID assigned by queue).
    /// </summary>
    /// <param name="queuePath">The full queue path</param>
    /// <param name="lookupId">The lookup ID assigned by the queue</param>
    /// <param name="peekOnly">
    /// If true, message is only peeked (not removed). If false, message is received and removed.
    /// Default is true.
    /// </param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result containing the QueueMessage object if found,
    /// or null if message doesn't exist, or failure result on error.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when queuePath is null or empty</exception>
    Task<OperationResult<QueueMessage?>> GetMessageByLookupIdAsync(
        string queuePath,
        long lookupId,
        bool peekOnly = true,
        CancellationToken cancellationToken = default);

    #endregion

    #region Message Operations

    /// <summary>
    /// Deletes a specific message from the queue.
    /// </summary>
    /// <param name="queuePath">The full queue path</param>
    /// <param name="messageId">The unique message ID to delete</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result indicating success or failure.
    /// Success means message was deleted. Failure may indicate message not found or access denied.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when queuePath or messageId is null or empty</exception>
    /// <remarks>
    /// This operation is irreversible. The message will be permanently removed from the queue.
    /// </remarks>
    Task<OperationResult> DeleteMessageAsync(
        string queuePath,
        string messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a message from one queue to another.
    /// </summary>
    /// <param name="sourceQueuePath">The source queue path</param>
    /// <param name="destinationQueuePath">The destination queue path</param>
    /// <param name="messageId">The unique message ID to move</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result indicating success or failure.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when sourceQueuePath, destinationQueuePath, or messageId is null or empty
    /// </exception>
    /// <remarks>
    /// The message is copied to the destination queue and then removed from the source queue.
    /// This is not an atomic operation - if the copy succeeds but delete fails, the message
    /// will exist in both queues.
    /// </remarks>
    Task<OperationResult> MoveMessageAsync(
        string sourceQueuePath,
        string destinationQueuePath,
        string messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Purges all messages from the specified queue.
    /// </summary>
    /// <param name="queuePath">The full queue path</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result indicating success or failure.
    /// Includes metadata with the number of messages purged.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when queuePath is null or empty</exception>
    /// <remarks>
    /// This operation is irreversible and will permanently delete ALL messages in the queue.
    /// Use with caution in production environments.
    /// </remarks>
    Task<OperationResult> PurgeQueueAsync(
        string queuePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to the specified queue.
    /// </summary>
    /// <param name="queuePath">The full queue path</param>
    /// <param name="message">The message to send</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result indicating success or failure.
    /// On success, includes the message ID in metadata.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when queuePath or message is null
    /// </exception>
    Task<OperationResult> SendMessageAsync(
        string queuePath,
        QueueMessage message,
        CancellationToken cancellationToken = default);

    #endregion

    #region Connection Testing

    /// <summary>
    /// Tests connectivity to a computer's MSMQ service.
    /// </summary>
    /// <param name="computerName">
    /// The computer name or IP address to test. Use "." or "localhost" for local computer.
    /// </param>
    /// <param name="timeoutSeconds">
    /// The timeout in seconds for the connection test. Default is 30 seconds.
    /// </param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result indicating whether the connection was successful.
    /// On failure, ErrorMessage contains details about why the connection failed.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when computerName is null or empty</exception>
    /// <remarks>
    /// This method attempts to enumerate queues on the target computer to verify connectivity.
    /// A successful result means MSMQ is accessible on the target computer with current credentials.
    /// </remarks>
    Task<OperationResult> TestConnectionAsync(
        string computerName,
        int timeoutSeconds = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a queue exists and is accessible.
    /// </summary>
    /// <param name="queuePath">The full queue path to check</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result containing true if queue exists and is accessible, false otherwise.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when queuePath is null or empty</exception>
    Task<OperationResult<bool>> QueueExistsAsync(
        string queuePath,
        CancellationToken cancellationToken = default);

    #endregion
}
