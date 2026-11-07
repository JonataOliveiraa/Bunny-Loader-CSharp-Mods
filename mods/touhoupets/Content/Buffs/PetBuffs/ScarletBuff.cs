using Terraria.ModLoader;
using TouhouPetsMod.Content.Projectiles.Pets;

namespace TouhouPetsMod.Content.Buffs.PetBuffs;

public class ScarletBuff : BasicPetBuff
{
    protected override int[] GetPets() => new[] { ModContent.ProjectileType<Remilia>(), ModContent.ProjectileType<Flandre>() };
}
