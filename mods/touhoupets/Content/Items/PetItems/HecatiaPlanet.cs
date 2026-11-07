using Terraria.ModLoader;
using TouhouPetsMod.Content.Buffs.PetBuffs;
using TouhouPetsMod.Content.Projectiles.Pets;

namespace TouhouPetsMod.Content.Items.PetItems;

public class HecatiaPlanet : BasicPetItem
{
    protected override int PetType => ModContent.ProjectileType<Hecatia>();
    protected override int PetBuffType => ModContent.BuffType<HecatiaBuff>();
    protected override (int Width, int Height) Size => (26, 26);
}
