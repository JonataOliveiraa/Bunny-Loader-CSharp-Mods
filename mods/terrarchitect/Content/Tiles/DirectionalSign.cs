using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace terrarchitect.Content.Tiles;

public class DirectionalSign : ModTile
{
    public override void SetStaticDefaults()
    {
        Main.tileSign[Type] = true;
        Main.tileFrameImportant[Type] = true;
        Main.tileLavaDeath[Type] = true;
        TileID.Sets.FramesOnKillWall[Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
        TileObjectData.newTile.Width = 2;
        TileObjectData.newTile.Height = 2;
        TileObjectData.newTile.CoordinateHeights = new[] { 16, 16 };
        TileObjectData.newTile.RandomStyleRange = 4;
        TileObjectData.newTile.StyleHorizontal = true;
        TileObjectData.addTile(Type);
        TileID.Sets.DisableSmartCursor[Type] = true;

        RegisterItemDrop(ModContent.ItemType<Items.DirectionalSignItem>());
    }

    public override void KillMultiTile(int i, int j, int frameX, int frameY)
    {
        Sign.KillSign(i, j);
    }
}
