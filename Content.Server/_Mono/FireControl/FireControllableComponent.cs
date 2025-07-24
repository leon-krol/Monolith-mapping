// SPDX-FileCopyrightText: 2025 Ark
// SPDX-FileCopyrightText: 2025 ark1368
// SPDX-FileCopyrightText: 2025 starch
//
// SPDX-License-Identifier: AGPL-3.0-or-later

// Copyright Rane (elijahrane@gmail.com) 2025
// All rights reserved. Relicensed under AGPL with permission

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Mono.FireControl;

[RegisterComponent]
public sealed partial class FireControllableComponent : Component
{
    /// <summary>
    /// Reference to the controlling server, if any.
    /// </summary>
    [ViewVariables]
    public EntityUid? ControllingServer = null;

    /// <summary>
    /// When the weapon can next be fired
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextFire = TimeSpan.Zero;

    /// <summary>
    /// Cooldown between firing, in seconds
    /// </summary>
    [DataField]
    public float FireCooldown = 0.2f;
}
