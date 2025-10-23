using System.Text.Json;
using MsMqApp.Models.Domain;
using MsMqApp.Models.Enums;
using MsMqApp.Models.Results;

namespace MsMqApp.Services.FormatHandlers;

/// <summary>
/// Handler for JSON message format
/// </summary>
public class JsonFormatHandler : IFormatHandler
{
    private static readonly JsonSerializerOptions _prettyOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions _compactOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    public MessageBodyFormat Format => MessageBodyFormat.Json;

    public bool CanHandle(MessageBody messageBody)
    {
        if (messageBody == null || string.IsNullOrWhiteSpace(messageBody.RawContent))
            return false;

        var trimmed = messageBody.RawContent.Trim();
        return (trimmed.StartsWith("{") && trimmed.EndsWith("}")) ||
               (trimmed.StartsWith("[") && trimmed.EndsWith("]"));
    }

    public OperationResult<bool> Validate(MessageBody messageBody)
    {
        if (messageBody == null)
            return OperationResult<bool>.Failure("Message body is null");

        try
        {
            using var doc = JsonDocument.Parse(messageBody.RawContent);
            return OperationResult<bool>.Successful(true);
        }
        catch (JsonException ex)
        {
            var result = OperationResult<bool>.Successful(false);
            result.ErrorMessage = $"Invalid JSON: {ex.Message}";
            return result;
        }
    }

    public OperationResult<string> FormatForDisplay(MessageBody messageBody, int maxLength = 0)
    {
        if (messageBody == null)
            return OperationResult<string>.Failure("Message body is null");

        try
        {
            using var doc = JsonDocument.Parse(messageBody.RawContent);
            var formatted = JsonSerializer.Serialize(doc, _prettyOptions);

            if (maxLength > 0 && formatted.Length > maxLength)
            {
                formatted = formatted.Substring(0, maxLength) + "\n... (truncated)";
            }

            return OperationResult<string>.Successful(formatted);
        }
        catch (Exception ex)
        {
            // If formatting fails, return raw content
            var content = messageBody.RawContent;
            if (maxLength > 0 && content.Length > maxLength)
            {
                content = content.Substring(0, maxLength) + "... (truncated)";
            }

            var result = OperationResult<string>.Successful(content);
            result.ErrorMessage = $"JSON formatting failed: {ex.Message}";
            return result;
        }
    }

    public OperationResult<T?> Deserialize<T>(MessageBody messageBody) where T : class
    {
        if (messageBody == null)
            return OperationResult<T?>.Failure("Message body is null");

        try
        {
            var obj = JsonSerializer.Deserialize<T>(messageBody.RawContent, _compactOptions);
            return OperationResult<T?>.Successful(obj);
        }
        catch (JsonException ex)
        {
            return OperationResult<T?>.Failure($"JSON deserialization failed: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            return OperationResult<T?>.Failure($"Failed to deserialize JSON: {ex.Message}", ex);
        }
    }

    public OperationResult<MessageBody> Serialize<T>(T obj, bool indent = true) where T : class
    {
        if (obj == null)
            return OperationResult<MessageBody>.Failure("Object is null");

        try
        {
            var options = indent ? _prettyOptions : _compactOptions;
            var json = JsonSerializer.Serialize(obj, options);

            var messageBody = new MessageBody(json)
            {
                Format = MessageBodyFormat.Json,
                Encoding = "UTF-8"
            };

            return OperationResult<MessageBody>.Successful(messageBody);
        }
        catch (JsonException ex)
        {
            return OperationResult<MessageBody>.Failure($"JSON serialization failed: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            return OperationResult<MessageBody>.Failure($"Failed to serialize to JSON: {ex.Message}", ex);
        }
    }
}
