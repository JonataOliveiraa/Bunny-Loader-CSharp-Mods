using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using topdownmode.Common.Systems;
using topdownmode.Content.Items;

namespace topdownmode.Content.Tiles;

public class TopDownCaveStairs : ModTile
{
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileNoAttach[Type] = true;
        Main.tileLavaDeath[Type] = false;
        DustType = DustID.Stone;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
        TileObjectData.newTile.LavaDeath = false;
        TileObjectData.addTile(Type);
        AddMapEntry(new Color(96, 84, 76), CreateMapEntryName());
    }

    public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 4;

    public override bool RightClick(int i, int j)
    {
        TopDownCave.Use(i, j, true);
        return true;
    }

    public override void MouseOver(int i, int j)
    {
        if (!TopDownCave.Near(i, j))
            return;
        Player player = Main.LocalPlayer;
        player.noThrow = 2;
        player.cursorItemIconEnabled = true;
        player.cursorItemIconID = ModContent.ItemType<TopDownCaveStairsItem>();
    }
}
