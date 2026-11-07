using Terraria.ModLoader;
using TouhouPetsMod.Content.Buffs.PetBuffs;
using TouhouPetsMod.Content.Projectiles.Pets;

namespace TouhouPetsMod.Content.Items.PetItems;

public class RemiliaRedTea : BasicPetItem
{
    protected override int PetType => ModContent.ProjectileType<Remilia>();
    protected override int PetBuffType => ModContent.BuffType<RemiliaBuff>();
    protected override (int Width, int Height) Size => (34, 26);
}
