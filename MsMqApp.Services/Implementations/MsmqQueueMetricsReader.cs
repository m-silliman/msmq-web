using System.Runtime.InteropServices;
using Experimental.System.Messaging;
using MsMqApp.Models.Domain;
using MsMqApp.Services.Helpers;
using MsMqApp.Services.Interfaces;

namespace MsMqApp.Services.Implementations;

public sealed class MsmqQueueMetricsReader : IMsmqQueueMetricsReader
{
    // MQMGMTPROPS uses PROPVARIANT, whose vector union makes it 24 bytes on x64,
    // 16 on x86. Only scalar VT_UI4 values are requested; no native strings to free.
    public QueueMetrics Read(string computerName, string formatName)
    {
        if (formatName.StartsWith("FormatName:", StringComparison.OrdinalIgnoreCase))
            formatName = formatName[11..];
        var counts = ReadManagement(computerName, formatName);
        if (counts.MessageCount.HasValue && counts.JournalMessageCount.HasValue)
            return counts;

        // A direct name for another host can identify an outgoing queue. Never peek
        // its destination to infer the outgoing queue's count on this computer.
        if (!MsmqAddress.Server(formatName).Equals(MsmqAddress.Server(computerName), StringComparison.OrdinalIgnoreCase))
            return counts;

        using var queue = new MessageQueue("FormatName:" + formatName);
        var baseEmpty = !counts.MessageCount.HasValue && IsEmpty(queue);
        // A peek opens an inactive queue. Retry management counts while its handle
        // is still open; this also covers a message arriving during the probe.
        var activeCounts = ReadManagement(computerName, formatName);
        var messageCount = activeCounts.MessageCount ?? counts.MessageCount ?? (baseEmpty ? 0 : (int?)null);
        var journalCount = activeCounts.JournalMessageCount ?? counts.JournalMessageCount;
        if (!journalCount.HasValue && !formatName.Contains("\\SYSTEM$", StringComparison.OrdinalIgnoreCase)
            && !formatName.EndsWith(";journal", StringComparison.OrdinalIgnoreCase))
        {
            using var journal = new MessageQueue("FormatName:" + formatName + ";journal");
            var journalEmpty = IsEmpty(journal);
            var journalCounts = ReadManagement(computerName, formatName);
            journalCount = journalCounts.JournalMessageCount ?? (journalEmpty ? 0 : (int?)null);
        }
        return new QueueMetrics(messageCount, journalCount);
    }

    private static bool IsEmpty(MessageQueue queue)
    {
        queue.MessageReadPropertyFilter.ClearAll();
        queue.MessageReadPropertyFilter.Id = true;
        try
        {
            // One immediate metadata-only peek. No body, enumeration, receive, or mutation.
            using var message = queue.Peek(TimeSpan.Zero);
            return false;
        }
        catch (MessageQueueException ex) when (ex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
        {
            return true;
        }
        catch (MessageQueueException)
        {
            // Missing queues and permission/transport failures are not evidence of emptiness.
            return false;
        }
    }

    private static QueueMetrics ReadManagement(string computerName, string formatName)
    {
        var ids = Marshal.AllocHGlobal(8);
        var variantSize = IntPtr.Size == 8 ? 24 : 16;
        var variants = Marshal.AllocHGlobal(variantSize * 2);
        try
        {
            Marshal.WriteInt32(ids, 0, 7); // PROPID_MGMT_QUEUE_MESSAGE_COUNT
            Marshal.WriteInt32(ids, 4, 9); // PROPID_MGMT_QUEUE_JOURNAL_MESSAGE_COUNT
            Marshal.Copy(new byte[variantSize * 2], 0, variants, variantSize * 2);
            Marshal.WriteInt16(variants, 0, 1); // VT_NULL
            Marshal.WriteInt16(variants, variantSize, 1);
            var props = new ManagementProperties { Count = 2, Ids = ids, Variants = variants };
            var local = computerName is "." or "" ||
                computerName.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                computerName.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase);
            var result = MQMgmtGetInfo(local ? null : computerName, "QUEUE=" + formatName, ref props);
            // The caller can confirm inactive empty queues with a single read-only peek.
            if (result < 0)
                return new QueueMetrics(null, null);
            return new QueueMetrics(ReadCount(variants), ReadCount(variants + variantSize));
        }
        finally
        {
            Marshal.FreeHGlobal(variants);
            Marshal.FreeHGlobal(ids);
        }
    }

    private static int? ReadCount(IntPtr variant)
    {
        if (Marshal.ReadInt16(variant) != 19) // VT_UI4
            return null;
        var count = unchecked((uint)Marshal.ReadInt32(variant, 8));
        return count <= int.MaxValue ? (int)count : null;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ManagementProperties
    {
        public uint Count;
        public IntPtr Ids;
        public IntPtr Variants;
        public IntPtr Status; // MQMgmtGetInfo does not populate per-property status.
    }

    [DllImport("mqrt.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int MQMgmtGetInfo(string? computerName, string objectName,
        ref ManagementProperties properties);
}
