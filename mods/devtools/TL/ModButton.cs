using System.Collections.Generic;
using DevToolMod.TL.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;

namespace DevToolMod.TL;

public class ModButton
{
    public static readonly List<ModButton> Buttons = new();
    // Cooldown frames after a click, so a finger that just lifted does not hit the next button
    // with the same touch (Bunny's own GUI ignores input for 2 frames after opening, the same
    // kind of protection).
    public static int Cooldown;
    public static List<ModButton> ClickedButtons = new();

    public static int Time => ModInterface.Time;

    public Rectangle Area = new(0, 0, 1, 1);

    public bool Hovered;
    public bool Pressed;
    public bool Clicked;

    public int ClickedTime = 10;
    public int LastClicked;

    public ModButton()
    {
        Buttons.Add(this);
    }

    public virtual void Clear()
    {
        Hovered = Pressed = Clicked = false;
        LastClicked = 0;
    }

    public virtual Color GetColor() => Color.White;

    public virtual Texture2D GetIconTexture() => default;

    public virtual Texture2D GetTexture() => default;

    public virtual int? ClickSound() => 12;

    public virtual void OnClick() { }

    public virtual void Draw(Rectangle rect, Texture2D icon = default)
    {
        Texture2D texture = GetTexture();
        if (!texture.IsNull)
        {
            Color color = GetColor();
            DrawHelper.Rectangle(texture, rect, color);
            if (!icon.IsNull) DrawHelper.Rectangle(icon, rect, color);
        }
    }

    public static void ClearAll()
    {
        ClickedButtons = new List<ModButton>();
        foreach (ModButton button in Buttons)
            button.Clear();
    }

    public static void UpdateAll()
    {
        UpdateClickedButtons();
        if (Cooldown > 0) Cooldown--;
    }

    public static void UpdateButton(ModButton button, Rectangle rect)
    {
        button.Hovered = !button.Clicked && rect.Contains(Main.mouseX, Main.mouseY);

        if (Cooldown <= 0 && Main.mouseLeft && Main.mouseLeftRelease && !button.Pressed && !button.Clicked && button.Hovered)
        {
            button.Pressed = true;
            // This touch landed on a mod button: grab the UI cursor drag, or the game keeps
            // turning the view or moving as the finger slides.
            ModInterface.CaptureDrag(-1);
        }
        else if (Main.mouseLeftRelease && button.Pressed)
        {
            button.Pressed = false;
            if (button.Hovered) Click(button);
        }
    }

    static void UpdateClickedButtons()
    {
        var clicked = new List<ModButton>();
        foreach (ModButton b in ClickedButtons)
        {
            if (Time - b.LastClicked >= b.ClickedTime)
            {
                b.Clicked = false;
                continue;
            }
            clicked.Add(b);
        }
        ClickedButtons = clicked;
    }

    static void Click(ModButton button)
    {
        int? soundId = button.ClickSound();
        if (soundId != null)
            SoundEngine.PlaySound(soundId.Value, Main.LocalPlayer.Center, 1, 0f);

        button.OnClick();

        button.Pressed = false;
        button.Clicked = true;
        button.LastClicked = Time;
        ClickedButtons.Add(button);
        Cooldown = 3;
    }
}
