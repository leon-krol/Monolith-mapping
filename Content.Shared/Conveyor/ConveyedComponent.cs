// SPDX-FileCopyrightText: 2025 metalgearsloth
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared.Conveyor;

/// <summary>
/// Indicates this entity is currently contacting a conveyor and will subscribe to events as appropriate.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ConveyedComponent : Component
{
    // TODO: Delete if pulling gets fixed.
    /// <summary>
    /// True if currently conveying.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Conveying;
}
