using Terraria.ModLoader;
using TouhouPetsMod.Content.Buffs.PetBuffs;
using TouhouPetsMod.Content.Projectiles.Pets;

namespace TouhouPetsMod.Content.Items.PetItems;

public class CirnoIceShard : BasicPetItem
{
    protected override int PetType => ModContent.ProjectileType<Cirno>();
    protected override int PetBuffType => ModContent.BuffType<CirnoBuff>();
    protected override (int Width, int Height) Size => (24, 26);
}
