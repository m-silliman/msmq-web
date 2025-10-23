namespace MsMqApp.Models.Enums;

/// <summary>
/// Represents the format of a message body
/// </summary>
public enum MessageBodyFormat
{
    /// <summary>
    /// Unknown or undetected format
    /// </summary>
    Unknown,

    /// <summary>
    /// Plain text format
    /// </summary>
    Text,

    /// <summary>
    /// XML format
    /// </summary>
    Xml,

    /// <summary>
    /// JSON format
    /// </summary>
    Json,

    /// <summary>
    /// Binary format (displayed as hex)
    /// </summary>
    Binary,

    /// <summary>
    /// Serialized .NET object
    /// </summary>
    Serialized
}
