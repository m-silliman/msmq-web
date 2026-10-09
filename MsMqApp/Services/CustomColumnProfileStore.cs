using System.Text.Json;
using Microsoft.JSInterop;
using MsMqApp.Models.Configuration;
using MsMqApp.Services.Interfaces;

namespace MsMqApp.Services.Implementations;

/// <summary>
/// Persists custom column definitions in the browser's global application storage.
/// Profiles are deliberately keyed by server and queue so they survive connection changes.
/// </summary>
public sealed class CustomColumnProfileStore : ICustomColumnProfileStore
{
    private const string StorageKey = "msmq-app-custom-column-profiles";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly IJSRuntime _jsRuntime;

    public CustomColumnProfileStore(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    }

    public async Task<CustomColumnProfile?> GetAsync(string serverKey, string queueKey)
    {
        var profiles = await ReadProfilesAsync();
        return profiles.FirstOrDefault(profile =>
            string.Equals(profile.ServerKey, serverKey, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(profile.QueueKey, queueKey, StringComparison.OrdinalIgnoreCase));
    }

    public async Task SaveAsync(CustomColumnProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var profiles = await ReadProfilesAsync();
        var existing = profiles.FindIndex(item =>
            string.Equals(item.ServerKey, profile.ServerKey, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.QueueKey, profile.QueueKey, StringComparison.OrdinalIgnoreCase));

        if (existing >= 0)
        {
            profiles[existing] = profile;
        }
        else
        {
            profiles.Add(profile);
        }

        var serialized = JsonSerializer.Serialize(profiles, JsonOptions);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, serialized);
    }

    private async Task<List<CustomColumnProfile>> ReadProfilesAsync()
    {
        try
        {
            var serialized = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (string.IsNullOrWhiteSpace(serialized))
            {
                return [];
            }

            return JsonSerializer.Deserialize<List<CustomColumnProfile>>(serialized, JsonOptions) ?? [];
        }
        catch (JSException)
        {
            return [];
        }
        catch (InvalidOperationException)
        {
            // JS interop is unavailable during prerendering. The caller can retry after render.
            return [];
        }
        catch (JsonException)
        {
            // Corrupt settings should not prevent the message list from rendering.
            return [];
        }
    }
}

