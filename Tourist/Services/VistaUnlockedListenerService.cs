using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace Tourist.Services;

public class VistaUnlockedListenerService(IGameInteropProvider gameInteropProvider, IPluginLog pluginLog, VfxService vfxService) : IHostedService
{

    [Signature("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 8B 4C 24 70 E8")]
    private Hook<VistaUnlockedDelegate>? _vistaUnlockedHook;


    public Task StartAsync(CancellationToken cancellationToken)
    {
        _vistaUnlockedHook = gameInteropProvider.HookFromSignature<VistaUnlockedDelegate>(
            "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 8B 4C 24 70 E8",
            OnVistaUnlocked);
        _vistaUnlockedHook.Enable();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _vistaUnlockedHook?.Dispose();
        _vistaUnlockedHook = null;

        return Task.CompletedTask;
    }

    private nint OnVistaUnlocked(ushort index, int a2, int a3)
    {
        try
        {
            pluginLog.Info($"Vista {index} completed, removing VFX");
            vfxService.QueueRemove(index);
        }
        catch (Exception ex)
        {
            pluginLog.Error(ex, "Exception in OnVistaUnlocked");
        }

        return _vistaUnlockedHook!.Original(index, a2, a3);
    }

    private delegate nint VistaUnlockedDelegate(ushort index, int a2, int a3);
}
