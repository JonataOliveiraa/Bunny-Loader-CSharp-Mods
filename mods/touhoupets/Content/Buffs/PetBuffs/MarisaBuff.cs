using Terraria.ModLoader;
using TouhouPetsMod.Content.Projectiles.Pets;

namespace TouhouPetsMod.Content.Buffs.PetBuffs;

public class MarisaBuff : BasicPetBuff
{
    protected override int[] GetPets() => new[] { ModContent.ProjectileType<Marisa>() };
    protected override bool LightPet => true;
}
