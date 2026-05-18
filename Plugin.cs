using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;

namespace Shadowgaze;

public class Plugin : IDalamudPlugin {
    internal Configuration Config { get; }
    internal PlayerWatcher Watcher { get; }

    private WindowSystem WindowSystem { get; } = new("Shadowgaze");
    private MainWindow MainWindow { get; }

    public Plugin(IDalamudPluginInterface pluginInterface) {
        pluginInterface.Create<Service>();

        Config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Config.Initialize(Service.Interface);

        Watcher = new PlayerWatcher(this);
        MainWindow = new MainWindow(this);
        WindowSystem.AddWindow(MainWindow);

        Service.CommandManager.AddHandler("/shadowgaze", new CommandInfo(OnCommand) {
            HelpMessage = "Toggle the Shadowgaze window",
        });
        Service.CommandManager.AddHandler("/sgaze", new CommandInfo(OnCommand) {
            HelpMessage = "Alias for /shadowgaze",
        });

        Service.Interface.UiBuilder.Draw += WindowSystem.Draw;
        Service.Interface.UiBuilder.OpenMainUi += MainWindow.Toggle;
        Service.Interface.UiBuilder.OpenConfigUi += MainWindow.Toggle;
    }

    public void Dispose() {
        Service.Interface.UiBuilder.OpenConfigUi -= MainWindow.Toggle;
        Service.Interface.UiBuilder.OpenMainUi -= MainWindow.Toggle;
        Service.Interface.UiBuilder.Draw -= WindowSystem.Draw;
        Service.CommandManager.RemoveHandler("/sgaze");
        Service.CommandManager.RemoveHandler("/shadowgaze");
        Watcher.Dispose();
    }

    private void OnCommand(string command, string args) => MainWindow.Toggle();
}
