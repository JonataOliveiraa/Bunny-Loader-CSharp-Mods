using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace terrarchitect.Content.Tiles;

public class Clock : ModTile
{
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        TileID.Sets.FramesOnKillWall[Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
        TileObjectData.newTile.Width = 2;
        TileObjectData.newTile.Height = 2;
        TileObjectData.newTile.CoordinateHeights = new[] { 16, 16 };
        TileObjectData.newTile.Origin = new Point16(0, 0);
        TileObjectData.addTile(Type);
        TileID.Sets.DisableSmartCursor[Type] = true;

        RegisterItemDrop(ModContent.ItemType<Items.ClockItem>());
    }
}
