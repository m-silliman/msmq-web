using MsMqApp.Models.Domain;
using MsMqApp.Models.Enums;
using MsMqApp.Models.Results;

namespace MsMqApp.Services.FormatHandlers;

/// <summary>
/// Handler for plain text message format
/// </summary>
public class TextFormatHandler : IFormatHandler
{
    public MessageBodyFormat Format => MessageBodyFormat.Text;

    public bool CanHandle(MessageBody messageBody)
    {
        if (messageBody == null || string.IsNullOrEmpty(messageBody.RawContent))
            return false;

        // Text handler can handle anything that's printable
        return IsPrintableText(messageBody.RawContent);
    }

    public OperationResult<bool> Validate(MessageBody messageBody)
    {
        if (messageBody == null)
            return OperationResult<bool>.Failure("Message body is null");

        // Plain text is always valid if it's printable
        var isValid = IsPrintableText(messageBody.RawContent);
        return OperationResult<bool>.Successful(isValid);
    }

    public OperationResult<string> FormatForDisplay(MessageBody messageBody, int maxLength = 0)
    {
        if (messageBody == null)
            return OperationResult<string>.Failure("Message body is null");

        var content = messageBody.RawContent ?? string.Empty;

        if (maxLength > 0 && content.Length > maxLength)
        {
            content = content.Substring(0, maxLength) + "... (truncated)";
        }

        return OperationResult<string>.Successful(content);
    }

    public OperationResult<T?> Deserialize<T>(MessageBody messageBody) where T : class
    {
        if (messageBody == null)
            return OperationResult<T?>.Failure("Message body is null");

        // For text, we can only deserialize to string
        if (typeof(T) == typeof(string))
        {
            var result = messageBody.RawContent as T;
            return OperationResult<T?>.Successful(result);
        }

        return OperationResult<T?>.Failure(
            $"Cannot deserialize plain text to {typeof(T).Name}. Only string is supported.");
    }

    public OperationResult<MessageBody> Serialize<T>(T obj, bool indent = true) where T : class
    {
        if (obj == null)
            return OperationResult<MessageBody>.Failure("Object is null");

        try
        {
            var text = obj.ToString() ?? string.Empty;

            var messageBody = new MessageBody(text)
            {
                Format = MessageBodyFormat.Text,
                Encoding = "UTF-8"
            };

            return OperationResult<MessageBody>.Successful(messageBody);
        }
        catch (Exception ex)
        {
            return OperationResult<MessageBody>.Failure(
                $"Failed to serialize to text: {ex.Message}", ex);
        }
    }

    private static bool IsPrintableText(string content)
    {
        if (string.IsNullOrEmpty(content))
            return true;

        // Allow common whitespace and printable characters
        foreach (var c in content)
        {
            // Allow printable characters and common whitespace
            if (char.IsControl(c) && c != '\r' && c != '\n' && c != '\t')
                return false;
        }

        return true;
    }
}
