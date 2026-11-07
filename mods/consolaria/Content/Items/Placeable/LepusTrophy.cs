using Microsoft.Xna.Framework;

using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace Consolaria.Content.Items.Placeable {
    public class LepusTrophy : ModItem {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults() {
            int width = 32; int height = 40;
            Item.width = width; Item.height = height;

            Item.DefaultToPlaceableTile(ModContent.TileType<Tiles.LepusTrophy>());

            Item.maxStack = 9999;

            Item.rare = ItemRarityID.Blue;
            Item.value = Item.buyPrice(gold: 1);
        }
    }
}
