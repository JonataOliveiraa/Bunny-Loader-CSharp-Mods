using mitanpc.Content.Projectiles.Pets;
using Terraria;
using Terraria.ModLoader;

namespace mitanpc.Content.Buffs;

public class AlarmClockBuff : ModBuff
{
    public override void SetStaticDefaults()
    {
        Main.buffNoTimeDisplay[Type] = true;
        Main.vanityPet[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        bool pet = false;
        player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref pet, ModContent.ProjectileType<BrokenMitaPetProjectile>(), 18000);
    }
}
