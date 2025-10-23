using System.Text;
using MsMqApp.Models.Domain;
using MsMqApp.Models.Enums;
using MsMqApp.Models.Results;
using MsMqApp.Services.FormatHandlers;
using MsMqApp.Services.Interfaces;

namespace MsMqApp.Services.Implementations;

/// <summary>
/// Implementation of message serialization service using format handlers
/// </summary>
public class MessageSerializer : IMessageSerializer
{
    private readonly Dictionary<MessageBodyFormat, IFormatHandler> _handlers;

    public MessageSerializer(
        XmlFormatHandler xmlHandler,
        JsonFormatHandler jsonHandler,
        TextFormatHandler textHandler,
        BinaryFormatHandler binaryHandler)
    {
        _handlers = new Dictionary<MessageBodyFormat, IFormatHandler>
        {
            [MessageBodyFormat.Xml] = xmlHandler ?? throw new ArgumentNullException(nameof(xmlHandler)),
            [MessageBodyFormat.Json] = jsonHandler ?? throw new ArgumentNullException(nameof(jsonHandler)),
            [MessageBodyFormat.Text] = textHandler ?? throw new ArgumentNullException(nameof(textHandler)),
            [MessageBodyFormat.Binary] = binaryHandler ?? throw new ArgumentNullException(nameof(binaryHandler))
        };
    }

    public OperationResult<MessageBodyFormat> DetectFormat(MessageBody messageBody)
    {
        ArgumentNullException.ThrowIfNull(messageBody);

        // Try handlers in priority order: XML, JSON, Text, Binary
        var handlers = new[]
        {
            _handlers[MessageBodyFormat.Xml],
            _handlers[MessageBodyFormat.Json],
            _handlers[MessageBodyFormat.Text],
            _handlers[MessageBodyFormat.Binary]
        };

        foreach (var handler in handlers)
        {
            if (handler.CanHandle(messageBody))
            {
                return OperationResult<MessageBodyFormat>.Successful(handler.Format);
            }
        }

        return OperationResult<MessageBodyFormat>.Successful(MessageBodyFormat.Unknown);
    }

    public OperationResult<bool> ValidateFormat(MessageBody messageBody, MessageBodyFormat format)
    {
        ArgumentNullException.ThrowIfNull(messageBody);

        if (!_handlers.TryGetValue(format, out var handler))
        {
            return OperationResult<bool>.Failure($"No handler found for format: {format}");
        }

        return handler.Validate(messageBody);
    }

    public OperationResult<T?> Deserialize<T>(MessageBody messageBody, MessageBodyFormat? format = null) where T : class
    {
        ArgumentNullException.ThrowIfNull(messageBody);

        MessageBodyFormat targetFormat;
        if (format.HasValue)
        {
            targetFormat = format.Value;
        }
        else
        {
            var detectResult = DetectFormat(messageBody);
            targetFormat = detectResult.Success ? detectResult.Data : MessageBodyFormat.Unknown;
        }

        if (!_handlers.TryGetValue(targetFormat, out var handler))
        {
            return OperationResult<T?>.Failure($"No handler found for format: {targetFormat}");
        }

        return handler.Deserialize<T>(messageBody);
    }

    public OperationResult<T?> DeserializeBinary<T>(MessageBody messageBody) where T : class
    {
        ArgumentNullException.ThrowIfNull(messageBody);
        return _handlers[MessageBodyFormat.Binary].Deserialize<T>(messageBody);
    }

    public OperationResult<T?> DeserializeXml<T>(MessageBody messageBody) where T : class
    {
        ArgumentNullException.ThrowIfNull(messageBody);
        return _handlers[MessageBodyFormat.Xml].Deserialize<T>(messageBody);
    }

    public OperationResult<T?> DeserializeJson<T>(MessageBody messageBody) where T : class
    {
        ArgumentNullException.ThrowIfNull(messageBody);
        return _handlers[MessageBodyFormat.Json].Deserialize<T>(messageBody);
    }

