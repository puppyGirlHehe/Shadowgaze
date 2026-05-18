using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface.Windowing;
using Lumina.Excel.Sheets;

namespace Shadowgaze;

internal class MainWindow : Window {
    private Plugin Plugin { get; }

    private readonly List<IPlayerCharacter> _nearbyPlayers = [];
    private string _search = string.Empty;
    private string _selectedName = string.Empty;

    internal MainWindow(Plugin plugin) : base("Shadowgaze") {
        Plugin = plugin;
        Size = new Vector2(420, 420);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw() {
        var watcher = Plugin.Watcher;

        RefreshNearbyPlayers();

        // Header — watched player status
        if (watcher.IsWatching) {
            var rangeTag = watcher.WatchedPlayerInRange ? string.Empty : " (out of range)";
            ImGui.TextUnformatted($"Watching: {watcher.WatchedPlayerName}{rangeTag}");
            ImGui.SameLine();
            if (ImGui.SmallButton("Stop")) watcher.StopWatching();
        } else {
            ImGui.TextDisabled("Not watching anyone.");
        }

        ImGui.Separator();

        // Footer height: search row + list + watch button
        var listHeight = ImGui.GetTextLineHeightWithSpacing() * 3.5f;
        var footerHeight = ImGui.GetFrameHeightWithSpacing()   // search row
                         + listHeight
                         + ImGui.GetStyle().ItemSpacing.Y
                         + ImGui.GetFrameHeightWithSpacing();  // watch button

        // Scrollable log
        var logHeight = ImGui.GetContentRegionAvail().Y - footerHeight - ImGui.GetStyle().ItemSpacing.Y;
        ImGui.BeginChild("##log", new Vector2(-1, logHeight));
        if (watcher.Entries.Count == 0) {
            ImGui.TextDisabled(watcher.IsWatching ? "No targets logged yet." : "Search for a player below and click Watch.");
        } else {
            foreach (var entry in watcher.Entries)
                DrawEntry(entry);
        }
        ImGui.EndChild();

        ImGui.Separator();

        DrawFooter();
    }

    private void DrawFooter() {
        // Row 1: search input + Clear Log button
        var clearWidth = ImGui.CalcTextSize("Clear Log").X + ImGui.GetStyle().FramePadding.X * 2;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - clearWidth - ImGui.GetStyle().ItemSpacing.X);
        ImGui.InputTextWithHint("##search", "Search players...", ref _search, 64);
        ImGui.SameLine();
        if (ImGui.Button("Clear Log")) Plugin.Watcher.ClearLog();

        // Row 2: filtered player list
        var listHeight = ImGui.GetTextLineHeightWithSpacing() * 3.5f;
        ImGui.BeginChild("##playerlist", new Vector2(-1, listHeight));

        var filtered = string.IsNullOrWhiteSpace(_search)
            ? _nearbyPlayers
            : _nearbyPlayers.Where(p => p.Name.TextValue.Contains(_search, StringComparison.OrdinalIgnoreCase)).ToList();

        if (filtered.Count == 0) {
            ImGui.TextDisabled(_nearbyPlayers.Count == 0 ? "No players nearby." : "No matches.");
        } else {
            foreach (var p in filtered) {
                var isSelected = p.Name.TextValue == _selectedName;
                if (ImGui.Selectable(p.Name.TextValue, isSelected))
                    _selectedName = p.Name.TextValue;
                if (isSelected)
                    ImGui.SetItemDefaultFocus();
            }
        }
        ImGui.EndChild();

        // Row 3: Watch button
        var selected = _nearbyPlayers.FirstOrDefault(p => p.Name.TextValue == _selectedName);
        if (selected == null) ImGui.BeginDisabled();
        if (ImGui.Button("Watch", new Vector2(-1, 0)) && selected != null)
            Plugin.Watcher.Watch(selected);
        if (selected == null) ImGui.EndDisabled();
    }

    private void RefreshNearbyPlayers() {
        _nearbyPlayers.Clear();
        var localId = Service.ObjectTable.LocalPlayer?.GameObjectId;
        foreach (var obj in Service.ObjectTable) {
            if (obj is IPlayerCharacter pc && obj.GameObjectId != localId)
                _nearbyPlayers.Add(pc);
        }
        _nearbyPlayers.Sort((a, b) => string.Compare(a.Name.TextValue, b.Name.TextValue, StringComparison.Ordinal));
    }

    private void DrawEntry(TargetEntry entry) {
        if (Plugin.Config.ShowTimestamps) {
            ImGui.TextDisabled(entry.When.ToString("HH:mm:ss"));
            ImGui.SameLine();
        }

        ImGui.TextColored(KindColor(entry.TargetKind), $"[{KindLabel(entry.TargetKind)}]");
        ImGui.SameLine();

        if (entry.TargetKind == ObjectKind.Pc && entry.TargetHomeWorldId != 0) {
            var world = Service.DataManager.GetExcelSheet<World>()?.GetRow(entry.TargetHomeWorldId);
            var worldName = world?.Name.ToString() ?? "??";
            ImGui.TextUnformatted($"{entry.TargetName} ({worldName})");
        } else {
            ImGui.TextUnformatted(entry.TargetName);
        }
    }

    private static string KindLabel(ObjectKind kind) => kind switch {
        ObjectKind.Pc             => "Player",
        ObjectKind.BattleNpc      => "Enemy",
        ObjectKind.EventNpc       => "NPC",
        ObjectKind.Treasure       => "Chest",
        ObjectKind.Aetheryte      => "Aetheryte",
        ObjectKind.GatheringPoint => "Node",
        ObjectKind.EventObj       => "Object",
        _                         => kind.ToString(),
    };

    private static Vector4 KindColor(ObjectKind kind) => kind switch {
        ObjectKind.Pc        => new Vector4(0.4f, 0.8f, 1.0f, 1f),
        ObjectKind.BattleNpc => new Vector4(1.0f, 0.4f, 0.4f, 1f),
        ObjectKind.EventNpc  => new Vector4(0.9f, 0.8f, 0.4f, 1f),
        _                    => new Vector4(0.7f, 0.7f, 0.7f, 1f),
    };
}
