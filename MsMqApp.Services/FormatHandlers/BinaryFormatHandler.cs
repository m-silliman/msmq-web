using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using MsMqApp.Models.Domain;
using MsMqApp.Models.Enums;
using MsMqApp.Models.Results;

namespace MsMqApp.Services.FormatHandlers;

/// <summary>
/// Handler for binary message format
/// </summary>
public class BinaryFormatHandler : IFormatHandler
{
    public MessageBodyFormat Format => MessageBodyFormat.Binary;

    public bool CanHandle(MessageBody messageBody)
    {
        // Binary handler is the fallback for non-text content
        return messageBody != null && messageBody.RawBytes != null && messageBody.RawBytes.Length > 0;
    }

    public OperationResult<bool> Validate(MessageBody messageBody)
    {
        if (messageBody == null)
            return OperationResult<bool>.Failure("Message body is null");

        // Binary data is always valid if we have bytes
        var isValid = messageBody.RawBytes != null && messageBody.RawBytes.Length > 0;
        return OperationResult<bool>.Successful(isValid);
    }

    public OperationResult<string> FormatForDisplay(MessageBody messageBody, int maxLength = 0)
    {
        if (messageBody == null)
            return OperationResult<string>.Failure("Message body is null");

        try
        {
            var bytes = messageBody.RawBytes ?? Encoding.UTF8.GetBytes(messageBody.RawContent);
            var hexDump = GenerateHexDump(bytes, maxLength > 0 ? maxLength / 80 * 16 : 0);

            return OperationResult<string>.Successful(hexDump);
        }
        catch (Exception ex)
        {
            return OperationResult<string>.Failure($"Failed to format binary data: {ex.Message}", ex);
        }
    }

    public OperationResult<T?> Deserialize<T>(MessageBody messageBody) where T : class
    {
        if (messageBody == null)
            return OperationResult<T?>.Failure("Message body is null");

        return OperationResult<T?>.Failure(
            "Binary deserialization is not supported due to security concerns. " +
            "Use JSON or XML formats instead.");
    }

    public OperationResult<MessageBody> Serialize<T>(T obj, bool indent = true) where T : class
    {
        if (obj == null)
            return OperationResult<MessageBody>.Failure("Object is null");

        return OperationResult<MessageBody>.Failure(
            "Binary serialization is not supported due to security concerns. " +
            "Use JSON or XML formats instead.");
    }

    private static string GenerateHexDump(byte[] bytes, int maxBytes = 0)
    {
        const int bytesPerLine = 16;
        var sb = new StringBuilder();

        var limit = maxBytes > 0 ? Math.Min(maxBytes, bytes.Length) : bytes.Length;

        for (int i = 0; i < limit; i += bytesPerLine)
        {
            // Offset
            sb.Append($"{i:X8}  ");

            // Hex representation
            for (int j = 0; j < bytesPerLine; j++)
            {
                if (i + j < limit)
                {
                    sb.Append($"{bytes[i + j]:X2} ");
                }
                else
                {
                    sb.Append("   ");
                }

                // Extra space after 8 bytes
                if (j == 7)
                    sb.Append(" ");
            }

            sb.Append(" ");

            // ASCII representation
            for (int j = 0; j < bytesPerLine && i + j < limit; j++)
            {
                var b = bytes[i + j];
                sb.Append(b >= 32 && b <= 126 ? (char)b : '.');
            }

            sb.AppendLine();
        }

        if (maxBytes > 0 && bytes.Length > maxBytes)
        {
            sb.AppendLine($"... ({bytes.Length - maxBytes} more bytes)");
        }

        return sb.ToString();
    }
}
