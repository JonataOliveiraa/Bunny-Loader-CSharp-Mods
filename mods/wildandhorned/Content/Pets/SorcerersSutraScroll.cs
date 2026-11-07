// SPDX-License-Identifier: GPL-3.0-only
// Modified on 2026-11-06: ported from JavaScript to C#.
// Adapted from Wild and Horned 1.5.2 by FuzzyGolf291773.
using Terraria.ModLoader;
using WildAndHornedMod.Common.Pets;

namespace WildAndHornedMod.Content.Pets;

public class SorcerersSutraScroll : PetItem
{
    protected override PetDefinition Definition => PETS.Byakuren;
    protected override int PetType => ModContent.ProjectileType<Byakuren>();
    protected override int PetBuffType => ModContent.BuffType<ByakurenBuff>();
}
