using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Tourist.Windows;

namespace Tourist.Services;

public class CommandService(ICommandManager commandManager, MainWindow mainWindow)
    : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        commandManager.AddHandler("/tourist", new CommandInfo(HandleCommand) {HelpMessage = "Opens the Tourist interface"});
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        commandManager.RemoveHandler("/tourist");
        return Task.CompletedTask;
    }

    private void HandleCommand(string command, string arguments)
    {
        mainWindow.Toggle();
    }
}
