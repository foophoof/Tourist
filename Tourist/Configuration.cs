using System;
using Dalamud.Configuration;

namespace Tourist {
    [Serializable]
    public class Configuration : IPluginConfiguration {
        private Plugin Plugin { get; set; } = null!;

        public int Version { get; set; } = 1;

        public bool ShowFinished { get; set; } = true;
        public bool ShowUnavailable { get; set; } = true;
        public bool ShowArrVistas { get; set; } = true;
        public bool OnlyShowCurrentZone { get; set; }
        public bool ShowTimeUntilAvailable { get; set; } = true;
        public bool ShowTimeLeft { get; set; } = true;
        public SortMode SortMode { get; set; } = SortMode.Number;

        internal void Initialise(Plugin plugin) {
            this.Plugin = plugin;
        }

        internal void Save() {
            Plugin.Interface.SavePluginConfig(this);
        }
    }

    [Serializable]
    public enum SortMode {
        Number,
        Zone,
    }
}
