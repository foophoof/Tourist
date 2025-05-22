using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using Microsoft.Extensions.Hosting;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Tourist.Config;

namespace Tourist.Services;

public class MarkerService(
    IClientState clientState,
    PluginConfig pluginConfig,
    IPluginLog pluginLog,
    IDataManager dataManager,
    VfxService vfxService)
    : IHostedService
{
    private const string MarkerPath = "bgcommon/world/common/vfx_for_live/eff/b0810_tnsk_y.avfx";

    public Task StartAsync(CancellationToken cancellationToken)
    {
        clientState.TerritoryChanged += OnTerritoryChange;

        SpawnVfxForCurrentZone();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        clientState.TerritoryChanged -= OnTerritoryChange;

        return Task.CompletedTask;
    }

    private void OnTerritoryChange(ushort territory)
    {
        if (!pluginConfig.ShowArrVistas)
        {
            return;
        }

        try
        {
            vfxService.QueueRemoveAll();
            SpawnVfxForZone(territory);
        }
        catch (Exception e)
        {
            pluginLog.Error(e, "Exception in territory change");
        }
    }

    internal void SpawnVfxForCurrentZone()
    {
        SpawnVfxForZone(clientState.TerritoryType);
    }

    internal void RemoveAllVfx()
    {
        vfxService.QueueRemoveAll();
    }

    private void SpawnVfxForZone(ushort territory)
    {
        var row = 0;
        foreach (var adventure in dataManager.GetExcelSheet<Adventure>())
        {
            if (row >= 80)
            {
                break;
            }

            row += 1;

            if (adventure.Level.ValueNullable?.Territory.RowId != territory)
            {
                continue;
            }

            unsafe
            {
                var playerState = PlayerState.Instance();
                if (playerState != null && playerState->IsAdventureComplete((uint)(row - 1)))
                {
                    continue;
                }
            }


            var loc = adventure.Level.Value;
            var pos = new Vector3(loc.X, loc.Z, loc.Y + 0.5f);

            vfxService.QueueSpawn((ushort)row, MarkerPath, pos, Quaternion.Zero);
        }
    }
}
