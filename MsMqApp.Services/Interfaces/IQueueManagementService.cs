using MsMqApp.Models.Results;

namespace MsMqApp.Services.Interfaces;

/// <summary>
/// Service interface for managing MSMQ queue configuration and properties.
/// Provides methods for updating queue settings such as label, authentication, storage limits, etc.
/// </summary>
public interface IQueueManagementService
{
    /// <summary>
    /// Updates the properties of an existing MSMQ queue.
    /// </summary>
    /// <param name="queuePath">The full queue path (e.g., ".\private$\myqueue")</param>
    /// <param name="label">The queue label/description</param>
    /// <param name="authenticate">Whether authentication is required</param>
    /// <param name="maximumQueueSize">Maximum queue size in KB (0 = unlimited)</param>
    /// <param name="privacyLevel">Privacy level (0=None, 1=Optional, 2=Body)</param>
    /// <param name="useJournalQueue">Whether journaling is enabled</param>
    /// <param name="maximumJournalSize">Maximum journal size in KB (0 = unlimited)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result indicating success or failure with error details</returns>
    Task<OperationResult<bool>> UpdateQueuePropertiesAsync(
        string queuePath,
        string label,
        bool authenticate,
        long maximumQueueSize,
        int privacyLevel,
        bool useJournalQueue,
        long maximumJournalSize,
        CancellationToken cancellationToken = default);
}
