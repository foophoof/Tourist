using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace Tourist.Util;

public static class IGameGuiExt
{
    public static void OpenMapLocation(this IGameGui gameGui, Adventure adventure)
    {
        var loc = adventure.Level.Value;
        var map = loc.Map.Value;
        var terr = map.TerritoryType.Value;

        if (terr.RowId == 0)
        {
            return;
        }

        var mapLink = new MapLinkPayload(
            terr.RowId,
            map.RowId,
            (int)(loc.X * 1_000f),
            (int)(loc.Z * 1_000f)
        );

        gameGui.OpenMapWithMapLink(mapLink);
    }
}
