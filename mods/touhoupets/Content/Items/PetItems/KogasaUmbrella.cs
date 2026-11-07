using Terraria.ModLoader;
using TouhouPetsMod.Content.Buffs.PetBuffs;
using TouhouPetsMod.Content.Projectiles.Pets;

namespace TouhouPetsMod.Content.Items.PetItems;

public class KogasaUmbrella : BasicPetItem
{
    protected override int PetType => ModContent.ProjectileType<Kogasa>();
    protected override int PetBuffType => ModContent.BuffType<KogasaBuff>();
    protected override (int Width, int Height) Size => (38, 36);
}
