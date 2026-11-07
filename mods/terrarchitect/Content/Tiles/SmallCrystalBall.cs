using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace terrarchitect.Content.Tiles;

public class SmallCrystalBall : ModTile
{
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileNoAttach[Type] = true;
        Main.tileLavaDeath[Type] = true;
        Main.tileLighted[Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
        TileObjectData.newTile.CoordinateHeights = new[] { 16 };
        TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.Table | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
        TileObjectData.addTile(Type);
        TileID.Sets.DisableSmartCursor[Type] = true;
        RegisterItemDrop(ModContent.ItemType<Items.SmallCrystalBallItem>());
    }

    public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
    {
        r = (float)(0.6 / 0.8);
        g = (float)(0.3568627450980392 / 0.8);
        b = (float)(0.9607843137254902 / 0.8);
    }

    public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
    {
        Lighting.AddLight(i, j + 2, 1f, 1f, 1f);
        return true;
    }
}
