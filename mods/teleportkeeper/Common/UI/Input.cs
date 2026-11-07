using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameInput;

namespace TeleportKeeperMod.Common.UI;

public class TextField
{
    static bool supported = true;

    public string text;
    public string old;
    public bool focused;

    public TextField(string text = "")
    {
        this.text = text;
        old = text;
    }

    public bool Focus()
    {
        if (!supported)
            return false;
        Main.inputTextEnter = false;
        Main.inputTextEscape = false;
        Main.clrInput();
        old = text;
        focused = true;
        return true;
    }

    public void Blur()
    {
        if (!focused)
            return;
        focused = false;
        old = text;
        PlayerInput.WritingText = false;
    }

    public string? Update(float x, float y, float w, float h, int max = 40)
    {
        if (!focused)
            return null;
        try
        {
            PlayerInput.WritingText = true;
            string? next = Main.GetInputText(text, new Rectangle(Util.Round(x), Util.Round(y), Util.Round(w), Util.Round(h)), -1, false, false, false, true);
            bool changed = next != null && next != text;
            if (changed)
                text = next!.Length > max ? next[..max] : next;
            if (Main.inputTextEnter)
            {
                Main.inputTextEnter = false;
                Blur();
                return "enter";
            }
            if (Main.inputTextEscape)
            {
                Main.inputTextEscape = false;
                focused = false;
                text = old;
                return "escape";
            }
            return changed ? "changed" : null;
        }
        catch (Exception e)
        {
            supported = false;
            focused = false;
            Util.LogOnce("input", "game text input is unavailable (" + e.Message + ")");
            return null;
        }
    }
}
