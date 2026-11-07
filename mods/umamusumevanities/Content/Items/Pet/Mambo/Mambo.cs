using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using umamusumevanities.Common.Audio;

namespace umamusumevanities.Content.Items.Pet.Mambo;

public class Mambo : ModItem
{
    public override void SetDefaults()
    {
        Item.CloneDefaults(ItemID.ZephyrFish);
        Item.shoot = ModContent.ProjectileType<MamboProjectile>();
        Item.buffType = ModContent.BuffType<MamboBuff>();
    }

    public override bool? UseItem(Player player)
    {
        if (player.whoAmI == Main.myPlayer)
        {
            player.AddBuff(Item.buffType, 3600, false);
            UmamusumeAudio.PlayMambo(player.Center);
        }
        return null;
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Silk, 7)
            .AddTile(TileID.Loom)
            .Register();
    }
}
