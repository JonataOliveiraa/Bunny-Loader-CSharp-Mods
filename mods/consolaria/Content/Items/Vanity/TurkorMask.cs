using Microsoft.Xna.Framework;

using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace Consolaria.Content.Items.Vanity {
    [AutoloadEquip(EquipType.Head)]
    public class TurkorMask : ModItem {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults() {
            int width = 20; int height = 24;
            Item.width = width; Item.height = height;

            Item.value = Item.sellPrice(silver: 75);
            Item.rare = ItemRarityID.Blue;

            Item.vanity = true;
            Item.maxStack = 1;
        }
    }
}
