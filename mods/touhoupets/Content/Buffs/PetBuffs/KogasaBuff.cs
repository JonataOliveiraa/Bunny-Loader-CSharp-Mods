using Terraria.ModLoader;
using TouhouPetsMod.Content.Projectiles.Pets;

namespace TouhouPetsMod.Content.Buffs.PetBuffs;

public class KogasaBuff : BasicPetBuff
{
    protected override int[] GetPets() => new[] { ModContent.ProjectileType<Kogasa>() };
}
