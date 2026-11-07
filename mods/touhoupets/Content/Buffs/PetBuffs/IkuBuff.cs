using Terraria.ModLoader;
using TouhouPetsMod.Content.Projectiles.Pets;

namespace TouhouPetsMod.Content.Buffs.PetBuffs;

public class IkuBuff : BasicPetBuff
{
    protected override int[] GetPets() => new[] { ModContent.ProjectileType<Iku>() };
    protected override bool LightPet => true;
}
