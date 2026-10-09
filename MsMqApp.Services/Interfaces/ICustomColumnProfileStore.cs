using MsMqApp.Models.Configuration;

namespace MsMqApp.Services.Interfaces;

public interface ICustomColumnProfileStore
{
    Task<CustomColumnProfile?> GetAsync(string serverKey, string queueKey);

    Task SaveAsync(CustomColumnProfile profile);
}

