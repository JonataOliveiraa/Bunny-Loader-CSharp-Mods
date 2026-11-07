using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace TooltipIconMod.Common;

public class TooltipIconGlobalItem : GlobalItem
{
    const int RequiredSpacePixel = 26;

    static string? padding;
    static bool failed;

    static string Padding()
    {
        if (padding != null)
            return padding;
        SpriteFont font = FontAssets.MouseText.Value;
        string text = " ";
        while (text.Length < 64 && font.MeasureString(text).X < RequiredSpacePixel)
            text += " ";
        padding = text;
        return padding;
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        TooltipIcons.Initialize();
        foreach (TooltipLine line in tooltips)
        {
            if (TooltipIcons.HasIcon(line.Mod, line.Name))
                line.Text += Padding();
        }
    }

    public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
    {
        Icon? icon = TooltipIcons.GetIcon(line.Mod, line.Name);
        if (icon == null)
            return true;
        float scale = line.BaseScale.X;
        float alpha = line.Color.A / 255f;
        try
        {
            icon.Draw(item, line, Main.spriteBatch, new Vector2(line.X, line.Y), Color.Multiply(Color.White, alpha), scale);
        }
        catch (Exception e)
        {
            if (!failed)
            {
                failed = true;
                Mod.Logger.Warn("error while drawing the icon of " + line.Mod + "/" + line.Name + ": " + e);
            }
        }
        line.X += (int)(RequiredSpacePixel * scale);
        return true;
    }
}
