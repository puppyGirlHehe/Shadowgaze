using System;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace Shadowgaze;

[Serializable]
internal class Configuration : IPluginConfiguration {
    public int Version { get; set; } = 1;

    private IDalamudPluginInterface Interface { get; set; } = null!;

    public int PollFrequency { get; set; } = 100;
    public int MaxLogEntries { get; set; } = 200;
    public bool ShowTimestamps { get; set; } = true;

    public void Initialize(IDalamudPluginInterface pluginInterface) {
        Interface = pluginInterface;
    }

    public void Save() => Interface.SavePluginConfig(this);
}
