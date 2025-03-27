using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVWeather.Lumina;

namespace Tourist {
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Plugin : IDalamudPlugin {
        public static string Name => "Tourist";

        [PluginService]
        public static IPluginLog Log { get; private set; } = null!;

        [PluginService]
        internal static IDalamudPluginInterface Interface { get; private set; }

        [PluginService]
        internal static IClientState ClientState { get; private set; } = null!;

        [PluginService]
        internal static ICommandManager CommandManager { get; private set; } = null!;

        [PluginService]
        internal static IDataManager DataManager { get; private set; } = null!;

        [PluginService]
        internal static IFramework Framework { get; private set; } = null!;

        [PluginService]
        internal static IGameGui GameGui { get; private set; } = null!;

        [PluginService]
        internal static ISigScanner SigScanner { get; private set; } = null!;

        [PluginService]
        internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;

        internal Configuration Config { get; }
        internal PluginUi Ui { get; }
        internal FFXIVWeatherLuminaService Weather { get; }
        internal GameFunctions Functions { get; }
        private Commands Commands { get; }
        internal Markers Markers { get; }

        public Plugin(IDalamudPluginInterface pluginInterface) {
            Interface = pluginInterface;

            this.Config = Interface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Config.Initialise(this);

            this.Weather = new FFXIVWeatherLuminaService(DataManager.GameData);

            this.Functions = new GameFunctions(this);

            this.Markers = new Markers(this);

            this.Ui = new PluginUi(this);

            this.Commands = new Commands(this);
        }

        public void Dispose() {
            this.Commands.Dispose();
            this.Ui.Dispose();
            this.Markers.Dispose();
            this.Functions.Dispose();
        }
    }
}
