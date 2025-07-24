// SPDX-FileCopyrightText: 2021 Paul Ritter
// SPDX-FileCopyrightText: 2022 Leon Friedrich
// SPDX-FileCopyrightText: 2022 ShadowCommander
// SPDX-FileCopyrightText: 2022 ZeroDayDaemon
// SPDX-FileCopyrightText: 2022 wrexbe
// SPDX-FileCopyrightText: 2024 Plykiya
// SPDX-FileCopyrightText: 2025 Ark
// SPDX-FileCopyrightText: 2025 core-mene
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared.Inventory;

/// <summary>
///     Defines what slot types an item can fit into.
/// </summary>
[Serializable, NetSerializable]
[Flags]
public enum SlotFlags
{
    NONE = 0,
    PREVENTEQUIP = 1 << 0,
    HEAD = 1 << 1,
    EYES = 1 << 2,
    EARS = 1 << 3,
    MASK = 1 << 4,
    OUTERCLOTHING = 1 << 5,
    INNERCLOTHING = 1 << 6,
    NECK = 1 << 7,
    BACK = 1 << 8,
    BELT = 1 << 9,
    GLOVES = 1 << 10,
    IDCARD = 1 << 11,
    POCKET = 1 << 12,
    LEGS = 1 << 13, // Frontier: unused
    FEET = 1 << 14,
    SUITSTORAGE = 1 << 15,
    WALLET = 1 << 16, // Frontier: using an unused slot, redefine to a new bit if/when it's used (goodbye ushort)
    BALACLAVA = 1 << 17, // Mono start
    ARMBANDRIGHT = 1 << 18,
    ARMBANDLEFT = 1 << 19,
    HELMETCOVER = 1 << 20,
    HELMETATTACHMENT = 1 << 21, //Mono end
    All = ~NONE,

    WITHOUT_POCKET = All & ~POCKET
}