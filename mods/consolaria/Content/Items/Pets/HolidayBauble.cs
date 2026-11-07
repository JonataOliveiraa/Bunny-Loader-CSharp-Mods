using Microsoft.Xna.Framework;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Consolaria.Content.Items.Pets {
    public class HolidayBauble : ConsolariaPetItem {
        public override void SetStaticDefaults()
            => Item.ResearchUnlockCount = 1;


        public override void SetDefaults() {
            Item.DefaultToVanitypet(ModContent.ProjectileType<Projectiles.Friendly.Pets.Elfa>(), ModContent.BuffType<Buffs.Elfa>());

            int width = 22; int height = 24;
            Item.width = width; Item.height = height;

            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(gold: 1);
        }
    }
}