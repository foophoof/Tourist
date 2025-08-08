using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVWeather.Lumina;
using Dalamud.Bindings.ImGui;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Tourist.Config;
using Tourist.Services;
using Tourist.Util;

namespace Tourist.Windows;

public sealed class MainWindow : Window, IDisposable
{
    private readonly ExcelSheet<Adventure> _adventureSheet;
    private readonly IClientState _clientState;
    private readonly ConfigurationLoaderService _configurationLoaderService;
    private readonly IDataManager _dataManager;
    private readonly IGameGui _gameGui;
    private readonly MarkerService _markerService;
    private readonly PluginConfig _pluginConfig;
    private readonly FFXIVWeatherLuminaService _weatherLuminaService;
    private readonly ExcelSheet<Weather> _weatherSheet;

    public MainWindow(
        IClientState clientState,
        IDataManager dataManager,
        IGameGui gameGui,
        MarkerService markerService,
        PluginConfig pluginConfig,
        FFXIVWeatherLuminaService weatherLuminaService,
        ConfigurationLoaderService configurationLoaderService,
        ExcelSheet<Adventure> adventureSheet,
        ExcelSheet<Weather> weatherSheet) : base("Tourist##MainWindow", ImGuiWindowFlags.MenuBar)
    {
        _dataManager = dataManager;
        _pluginConfig = pluginConfig;
        _clientState = clientState;
        _gameGui = gameGui;
        _weatherLuminaService = weatherLuminaService;
        _markerService = markerService;
        _configurationLoaderService = configurationLoaderService;
        _adventureSheet = adventureSheet;
        _weatherSheet = weatherSheet;

        Size = new Vector2(350, 450);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose() { }

    public override void Draw()
    {
        DrawMenuBar();

        var adventures = GetAdventures();

        foreach (var group in adventures)
        {
            if (_pluginConfig.SortMode == SortMode.Zone)
            {
                var zoneName = group.First().row.Level.ValueNullable?.Map.ValueNullable?.PlaceName.ValueNullable?.Name.ExtractText()
                    .StripSoftHyphen().FirstCharToUpper();
                if (!ImGui.CollapsingHeader($"{zoneName}##group-{group.Key}"))
                    continue;

                using var indent = ImRaii.PushIndent();
                DrawGroup(group);
            }
            else
            {
                DrawGroup(group);
            }
        }
    }

    private void DrawMenuBar()
    {
        using var menuBar = ImRaii.MenuBar();
        if (!menuBar)
            return;

        DrawOptionsMenu();
        DrawHelpMenu();
    }

    private void DrawOptionsMenu()
    {
        using var menu = ImRaii.Menu("Options");
        if (!menu)
            return;

        DrawSortByMenu();
        DrawTimesMenu();
        DrawVisibilityMenu();
        DrawArrVistasMenuItem();
    }

    private void DrawSortByMenu()
    {
        using var menu = ImRaii.Menu("Sort by");
        if (!menu)
            return;

        foreach (var mode in Enum.GetValues<SortMode>())
        {
            if (!ImGui.MenuItem($"{mode}", _pluginConfig.SortMode == mode))
                continue;

            _pluginConfig.SortMode = mode;
            _configurationLoaderService.Save();
        }
    }

    private void DrawTimesMenu()
    {
        using var menu = ImRaii.Menu("Times");
        if (!menu)
            return;

        var showTimeUntilAvailable = _pluginConfig.ShowTimeUntilAvailable;
        if (ImGui.MenuItem("Show time until available", ref showTimeUntilAvailable))
        {
            _pluginConfig.ShowTimeUntilAvailable = showTimeUntilAvailable;
            _configurationLoaderService.Save();
        }

        var showTimeLeft = _pluginConfig.ShowTimeLeft;
        if (ImGui.MenuItem("Show time left", ref showTimeLeft))
        {
            _pluginConfig.ShowTimeLeft = showTimeLeft;
            _configurationLoaderService.Save();
        }
    }

    private void DrawVisibilityMenu()
    {
        var showFinished = _pluginConfig.ShowFinished;
        if (ImGui.MenuItem("Show finished", ref showFinished))
        {
            _pluginConfig.ShowFinished = showFinished;
            _configurationLoaderService.Save();
        }

        var showUnavailable = _pluginConfig.ShowUnavailable;
        if (ImGui.MenuItem("Show unavailable", ref showUnavailable))
        {
            _pluginConfig.ShowUnavailable = showUnavailable;
            _configurationLoaderService.Save();
        }

        var onlyShowCurrentZone = _pluginConfig.OnlyShowCurrentZone;
        if (ImGui.MenuItem("Show current zone only", ref onlyShowCurrentZone))
        {
            _pluginConfig.OnlyShowCurrentZone = onlyShowCurrentZone;
            _configurationLoaderService.Save();
        }
    }

    private void DrawArrVistasMenuItem()
    {
        var showArrVistas = _pluginConfig.ShowArrVistas;
        if (!ImGui.MenuItem("Add markers for ARR vistas", ref showArrVistas))
            return;

        _pluginConfig.ShowArrVistas = showArrVistas;
        _configurationLoaderService.Save();

        if (showArrVistas)
        {
            _markerService.SpawnVfxForCurrentZone();
        }
        else
        {
            _markerService.RemoveAllVfx();
        }
    }

    private void DrawHelpMenu()
    {
        using var menu = ImRaii.Menu("Help");
        if (!menu)
            return;

        using var vistaUnlockMenu = ImRaii.Menu("Can't unlock vistas 21 to 80");
        if (!vistaUnlockMenu)
            return;

        using var textWrapPos = ImRaii.TextWrapPos(ImGui.GetFontSize() * 10);
        ImGui.TextUnformatted(
            "Vistas 21 to 80 require the completion of the first 20. Talk to Millith Ironheart in Old Gridania to unlock the rest.");

    }

    private void DrawGroup(IGrouping<uint, (Adventure row, int idx)> group)
    {
        foreach (var (adventure, idx) in group)
        {
            using var id = ImRaii.PushId((int)adventure.RowId);

            bool has;
            unsafe
            {
                var playerState = PlayerState.Instance();
                has = playerState != null && playerState->IsAdventureComplete((uint)idx);
            }

            var available = adventure.Available(_weatherLuminaService);
            var availability = adventure.NextAvailable(_weatherLuminaService);

            DateTimeOffset? countdown = null;
            Vector4? colour = null;
            if (has)
            {
                colour = new Vector4(0.8f, 0.8f, 0.8f, 1.0f);
            }
            else if (available)
            {
                colour = new Vector4(0f, 1f, 0f, 1f);
                if (_pluginConfig.ShowTimeLeft)
                {
                    countdown = availability?.end;
                }
            }
            else if (availability.HasValue && _pluginConfig.ShowTimeUntilAvailable)
            {
                countdown = availability.Value.start;
            }

            var next = countdown.HasValue ? $" ({(countdown.Value - DateTimeOffset.UtcNow).ToHumanReadable()})" : string.Empty;

            var name = adventure.Name.ToDalamudString();
            using (ImRaii.PushColor(ImGuiCol.Text, colour.GetValueOrDefault(), colour != null))
                if (!ImGui.CollapsingHeader($"#{idx + 1:000} - {name.TextValue}{next}###adventure-{adventure.RowId}"))
                    continue;

            using (var table = ImRaii.Table("table", 2))
            {
                if (table)
                {
                    ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed,
                        ImGui.CalcTextSize("Eorzea time").X + ImGui.GetStyle().ItemSpacing.X * 2);
                    ImGui.TableSetupColumn("Value");

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.TextUnformatted("Command");
                    ImGui.TableSetColumnIndex(1);
                    ImGui.TextUnformatted(adventure.Emote.ValueNullable?.TextCommand.ValueNullable?.Command.ExtractText() ?? "<unk>");

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.TextUnformatted("Eorzea time");
                    ImGui.TableSetColumnIndex(1);
                    if (adventure.MinTime != 0 || adventure.MaxTime != 0)
                    {
                        ImGui.TextUnformatted($"{adventure.MinTime / 100:00}:00 to {adventure.MaxTime / 100 + 1:00}:00");
                    }
                    else
                    {
                        ImGui.TextUnformatted("Any");
                    }

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.TextUnformatted("Weather");
                    ImGui.TableSetColumnIndex(1);
                    ImGui.TextUnformatted(Weathers.WeatherString(adventure.RowId, _dataManager));
                }
            }

            if (ImGui.Button("Open map"))
            {
                _gameGui.OpenMapLocation(adventure);
            }
        }
    }
    private string WeatherString(uint[] weathers)
    {
        var weatherString = string.Join(", ", weathers
            .OrderBy(id => id)
            .Select(id => _weatherSheet.GetRowOrDefault(id))
            .Where(weather => weather.HasValue && weather.Value.RowId != 0)
            .Select(weather => weather!.Value.Name));
        return weatherString;
    }

