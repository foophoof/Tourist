using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System.Diagnostics.CodeAnalysis;
using Tourist.Services;

namespace Tourist;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class Plugin : IDalamudPlugin
{
    private readonly IPluginLog _pluginLog;
    private readonly NativeUiService _nativeUiService;

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        IPluginLog pluginLog,
        IFramework framework)
    {
        _nativeUiService = new NativeUiService(pluginInterface, framework, pluginLog);
        _nativeUiService.Startup();

        _pluginLog = pluginLog;
    }

    public void Dispose()
    {
        _pluginLog.Verbose("Plugin.Dispose, cleaning up NativeUiService");
        _nativeUiService.Cleanup();

        _pluginLog.Verbose("Plugin.Dispose, done");
    }
}
