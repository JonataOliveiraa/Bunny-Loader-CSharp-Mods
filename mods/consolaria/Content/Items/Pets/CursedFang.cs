using Terraria.ID;
using Microsoft.Xna.Framework;

using Terraria;
using Terraria.ModLoader;

namespace Consolaria.Content.Items.Pets {
    public class CursedFang : ModItem {
        public override void SetStaticDefaults() {

            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults() {
            Item.DefaultToVanitypet(ModContent.ProjectileType<Projectiles.Friendly.Pets.LilMarco>(), ModContent.BuffType<Buffs.LilMarco>());

            int width = 20; int height = 32;
            Item.width = width; Item.height = height;

            Item.rare = ItemRarityID.Master;
            Item.value = Item.sellPrice(gold: 5);
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame) {
            if (player.whoAmI == Main.myPlayer && player.itemTime == 0)
                player.AddBuff(Item.buffType, 3600);
        }
    }
}