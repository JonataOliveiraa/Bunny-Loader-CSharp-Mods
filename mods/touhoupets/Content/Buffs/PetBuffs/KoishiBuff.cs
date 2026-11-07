using Terraria.ModLoader;
using TouhouPetsMod.Content.Projectiles.Pets;

namespace TouhouPetsMod.Content.Buffs.PetBuffs;

public class KoishiBuff : BasicPetBuff
{
    protected override int[] GetPets() => new[] { ModContent.ProjectileType<Koishi>() };
}
