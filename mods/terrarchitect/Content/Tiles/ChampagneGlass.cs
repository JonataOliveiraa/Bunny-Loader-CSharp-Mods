using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace terrarchitect.Content.Tiles;

public class ChampagneGlass : ModTile
{
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileNoAttach[Type] = true;
        Main.tileLavaDeath[Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.StyleOnTable1x1);
        TileObjectData.newTile.CoordinateHeights = new[] { 16 };
        TileObjectData.addTile(Type);
        TileID.Sets.DisableSmartCursor[Type] = true;
        RegisterItemDrop(ModContent.ItemType<Items.ChampagneGlassItem>());
        AdjTiles = [TileID.Bottles];
    }
}
