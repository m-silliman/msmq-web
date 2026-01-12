using MsMqApp.Components;
using MsMqApp.Models.Configuration;
using MsMqApp.Services;
using MsMqApp.Services.FormatHandlers;
using MsMqApp.Services.Implementations;
using MsMqApp.Services.Interfaces;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Configure Windows Service support
builder.Host.UseWindowsService(options =>
{
    options.ServiceName = builder.Configuration.GetValue<string>("Service:ServiceName") ?? "MSMQMonitor";
});

// Configure application settings
builder.Services.Configure<ApplicationSettings>(
    builder.Configuration.GetSection("Application"));

builder.Services.Configure<ServiceSettings>(
    builder.Configuration.GetSection("Service"));

// Configure Kestrel web server with configurable port
var servicePort = builder.Configuration.GetValue<int>("Service:Port", 9090);
builder.WebHost.ConfigureKestrel(options =>
{
    // Listen on configured port for HTTP
    options.ListenAnyIP(servicePort, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });

    // Configure limits
    options.Limits.MaxRequestBodySize = 10485760; // 10 MB
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
});

// Configure logging
builder.Logging.ClearProviders();

if (builder.Environment.IsDevelopment())
{
    // Development logging: console and debug
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();
}
else
{
    // Production logging: Windows Event Log when running as service
    if (OperatingSystem.IsWindows())
    {
        builder.Logging.AddEventLog(settings =>
        {
            settings.SourceName = builder.Configuration.GetValue<string>("Service:ServiceName") ?? "MSMQMonitor";
            settings.LogName = "Application";
        });
    }

    // Also add console for manual runs
    builder.Logging.AddConsole();
}

// Set logging levels
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Http.Connections", LogLevel.Warning);

// Add services to the container
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register format handlers
builder.Services.AddSingleton<XmlFormatHandler>();
builder.Services.AddSingleton<JsonFormatHandler>();
builder.Services.AddSingleton<TextFormatHandler>();
builder.Services.AddSingleton<BinaryFormatHandler>();

// Register application services
builder.Services.AddScoped<IMsmqService, MsmqService>();
builder.Services.AddScoped<IMessageSerializer, MessageSerializer>();
builder.Services.AddScoped<IMessageOperationsService, MessageOperationsService>();
builder.Services.AddScoped<IQueueConnectionManager, QueueConnectionManager>();
builder.Services.AddScoped<IQueueManagementService, QueueManagementService>();

// Register UI services
builder.Services.AddScoped<IThemeService, ThemeService>();
builder.Services.AddScoped<IUiStateService, UiStateService>();

// Configure SignalR for Blazor Server
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 102400; // 100 KB
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
});

var app = builder.Build();

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var serviceSettings = builder.Configuration.GetSection("Service").Get<ServiceSettings>() ?? new ServiceSettings();

logger.LogInformation("MSMQ Monitor & Management Tool starting...");
logger.LogInformation("Service Name: {ServiceName}", serviceSettings.ServiceName);
logger.LogInformation("Listening on port: {Port}", serviceSettings.Port);
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("Running as Windows Service: {IsWindowsService}",
    OperatingSystem.IsWindows() && builder.Environment.IsProduction());

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// HTTPS redirection is disabled for service mode (uses HTTP only on configured port)
// app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Configure graceful shutdown
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

lifetime.ApplicationStopping.Register(() =>
{
    logger.LogInformation("MSMQ Monitor service is stopping...");
});

lifetime.ApplicationStopped.Register(() =>
{
    logger.LogInformation("MSMQ Monitor service has stopped.");
});

logger.LogInformation("MSMQ Monitor service started successfully. Access the application at http://localhost:{Port}", serviceSettings.Port);

app.Run();