    public OperationResult<MessageBody> Serialize<T>(T obj, MessageBodyFormat format) where T : class
    {
        ArgumentNullException.ThrowIfNull(obj);

        if (!_handlers.TryGetValue(format, out var handler))
        {
            return OperationResult<MessageBody>.Failure($"No handler found for format: {format}");
        }

        return handler.Serialize(obj);
    }

    public OperationResult<MessageBody> SerializeToXml<T>(T obj, bool indent = true) where T : class
    {
        ArgumentNullException.ThrowIfNull(obj);
        return _handlers[MessageBodyFormat.Xml].Serialize(obj, indent);
    }

    public OperationResult<MessageBody> SerializeToJson<T>(T obj, bool indent = true) where T : class
    {
        ArgumentNullException.ThrowIfNull(obj);
        return _handlers[MessageBodyFormat.Json].Serialize(obj, indent);
    }

    public OperationResult<MessageBody> SerializeToBinary<T>(T obj) where T : class
    {
        ArgumentNullException.ThrowIfNull(obj);
        return _handlers[MessageBodyFormat.Binary].Serialize(obj);
    }

    public OperationResult<string> FormatForDisplay(MessageBody messageBody, int maxLength = 0)
    {
        ArgumentNullException.ThrowIfNull(messageBody);

        var formatResult = DetectFormat(messageBody);
        if (!formatResult.Success)
        {
            return OperationResult<string>.Failure(formatResult.ErrorMessage ?? "Failed to detect format");
        }

        var format = formatResult.Data;

        if (!_handlers.TryGetValue(format, out var handler))
        {
            // Fallback to text
            handler = _handlers[MessageBodyFormat.Text];
        }

        return handler.FormatForDisplay(messageBody, maxLength);
    }

    public OperationResult<string> ToHexDump(MessageBody messageBody, int bytesPerLine = 16, int maxBytes = 0)
    {
        ArgumentNullException.ThrowIfNull(messageBody);
        return _handlers[MessageBodyFormat.Binary].FormatForDisplay(messageBody, maxBytes);
    }

    public OperationResult<string> ExtractText(MessageBody messageBody)
    {
        ArgumentNullException.ThrowIfNull(messageBody);
        return OperationResult<string>.Successful(messageBody.RawContent);
    }

    public OperationResult<string> DetectEncoding(MessageBody messageBody)
    {
        ArgumentNullException.ThrowIfNull(messageBody);
        return OperationResult<string>.Successful(messageBody.Encoding);
    }

    public OperationResult<MessageBody> ConvertEncoding(MessageBody messageBody, string sourceEncoding, string targetEncoding)
    {
        ArgumentNullException.ThrowIfNull(messageBody);
        ArgumentNullException.ThrowIfNull(sourceEncoding);
        ArgumentNullException.ThrowIfNull(targetEncoding);

        try
        {
            var sourceEnc = Encoding.GetEncoding(sourceEncoding);
            var targetEnc = Encoding.GetEncoding(targetEncoding);

            byte[] bytes;
            if (messageBody.RawBytes != null)
            {
                bytes = messageBody.RawBytes;
            }
            else
            {
                bytes = sourceEnc.GetBytes(messageBody.RawContent);
            }

            // Convert encoding
            var sourceText = sourceEnc.GetString(bytes);
            var targetBytes = targetEnc.GetBytes(sourceText);

            var newMessageBody = new MessageBody(targetBytes)
            {
                Format = messageBody.Format,
                Encoding = targetEncoding
            };

            return OperationResult<MessageBody>.Successful(newMessageBody);
        }
        catch (ArgumentException ex)
        {
            return OperationResult<MessageBody>.Failure($"Invalid encoding: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            return OperationResult<MessageBody>.Failure($"Encoding conversion failed: {ex.Message}", ex);
        }
    }
}
