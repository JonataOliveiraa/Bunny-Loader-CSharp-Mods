using Terraria.ModLoader;
using TouhouPetsMod.Content.Buffs.PetBuffs;
using TouhouPetsMod.Content.Projectiles.Pets;

namespace TouhouPetsMod.Content.Items.PetItems;

public class PatchouliMoon : BasicPetItem
{
    protected override int PetType => ModContent.ProjectileType<Patchouli>();
    protected override int PetBuffType => ModContent.BuffType<PatchouliBuff>();
    protected override (int Width, int Height) Size => (22, 22);
}
