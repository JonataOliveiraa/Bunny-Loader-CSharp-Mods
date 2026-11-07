using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OverhealthMod.Common.Configs;
using Terraria;
using Terraria.ModLoader;

namespace OverhealthMod.Common.Overhealth;

public static class OverhealthHud
{
    static Texture2D heart, bar, pixel;
    static bool loaded;
    static int frame;

    static Texture2D Load(string name) => ModContent.Request<Texture2D>("overhealth/Assets/Textures/" + name).Value;

    static Color Faded(int r, int g, int b, float opacity)
    {
        float k = Math.Clamp(opacity, 0f, 1f);
        return new Color((int)(r * k), (int)(g * k), (int)(b * k), (int)(255 * k));
    }

    public static void Draw(OverhealthConfig config)
    {
        frame++;
        if (!config.Enabled || !config.ShowUI || Main.gameMenu)
            return;

        float target = config.DemoHud ? Math.Max(OverhealthState.Over, 100) : OverhealthState.Over;
        OverhealthState.Shown += (target - OverhealthState.Shown) * 0.2f;
        if (MathF.Abs(target - OverhealthState.Shown) < 0.5f)
            OverhealthState.Shown = target;
        float shown = OverhealthState.Shown;
        if (shown < 0.5f)
            return;

        if (!loaded)
        {
            heart = Load("heart");
            bar = Load("bar");
            pixel = Load("px");
            loaded = true;
        }

        int width = Main.screenWidth, height = Main.screenHeight;
        float k = height / 1080f * (config.HudScale / 100f);
        float opacity = config.HudOpacity / 100f;
        float x = width * config.HudX / 100f, y = height * config.HudY / 100f;

        Player player = Main.player[Main.myPlayer];
        int maxOver = OverhealthState.MaxOverhealth(config, player.statLifeMax2);
        float fraction = Math.Clamp(shown / maxOver, 0f, 1f);
        float barWidth = 120f * k, barHeight = 8f * k;
        float pulse = 1f + 0.06f * MathF.Sin(frame * 0.12f);

        SpriteBatch batch = Main.spriteBatch;
        batch.Begin(SpriteSortMode.Deferred, true);
        try
        {
            // Dark backing, then the gold bar cropped to the fraction.
            batch.Draw(pixel, new Vector2(x, y + 20f * k), null, Faded(0, 0, 0, opacity * 0.65f), 0f, Vector2.Zero, new Vector2(barWidth, barHeight), SpriteEffects.None, 0f);
            var source = new Rectangle(0, 0, Math.Max(1, (int)MathF.Round(36f * fraction)), 12);
            batch.Draw(bar, new Vector2(x, y + 20f * k), source, Faded(255, 255, 255, opacity), 0f, Vector2.Zero, new Vector2(barWidth / 36f, barHeight / 12f), SpriteEffects.None, 0f);

            // The gold heart, pulsing a little.
            float heartScale = 30f * k / 26f * pulse;
            batch.Draw(heart, new Vector2(x - 4f * k, y - 4f * k - (heartScale - 30f * k / 26f) * 13f), null, Faded(255, 255, 255, opacity), 0f, Vector2.Zero, new Vector2(heartScale, heartScale), SpriteEffects.None, 0f);

            Utils.DrawBorderString(batch, "+" + (int)MathF.Round(shown), new Vector2(x + 32f * k, y - 4f * k), Faded(255, 233, 153, opacity), 0.9f * k, 0f, 0f, -1);
        }
        finally
        {
            batch.End();
        }
    }
}
