using MsMqApp.Models.Enums;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace MsMqApp.Models.Domain;

/// <summary>
/// Represents a message body with automatic format detection and serialization
/// </summary>
public class MessageBody
{
    private string _rawContent = string.Empty;
    private byte[]? _rawBytes;
    private MessageBodyFormat? _detectedFormat;

    /// <summary>
    /// Gets or sets the raw content as a string
    /// </summary>
    public string RawContent
    {
        get => _rawContent;
        set
        {
            _rawContent = value;
            _detectedFormat = null; // Reset detection when content changes
        }
    }

    /// <summary>
    /// Gets or sets the raw bytes of the message body
    /// </summary>
    public byte[]? RawBytes
    {
        get => _rawBytes;
        set
        {
            _rawBytes = value;
            _detectedFormat = null; // Reset detection when content changes
        }
    }

    /// <summary>
    /// Gets or sets the detected or manually specified format
    /// </summary>
    public MessageBodyFormat Format { get; set; } = MessageBodyFormat.Unknown;

    /// <summary>
    /// Gets or sets the character encoding used (default UTF-8)
    /// </summary>
    public string Encoding { get; set; } = "UTF-8";

    /// <summary>
    /// Gets the size of the message body in bytes
    /// </summary>
    public long SizeBytes => _rawBytes?.Length ?? System.Text.Encoding.UTF8.GetByteCount(_rawContent);

    /// <summary>
    /// Gets or sets whether the format was manually overridden
    /// </summary>
    public bool IsFormatOverridden { get; set; }

    /// <summary>
    /// Initializes a new instance of MessageBody
    /// </summary>
    public MessageBody()
    {
    }

    /// <summary>
    /// Initializes a new instance of MessageBody with string content
    /// </summary>
    public MessageBody(string content)
    {
        _rawContent = content;
    }

    /// <summary>
    /// Initializes a new instance of MessageBody with byte array
    /// </summary>
    public MessageBody(byte[] bytes)
    {
        _rawBytes = bytes;
    }

    /// <summary>
    /// Automatically detects the format of the message body
    /// </summary>
    public MessageBodyFormat DetectFormat()
    {
        if (_detectedFormat.HasValue && !IsFormatOverridden)
        {
            return _detectedFormat.Value;
        }

        // If we have bytes, try to convert to string first
        if (_rawBytes != null && _rawBytes.Length > 0)
        {
            try
            {
                _rawContent = System.Text.Encoding.UTF8.GetString(_rawBytes);
            }
            catch
            {
                _detectedFormat = MessageBodyFormat.Binary;
                return _detectedFormat.Value;
            }
        }

        if (string.IsNullOrWhiteSpace(_rawContent))
        {
            _detectedFormat = MessageBodyFormat.Unknown;
            return _detectedFormat.Value;
        }

        var trimmed = _rawContent.Trim();

        // Check for XML
        if (trimmed.StartsWith("<") && trimmed.EndsWith(">"))
        {
            if (IsValidXml(trimmed))
            {
                _detectedFormat = MessageBodyFormat.Xml;
                return _detectedFormat.Value;
            }
        }

        // Check for JSON
        if ((trimmed.StartsWith("{") && trimmed.EndsWith("}")) ||
            (trimmed.StartsWith("[") && trimmed.EndsWith("]")))
        {
            if (IsValidJson(trimmed))
            {
                _detectedFormat = MessageBodyFormat.Json;
                return _detectedFormat.Value;
            }
        }

        // Check if it's printable text
        if (IsPrintableText(_rawContent))
        {
            _detectedFormat = MessageBodyFormat.Text;
            return _detectedFormat.Value;
        }

        // Default to binary
        _detectedFormat = MessageBodyFormat.Binary;
        return _detectedFormat.Value;
    }

    /// <summary>
    /// Gets the formatted content for display
    /// </summary>
    public string GetFormattedContent()
    {
        var format = Format != MessageBodyFormat.Unknown ? Format : DetectFormat();

        return format switch
        {
            MessageBodyFormat.Xml => FormatXml(_rawContent),
            MessageBodyFormat.Json => FormatJson(_rawContent),
            MessageBodyFormat.Binary => FormatBinary(_rawBytes ?? System.Text.Encoding.UTF8.GetBytes(_rawContent)),
            MessageBodyFormat.Text => _rawContent,
            _ => _rawContent
        };
    }

    /// <summary>
    /// Gets the content as a hex dump for binary data
    /// </summary>
    public string GetHexDump()
    {
        var bytes = _rawBytes ?? System.Text.Encoding.UTF8.GetBytes(_rawContent);
        return FormatBinary(bytes);
    }

    private static bool IsValidXml(string content)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(content);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidJson(string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsPrintableText(string content)
    {
        if (string.IsNullOrEmpty(content)) return false;

        foreach (var c in content)
        {
            if (!char.IsControl(c) || c == '\r' || c == '\n' || c == '\t')
                continue;
            return false;
        }
        return true;
    }

    private static string FormatXml(string xml)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            };

            using var stringWriter = new StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, settings);
            doc.Save(xmlWriter);
            return stringWriter.ToString();
        }
        catch
        {
            return xml;
        }
    }

    private static string FormatJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return json;
        }
    }

    private static string FormatBinary(byte[] bytes)
    {
        var sb = new StringBuilder();
        const int bytesPerLine = 16;

        for (int i = 0; i < bytes.Length; i += bytesPerLine)
        {
            sb.Append($"{i:X8}  ");

            // Hex representation
            for (int j = 0; j < bytesPerLine; j++)
            {
                if (i + j < bytes.Length)
                    sb.Append($"{bytes[i + j]:X2} ");
                else
                    sb.Append("   ");

                if (j == 7) sb.Append(" ");
            }

            sb.Append(" ");

            // ASCII representation
            for (int j = 0; j < bytesPerLine && i + j < bytes.Length; j++)
            {
                var b = bytes[i + j];
                sb.Append(b >= 32 && b <= 126 ? (char)b : '.');
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns a string representation of this message body
    /// </summary>
    public override string ToString()
    {
        return $"Format: {Format}, Size: {SizeBytes} bytes";
    }
}
