using mitanpc.Content.Buffs;
using mitanpc.Content.Projectiles.Pets;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace mitanpc.Content.Items.Pets;

public class AlarmClock : ModItem
{
    public override void SetDefaults()
    {
        Item.CloneDefaults(ItemID.ZephyrFish);
        Item.shoot = ModContent.ProjectileType<BrokenMitaPetProjectile>();
        Item.buffType = ModContent.BuffType<AlarmClockBuff>();
        Item.value = Item.buyPrice(0, 12, 0, 0);
    }

    public override bool? UseItem(Player player)
    {
        if (player.whoAmI == Main.myPlayer)
            player.AddBuff(Item.buffType, 3600, false);
        return true;
    }
}
