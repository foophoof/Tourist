using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using KamiToolKit;
using Microsoft.Extensions.Hosting;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Tourist.Addons;

namespace Tourist.Services;

public class NativeUiService(IDalamudPluginInterface pluginInterface, IPluginLog pluginLog, IFramework framework) : IHostedService
{
    private NativeController? _nativeController;
    private AddonTourist? _addonTourist;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_nativeController is not null)
        {
            pluginLog.Error("Starting NativeUIService when NativeController already created");
        }

        _nativeController = new NativeController(pluginInterface);
        _addonTourist = new AddonTourist(_nativeController)
        {
            InternalName = "Tourist",
            Title = "Tourist",
            Size = new Vector2(350.0f, 450.0f),
        };

        framework.RunOnFrameworkThread(() => { _addonTourist.Open(_nativeController); });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _addonTourist?.Dispose();
        _nativeController?.Dispose();

        return Task.CompletedTask;
    }
}
