using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace terrarchitect.Content.Tiles;

public class HayBale : ModTile
{
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileLavaDeath[Type] = true;
        Main.tileTable[Type] = true;
        Main.tileSolidTop[Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
        TileObjectData.newTile.CoordinateHeights = new[] { 16, 18 };
        TileObjectData.addTile(Type);
        TileID.Sets.DisableSmartCursor[Type] = true;
        AdjTiles = [TileID.FishingCrate];

        RegisterItemDrop(ModContent.ItemType<Items.HayBaleItem>());
    }
}
