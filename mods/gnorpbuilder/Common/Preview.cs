using System;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace GnorpBuilderMod.Common;

// The anchor touch button, and the translucent image and dust outline of the held globe's structure.
public class PreviewSystem : ModSystem
{
    const int PreviewDust = 89;
    const int LargeArenaW = 255, LargeArenaH = 74;

    public static ModTouchButton AnchorButton = null!;

    float dustTimer;

    public override void Load()
    {
        AnchorButton = TouchButtonLoader.RegisterButton(Mod, "Anchor");
        AnchorButton.Visible = () =>
        {
            try { return Globe.HeldPreview(Main.LocalPlayer) != null; }
            catch (Exception) { return false; }
        };
        AnchorButton.OnPress = player =>
        {
            AnchorPoint anchor = Placement.CycleAnchor();
            SoundEngine.PlaySound(SoundID.Item1, player.Center, 0f, 1f);
            CombatText.NewText(player.getRect(), Color.White, Util.T("BuildConfig.Anchors." + anchor), false, false);
        };
    }

    public override void OnModLoad()
    {
        On_Main.DoDraw_DrawNPCsOverTiles += (orig, self) =>
        {
            orig(self);
            try
            {
                DrawPreview();
            }
            catch (Exception e)
            {
                Mod.Logger.Warn("the preview failed: " + e.Message);
            }
        };
    }

    public static string AnchorName() => Util.T("BuildConfig.Anchors." + GnorpConfig.Instance.Anchor);

    static Color BiomeColor(Player player)
    {
        if (player.ZoneHallow) return Color.Pink;
        if (player.ZoneGlowshroom) return Color.Blue;
        if (player.ZoneJungle) return Color.LimeGreen;
        if (player.ZoneSnow) return Color.Cyan;
        if (player.ZoneBeach) return new Color(150, 100, 60);
        if (player.ZoneDesert) return Color.Yellow;
        if (player.ZoneRockLayerHeight) return Color.Gray;
        return Color.White;
    }

    static HeldGlobe? Held()
    {
        Player player = Main.LocalPlayer;
        if (player.IsNull || !player.active || player.dead || Main.gameMenu || Main.mapFullscreen) return null;
        return Globe.HeldPreview(player);
    }

    public override void PostUpdateEverything()
    {
        if (Main.netMode == NetmodeID.Server || !GnorpConfig.Instance.EnablePreviews) return;
        if (Held() is not HeldGlobe held) return;

        dustTimer += 2f / 60f;
        if (dustTimer < 0.05f) return;
        dustTimer = 0f;

        Globe globe = held.Globe;
        bool arena = globe.Arena != null;
        int sizeW = LargeArenaW, sizeH = LargeArenaH;
        if (!arena && !Structures.Dimensions(held.Name, out sizeW, out sizeH)) return;

        Placement.TopLeftAt(held.Name, Main.MouseWorld, out int atX, out int atY);
        float x0 = atX * 16, y0 = atY * 16, w = sizeW * 16, h = sizeH * 16;
        Color color = Color.Multiply(globe.Biome != null || globe.Elevator == "Biome" ? BiomeColor(Main.LocalPlayer) : Color.White, 0.5f);
        float chance = arena ? 0.15f : 0.35f;
        int count = arena ? sizeW + sizeH : (sizeW + sizeH) * 2;
        float perimeter = 2 * (w + h);

        for (int i = 0; i < count; i++)
        {
            if (Main.rand.NextFloat() > chance) continue;

            // Walks the rectangle's outline, clockwise from the top left corner.
            float d = (float)i / count * perimeter;
            float x, y;
            if (d < w)
            {
                x = x0 + d;
                y = y0;
            }
            else if ((d -= w) < h)
            {
                x = x0 + w;
                y = y0 + d;
            }
            else if ((d -= h) < w)
            {
                x = x0 + w - d;
                y = y0 + h;
            }
            else
            {
                d -= w;
                x = x0;
                y = y0 + h - d;
            }

            var velocity = new Vector2(Main.rand.NextFloat() * 0.2f - 0.1f, Main.rand.NextFloat() * 0.2f - 0.1f);
            Dust dust = Dust.NewDustPerfect(new Vector2(x, y), PreviewDust, velocity, 0, color, 0.7f + Main.rand.NextFloat() * 0.2f);
            if (!dust.IsNull) dust.noGravity = true;
        }
    }

    static void DrawPreview()
    {
        if (Main.gameMenu || Main.netMode == NetmodeID.Server) return;
        GnorpConfig config = GnorpConfig.Instance;
        if (!config.ImagePreview) return;
        if (Held() is not HeldGlobe held || held.Texture == null) return;
        Texture2D texture = held.Texture.Value;
        if (texture.IsNull) return;

        Placement.TopLeftAt(held.Name, Main.MouseWorld, out int atX, out int atY);
        Vector2 screen = Main.screenPosition;
        var position = new Vector2(atX * 16 - screen.X, atY * 16 - screen.Y);

        SpriteBatch sb = Main.spriteBatch;
        bool open = sb._beginCalled;
        if (!open)
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, default, Main.GameViewMatrix.TransformationMatrix, true);
        try
        {
            sb.Draw(texture, position, default(GameNullable<Rectangle>), Color.Multiply(Color.White, config.PreviewOpacity / 100f), 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        }
        finally
        {
            if (!open) sb.End();
        }
    }
}
