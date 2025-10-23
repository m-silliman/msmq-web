using MsMqApp.Models.Domain;
using MsMqApp.Models.Enums;
using MsMqApp.Models.Results;

namespace MsMqApp.Services.Interfaces;

/// <summary>
/// Service interface for performing operations on MSMQ messages.
/// Provides high-level message operations including delete, move, resend, export, and purge.
/// </summary>
/// <remarks>
/// This service orchestrates operations using IMsmqService and provides additional functionality
/// such as export, validation, and comprehensive error handling with logging.
/// All operations return OperationResult to indicate success or failure with detailed error information.
/// </remarks>
public interface IMessageOperationsService
{
    /// <summary>
    /// Deletes a specific message from a queue.
    /// </summary>
    /// <param name="queuePath">The full path of the queue containing the message</param>
    /// <param name="messageId">The unique identifier of the message to delete</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result indicating success or failure.
    /// On success, includes metadata with deletion timestamp.
    /// On failure, includes detailed error message and exception if applicable.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when queuePath or messageId is null or empty
    /// </exception>
    /// <remarks>
    /// This operation is irreversible. The message will be permanently removed from the queue.
    /// Validates that the queue exists and is accessible before attempting deletion.
    /// Logs all deletion operations for audit trail.
    /// </remarks>
    Task<OperationResult> DeleteMessageAsync(
        string queuePath,
        string messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a message from one queue to another.
    /// </summary>
    /// <param name="sourceQueuePath">The full path of the source queue</param>
    /// <param name="destinationQueuePath">The full path of the destination queue</param>
    /// <param name="messageId">The unique identifier of the message to move</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result indicating success or failure.
    /// On success, includes metadata with move timestamp and destination queue path.
    /// On failure, includes detailed error message explaining what went wrong.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null or empty
    /// </exception>
    /// <remarks>
    /// The operation validates both queues exist and are accessible before proceeding.
    /// The message is copied to the destination queue, then removed from the source queue.
    /// Note: This is not an atomic operation. If copying succeeds but deletion fails,
    /// the message will exist in both queues. The operation result will indicate partial success.
    /// Logs all move operations including source and destination queues.
    /// </remarks>
    Task<OperationResult> MoveMessageAsync(
        string sourceQueuePath,
        string destinationQueuePath,
        string messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resends (copies) a message to a specified queue, keeping the original in place.
    /// </summary>
    /// <param name="sourceQueuePath">The full path of the queue containing the original message</param>
    /// <param name="destinationQueuePath">The full path of the queue to send the message to</param>
    /// <param name="messageId">The unique identifier of the message to resend</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result indicating success or failure.
    /// On success, includes metadata with the new message ID and resend timestamp.
    /// On failure, includes detailed error message.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null or empty
    /// </exception>
    /// <remarks>
    /// Unlike MoveMessage, this operation creates a copy of the message in the destination queue
    /// while leaving the original message intact in the source queue.
    /// The new message will have a new message ID and sent timestamp.
    /// All other properties (label, body, priority, etc.) are preserved.
    /// Useful for reprocessing failed messages or duplicating messages for testing.
    /// Logs all resend operations with source and destination information.
    /// </remarks>
    Task<OperationResult> ResendMessageAsync(
        string sourceQueuePath,
        string destinationQueuePath,
        string messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a message to a file in the specified format.
    /// </summary>
    /// <param name="queuePath">The full path of the queue containing the message</param>
    /// <param name="messageId">The unique identifier of the message to export</param>
    /// <param name="filePath">The full file path where the exported data should be saved</param>
    /// <param name="format">The export format (JSON, XML, CSV, Text, or Binary)</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result indicating success or failure.
    /// On success, includes metadata with file path, file size, and export timestamp.
    /// On failure, includes detailed error message.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when queuePath, messageId, or filePath is null or empty
    /// </exception>
    /// <remarks>
    /// Export formats:
    /// - JSON: Full message metadata and body in JSON format
    /// - XML: Full message metadata and body in XML format
    /// - CSV: Tabular format with headers (useful for bulk exports)
    /// - Text: Human-readable plain text format
    /// - Binary: Preserves exact message body as binary file
    ///
    /// The file will be created or overwritten if it already exists.
    /// Validates queue and message exist before attempting export.
    /// Ensures the destination directory exists; creates it if necessary.
    /// Logs all export operations including format and destination.
    /// </remarks>
    Task<OperationResult> ExportMessageAsync(
        string queuePath,
        string messageId,
        string filePath,
        ExportFormat format,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports multiple messages to a file in the specified format.
    /// </summary>
    /// <param name="queuePath">The full path of the queue containing the messages</param>
    /// <param name="messageIds">Collection of message IDs to export</param>
    /// <param name="filePath">The full file path where the exported data should be saved</param>
    /// <param name="format">The export format (JSON, XML, CSV, or Text)</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result indicating success or failure.
    /// On success, includes metadata with file path, number of messages exported, and file size.
    /// On partial success, includes list of failed message IDs in metadata.
    /// On failure, includes detailed error message.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when queuePath, messageIds, or filePath is null or empty
    /// </exception>
    /// <remarks>
    /// Binary format is not supported for bulk exports.
    /// For CSV format, all messages are included in a single table.
    /// For JSON/XML, messages are included in an array/collection.
    /// If some messages fail to export, the operation continues and reports partial success.
    /// Logs bulk export operations including message count and success rate.
    /// </remarks>
    Task<OperationResult> ExportMessagesAsync(
        string queuePath,
        IEnumerable<string> messageIds,
        string filePath,
        ExportFormat format,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Purges all messages from the specified queue.
    /// </summary>
    /// <param name="queuePath">The full path of the queue to purge</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result indicating success or failure.
    /// On success, includes metadata with the number of messages purged and purge timestamp.
    /// On failure, includes detailed error message.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when queuePath is null or empty
    /// </exception>
    /// <remarks>
    /// This operation is IRREVERSIBLE and will permanently delete ALL messages in the queue.
    /// Use with extreme caution, especially in production environments.
    /// Validates that the queue exists and is accessible before purging.
    /// Retrieves message count before purging to include in operation result.
    /// Logs all purge operations with queue path and message count for audit trail.
    /// Consider using a confirmation dialog before calling this method.
    /// </remarks>
    Task<OperationResult> PurgeQueueAsync(
        string queuePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple messages from a queue.
    /// </summary>
    /// <param name="queuePath">The full path of the queue containing the messages</param>
    /// <param name="messageIds">Collection of message IDs to delete</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// An operation result indicating success or failure.
    /// On success, includes metadata with number of messages deleted.
    /// On partial success, includes list of failed message IDs in metadata.
    /// On failure, includes detailed error message.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when queuePath or messageIds is null or empty
    /// </exception>
    /// <remarks>
    /// This operation is irreversible. Messages will be permanently removed from the queue.
    /// The operation continues even if some deletions fail.
    /// Results include count of successful and failed deletions.
    /// Logs bulk deletion operations with success rate.
    /// </remarks>
    Task<OperationResult> DeleteMessagesAsync(
        string queuePath,
        IEnumerable<string> messageIds,
        CancellationToken cancellationToken = default);
}
