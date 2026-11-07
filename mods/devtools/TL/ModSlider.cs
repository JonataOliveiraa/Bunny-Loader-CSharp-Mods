using System;
using DevToolMod.TL.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using static DevToolMod.TL.Utilities.UIScaleHelper;

namespace DevToolMod.TL;

public class ModSlider
{
    public double MinValue;
    public double MaxValue;
    public double DefaultValue;
    public double Value;
    public double Position;
    public Rectangle Area = new(0, 0, 1, 1);
    public bool Hovered;
    public bool Clicked;

    public ModSlider(double min = 0, double max = 1, double? defaultValue = null)
    {
        MinValue = min;
        MaxValue = max;
        DefaultValue = Value = defaultValue ?? min;
    }

    public virtual Texture2D GetBackTexture() => default;

    public virtual Color GetBackTextureColor() => Color.White;

    public virtual Texture2D GetButtonTexture() => default;

    public virtual Color GetButtonTextureColor() => Color.White;

    public virtual double GetButtonPosition() => Math.Max(0, Math.Min(1, (Value - MinValue) / (MaxValue - MinValue)));

    public virtual void OnClick() { }

    public virtual void Draw(Rectangle rect)
    {
        Position = GetButtonPosition();
        DrawHelper.Rectangle(GetBackTexture(), rect, GetBackTextureColor());
        Texture2D buttonTexture = GetButtonTexture();
        var buttonRect = new Rectangle((int)(rect.X + (rect.Width - S(buttonTexture.Width)) * Position), rect.Y, (int)S(buttonTexture.Width), (int)S(buttonTexture.Height));
        DrawHelper.Rectangle(buttonTexture, buttonRect, GetButtonTextureColor());
    }

    public static void UpdateSlider(ModSlider slider, Rectangle rect)
    {
        slider.Hovered = rect.Contains(Main.mouseX, Main.mouseY);

        if (Main.mouseLeft && Main.mouseLeftRelease && slider.Hovered)
        {
            slider.Clicked = true;
            // While dragging the slider, the game must not turn the view (same as ModButton).
            ModInterface.CaptureDrag(-1);
        }

        if (slider.Clicked)
        {
            double t = Math.Max(0, Math.Min(1, (double)(Main.mouseX - rect.X) / rect.Width));
            slider.Value = slider.MinValue + t * (slider.MaxValue - slider.MinValue);
            slider.OnClick();
            if (!Main.mouseLeft)
                slider.Clicked = false;
        }
    }
}
