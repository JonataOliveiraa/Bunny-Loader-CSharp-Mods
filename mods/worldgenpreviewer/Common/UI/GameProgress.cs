using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.UI;
using Terraria.WorldBuilding;
using WorldGenPreviewerMod.Common.Gen;

namespace WorldGenPreviewerMod.Common.UI;

public class GameProgress : UIElement
{
    public const float SCALE = 0.6f;

    readonly float scale;

    public GameProgress(float scale)
    {
        this.scale = scale;
        IgnoresMouseInteraction = true;
    }

    static float Clamp(double v) => v < 0 ? 0 : v > 1 ? 1 : (float)v;

    static Color Mix(Color a, Color b) => new(Util.Round((a.R + b.R) / 2.0), Util.Round((a.G + b.G) / 2.0), Util.Round((a.B + b.B) / 2.0), Util.Round((a.A + b.A) / 2.0));

    static void ProgressNow(out float value, out float total, out string message)
    {
        GenerationProgress p = State.progress;
        double v = 0, t = double.NaN;
        message = "";
        if (!p.IsNull)
        {
            v = p.Value;
            t = p.TotalProgress;
            message = p.Message ?? "";
        }
        if (double.IsNaN(v))
            v = 0;
        if (!double.IsFinite(t))
        {
            double sum = 0, done = 0;
            double[] weights = State.weights;
            int index = State.index;
            for (int i = 0; i < weights.Length; i++)
            {
                double w = weights[i];
                sum += w;
                if (i < index)
                    done += w;
                else if (i == index)
                    done += w * Clamp(v);
            }
            t = sum > 0 ? done / sum : 0;
        }
        value = Clamp(v);
        total = Clamp(t);
    }

    public static int ProgressHeight(float scale)
    {
        String_Layout status = GameBindings.StatusLayout;
        SpriteFont font = status.GetFont();
        float h = font.MeasureString("Ag").Y * status.Scale * SCALE * scale;
        GameBindings.ReadBars(out GameBindings.Bar first, out GameBindings.Bar second);
        h += first.texture.Height * first.backingScale * SCALE * scale + 2;
        h += second.texture.Height * second.backingScale * SCALE * scale + 2;
        return (int)Math.Ceiling(h);
    }

    float DrawBar(SpriteBatch spriteBatch, in GameBindings.Bar bar, float k, float cx, float y, float value, float total)
    {
        float s = bar.backingScale * k;
        float w = bar.texture.Width * s, h = bar.texture.Height * s;
        float left = cx - w / 2;
        float fx = bar.fillingLocation.X * s, fy = bar.fillingLocation.Y * s;
        int fw = Util.Round(bar.fillingSize.X * s), fh = Util.Round(bar.fillingSize.Y * s);
        float fraction = bar.total ? total : value;
        GUIProgressBar.DrawFilling2(spriteBatch, new Vector2(left + fx, y + fy), fh, Util.Round(fw * fraction), fw, bar.color, Mix(bar.color, bar.empty), bar.empty);
        spriteBatch.Draw(bar.texture, new Rectangle(Util.Round(left), Util.Round(y), Util.Round(w), Util.Round(h)), Color.White);
        return y + h + 2;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle d = GetDimensions();
        ProgressNow(out float value, out float total, out string message);
        float k = SCALE * scale;
        float cx = d.X + d.Width / 2;
        String_Layout status = GameBindings.StatusLayout;
        SpriteFont font = status.GetFont();
        float textScale = status.Scale * k;
        Vector2 size = font.MeasureString(message.Length > 0 ? message : " ");
        float y = d.Y;
        Utils.DrawBorderStringFourWay(spriteBatch, font, message, cx - size.X * textScale / 2, y, status.Color, Color.Black, Vector2.Zero, textScale);
        y += font.MeasureString("Ag").Y * textScale;
        GameBindings.ReadBars(out GameBindings.Bar first, out GameBindings.Bar second);
        y = DrawBar(spriteBatch, first, k, cx, y, value, total);
        DrawBar(spriteBatch, second, k, cx, y, value, total);
    }
}
