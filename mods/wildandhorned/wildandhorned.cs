// SPDX-License-Identifier: GPL-3.0-only
// Modified on 2026-11-06: ported from JavaScript to C#.
using Terraria;
using Terraria.ModLoader;
using WildAndHornedMod.Common.Pets;

public class WildAndHorned : Mod
{
    public override object? Call(params object?[] args)
    {
        string? command = args.Length > 0 ? args[0] as string : null;
        PetBase? pet = args.Length > 1 && args[1] is Projectile projectile && !projectile.IsNull ? projectile.ModProjectile as PetBase : null;
        if (command == "IsKasenTransforming")
            return pet != null && pet.Definition.Name == "Kasen" && pet.IsTransforming;
        if (pet == null)
            return null;
        switch (command)
        {
            case "GetKasenForm":
                return pet.Definition.Name == "Kasen" ? pet.DisplayedForm : null;
            case "GetPetHouseStoriesVariant":
                return pet.Definition.Name == "Kasen" && pet.DisplayedForm == "Oni" ? "Oni" : "";
            case "GetPetAction":
                return pet.CurrentAction;
            default:
                return null;
        }
    }
}
