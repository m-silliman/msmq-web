using System.Runtime.InteropServices;

namespace MsMqApp.Services.Helpers;

/// <summary>
/// Reads the MSMQ body-size property without requesting the message body.
/// </summary>
/// <remarks>
/// Experimental.System.Messaging does not expose PROPID_M_BODY_SIZE through
/// MessagePropertyFilter. The native API does, and a lookup peek with only
/// this property selected avoids allocating or transferring the body bytes.
/// </remarks>
internal sealed class MsmqMessageBodySizeReader : IDisposable
{
    private const uint MqPeekAccess = 0x00000020;
    private const uint MqDenyNone = 0x00000000;
    private const uint MqLookupPeekCurrent = 0x40000010;
    private const int PropIdMessageBodySize = 10;
    private const ushort VariantTypeUInt32 = 19;

    private IntPtr _queueHandle;

    private MsmqMessageBodySizeReader(IntPtr queueHandle)
    {
        _queueHandle = queueHandle;
    }

    /// <summary>
    /// Opens a metadata-only peek handle. Returns null when native MSMQ access
    /// is unavailable so callers can keep list retrieval functional.
    /// </summary>
    public static MsmqMessageBodySizeReader? TryCreate(string queuePath)
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(queuePath))
        {
            return null;
        }

        try
        {
            var formatName = MsmqAddress.ToFormatName(queuePath);
            var result = NativeMethods.MQOpenQueue(
                formatName,
                MqPeekAccess,
                MqDenyNone,
                out var queueHandle);

            if (result != 0 || queueHandle == IntPtr.Zero)
            {
                return null;
            }

            return new MsmqMessageBodySizeReader(queueHandle);
        }
        catch (DllNotFoundException)
        {
            return null;
        }
        catch (EntryPointNotFoundException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the body size for a message lookup ID without reading its body.
    /// </summary>
    public long? TryRead(long lookupId)
    {
        if (_queueHandle == IntPtr.Zero || lookupId <= 0)
        {
            return null;
        }

        var propertyIds = new[] { PropIdMessageBodySize };
        var properties = new[] { new NativePropVariant { VariantType = VariantTypeUInt32 } };
        var statuses = new int[1];
        var propertyIdsHandle = default(GCHandle);
        var propertiesHandle = default(GCHandle);
        var statusesHandle = default(GCHandle);

        try
        {
            propertyIdsHandle = GCHandle.Alloc(propertyIds, GCHandleType.Pinned);
            propertiesHandle = GCHandle.Alloc(properties, GCHandleType.Pinned);
            statusesHandle = GCHandle.Alloc(statuses, GCHandleType.Pinned);

            var messageProperties = new NativeMessageProperties
            {
                PropertyCount = 1,
                PropertyIds = propertyIdsHandle.AddrOfPinnedObject(),
                PropertyValues = propertiesHandle.AddrOfPinnedObject(),
                Statuses = statusesHandle.AddrOfPinnedObject()
            };

            var result = NativeMethods.MQReceiveMessageByLookupId(
                _queueHandle,
                unchecked((ulong)lookupId),
                MqLookupPeekCurrent,
                ref messageProperties,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);

            if (result != 0 || statuses[0] != 0 || properties[0].VariantType != VariantTypeUInt32)
            {
                return null;
            }

            return properties[0].UInt32Value;
        }
        catch (DllNotFoundException)
        {
            return null;
        }
        catch (EntryPointNotFoundException)
        {
            return null;
        }
        catch (ExternalException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        finally
        {
            if (statusesHandle.IsAllocated) statusesHandle.Free();
            if (propertiesHandle.IsAllocated) propertiesHandle.Free();
            if (propertyIdsHandle.IsAllocated) propertyIdsHandle.Free();
        }
    }

    public void Dispose()
    {
        var queueHandle = _queueHandle;
        _queueHandle = IntPtr.Zero;
        if (queueHandle != IntPtr.Zero)
        {
            NativeMethods.MQCloseQueue(queueHandle);
        }

        GC.SuppressFinalize(this);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMessageProperties
    {
        public uint PropertyCount;
        public IntPtr PropertyIds;
        public IntPtr PropertyValues;
        public IntPtr Statuses;
    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    private struct NativePropVariant
    {
        [FieldOffset(0)]
        public ushort VariantType;

        [FieldOffset(8)]
        public uint UInt32Value;
    }

    private static class NativeMethods
    {
        [DllImport("mqrt.dll", CharSet = CharSet.Unicode, ExactSpelling = true,
            CallingConvention = CallingConvention.StdCall)]
        public static extern int MQOpenQueue(
            [MarshalAs(UnmanagedType.LPWStr)] string formatName,
            uint access,
            uint shareMode,
            out IntPtr queueHandle);

        [DllImport("mqrt.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern int MQReceiveMessageByLookupId(
            IntPtr queueHandle,
            ulong lookupId,
            uint lookupAction,
            ref NativeMessageProperties messageProperties,
            IntPtr overlapped,
            IntPtr receiveCallback,
            IntPtr transaction);

        [DllImport("mqrt.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern int MQCloseQueue(IntPtr queueHandle);
    }
}
