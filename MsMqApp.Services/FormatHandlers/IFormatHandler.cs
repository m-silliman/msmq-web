using MsMqApp.Models.Domain;
using MsMqApp.Models.Enums;
using MsMqApp.Models.Results;

namespace MsMqApp.Services.FormatHandlers;

/// <summary>
/// Interface for message format handlers
/// </summary>
internal interface IFormatHandler
{
    /// <summary>
    /// Gets the format this handler supports
    /// </summary>
    MessageBodyFormat Format { get; }

    /// <summary>
    /// Detects if the message body matches this format
    /// </summary>
    bool CanHandle(MessageBody messageBody);

    /// <summary>
    /// Validates the message body against this format
    /// </summary>
    OperationResult<bool> Validate(MessageBody messageBody);

    /// <summary>
    /// Formats the message body for display with syntax highlighting hints
    /// </summary>
    OperationResult<string> FormatForDisplay(MessageBody messageBody, int maxLength = 0);

    /// <summary>
    /// Deserializes the message body to a specific type
    /// </summary>
    OperationResult<T?> Deserialize<T>(MessageBody messageBody) where T : class;

    /// <summary>
    /// Serializes an object to a message body
    /// </summary>
    OperationResult<MessageBody> Serialize<T>(T obj, bool indent = true) where T : class;
}
