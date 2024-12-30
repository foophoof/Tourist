using System;
using System.Linq;
using System.Numerics;
using Dalamud.Utility;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace Tourist {
    public class PluginUi : IDisposable {
        private Plugin Plugin { get; }

        private bool _show;

        internal bool Show {
            get => this._show;
            set => this._show = value;
        }

        public PluginUi(Plugin plugin) {
            this.Plugin = plugin;

            this.Plugin.Interface.UiBuilder.Draw += this.Draw;
            this.Plugin.Interface.UiBuilder.OpenConfigUi += this.OpenConfig;
        }

        public void Dispose() {
            this.Plugin.Interface.UiBuilder.OpenConfigUi -= this.OpenConfig;
            this.Plugin.Interface.UiBuilder.Draw -= this.Draw;
        }

        private void OpenConfig() {
            this.Show = true;
        }

        private void Draw() {
            ImGui.SetNextWindowSize(new Vector2(350f, 450f), ImGuiCond.FirstUseEver);

            if (!this.Show) {
                return;
            }

            if (!ImGui.Begin(Plugin.Name, ref this._show, ImGuiWindowFlags.MenuBar)) {
                ImGui.End();
                return;
            }

            if (ImGui.BeginMenuBar()) {
                if (ImGui.BeginMenu("Options")) {
                    if (ImGui.BeginMenu("Sort by")) {
                        foreach (var mode in (SortMode[]) Enum.GetValues(typeof(SortMode))) {
                            if (!ImGui.MenuItem($"{mode}", null, this.Plugin.Config.SortMode == mode)) {
                                continue;
                            }

                            this.Plugin.Config.SortMode = mode;
                            this.Plugin.Config.Save();
                        }

                        ImGui.EndMenu();
                    }

                    if (ImGui.BeginMenu("Times")) {
                        var showTimeUntil = this.Plugin.Config.ShowTimeUntilAvailable;
                        if (ImGui.MenuItem("Show time until available", null, ref showTimeUntil)) {
                            this.Plugin.Config.ShowTimeUntilAvailable = showTimeUntil;
                            this.Plugin.Config.Save();
                        }

                        var showTimeLeft = this.Plugin.Config.ShowTimeLeft;
                        if (ImGui.MenuItem("Show time left", null, ref showTimeLeft)) {
                            this.Plugin.Config.ShowTimeLeft = showTimeLeft;
                            this.Plugin.Config.Save();
                        }

                        ImGui.EndMenu();
                    }

                    if (ImGui.BeginMenu("Visibility")) {
                        var showFinished = this.Plugin.Config.ShowFinished;
                        if (ImGui.MenuItem("Show finished", null, ref showFinished)) {
                            this.Plugin.Config.ShowFinished = showFinished;
                            this.Plugin.Config.Save();
                        }

                        var showUnavailable = this.Plugin.Config.ShowUnavailable;
                        if (ImGui.MenuItem("Show unavailable", null, ref showUnavailable)) {
                            this.Plugin.Config.ShowUnavailable = showUnavailable;
                            this.Plugin.Config.Save();
                        }

                        var onlyCurrent = this.Plugin.Config.OnlyShowCurrentZone;
                        if (ImGui.MenuItem("Show current zone only", null, ref onlyCurrent)) {
                            this.Plugin.Config.OnlyShowCurrentZone = onlyCurrent;
                            this.Plugin.Config.Save();
                        }

                        ImGui.EndMenu();
                    }

                    var showArrVistas = this.Plugin.Config.ShowArrVistas;
                    if (ImGui.MenuItem("Add markers for ARR vistas", null, ref showArrVistas)) {
                        this.Plugin.Config.ShowArrVistas = showArrVistas;
                        this.Plugin.Config.Save();

                        if (showArrVistas) {
                            var territory = this.Plugin.ClientState.TerritoryType;
                            this.Plugin.Markers.SpawnVfxForCurrentZone(territory);
                        } else {
                            this.Plugin.Markers.RemoveAllVfx();
                        }
                    }

                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Help")) {
                    if (ImGui.BeginMenu("Can't unlock vistas 21 to 80")) {
                        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 10);
                        ImGui.TextUnformatted("Vistas 21 to 80 require the completion of the first 20. Talk to Millith Ironheart in Old Gridania to unlock the rest.");
                        ImGui.PopTextWrapPos();

                        ImGui.EndMenu();
                    }

                    ImGui.EndMenu();
                }

                ImGui.EndMenuBar();
            }

            if (ImGui.BeginChild("tourist-adventures", new Vector2(0, 0))) {
                const uint first = 2162688;

                var adventures = this.Plugin.DataManager.GetExcelSheet<Adventure>()!
                    .Select(adventure => (idx: adventure.RowId - first, adventure))
                    .OrderBy(entry => this.Plugin.Config.SortMode switch {
                        SortMode.Number => entry.idx,
                        SortMode.Zone => entry.adventure.Level.Value!.Map.RowId,
                        _ => throw new ArgumentOutOfRangeException(),
                    });

                Map? lastMap = null;
                var lastTree = false;

                foreach (var (idx, adventure) in adventures) {
                    if (this.Plugin.Config.OnlyShowCurrentZone && adventure.Level.Value!.Territory.RowId != this.Plugin.ClientState.TerritoryType) {
                        continue;
                    }

                    var has = this.Plugin.Functions.HasVistaUnlocked((short) idx);

                    if (!this.Plugin.Config.ShowFinished && has) {
                        continue;
                    }

                    var available = adventure.Available(this.Plugin.Weather);

                    if (!this.Plugin.Config.ShowUnavailable && !available) {
                        continue;
                    }

                    if (this.Plugin.Config.SortMode == SortMode.Zone) {
                        var map = adventure.Level.Value!.Map.ValueNullable;
                        if (lastMap.GetValueOrDefault().Id != map.GetValueOrDefault().Id) {
                            if (lastMap != null) {
                                ImGui.TreePop();
                            }

                            lastTree = ImGui.CollapsingHeader($"{map!.Value.PlaceName.Value!.Name.ExtractText()}");
                            ImGui.TreePush();
                        }

                        lastMap = map;

                        if (!lastTree) {
                            continue;
                        }
                    }

                    var availability = adventure.NextAvailable(this.Plugin.Weather);

                    DateTimeOffset? countdown = null;
                    if (has) {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.8f, 0.8f, 0.8f, 1f));
                    } else if (available) {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0f, 1f, 0f, 1f));
                        if (this.Plugin.Config.ShowTimeLeft) {
                            countdown = availability?.end;
                        }
                    } else if (availability != null && this.Plugin.Config.ShowTimeUntilAvailable) {
                        countdown = availability.Value.start;
                    }

                    var next = countdown == null
                        ? string.Empty
                        : $" ({(countdown.Value - DateTimeOffset.UtcNow).ToHumanReadable()})";

                    var name = adventure.Name.ToDalamudString();
                    var header = ImGui.CollapsingHeader($"#{idx + 1} - {name.TextValue}{next}###adventure-{adventure.RowId}");

                    if (has || available) {
                        ImGui.PopStyleColor();
                    }

                    if (!header) {
                        continue;
                    }

                    ImGui.Columns(2);
                    ImGui.SetColumnWidth(0, ImGui.CalcTextSize("Eorzea time").X + ImGui.GetStyle().ItemSpacing.X * 2);

                    ImGui.TextUnformatted("Command");
                    ImGui.NextColumn();

                    ImGui.TextUnformatted(adventure.Emote.Value.TextCommand.Value.Command.ExtractText() ?? "<unk>");
                    ImGui.NextColumn();

                    ImGui.TextUnformatted("Eorzea time");
                    ImGui.NextColumn();

                    if (adventure.MinTime != 0 || adventure.MaxTime != 0) {
                        ImGui.TextUnformatted($"{adventure.MinTime / 100:00}:00 to {adventure.MaxTime / 100 + 1:00}:00");
                    } else {
                        ImGui.TextUnformatted("Any");
                    }

                    ImGui.NextColumn();

                    ImGui.TextUnformatted("Weather");
                    ImGui.NextColumn();

                    if (Weathers.All.TryGetValue(adventure.RowId, out var weathers)) {
                        var weatherString = string.Join(", ", weathers
                            .OrderBy(id => id)
                            .Select(id => this.Plugin.DataManager.GetExcelSheet<Weather>()!.GetRowOrDefault(id))
                            .Where(weather => weather != null && weather.Value.RowId != 0)
                            .Select(weather => weather!.Value.Name));
                        ImGui.TextUnformatted(weatherString);
                    } else {
                        ImGui.TextUnformatted("Any");
                    }

                    ImGui.Columns();

                    if (ImGui.Button($"Open map##{adventure.RowId}")) {
                        this.Plugin.GameGui.OpenMapLocation(adventure);
                    }
                }

                ImGui.EndChild();
            }

            ImGui.End();
        }
    }
}
