// SPDX-License-Identifier: GPL-3.0-only
// Modified on 2026-11-06: ported from JavaScript to C#.
using System.Linq;
using Terraria.ModLoader;
using WildAndHornedMod.Common.Pets;

namespace WildAndHornedMod.Common;

// Optional integration for a Bunny Loader port of Touhou Little Friend.
public class GapShopItems : ModSystem
{
    public override void PostSetupContent()
    {
        if (!ModContent.TryFind("touhoupets/YukariPortal", out ModNPC portal))
            return;
        foreach (PetDefinition d in PETS.All)
        {
            if (!NPCShopDatabase.TryGetNPCShop(NPCShopDatabase.GetShopName(portal.Type, d.Light ? "Shop2" : "Shop"), out NPCShop shop))
                continue;
            if (!Mod.TryFind(d.Item, out ModItem item))
                continue;
            if (!shop.Entries.Any(entry => entry.Item.type == item.Type))
                shop.Add(item.Type);
        }
    }
}
