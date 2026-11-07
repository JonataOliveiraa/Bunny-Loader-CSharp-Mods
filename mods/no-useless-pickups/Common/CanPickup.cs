using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoUselessPickupsMod.Common;

// The JavaScript class is named CanPickup, but a C# member cannot share its class's name.
// A GlobalItem without saved data is not keyed by its class name, so nothing is lost.
public class CanPickupItem : GlobalItem
{
    public override bool CanPickup(WorldItem worldItem, Player player)
    {
        int type = worldItem.type;

        if (ItemID.Sets.IsAPickup[type] && type >= 58 && type <= 3453)
        {
            switch (type)
            {
                case 58: // Heart
                case 1734:
                case 1867:
                    return player.statLife < player.statLifeMax2 - 20;
                case 184: // Star
                case 1735:
                case 1868:
                    return player.statMana < player.statManaMax2 - 100;
                // The range test above keeps the next two out, as it does in the JavaScript version.
                case 3454: // Nebula Life
                    return player.FindBuffIndex(175) == -1;
                case 3455: // Nebula Mana
                    return player.FindBuffIndex(178) == -1;
                case 3453: // Nebula Damage
                    return player.FindBuffIndex(181) == -1;
            }
        }

        return base.CanPickup(worldItem, player);
    }
}
