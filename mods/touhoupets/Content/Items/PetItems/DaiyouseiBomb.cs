using Terraria.ModLoader;
using TouhouPetsMod.Content.Buffs.PetBuffs;
using TouhouPetsMod.Content.Projectiles.Pets;

namespace TouhouPetsMod.Content.Items.PetItems;

public class DaiyouseiBomb : BasicPetItem
{
    protected override int PetType => ModContent.ProjectileType<Daiyousei>();
    protected override int PetBuffType => ModContent.BuffType<DaiyouseiBuff>();
    protected override (int Width, int Height) Size => (26, 26);
}
