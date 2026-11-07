using Terraria.ModLoader;
using TouhouPetsMod.Content.Buffs.PetBuffs;
using TouhouPetsMod.Content.Projectiles.Pets;

namespace TouhouPetsMod.Content.Items.PetItems;

public class UtsuhoEye : BasicPetItem
{
    protected override int PetType => ModContent.ProjectileType<Utsuho>();
    protected override int PetBuffType => ModContent.BuffType<UtsuhoBuff>();
    protected override (int Width, int Height) Size => (22, 24);
}
