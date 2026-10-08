namespace MsMqApp.Models.Domain;

/// <summary>Management counts; null means unavailable, never an inferred empty queue.</summary>
public sealed record QueueMetrics(int? MessageCount, int? JournalMessageCount);