    private IEnumerable<IGrouping<uint, (Adventure row, int idx)>> GetAdventures()
    {
        return _adventureSheet
            .Select((row, idx) => (row, idx))
            .OrderBy(entry => _pluginConfig.SortMode switch
            {
                SortMode.Number => (uint)entry.idx,
                SortMode.Zone => entry.row.Level.Value.Map.RowId,
                _ => throw new ArgumentOutOfRangeException(),
            })
            .Where(ShouldShow)
            .GroupBy(entry => _pluginConfig.SortMode switch
            {
                SortMode.Number => (uint)entry.idx,
                SortMode.Zone => entry.row.Level.Value.Map.RowId,
                _ => throw new ArgumentOutOfRangeException(),
            });
    }

    private bool ShouldShow((Adventure row, int idx) entry)
    {
        if (_pluginConfig.OnlyShowCurrentZone && entry.row.Level.Value.Territory.RowId != _clientState.TerritoryType)
        {
            return false;
        }

        bool has;
        unsafe
        {
            var playerState = PlayerState.Instance();
            has = playerState != null && playerState->IsAdventureComplete((uint)entry.idx);
        }

        if (!_pluginConfig.ShowFinished && has)
        {
            return false;
        }

        var available = entry.row.Available(_weatherLuminaService);

        return _pluginConfig.ShowUnavailable || available;
    }
}
