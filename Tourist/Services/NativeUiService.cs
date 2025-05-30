using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using KamiToolKit;
using System.Numerics;
using Tourist.Addons;

namespace Tourist.Services;

public class NativeUiService(IDalamudPluginInterface pluginInterface, IFramework framework, IPluginLog pluginLog)
{
    private NativeController? _nativeController;
    private AddonTourist? _addonTourist;

    internal void Startup()
    {
        // ThreadSafety.AssertMainThread();
        pluginLog.Verbose("Setting up Tourist NativeUiService");
        _nativeController = new NativeController(pluginInterface);
        _addonTourist = new AddonTourist(_nativeController)
        {
            InternalName = "Tourist",
            Title = "Tourist",
            Size = new Vector2(350.0f, 450.0f),
        };
        pluginInterface.UiBuilder.OpenMainUi += OpenUI;
    }

    // Must be called on framework thread
    private void DisposeThings()
    {
        ThreadSafety.AssertMainThread();

        pluginInterface.UiBuilder.OpenMainUi -= OpenUI;

        pluginLog.Info("Disposing Native UI stuff, on main thread");
        if (_addonTourist is null)
        {
            pluginLog.Verbose("_addonTourist is null");
        }

        if (_nativeController is null)
        {
            pluginLog.Verbose("_nativeController is null");
        }
        _addonTourist?.Dispose();
        _nativeController?.Dispose();
        pluginLog.Info("Done disposing Native UI stuff");

        // _addonTourist = null;
        // _nativeController = null;
    }

    internal void Cleanup()
    {
        DisposeThings();
    }

    private void OpenUI()
    {
        if (_nativeController is null)
            return;
        _addonTourist?.Open(_nativeController);
    }
}
