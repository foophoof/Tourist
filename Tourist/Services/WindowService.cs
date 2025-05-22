using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tourist.Services;

public class WindowService(IDalamudPluginInterface pluginInterface, IEnumerable<Window> pluginWindows, WindowSystem windowSystem) : IHostedService
{

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var pluginWindow in pluginWindows)
        {
            windowSystem.AddWindow(pluginWindow);
        }

        pluginInterface.UiBuilder.Draw += UiBuilderOnDraw;

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        pluginInterface.UiBuilder.Draw -= UiBuilderOnDraw;

        windowSystem.RemoveAllWindows();

        return Task.CompletedTask;
    }

    private void UiBuilderOnDraw()
    {
        windowSystem.Draw();
    }
}
