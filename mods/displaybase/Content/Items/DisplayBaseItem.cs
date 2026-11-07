using DisplayBaseMod.Lib;
using Terraria.ID;

namespace DisplayBaseMod.Content.Items;

public class DisplayBaseItem : BaseDisplayBaseItem
{
    protected override int Style => 0;

    public override void AddRecipes()
    {
        // 3066 is the Smooth Marble Block.
        CreateRecipe().AddIngredient(ItemID.MarbleBlock, 6).AddTile(TileID.HeavyWorkBench).Register();
    }
}
