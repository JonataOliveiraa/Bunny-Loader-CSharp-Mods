using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace terrarchitect.Content.Tiles;

public class TowerClock : ModTile
{
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileLavaDeath[Type] = true;
        TileID.Sets.FramesOnKillWall[Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
        TileObjectData.newTile.Width = 4;
        TileObjectData.newTile.Height = 4;
        TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16, 16 };
        TileObjectData.addTile(Type);
        TileID.Sets.DisableSmartCursor[Type] = true;

        RegisterItemDrop(ModContent.ItemType<Items.TowerClockItem>());
    }

    public override void NearbyEffects(int i, int j, bool closer)
    {
        if (closer) Main.SceneMetrics.HasClock = true;
    }
}
