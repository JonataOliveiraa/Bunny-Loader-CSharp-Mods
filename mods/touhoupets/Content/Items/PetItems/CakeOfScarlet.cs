// Remilia and Flandre together.
using Terraria.ModLoader;
using TouhouPetsMod.Content.Buffs.PetBuffs;

namespace TouhouPetsMod.Content.Items.PetItems;

public class CakeOfScarlet : BasicPetItem
{
    protected override int PetBuffType => ModContent.BuffType<ScarletBuff>();
    protected override (int Width, int Height) Size => (26, 32);

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ModContent.ItemType<RemiliaRedTea>(), 1)
            .AddIngredient(ModContent.ItemType<FlandrePudding>(), 1)
            .Register();
    }
}
