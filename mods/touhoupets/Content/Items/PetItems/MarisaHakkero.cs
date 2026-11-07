using Terraria.ModLoader;
using TouhouPetsMod.Content.Buffs.PetBuffs;
using TouhouPetsMod.Content.Projectiles.Pets;

namespace TouhouPetsMod.Content.Items.PetItems;

public class MarisaHakkero : BasicPetItem
{
    protected override int PetType => ModContent.ProjectileType<Marisa>();
    protected override int PetBuffType => ModContent.BuffType<MarisaBuff>();
    protected override (int Width, int Height) Size => (26, 24);
}
