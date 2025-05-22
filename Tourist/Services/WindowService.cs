using DalaMock.Host.Factories;
using DalaMock.Shared.Interfaces;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tourist.Services;

public class WindowService(
    IDalamudPluginInterface pluginInterface,
    IEnumerable<Window> pluginWindows,
    IWindowSystemFactory windowSystemFactory) : IHostedService
{
    private IWindowSystem WindowSystem { get; } = windowSystemFactory.Create("Tourist");

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var pluginWindow in pluginWindows)
        {
            WindowSystem.AddWindow(pluginWindow);
        }

        pluginInterface.UiBuilder.Draw += UiBuilderOnDraw;

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        pluginInterface.UiBuilder.Draw -= UiBuilderOnDraw;

        WindowSystem.RemoveAllWindows();

        return Task.CompletedTask;
    }

    private void UiBuilderOnDraw()
    {
        WindowSystem.Draw();
    }
}
