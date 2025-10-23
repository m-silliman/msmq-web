using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using MsMqApp.Models.Domain;
using MsMqApp.Models.Enums;
using MsMqApp.Models.Results;

namespace MsMqApp.Services.FormatHandlers;

/// <summary>
/// Handler for XML message format
/// </summary>
public class XmlFormatHandler : IFormatHandler
{
    public MessageBodyFormat Format => MessageBodyFormat.Xml;

    public bool CanHandle(MessageBody messageBody)
    {
        if (messageBody == null || string.IsNullOrWhiteSpace(messageBody.RawContent))
            return false;

        var trimmed = messageBody.RawContent.Trim();
        return trimmed.StartsWith("<") && trimmed.Contains(">");
    }

    public OperationResult<bool> Validate(MessageBody messageBody)
    {
        if (messageBody == null)
            return OperationResult<bool>.Failure("Message body is null");

        try
        {
            var doc = XDocument.Parse(messageBody.RawContent);
            return OperationResult<bool>.Successful(true);
        }
        catch (XmlException ex)
        {
            var result = OperationResult<bool>.Successful(false);
            result.ErrorMessage = $"Invalid XML: {ex.Message}";
            return result;
        }
    }

    public OperationResult<string> FormatForDisplay(MessageBody messageBody, int maxLength = 0)
    {
        if (messageBody == null)
            return OperationResult<string>.Failure("Message body is null");

        try
        {
            var doc = XDocument.Parse(messageBody.RawContent);

            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace,
                OmitXmlDeclaration = false,
                Encoding = Encoding.UTF8
            };

            using var stringWriter = new StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, settings);

            doc.Save(xmlWriter);
            xmlWriter.Flush();

            var formatted = stringWriter.ToString();

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
            result.ErrorMessage = $"XML formatting failed: {ex.Message}";
            return result;
        }
    }

    public OperationResult<T?> Deserialize<T>(MessageBody messageBody) where T : class
    {
        if (messageBody == null)
            return OperationResult<T?>.Failure("Message body is null");

        try
        {
            var serializer = new XmlSerializer(typeof(T));
            using var reader = new StringReader(messageBody.RawContent);
            var obj = serializer.Deserialize(reader) as T;

            return OperationResult<T?>.Successful(obj);
        }
        catch (InvalidOperationException ex)
        {
            return OperationResult<T?>.Failure($"XML deserialization failed: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            return OperationResult<T?>.Failure($"Failed to deserialize XML: {ex.Message}", ex);
        }
    }

    public OperationResult<MessageBody> Serialize<T>(T obj, bool indent = true) where T : class
    {
        if (obj == null)
            return OperationResult<MessageBody>.Failure("Object is null");

        try
        {
            var serializer = new XmlSerializer(typeof(T));

            var settings = new XmlWriterSettings
            {
                Indent = indent,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace,
                OmitXmlDeclaration = false,
                Encoding = Encoding.UTF8
            };

            using var stringWriter = new StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, settings);

            serializer.Serialize(xmlWriter, obj);
            xmlWriter.Flush();

            var xml = stringWriter.ToString();
            var messageBody = new MessageBody(xml)
            {
                Format = MessageBodyFormat.Xml,
                Encoding = "UTF-8"
            };

            return OperationResult<MessageBody>.Successful(messageBody);
        }
        catch (InvalidOperationException ex)
        {
            return OperationResult<MessageBody>.Failure($"XML serialization failed: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            return OperationResult<MessageBody>.Failure($"Failed to serialize to XML: {ex.Message}", ex);
        }
    }
}
