using System;
using DisplayBaseMod.Common.Configs;
using DisplayBaseMod.Content.Items;
using DisplayBaseMod.Content.TileEntities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Terraria.UI;

namespace DisplayBaseMod.Content.Tiles;

public class DisplayBaseTile : ModTile
{
    static Asset<Texture2D>? glitter;
    static bool glitterTried;

    static Asset<Texture2D>? Glitter(Mod mod)
    {
        if (!glitterTried)
        {
            glitterTried = true;
            try
            {
                glitter = mod.Assets.Request<Texture2D>("Assets/Images/Glitter");
            }
            catch (Exception e)
            {
                mod.Logger.Warn("The glitter texture did not load.: " + e.Message);
            }
        }
        return glitter;
    }

    static int Byte(float value) => Math.Clamp((int)MathF.Floor(value + 0.5f), 0, 255);

    static Color Scaled(Color c, float k) => new(Byte(c.R * k), Byte(c.G * k), Byte(c.B * k), Byte(c.A * k));

    static Color Additive(int r, int g, int b, float k) => new(Byte(r * k), Byte(g * k), Byte(b * k), 0);

    static TEDisplayBase? EntityAt(int i, int j)
    {
        TEDisplayBase? entity = TEDisplayBase.At(i, j);
        return entity != null && !entity.IsEmpty ? entity : null;
    }

    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileLavaDeath[Type] = true;
        Main.tileWaterDeath[Type] = false;
        Main.tileLighted[Type] = true;
        Main.tileNoAttach[Type] = true;
        TileID.Sets.DisableSmartCursor[Type] = true;
        TileID.Sets.AvoidedByNPCs[Type] = true;
        TileID.Sets.InteractableByNPCs[Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
        TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 18 };
        TileObjectData.newTile.HookPostPlaceMyPlayer = TEDisplayBase.Template.Generic_HookPostPlaceMyPlayer;
        TileObjectData.newTile.StyleHorizontal = true;
        TileObjectData.addTile(Type);
        RegisterItemDrop(ModContent.ItemType<DisplayBaseItem>(), 0);
        RegisterItemDrop(ModContent.ItemType<DisplayBaseItemGranite>(), 1);
        AddMapEntry(new Color(233, 207, 94));
    }

    public override bool CreateDust(int i, int j, ref int type) => false;

    public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 0 : 2;

    public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
    {
        float light = DisplayBaseConfig.Instance.LightIntensity;
        if (light <= 0 || EntityAt(i, j) == null)
            return;
        r = g = b = light;
    }

    public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
    {
        TEDisplayBase? entity = EntityAt(i, j);
        if (entity == null)
            return;
        DisplayBaseConfig cfg = DisplayBaseConfig.Instance;
        Item item = entity.DrawItem();

        Main.instance.LoadItem(entity.ItemType);
        Texture2D texture = TextureAssets.Item[entity.ItemType].Value;
        DrawAnimation animation = Main.itemAnimations[entity.ItemType];
        Rectangle frame = !animation.IsNull ? animation.GetFrame(texture, -1) : new Rectangle(0, 0, texture.Width, texture.Height);
        int width = frame.Width, height = frame.Height;

        float scale = 1f;
        if (width > 40 || height > 40)
            scale = width > height ? 40f / width : 40f / height;
        scale *= item.scale;

        Color light = Lighting.GetColor(i + 1, j + 1);
        Color color = light;
        float brightness = Lighting.Brightness(i + 1, j + 1);
        if (cfg.UseBrightness)
        {
            color = Color.White;
            brightness = 1f;
        }
        float lightScale = 1f;
        ItemSlot.GetItemLight(ref color, ref lightScale, item, false, 1f);
        scale *= lightScale;

        float off = Main.drawToScreen ? 0 : Main.offScreenRange;
        Vector2 sp = Main.screenPosition;
        var center = new Vector2(i * 16 - MathF.Truncate(sp.X) + 24 + off, j * 16 - MathF.Truncate(sp.Y) + 24 + off);
        var origin = new Vector2(width / 2f, height / 2f);

        Asset<Texture2D>? glitterAsset = cfg.UseGlitter ? Glitter(Mod) : null;
        if (glitterAsset != null)
        {
            Texture2D tex = glitterAsset.Value;
            float c = Math.Min(Math.Max(brightness, 0.5f), 1.2f);
            float k = c * c * 100 / 255;
            float spin = MathF.PI * 2 * (Main.LocalPlayer.miscCounter / 100f);
            var half = new Vector2(tex.Width / 2f, tex.Height / 2f);
            spriteBatch.Draw(tex, center, null, Additive(180, 165, 220, k), spin, half, 0.25f + MathF.Cos(spin) * 0.2f, SpriteEffects.FlipHorizontally, 0f);
            spriteBatch.Draw(tex, center, null, Additive(180, 165, 220, k), -spin, half, 0.25f - MathF.Sin(spin) * 0.2f, SpriteEffects.None, 0f);
        }

        if (cfg.UseOutline)
        {
            float t = Main.LocalPlayer.miscCounterNormalized + Main.GlobalTimeWrappedHourly * 0.04f;
            for (float f = 0; f < 1; f += 0.25f)
            {
                float a = (f + t * 2) * MathF.PI * 2;
                var at = new Vector2(center.X - MathF.Sin(a) * 4, center.Y + MathF.Cos(a) * 4);
                spriteBatch.Draw(texture, at, frame, Scaled(new Color(230, 230, 240, 25), brightness), 0f, origin, scale, SpriteEffects.None, 0f);
            }
            for (float f = 0; f < 1; f += 0.34f)
            {
                float a = (f + t * 2) * MathF.PI * 2;
                var at = new Vector2(center.X - MathF.Sin(a) * 2, center.Y + MathF.Cos(a) * 2);
                spriteBatch.Draw(texture, at, frame, Scaled(new Color(180, 165, 220, 34), brightness), 0f, origin, scale, SpriteEffects.None, 0f);
            }
        }

        spriteBatch.Draw(texture, center, frame, color, 0f, origin, scale, SpriteEffects.None, 0f);
        Color tint = item.color;
        if (tint.R != 0 || tint.G != 0 || tint.B != 0 || tint.A != 0)
            spriteBatch.Draw(texture, center, frame, item.GetColor(light), 0f, origin, scale, SpriteEffects.None, 0f);
    }

    public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
    {
        Point16 top = TEDisplayBase.TopLeft(i, j);
        TEDisplayBase? entity = TEDisplayBase.At(top.X, top.Y);
        if (entity == null || entity.IsEmpty)
            return;
        fail = true;
        entity.DropItem();
        if (!Main.dedServ)
            Main.LocalPlayer.InterruptItemUsageIfOverTile(Type);
    }

    public override void KillMultiTile(int i, int j, int frameX, int frameY) => TEDisplayBase.Template.Kill(i, j);

    public override bool RightClick(int i, int j)
    {
        try
        {
            TEDisplayBase.OnPlayerInteraction(Main.player[Main.myPlayer], i, j);
        }
        catch (Exception e)
        {
            Mod.Logger.Error("error when using the base: " + e);
        }
        return true;
    }

    public override void MouseOver(int i, int j)
    {
        Point16 top = TEDisplayBase.TopLeft(i, j);
        TEDisplayBase? entity = TEDisplayBase.At(top.X, top.Y);
        Player player = Main.player[Main.myPlayer];
        bool granite = Main.tile[top.X, top.Y].TileFrameX / 54 == 1;
        int baseItem = granite ? ModContent.ItemType<DisplayBaseItemGranite>() : ModContent.ItemType<DisplayBaseItem>();
        player.cursorItemIconID = entity != null && !entity.IsEmpty ? entity.ItemType : baseItem;
        player.noThrow = 2;
        player.cursorItemIconEnabled = true;
    }
}
