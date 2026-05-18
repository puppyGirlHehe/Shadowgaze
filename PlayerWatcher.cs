using System;
using System.Collections.Generic;
using System.Diagnostics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;

namespace Shadowgaze;

internal class PlayerWatcher : IDisposable {
    private Plugin Plugin { get; }
    private Stopwatch UpdateWatch { get; } = new();

    public ulong? WatchedGameObjectId { get; private set; }
    public string WatchedPlayerName { get; private set; } = string.Empty;
    public bool WatchedPlayerInRange { get; private set; }
    public bool IsWatching => WatchedGameObjectId.HasValue;

    private ulong LastTargetId { get; set; } = ulong.MaxValue;

    private List<TargetEntry> Log { get; } = [];
    public IReadOnlyList<TargetEntry> Entries => Log;

    internal PlayerWatcher(Plugin plugin) {
        Plugin = plugin;
        UpdateWatch.Start();
        Service.Framework.Update += OnFrameworkUpdate;
    }

    public void Dispose() {
        Service.Framework.Update -= OnFrameworkUpdate;
    }

    public void Watch(IPlayerCharacter player) {
        WatchedGameObjectId = player.GameObjectId;
        WatchedPlayerName = player.Name.TextValue;
        WatchedPlayerInRange = true;
        LastTargetId = ulong.MaxValue;
    }

    public void StopWatching() {
        WatchedGameObjectId = null;
        WatchedPlayerName = string.Empty;
        WatchedPlayerInRange = false;
        LastTargetId = ulong.MaxValue;
    }

    public void ClearLog() => Log.Clear();

    private void OnFrameworkUpdate(IFramework framework) {
        if (UpdateWatch.Elapsed < TimeSpan.FromMilliseconds(Plugin.Config.PollFrequency)) return;
        UpdateWatch.Restart();
        Update();
    }

    private void Update() {
        if (!WatchedGameObjectId.HasValue) return;

        var actor = Service.ObjectTable.SearchById(WatchedGameObjectId.Value);
        if (actor == null) {
            WatchedPlayerInRange = false;
            return;
        }

        WatchedPlayerInRange = true;

        var currentTargetId = actor.TargetObjectId;
        if (currentTargetId == LastTargetId) return;

        var target = Service.ObjectTable.SearchById(currentTargetId);
        if (target == null) return; // deselected or out of range — skip without updating LastTargetId

        LastTargetId = currentTargetId;

        uint homeWorldId = target is IPlayerCharacter pc ? pc.HomeWorld.RowId : 0;
        Log.Insert(0, new TargetEntry(DateTime.Now, target.Name.TextValue, target.ObjectKind, homeWorldId));

        while (Log.Count > Plugin.Config.MaxLogEntries)
            Log.RemoveAt(Log.Count - 1);
    }
}
