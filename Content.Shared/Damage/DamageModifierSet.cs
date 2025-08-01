// SPDX-FileCopyrightText: 2022 mirrorcult
// SPDX-FileCopyrightText: 2022 wrexbe
// SPDX-FileCopyrightText: 2023 DrSmugleaf
// SPDX-FileCopyrightText: 2023 Leon Friedrich
// SPDX-FileCopyrightText: 2024 themias
// SPDX-FileCopyrightText: 2025 starch
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom; // goob change
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Damage
{
    /// <summary>
    ///     A set of coefficients or flat modifiers to damage types. Can be applied to <see cref="DamageSpecifier"/> using <see
    ///     cref="DamageSpecifier.ApplyModifierSet(DamageSpecifier, DamageModifierSet)"/>. This can be done several times as the
    ///     <see cref="DamageSpecifier"/> is passed to it's final target. By default the receiving <see cref="DamageableComponent"/>, will
    ///     also apply it's own <see cref="DamageModifierSet"/>.
    /// </summary>
    /// <remarks>
    /// The modifier will only ever be applied to damage that is being dealt. Healing is unmodified.
    /// </remarks>
    [DataDefinition]
    [Serializable, NetSerializable]
    [Virtual]
    public partial class DamageModifierSet
    {
        [DataField("coefficients", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<float, DamageTypePrototype>))]
        public Dictionary<string, float> Coefficients = new();

        [DataField("flatReductions", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<float, DamageTypePrototype>))]
        public Dictionary<string, float> FlatReduction = new();

        /// <summary>
        /// Goobstation.
        /// Whether this modifier set will ignore incoming damage partial armor penetration, positive or negative.
        /// Used mainly for species modifier sets.
        /// </summary>
        [DataField(customTypeSerializer: typeof(FlagSerializer<ArmorPierceFlags>))]
        public int IgnoreArmorPierceFlags = (int) PartialArmorPierceFlags.None;
    }

    // Goobstation start
    public sealed class ArmorPierceFlags;

    [Flags, Serializable]
    [FlagsFor(typeof(ArmorPierceFlags))]
    public enum PartialArmorPierceFlags
    {
        None = 0,
        Positive = 1 << 0,
        Negative = 1 << 1,
        All = Positive | Negative,
    }
}
