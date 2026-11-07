using Microsoft.Xna.Framework;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Consolaria.Content.Items.Pets {
    public class VialOfBlood : ConsolariaPetItem {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults() {
            Item.DefaultToVanitypet(ModContent.ProjectileType<Projectiles.Friendly.Pets.Bat>(), ModContent.BuffType<Buffs.Bat>());

            int width = 14; int height = 28;
            Item.width = width; Item.height = height;

            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(gold: 3);
        }
    }
}