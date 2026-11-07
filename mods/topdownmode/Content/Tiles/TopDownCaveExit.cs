using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.ObjectData;
using topdownmode.Common.Systems;
using topdownmode.Content.Items;

namespace topdownmode.Content.Tiles;

public class TopDownCaveExit : ModTile
{
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileNoAttach[Type] = true;
        Main.tileLavaDeath[Type] = false;
        Main.tileLighted[Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
        TileObjectData.newTile.LavaDeath = false;
        TileObjectData.addTile(Type);
        AddMapEntry(new Color(214, 190, 130), CreateMapEntryName());
    }

    public override bool CanKillTile(int i, int j, ref bool blockDamaged) => false;

    public override bool Slope(int i, int j) => false;

    public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
    {
        r = 0.55f;
        g = 0.48f;
        b = 0.3f;
    }

    public override bool RightClick(int i, int j)
    {
        TopDownCave.Use(i, j, false);
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
