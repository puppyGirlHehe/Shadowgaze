using System;
using Dalamud.Game.ClientState.Objects.Enums;

namespace Shadowgaze;

internal record TargetEntry(
    DateTime When,
    string TargetName,
    ObjectKind TargetKind,
    uint TargetHomeWorldId
);
