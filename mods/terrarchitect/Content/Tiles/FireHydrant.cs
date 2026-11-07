using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace terrarchitect.Content.Tiles;

public class FireHydrant : ModTile
{
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileNoAttach[Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
        TileObjectData.newTile.CoordinateHeights = new[] { 16, 18 };
        TileObjectData.addTile(Type);
        TileID.Sets.DisableSmartCursor[Type] = true;

        RegisterItemDrop(ModContent.ItemType<Items.FireHydrantItem>());
    }
}
