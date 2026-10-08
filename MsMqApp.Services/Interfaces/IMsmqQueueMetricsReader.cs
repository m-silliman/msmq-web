using MsMqApp.Models.Domain;

namespace MsMqApp.Services.Interfaces;

/// <summary>Reads paired counts without enumerating messages. Called on a bounded worker.</summary>
public interface IMsmqQueueMetricsReader
{
    QueueMetrics Read(string computerName, string formatName);
}
