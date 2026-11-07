using DisplayBaseMod.Lib;
using Terraria.ID;

namespace DisplayBaseMod.Content.Items;

public class DisplayBaseItemGranite : BaseDisplayBaseItem
{
    protected override int Style => 1;

    public override void AddRecipes()
    {
        // 3086 is the Granite Block.
        CreateRecipe().AddIngredient(ItemID.Granite, 6).AddTile(TileID.HeavyWorkBench).Register();
    }
}
