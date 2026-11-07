// Satori and Koishi together.
using Terraria.ModLoader;
using TouhouPetsMod.Content.Buffs.PetBuffs;

namespace TouhouPetsMod.Content.Items.PetItems;

public class TheThirdEye : BasicPetItem
{
    protected override int PetBuffType => ModContent.BuffType<KomeijiBuff>();
    protected override (int Width, int Height) Size => (32, 34);

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ModContent.ItemType<SatoriSlippers>(), 1)
            .AddIngredient(ModContent.ItemType<KoishiTelephone>(), 1)
            .Register();
    }
}
