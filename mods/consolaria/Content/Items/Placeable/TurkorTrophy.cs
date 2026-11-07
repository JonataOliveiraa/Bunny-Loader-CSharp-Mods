using Microsoft.Xna.Framework;

using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace Consolaria.Content.Items.Placeable {
    public class TurkorTrophy : ModItem {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults() {
            int width = 32; int height = width;
            Item.width = width; Item.height = height;

            Item.maxStack = 9999;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(gold: 1);
            Item.DefaultToPlaceableTile(ModContent.TileType<Tiles.TurkorTrophy>());
        }
    }
}
