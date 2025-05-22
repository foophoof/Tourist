using Dalamud.Configuration;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Tourist.Config;

public partial class PluginConfig : IPluginConfiguration
{
    [JsonIgnore] public const int CurrentConfigVersion = 1;
    [JsonIgnore] private static IDalamudPluginInterface? _pluginInterface;
    [JsonIgnore] private static IPluginLog? _pluginLog;
    [JsonIgnore] public int LastSavedConfigHash { get; set; }
    [JsonIgnore] public static JsonSerializerOptions? SerializerOptions { get; private set; } = new() {IncludeFields = true, WriteIndented = true};

    public static PluginConfig Load(IServiceProvider serviceProvider)
    {
        _pluginInterface = serviceProvider.GetRequiredService<IDalamudPluginInterface>();
        _pluginLog = serviceProvider.GetRequiredService<IPluginLog>();

        var fileInfo = _pluginInterface.ConfigFile;
        if (!fileInfo.Exists || fileInfo.Length < 2)
        {
            return new PluginConfig();
        }

        var json = File.ReadAllText(fileInfo.FullName);
        var node = JsonNode.Parse(json);
        if (node is not JsonObject config)
        {
            return new PluginConfig();
        }

        var version = config[nameof(Version)]?.GetValue<int>();
        if (version == null)
        {
            return new PluginConfig();
        }

        return node.Deserialize<PluginConfig>(SerializerOptions) ?? new PluginConfig();
    }

    public void Save()
    {
        try
        {
            var serialized = JsonSerializer.Serialize(this, SerializerOptions);
            var hash = serialized.GetHashCode();

            if (LastSavedConfigHash == hash)
                return;

            FilesystemUtil.WriteAllTextSafe(_pluginInterface!.ConfigFile.FullName, serialized);
            LastSavedConfigHash = hash;
            _pluginLog?.Information("Configuration saved.");
        }
        catch (Exception e)
        {
            _pluginLog?.Error(e, "Error saving config");
        }
    }
}

[SuppressMessage("ReSharper", "RedundantDefaultMemberInitializer")]
public partial class PluginConfig
{
    public bool OnlyShowCurrentZone = false;
    public bool ShowArrVistas = true;
    public bool ShowFinished = true;
    public bool ShowTimeLeft = true;
    public bool ShowTimeUntilAvailable = true;
    public bool ShowUnavailable = true;
    public SortMode SortMode = SortMode.Number;

    public int Version { get; set; } = CurrentConfigVersion;
}

[Serializable]
public enum SortMode
{
    Number,
    Zone,
}
