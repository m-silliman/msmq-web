namespace MsMqApp.Models.Enums;

/// <summary>
/// Defines the available export formats for messages.
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// Export as JSON format with full message metadata.
    /// </summary>
    Json,

    /// <summary>
    /// Export as XML format with full message metadata.
    /// </summary>
    Xml,

    /// <summary>
    /// Export as CSV format (tabular data).
    /// </summary>
    Csv,

    /// <summary>
    /// Export as plain text format.
    /// </summary>
    Text,

    /// <summary>
    /// Export as binary file (preserves exact message body).
    /// </summary>
    Binary
}
