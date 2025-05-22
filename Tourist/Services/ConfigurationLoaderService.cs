using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Tourist.Config;

namespace Tourist.Services;

public class ConfigurationLoaderService(IDalamudPluginInterface pluginInterface, IPluginLog pluginLog) : IHostedService
{
    private PluginConfig? _pluginConfig;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Save();
        pluginLog.Verbose("Stopping configuration loader, saving.");
        return Task.CompletedTask;
    }

    public PluginConfig GetPluginConfig()
    {
        if (_pluginConfig != null)
            return _pluginConfig;

        try
        {
            _pluginConfig = pluginInterface.GetPluginConfig() as PluginConfig ?? new PluginConfig();
        }
        catch (Exception e)
        {
            pluginLog.Error(e, "Failed to load configuration");
            _pluginConfig = new PluginConfig();
        }

        return _pluginConfig;
    }

    public void Save()
    {
        pluginInterface.SavePluginConfig(GetPluginConfig());
    }
}
