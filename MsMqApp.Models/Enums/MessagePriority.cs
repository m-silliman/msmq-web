namespace MsMqApp.Models.Enums;

/// <summary>
/// Represents the priority of an MSMQ message
/// </summary>
public enum MessagePriority
{
    /// <summary>
    /// Lowest priority (0)
    /// </summary>
    Lowest = 0,

    /// <summary>
    /// Very Low priority (1)
    /// </summary>
    VeryLow = 1,

    /// <summary>
    /// Low priority (2)
    /// </summary>
    Low = 2,

    /// <summary>
    /// Normal priority (3) - Default
    /// </summary>
    Normal = 3,

    /// <summary>
    /// Above Normal priority (4)
    /// </summary>
    AboveNormal = 4,

    /// <summary>
    /// High priority (5)
    /// </summary>
    High = 5,

    /// <summary>
    /// Very High priority (6)
    /// </summary>
    VeryHigh = 6,

    /// <summary>
    /// Highest priority (7)
    /// </summary>
    Highest = 7
}
