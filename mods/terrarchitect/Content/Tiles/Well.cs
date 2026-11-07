using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace terrarchitect.Content.Tiles;

public class Well : ModTile
{
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileNoAttach[Type] = true;
        Main.tileLavaDeath[Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
        TileObjectData.newTile.Width = 4;
        TileObjectData.newTile.Height = 5;
        TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16, 16, 18 };
        TileObjectData.addTile(Type);
        TileID.Sets.DisableSmartCursor[Type] = true;
        AdjTiles = [TileID.Sinks];

        RegisterItemDrop(ModContent.ItemType<Items.WellItem>());
    }
}
