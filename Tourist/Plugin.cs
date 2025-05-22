using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVWeather.Lumina;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Tourist.Config;
using Tourist.Services;
using Tourist.Windows;

namespace Tourist;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class Plugin : IDalamudPlugin
{
    private readonly IHost _host;

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        IDataManager dataManager,
        ICommandManager commandManager,
        IPluginLog pluginLog,
        IClientState clientState,
        IGameGui gameGui,
        IFramework framework,
        IGameInteropProvider gameInteropProvider)
    {
        _host = new HostBuilder()
            .UseContentRoot(pluginInterface.ConfigDirectory.FullName)
            .ConfigureLogging(lb =>
            {
                lb.ClearProviders();
                lb.SetMinimumLevel(LogLevel.Trace);
            })
            .ConfigureServices(collection =>
            {
                collection.AddSingleton(pluginInterface);
                collection.AddSingleton(dataManager);
                collection.AddSingleton(commandManager);
                collection.AddSingleton(pluginLog);
                collection.AddSingleton(clientState);
                collection.AddSingleton(gameGui);
                collection.AddSingleton(framework);
                collection.AddSingleton(gameInteropProvider);
                collection.AddSingleton<WindowService>();
                collection.AddSingleton<InstallerWindowService>();
                collection.AddSingleton<CommandService>();
                collection.AddSingleton<VfxService>();
                collection.AddSingleton<MarkerService>();
                collection.AddSingleton<VistaUnlockedListenerService>();
                collection.AddSingleton<MainWindow>();

                collection.AddSingleton<Window>(provider => provider.GetRequiredService<MainWindow>());

                collection.AddSingleton(s => new FFXIVWeatherLuminaService(s.GetRequiredService<IDataManager>().GameData));

                collection.AddSingleton(PluginConfig.Load);

                collection.AddSingleton(new WindowSystem("Tourist"));

                collection.AddHostedService(p => p.GetRequiredService<WindowService>());
                collection.AddHostedService(p => p.GetRequiredService<CommandService>());
                collection.AddHostedService(p => p.GetRequiredService<InstallerWindowService>());
                collection.AddHostedService(p => p.GetRequiredService<VfxService>());
                collection.AddHostedService(p => p.GetRequiredService<VistaUnlockedListenerService>());
                collection.AddHostedService(p => p.GetRequiredService<MarkerService>());
            })
            .Build();

        _ = _host.StartAsync();
    }

    void IDisposable.Dispose()
    {
        _host.StopAsync().GetAwaiter().GetResult();
        _host.Dispose();
    }
}
