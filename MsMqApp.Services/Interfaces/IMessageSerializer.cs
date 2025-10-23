using MsMqApp.Models.Domain;
using MsMqApp.Models.Enums;
using MsMqApp.Models.Results;

namespace MsMqApp.Services.Interfaces;

/// <summary>
/// Service interface for deserializing and serializing MSMQ message bodies to various formats.
/// Supports XML, JSON, Text, and Binary formats with automatic format detection.
/// </summary>
/// <remarks>
/// This service provides intelligent message body parsing and formatting capabilities.
/// It can automatically detect message formats and provide appropriate deserialization.
/// </remarks>
public interface IMessageSerializer
{
    #region Format Detection

    /// <summary>
    /// Automatically detects the format of a message body.
    /// </summary>
    /// <param name="messageBody">The message body to analyze</param>
    /// <returns>
    /// An operation result containing the detected MessageBodyFormat.
    /// Returns Unknown if format cannot be determined.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when messageBody is null</exception>
    /// <remarks>
    /// Detection is performed by analyzing the content structure:
    /// - XML: Starts with '&lt;' and contains valid XML structure
    /// - JSON: Starts with '{' or '[' and contains valid JSON structure
    /// - Text: Contains printable characters
    /// - Binary: Contains non-printable characters or binary data
    /// </remarks>
    OperationResult<MessageBodyFormat> DetectFormat(MessageBody messageBody);

    /// <summary>
    /// Validates whether a message body conforms to the specified format.
    /// </summary>
    /// <param name="messageBody">The message body to validate</param>
    /// <param name="format">The format to validate against</param>
    /// <returns>
    /// An operation result containing true if the body is valid for the specified format,
    /// false otherwise. ErrorMessage contains validation details on failure.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when messageBody is null</exception>
    OperationResult<bool> ValidateFormat(MessageBody messageBody, MessageBodyFormat format);

    #endregion

    #region Deserialization

    /// <summary>
    /// Deserializes a message body to a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    /// <param name="messageBody">The message body to deserialize</param>
    /// <param name="format">
    /// Optional format hint. If not specified, format will be auto-detected.
    /// </param>
    /// <returns>
    /// An operation result containing the deserialized object of type T,
    /// or failure result if deserialization fails.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when messageBody is null</exception>
    /// <remarks>
    /// Supports deserialization from XML and JSON formats.
    /// For binary formats, consider using DeserializeBinary method.
    /// </remarks>
    OperationResult<T?> Deserialize<T>(
        MessageBody messageBody,
        MessageBodyFormat? format = null) where T : class;

    /// <summary>
    /// Deserializes a binary message body using .NET binary serialization.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    /// <param name="messageBody">The message body containing binary data</param>
    /// <returns>
    /// An operation result containing the deserialized object of type T,
    /// or failure result if deserialization fails.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when messageBody is null</exception>
    /// <remarks>
    /// WARNING: Binary deserialization can be a security risk.
    /// Only deserialize messages from trusted sources.
    /// </remarks>
    OperationResult<T?> DeserializeBinary<T>(MessageBody messageBody) where T : class;

    /// <summary>
    /// Deserializes an XML message body to a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    /// <param name="messageBody">The message body containing XML data</param>
    /// <returns>
    /// An operation result containing the deserialized object of type T,
    /// or failure result if XML deserialization fails.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when messageBody is null</exception>
    OperationResult<T?> DeserializeXml<T>(MessageBody messageBody) where T : class;

    /// <summary>
    /// Deserializes a JSON message body to a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    /// <param name="messageBody">The message body containing JSON data</param>
    /// <returns>
    /// An operation result containing the deserialized object of type T,
    /// or failure result if JSON deserialization fails.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when messageBody is null</exception>
    OperationResult<T?> DeserializeJson<T>(MessageBody messageBody) where T : class;

    #endregion

    #region Serialization

    /// <summary>
    /// Serializes an object to a message body in the specified format.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize</typeparam>
    /// <param name="obj">The object to serialize</param>
    /// <param name="format">The target format (XML, JSON, Binary, or Text)</param>
    /// <returns>
    /// An operation result containing a MessageBody with the serialized data,
    /// or failure result if serialization fails.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when obj is null</exception>
    /// <remarks>
    /// - XML: Uses XmlSerializer
    /// - JSON: Uses System.Text.Json
    /// - Binary: Uses .NET binary serialization
    /// - Text: Uses ToString() method
    /// </remarks>
    OperationResult<MessageBody> Serialize<T>(T obj, MessageBodyFormat format) where T : class;

