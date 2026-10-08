namespace MsMqApp.Services.Helpers;

public static class MsmqAddress
{
    /// <summary>Preserve discovered format names; only ordinary queue paths need a direct prefix.</summary>
    public static string ToFormatName(string address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        var name = address.Trim();
        if (name.StartsWith("FormatName:", StringComparison.OrdinalIgnoreCase))
            return name[11..];
        if (name.Contains('=') || name.StartsWith("OS:", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("TCP:", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("HTTP:", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("HTTPS:", StringComparison.OrdinalIgnoreCase))
            return name;
        return "DIRECT=OS:" + name;
    }

    public static string Server(string address)
    {
        var name = address.Trim();
        if (name.StartsWith("FormatName:", StringComparison.OrdinalIgnoreCase)) name = name[11..];
        if (name.StartsWith("DIRECT=", StringComparison.OrdinalIgnoreCase)) name = name[7..];
        if (name.StartsWith("OS:", StringComparison.OrdinalIgnoreCase)) name = name[3..];
        if (name.StartsWith("TCP:", StringComparison.OrdinalIgnoreCase)) name = name[4..];
        if (Uri.TryCreate(name, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
            name = uri.Host;
        else if (name.Contains('='))
            return "opaque-format-names"; // Still subject to the shared global worker limit.
        else
            name = name.Split('\\')[0];
        return name is "." or "" || name.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            ? Environment.MachineName : name;
    }
}
