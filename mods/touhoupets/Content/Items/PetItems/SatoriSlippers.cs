using Terraria.ModLoader;
using TouhouPetsMod.Content.Buffs.PetBuffs;
using TouhouPetsMod.Content.Projectiles.Pets;

namespace TouhouPetsMod.Content.Items.PetItems;

public class SatoriSlippers : BasicPetItem
{
    protected override int PetType => ModContent.ProjectileType<Satori>();
    protected override int PetBuffType => ModContent.BuffType<SatoriBuff>();
    protected override (int Width, int Height) Size => (30, 24);
}
