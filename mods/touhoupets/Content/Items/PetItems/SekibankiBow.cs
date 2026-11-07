using Terraria.ModLoader;
using TouhouPetsMod.Content.Buffs.PetBuffs;
using TouhouPetsMod.Content.Projectiles.Pets;

namespace TouhouPetsMod.Content.Items.PetItems;

public class SekibankiBow : BasicPetItem
{
    protected override int PetType => ModContent.ProjectileType<Sekibanki>();
    protected override int PetBuffType => ModContent.BuffType<SekibankiBuff>();
    protected override (int Width, int Height) Size => (28, 20);
}
