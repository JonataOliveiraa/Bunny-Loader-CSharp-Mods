using DisplayBaseMod.Content.Tiles;
using Terraria.ModLoader;

namespace DisplayBaseMod.Lib;

public abstract class BaseDisplayBaseItem : ModItem
{
    protected abstract int Style { get; }

    public override void SetDefaults()
    {
        Item.width = 32;
        Item.height = 32;
        Item.DefaultToPlaceableTile(ModContent.TileType<DisplayBaseTile>(), Style);
        Item.value = 0;
        Item.rare = 0;
        Item.placeStyle = Style;
    }
}
