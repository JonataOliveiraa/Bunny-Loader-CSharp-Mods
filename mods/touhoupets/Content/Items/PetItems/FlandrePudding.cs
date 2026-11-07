using Terraria.ModLoader;
using TouhouPetsMod.Content.Buffs.PetBuffs;
using TouhouPetsMod.Content.Projectiles.Pets;

namespace TouhouPetsMod.Content.Items.PetItems;

public class FlandrePudding : BasicPetItem
{
    protected override int PetType => ModContent.ProjectileType<Flandre>();
    protected override int PetBuffType => ModContent.BuffType<FlandreBuff>();
    protected override (int Width, int Height) Size => (30, 36);
}
