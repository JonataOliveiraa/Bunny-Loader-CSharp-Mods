using Microsoft.Xna.Framework;

using Terraria;
using Terraria.ModLoader;

namespace Consolaria.Content.Items.Pets {
    public class ShinyBlackSlab : ConsolariaPetItem {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults() {
            Item.DefaultToVanitypet(ModContent.ProjectileType<Content.Projectiles.Friendly.Pets.AndroidGuy>(), ModContent.BuffType<Content.Buffs.Android>());

            int width = 32; int height = 32;
            Item.width = width; Item.height = height;

            Item.rare = 5;
            Item.value = Item.buyPrice(gold: 50);
        }
    }
}