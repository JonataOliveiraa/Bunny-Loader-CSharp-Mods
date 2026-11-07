using Terraria.ModLoader;
using TouhouPetsMod.Content.Buffs.PetBuffs;
using TouhouPetsMod.Content.Projectiles.Pets;

namespace TouhouPetsMod.Content.Items.PetItems;

public class TenshiKeyStone : BasicPetItem
{
    protected override int PetType => ModContent.ProjectileType<Tenshi>();
    protected override int PetBuffType => ModContent.BuffType<TenshiBuff>();
    protected override (int Width, int Height) Size => (32, 24);
}
