using Terraria;
using Terraria.ModLoader;

namespace BunnySocialMod.Common;

// The equipment array comes in pairs:
//   armor[0..2]  functional armor        <->  armor[10..12]  vanity armor
//   armor[3..9]  functional accessories  <->  armor[13..19]  vanity accessories
// After the game applies the real equipment, each vanity item is also applied as
// functional equipment, using the matching functional slot (slot - 10) so the game
// reads the right visibility flag.
public class VanityAsAccessory : ModPlayer
{
    const int FirstVanity = 10;
    const int LastVanity = 19;

    public override void PostUpdateEquips()
    {
        var armor = Player.armor;
        for (int slot = FirstVanity; slot <= LastVanity; slot++)
        {
            Item item = armor[slot];
            if (item.type <= 0 || item.stack <= 0)
                continue;

            int functionalSlot = slot - FirstVanity;
            // The extra accessory slots stay off until the game unlocks them.
            if (!Player.IsItemSlotUnlockedAndUsable(functionalSlot))
                continue;

            Player.ApplyEquipFunctional(functionalSlot, item);

            if (item.defense > 0)
                Player.statDefense += item.defense;

            // Reforge bonuses are a separate step in the game.
            if (item.accessory && item.prefix > 0)
                Player.GrantPrefixBenefits(item);
        }
    }
}
