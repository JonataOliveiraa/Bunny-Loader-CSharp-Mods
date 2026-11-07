using mitanpc.Content.Projectiles.Weapons.Melee;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace mitanpc.Content.Items.Weapons.Melee;

public class GameCartridge : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 38;
        Item.height = 32;
        Item.damage = 25;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.useAnimation = 16;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.useTime = 16;
        Item.knockBack = 2;
        Item.UseSound = SoundID.Item1;
        Item.autoReuse = true;
        Item.maxStack = 1;
        Item.value = Item.buyPrice(0, 8, 0, 0);
        Item.rare = ItemRarityID.Blue;
        Item.shoot = ModContent.ProjectileType<GameCartridgeProjectile>();
        Item.shootSpeed = 12;
        Item.DamageType = DamageClass.Melee;
    }
}
