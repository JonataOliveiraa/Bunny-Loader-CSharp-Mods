using BestiaryDisplaysMod.Common;
using BestiaryDisplaysMod.Common.UI;
using BestiaryDisplaysMod.Content.TileEntities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace BestiaryDisplaysMod.Lib;

public abstract class BaseHologramProjectorTile : ModTile
{
    const int HoldTicks = 50;
    const int FadeTicks = 25;
    const int Frames = 5;
    const int FrameStride = 34;

    int itemType = -1;

    public abstract bool IsAdvanced { get; }

    public float MaxScale => IsAdvanced ? 10f : 2f;
    public float MinScale => IsAdvanced ? 0.1f : 0.25f;
    public int MaxOffset => IsAdvanced ? 2000 : 160;

    // The item that places this tile: its name with Item in place of Tile.
    public string ItemName => Name.EndsWith("Tile") ? Name[..^4] + "Item" : Name;

    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileNoAttach[Type] = true;
        Main.tileLavaDeath[Type] = true;
        TileID.Sets.DisableSmartCursor[Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
        TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.Table, 3, 0);
        TileObjectData.newTile.CoordinateHeights = [16, 16];
        TileObjectData.newTile.StyleHorizontal = true;
        TileObjectData.newTile.HookPostPlaceMyPlayer = HologramTileEntity.Template.Generic_HookPostPlaceMyPlayer;
        TileObjectData.addTile(Type);
        AddMapEntry(new Color(80, 180, 255));
        DustType = 226;
    }

    public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
    {
        Tile tile = Main.tile[i, j];
        int col = tile.frameX % 54 / 18;
        int row = tile.frameY % 36 / 18;
        HologramTileEntity? entity = HologramTileEntity.At(i, j);
        if (entity == null || !entity.TileInvisible)
            DrawFrame(i, j, col, row, spriteBatch);
        return false;
    }

    void DrawFrame(int i, int j, int col, int row, SpriteBatch sb)
    {
        Texture2D tex = TextureAssets.Tile[Type].Value;
        const int period = HoldTicks + FadeTicks;
        int t = (int)(Main.GameUpdateCount % (period * Frames));
        int current = t / period;
        int next = (current + 1) % Frames;
        int inFrame = t % period;
        float fade = inFrame < HoldTicks ? 0 : (inFrame - HoldTicks) / (float)FadeTicks;
        int off = Main.drawToScreen ? 0 : Main.offScreenRange;
        Vector2 sp = Main.screenPosition;
        var pos = new Vector2(i * 16 - sp.X + off, j * 16 - sp.Y + off);
        Color color = Lighting.GetColor(i, j);
        sb.Draw(tex, pos, new Rectangle(col * 18, current * FrameStride + row * 16, 16, 16), color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        if (fade > 0)
        {
            var faded = new Color(color.R, color.G, color.B, (byte)Util.Round(color.A * fade));
            sb.Draw(tex, pos, new Rectangle(col * 18, next * FrameStride + row * 16, 16, 16), faded, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        }
    }

    public override bool RightClick(int i, int j)
    {
        HologramTileEntity? entity = HologramTileEntity.At(i, j);
        if (entity == null)
            return false;
        HologramMenu.Open(entity, this);
        return true;
    }

    public override void MouseOver(int i, int j)
    {
        Player player = Main.LocalPlayer;
        player.noThrow = 2;
        player.cursorItemIconEnabled = true;
        if (itemType < 0)
            itemType = ModContent.Find<ModItem>(Mod.Name, ItemName).Type;
        player.cursorItemIconID = itemType;
    }

    public override void KillMultiTile(int i, int j, int frameX, int frameY) => HologramTileEntity.Template.Kill(i, j);
}
