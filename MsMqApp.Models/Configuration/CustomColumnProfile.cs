namespace MsMqApp.Models.Configuration;

/// <summary>
/// The custom JSON columns associated with one server and queue.
/// </summary>
public sealed class CustomColumnProfile
{
    public int Version { get; set; } = 1;

    public string ServerKey { get; set; } = string.Empty;

    public string QueueKey { get; set; } = string.Empty;

    public List<CustomColumnDefinition> Columns { get; set; } = [];
}

/// <summary>
/// A user-defined column that reads a value from a message JSON body.
/// </summary>
public sealed class CustomColumnDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Header { get; set; } = string.Empty;

    public string JsonPath { get; set; } = string.Empty;

    public bool IsVisible { get; set; } = true;

    public int Order { get; set; }
}

