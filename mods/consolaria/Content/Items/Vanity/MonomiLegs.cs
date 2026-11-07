using Microsoft.Xna.Framework;

using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace Consolaria.Content.Items.Vanity {
    [AutoloadEquip(EquipType.Legs)]

    public class MonomiLegs : ModItem {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults() {
            int width = 30; int height = 18;
            Item.width = width; Item.height = height;

            Item.rare = ItemRarityID.White;
            Item.value = Item.sellPrice(silver: 15);
            Item.vanity = true;
        }
    }
}
