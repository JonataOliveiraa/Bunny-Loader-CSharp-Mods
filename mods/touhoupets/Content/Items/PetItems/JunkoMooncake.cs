using Terraria.ModLoader;
using TouhouPetsMod.Content.Buffs.PetBuffs;
using TouhouPetsMod.Content.Projectiles.Pets;

namespace TouhouPetsMod.Content.Items.PetItems;

public class JunkoMooncake : BasicPetItem
{
    protected override int PetType => ModContent.ProjectileType<Junko>();
    protected override int PetBuffType => ModContent.BuffType<JunkoBuff>();
    protected override (int Width, int Height) Size => (26, 22);
}
