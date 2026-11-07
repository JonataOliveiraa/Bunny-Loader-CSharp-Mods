using Terraria.ModLoader;
using TouhouPetsMod.Content.Buffs.PetBuffs;
using TouhouPetsMod.Content.Projectiles.Pets;

namespace TouhouPetsMod.Content.Items.PetItems;

public class HinaDoll : BasicPetItem
{
    protected override int PetType => ModContent.ProjectileType<Hina>();
    protected override int PetBuffType => ModContent.BuffType<HinaBuff>();
    protected override (int Width, int Height) Size => (34, 36);
}
