using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace Tourist {
    public class Markers : IDisposable {
        private const string MarkerPath = "bgcommon/world/common/vfx_for_live/eff/b0810_tnsk_y.avfx";

        private Plugin Plugin { get; }
        private Dictionary<uint, nint> Spawned { get; } = new();
        private HashSet<nint> Queue { get; } = new();

        public Markers(Plugin plugin) {
            this.Plugin = plugin;

            this.Plugin.ClientState.TerritoryChanged += this.OnTerritoryChange;
            this.Plugin.Framework.Update += this.OnFrameworkUpdate;

            if (this.Plugin.Config.ShowArrVistas) {
                this.SpawnVfxForCurrentZone(this.Plugin.ClientState.TerritoryType);
            }
        }

        public void Dispose() {
            this.Plugin.Framework.Update -= this.OnFrameworkUpdate;
            this.Plugin.ClientState.TerritoryChanged -= this.OnTerritoryChange;
            this.RemoveAllVfx();
        }

        internal void RemoveVfx(ushort index) {
            var adventure = this.Plugin.DataManager.GetExcelSheet<Adventure>()!
                .Skip(index)
                .First();

            if (!this.Spawned.TryGetValue(adventure.RowId, out var vfx)) {
                return;
            }

            this.Plugin.Functions.RemoveVfx(vfx);
            this.Spawned.Remove(adventure.RowId);
        }

        internal void RemoveAllVfx() {
            foreach (var vfx in this.Spawned.Values) {
                this.Plugin.Functions.RemoveVfx(vfx);
            }

            this.Spawned.Clear();
        }

        internal void SpawnVfxForCurrentZone(ushort territory) {
            var row = 0;
            foreach (var adventure in this.Plugin.DataManager.GetExcelSheet<Adventure>()!) {
                if (row >= 80) {
                    break;
                }

                row += 1;

                if (adventure.Level.Value!.Territory.RowId != territory) {
                    continue;
                }

                if (this.Plugin.Functions.HasVistaUnlocked((short) (row - 1))) {
                    continue;
                }

                var loc = adventure.Level.Value;
                var pos = new Vector3(loc.X, loc.Z, loc.Y + 0.5f);
                var vfx = this.Plugin.Functions.SpawnVfx(MarkerPath, pos);
                this.Spawned[adventure.RowId] = vfx;
                this.Queue.Add(vfx);
            }
        }

        private void OnTerritoryChange(ushort territory) {
            if (!this.Plugin.Config.ShowArrVistas) {
                return;
            }

            try {
                this.RemoveAllVfx();
                this.SpawnVfxForCurrentZone(territory);
            } catch (Exception ex) {
                Plugin.Log.Error(ex, "Exception in territory change");
            }
        }

        private void OnFrameworkUpdate(IFramework framework1) {
            foreach (var vfx in this.Queue.ToArray()) {
                if (this.Plugin.Functions.PlayVfx(vfx)) {
                    this.Queue.Remove(vfx);
                }
            }
        }
    }
}
