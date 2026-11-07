// SPDX-License-Identifier: GPL-3.0-only
// Modified on 2026-11-06: ported from JavaScript to C#.
// Adapted from Wild and Horned 1.5.2 by FuzzyGolf291773.
using Terraria.ModLoader;
using WildAndHornedMod.Common.Pets;

namespace WildAndHornedMod.Content.Pets;

public class InuSakuyaBuff : PetBuff
{
    protected override PetDefinition Definition => PETS.InuSakuya;
    protected override int PetType => ModContent.ProjectileType<InuSakuya>();
}
