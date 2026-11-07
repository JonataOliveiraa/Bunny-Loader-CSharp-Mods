using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace terrarchitect.Content.Tiles;

public class BeeNest : ModTile
{
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileNoAttach[Type] = true;
        Main.tileLavaDeath[Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
        TileObjectData.newTile.CoordinateHeights = new[] { 16, 16 };
        TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
        TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
        TileObjectData.addTile(Type);
        TileID.Sets.DisableSmartCursor[Type] = true;

        RegisterItemDrop(ModContent.ItemType<Items.BeeNestItem>());
    }

    public override bool RightClick(int i, int j)
    {
        SpawnBees(i, j);
        return true;
    }

    public override void HitWire(int i, int j)
    {
        Tile tile = Main.tile[i, j];
        int left = i - tile.frameX / 18;
        int top = j - tile.frameY / 18;
        for (int x = left; x < left + 2; x++)
        {
            for (int y = top; y < top + 2; y++) Wiring.SkipWire(x, y);
        }
        SpawnBees(left, top);
    }

    private static void SpawnBees(int i, int j)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;

        IEntitySource source = NPC.GetSpawnSourceForNaturalSpawn();
        NPC.NewNPC(source, i * 16 - 8, j * 16, NPCID.BeeSmall, 0, 0, 0, 0, 0, 255);
        NPC.NewNPC(source, i * 16 + 8, j * 16, NPCID.BeeSmall, 0, 0, 0, 0, 0, 255);
        NPC.NewNPC(source, i * 16, j * 16 - 8, NPCID.BeeSmall, 0, 0, 0, 0, 0, 255);
    }

    public override void MouseOver(int i, int j)
    {
        Player player = Main.LocalPlayer;
        player.noThrow = 2;
        player.cursorItemIconEnabled = true;
        player.cursorItemIconID = ModContent.ItemType<Items.BeeNestItem>();
    }
}