    /// <summary>
    /// Serializes an object to XML format.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize</typeparam>
    /// <param name="obj">The object to serialize</param>
    /// <param name="indent">If true, produces indented/pretty-printed XML. Default is true.</param>
    /// <returns>
    /// An operation result containing a MessageBody with XML data,
    /// or failure result if serialization fails.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when obj is null</exception>
    OperationResult<MessageBody> SerializeToXml<T>(T obj, bool indent = true) where T : class;

    /// <summary>
    /// Serializes an object to JSON format.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize</typeparam>
    /// <param name="obj">The object to serialize</param>
    /// <param name="indent">If true, produces indented/pretty-printed JSON. Default is true.</param>
    /// <returns>
    /// An operation result containing a MessageBody with JSON data,
    /// or failure result if serialization fails.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when obj is null</exception>
    OperationResult<MessageBody> SerializeToJson<T>(T obj, bool indent = true) where T : class;

    /// <summary>
    /// Serializes an object to binary format using .NET binary serialization.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize</typeparam>
    /// <param name="obj">The object to serialize (must be marked as Serializable)</param>
    /// <returns>
    /// An operation result containing a MessageBody with binary data,
    /// or failure result if serialization fails.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when obj is null</exception>
    /// <remarks>
    /// The type T must be marked with [Serializable] attribute.
    /// Consider using JSON or XML for better interoperability.
    /// </remarks>
    OperationResult<MessageBody> SerializeToBinary<T>(T obj) where T : class;

    #endregion

    #region Formatting and Display

    /// <summary>
    /// Formats a message body for display with syntax highlighting hints.
    /// </summary>
    /// <param name="messageBody">The message body to format</param>
    /// <param name="maxLength">
    /// Maximum length of the formatted output. Use 0 for unlimited.
    /// Default is 0 (unlimited).
    /// </param>
    /// <returns>
    /// An operation result containing the formatted string ready for display.
    /// For XML/JSON, returns indented/pretty-printed output.
    /// For binary, returns hex dump.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when messageBody is null</exception>
    OperationResult<string> FormatForDisplay(
        MessageBody messageBody,
        int maxLength = 0);

    /// <summary>
    /// Converts a message body to a hex dump representation.
    /// </summary>
    /// <param name="messageBody">The message body to convert</param>
    /// <param name="bytesPerLine">Number of bytes to display per line. Default is 16.</param>
    /// <param name="maxBytes">Maximum number of bytes to include. Use 0 for unlimited. Default is 0.</param>
    /// <returns>
    /// An operation result containing the hex dump string in the format:
    /// "00000000  48 65 6C 6C 6F 20 57 6F 72 6C 64 21 00 00 00 00  Hello World!...."
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when messageBody is null</exception>
    OperationResult<string> ToHexDump(
        MessageBody messageBody,
        int bytesPerLine = 16,
        int maxBytes = 0);

    /// <summary>
    /// Extracts plain text from a message body, regardless of format.
    /// </summary>
    /// <param name="messageBody">The message body to extract text from</param>
    /// <returns>
    /// An operation result containing the plain text representation.
    /// For XML/JSON, returns the raw content.
    /// For binary, returns a hex representation or decoded text if possible.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when messageBody is null</exception>
    OperationResult<string> ExtractText(MessageBody messageBody);

    #endregion

    #region Encoding

    /// <summary>
    /// Detects the character encoding of a message body.
    /// </summary>
    /// <param name="messageBody">The message body to analyze</param>
    /// <returns>
    /// An operation result containing the detected encoding name (e.g., "UTF-8", "ASCII").
    /// Returns "UTF-8" as default if detection is inconclusive.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when messageBody is null</exception>
    OperationResult<string> DetectEncoding(MessageBody messageBody);

    /// <summary>
    /// Converts a message body from one encoding to another.
    /// </summary>
    /// <param name="messageBody">The message body to convert</param>
    /// <param name="sourceEncoding">The source encoding name (e.g., "UTF-8")</param>
    /// <param name="targetEncoding">The target encoding name (e.g., "ASCII")</param>
    /// <returns>
    /// An operation result containing a new MessageBody with the converted content,
    /// or failure result if conversion fails.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when messageBody, sourceEncoding, or targetEncoding is null
    /// </exception>
    OperationResult<MessageBody> ConvertEncoding(
        MessageBody messageBody,
        string sourceEncoding,
        string targetEncoding);

    #endregion
}
