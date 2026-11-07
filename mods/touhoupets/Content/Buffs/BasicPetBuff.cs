// The base of every pet buff (BasicPetBuff.cs): keeps itself on and brings
// back its pets when they are missing. A base, not a buff.
using Terraria;
using Terraria.ModLoader;

namespace TouhouPetsMod.Content.Buffs;

public abstract class BasicPetBuff : ModBuff
{
    int[]? pets;

    // The pets of this buff (two for the pairs).
    protected abstract int[] GetPets();

    // A light pet (the light pet slot) or a vanity one (the pet slot).
    protected virtual bool LightPet => false;

    public override void SetStaticDefaults()
    {
        Main.buffNoTimeDisplay[Type] = true;
        Main.vanityPet[Type] = !LightPet;
        Main.lightPet[Type] = LightPet;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        player.buffTime[buffIndex] = 18000;
        if (player.whoAmI != Main.myPlayer)
            return;
        pets ??= GetPets();
        foreach (int type in pets)
        {
            if (player.ownedProjectileCounts[type] > 0)
                continue;
            Projectile.NewProjectile(player.GetProjectileSource_Buff(buffIndex), player.position.X + player.width / 2f, player.position.Y + player.height / 2f,
                0f, 0f, type, 0, 0f, player.whoAmI, 0f, 0f, 0f);
        }
    }
}
