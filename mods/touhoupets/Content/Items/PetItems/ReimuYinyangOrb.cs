using Terraria.ModLoader;
using TouhouPetsMod.Content.Buffs.PetBuffs;
using TouhouPetsMod.Content.Projectiles.Pets;

namespace TouhouPetsMod.Content.Items.PetItems;

public class ReimuYinyangOrb : BasicPetItem
{
    protected override int PetType => ModContent.ProjectileType<Reimu>();
    protected override int PetBuffType => ModContent.BuffType<ReimuBuff>();
    protected override (int Width, int Height) Size => (26, 26);
}
