using mitanpc.Content.Buffs;
using mitanpc.Content.Projectiles.Pets;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace mitanpc.Content.Items.Pets;

public class GameConsole : ModItem
{
    public override void SetDefaults()
    {
        Item.CloneDefaults(ItemID.ZephyrFish);
        Item.shoot = ModContent.ProjectileType<FlyPetProjectile>();
        Item.buffType = ModContent.BuffType<GameConsoleBuff>();
        Item.UseSound = new SoundStyle("mitanpc/Assets/Sounds/MarioCoinGrab") { Volume = 0.35f };
        Item.value = Item.buyPrice(0, 12, 0, 0);
    }

    public override bool? UseItem(Player player)
    {
        if (player.whoAmI == Main.myPlayer)
            player.AddBuff(Item.buffType, 3600, false);
        return true;
    }
}
