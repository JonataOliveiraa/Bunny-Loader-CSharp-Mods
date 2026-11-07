using Terraria.ModLoader;
using TouhouPetsMod.Content.Buffs.PetBuffs;
using TouhouPetsMod.Content.Projectiles.Pets;

namespace TouhouPetsMod.Content.Items.PetItems;

public class IkuOarfish : BasicPetItem
{
    protected override int PetType => ModContent.ProjectileType<Iku>();
    protected override int PetBuffType => ModContent.BuffType<IkuBuff>();
    protected override (int Width, int Height) Size => (30, 28);
}
