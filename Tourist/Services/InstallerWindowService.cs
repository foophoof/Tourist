using Dalamud.Plugin;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Tourist.Windows;

namespace Tourist.Services;

public class InstallerWindowService(IDalamudPluginInterface pluginInterface, MainWindow mainWindow) : IHostedService
{

    public Task StartAsync(CancellationToken cancellationToken)
    {
        pluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        pluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

        return Task.CompletedTask;
    }

    private void ToggleMainUi()
    {
        mainWindow.Toggle();
    }
}
