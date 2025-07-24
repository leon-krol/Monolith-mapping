// SPDX-FileCopyrightText: 2025 Redrover1760
// SPDX-FileCopyrightText: 2025 metalgearsloth
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared.Light.Components;

/// <summary>
/// Counts the tile this entity on as being rooved.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class IsRoofComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// Color for this roof. If null then falls back to the grid's color.
    /// </summary>
    /// <remarks>
    /// If a tile is marked as rooved then the tile color will be used over any entity's colors on the tile.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public Color? Color;
}
