using System.Collections.Generic;
using BetterReforgeMod.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;

namespace BetterReforgeMod.Common.UI;

public class ModButton
{
    public static readonly List<ModButton> Buttons = new();
    public static List<ModButton> ClickedButtons = new();

    public static int Time => ModInterface.Time;

    public Rectangle Area;
    public bool Hovered, Pressed, Clicked;
    public int ClickedTime = 10;
    public int LastClicked;

    public ModButton()
    {
        Buttons.Add(this);
    }

    public void Clear()
    {
        Hovered = Pressed = Clicked = false;
        LastClicked = 0;
    }

    public virtual Color GetColor() => Color.White;

    public virtual Texture2D GetIconTexture() => default;

    public virtual Texture2D GetTexture() => default;

    public virtual void OnMouseOver() { }

    public virtual void OnMouseOut() { }

    public virtual void WhileHovered() { }

    public virtual int? ClickSound() => 12;

    public virtual void OnClick() { }

    public virtual void Draw(Rectangle rect)
    {
        Texture2D texture = GetTexture();
        if (texture.IsNull)
            return;
        Color color = GetColor();
        DrawUtils.Rect(texture, rect, color);
        Texture2D icon = GetIconTexture();
        if (!icon.IsNull)
            DrawUtils.Rect(icon, rect, color);
    }

    public static void ClearAll()
    {
        ClickedButtons = new List<ModButton>();
        foreach (ModButton button in Buttons)
            button.Clear();
    }

    public static void Update() => UpdateClickedButtons();

    public static void UpdateButton(ModButton button, Rectangle rect)
    {
        bool wasHovered = button.Hovered;
        button.Hovered = !button.Clicked && rect.Contains(Main.mouseX, Main.mouseY);
        if (button.Hovered)
        {
            if (!wasHovered)
                button.OnMouseOver();
            button.WhileHovered();
        }
        else if (wasHovered)
        {
            button.OnMouseOut();
        }

        if (Main.mouseLeft && Main.mouseLeftRelease && !button.Pressed && !button.Clicked && button.Hovered)
        {
            button.Pressed = true;
        }
        else if (Main.mouseLeftRelease && button.Pressed)
        {
            button.Pressed = false;
            if (button.Hovered)
                HandleClick(button);
        }
    }

    static void UpdateClickedButtons()
    {
        var clicked = new List<ModButton>();
        foreach (ModButton button in ClickedButtons)
        {
            if (Time - button.LastClicked >= button.ClickedTime)
            {
                button.Clicked = false;
                continue;
            }
            clicked.Add(button);
        }
        ClickedButtons = clicked;
    }

    static void HandleClick(ModButton button)
    {
        if (button.ClickSound() is int soundId)
            SoundEngine.PlaySound(soundId, Main.LocalPlayer.Center, 1, 0f);

        button.OnClick();

        button.Pressed = false;
        button.Clicked = true;
        button.LastClicked = Time;
        ClickedButtons.Add(button);
    }
}
