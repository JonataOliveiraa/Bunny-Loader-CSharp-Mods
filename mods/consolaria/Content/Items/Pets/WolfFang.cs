using Microsoft.Xna.Framework;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Consolaria.Content.Items.Pets {
    public class WolfFang : ConsolariaPetItem {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults() {
            Item.DefaultToVanitypet(ModContent.ProjectileType<Projectiles.Friendly.Pets.Werewolf>(), ModContent.BuffType<Buffs.Werewolf>());

            int width = 30; int height = width;
            Item.width = width; Item.height = height;

            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(gold: 3);
        }
    }
}