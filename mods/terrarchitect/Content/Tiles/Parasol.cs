using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace terrarchitect.Content.Tiles;

public class Parasol : ModTile
{
    private Asset<Texture2D> TopTexture = null!;

    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileNoAttach[Type] = true;
        Main.tileLavaDeath[Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
        TileObjectData.newTile.Origin = new Point16(0, 1);
        TileObjectData.newTile.Width = 1;
        TileObjectData.newTile.Height = 3;
        TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 18 };
        TileObjectData.addTile(Type);
        TileID.Sets.DisableSmartCursor[Type] = true;

        RegisterItemDrop(ModContent.ItemType<Items.ParasolItem>());
        TopTexture = ModContent.Request<Texture2D>("terrarchitect/Content/Tiles/ParasolTop");
    }

    public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
    {
        Tile tile = Main.tile[i, j];
        if (tile.frameY != 0) return;

        Vector2 position = new Vector2(i * 16 - MathF.Floor(Main.screenPosition.X) - 22, j * 16 - MathF.Floor(Main.screenPosition.Y) - 24);
        spriteBatch.Draw(TopTexture.Value, position, new Rectangle(0, 0, 62, 24), Color.White, 0, new Vector2(0, 0), 1, SpriteEffects.None, 0);
    }
}
