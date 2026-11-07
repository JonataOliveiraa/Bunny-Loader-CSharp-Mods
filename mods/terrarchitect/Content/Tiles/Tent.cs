using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace terrarchitect.Content.Tiles;

public class Tent : ModTile
{
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileNoAttach[Type] = true;
        Main.tileLavaDeath[Type] = true;
        TileID.Sets.HasOutlines[Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
        TileObjectData.newTile.CoordinateHeights = new[] { 16, 18 };
        TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
        TileObjectData.newTile.StyleHorizontal = true;
        TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
        TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
        TileObjectData.addAlternate(1);
        TileObjectData.addTile(Type);
        AdjTiles = [TileID.Beds];
        TileID.Sets.IsValidSpawnPoint[Type] = true;

        RegisterItemDrop(ModContent.ItemType<Items.TentItem>());
    }

    public override bool RightClick(int i, int j)
    {
        Player player = Main.LocalPlayer;
        Tile tile = Main.tile[i, j];
        int left = i - tile.frameX % 54 / 18;
        int top = j - tile.frameY / 18;
        int spawnX = left + (tile.frameX >= 54 ? 1 : 2);
        int spawnY = top + 2;
        player.FindSpawn();
        if (player.SpawnX == spawnX && player.SpawnY == spawnY)
        {
            player.RemoveSpawn();
            Main.NewText("Spawn point removed!", 255, 240, 20, false);
        }
        else if (Player.CheckSpawn(spawnX, spawnY))
        {
            player.ChangeSpawn(spawnX, spawnY);
            Main.NewText("Spawn point set!", 255, 240, 20, false);
        }
        return true;
    }

    public override void MouseOver(int i, int j)
    {
        Player player = Main.LocalPlayer;
        player.noThrow = 2;
        player.cursorItemIconEnabled = true;
        player.cursorItemIconID = ModContent.ItemType<Items.TentItem>();
    }
}
