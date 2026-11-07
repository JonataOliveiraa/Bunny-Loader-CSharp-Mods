using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;

namespace TeleportKeeperMod.Common;

public class MapIcons : ModMapLayer
{
    static SpriteFrame frame;
    static bool frameReady, broken;

    // Using a point can remove it while the list is being walked.
    static readonly List<TeleportPoint> pointBuffer = new();
    static readonly List<DeathPoint> deathBuffer = new();
    static readonly char[] Brackets = { '[', ']' };

    static SpriteFrame Frame()
    {
        if (!frameReady)
        {
            frame = SpriteFrame.New(1, 1, 0, 0);
            frame.PaddingY = 0;
            frameReady = true;
        }
        return frame;
    }

    static string ColoredName(TeleportPoint point)
    {
        if (point.color == "White" || point.name.IndexOfAny(Brackets) >= 0)
            return point.name;
        return "[c/" + NameColors.NameColorHex(point.color) + ":" + point.name + "]";
    }

    static void MouseText(string text, int rare) => Main.instance.MouseText(text, rare, 0, -1, -1, -1, -1, 0);

    static bool Released() => !Main.mouseLeft && Main.mouseLeftRelease;

    static bool DrawIcon(ref MapOverlayDrawContext context, Texture2D texture, float x, float y) =>
        context.Draw(texture, new Vector2(x, y), Color.White, Frame(), 1f, 1.15f, Alignment.Center).IsMouseOver;

    public override void Draw(ref MapOverlayDrawContext context, ref string text)
    {
        if (broken)
            return;
        try
        {
            DrawIcons(ref context);
        }
        catch (Exception e)
        {
            broken = true;
            ModContent.GetInstance<TeleportKeeper>().Logger.Info("map icons disabled after an error: " + e);
        }
    }

    static void DrawIcons(ref MapOverlayDrawContext context)
    {
        if (!Data.loaded)
            return;
        pointBuffer.Clear();
        pointBuffer.AddRange(Data.points);
        foreach (TeleportPoint point in pointBuffer)
        {
            if (DrawIcon(ref context, Icons.IconTexture(point.icon), point.x, point.y))
            {
                MouseText(ColoredName(point), 0);
                if (Released())
                    Teleports.UsePoint(point);
            }
        }
        deathBuffer.Clear();
        deathBuffer.AddRange(Data.deaths);
        foreach (DeathPoint death in deathBuffer)
        {
            if (DrawIcon(ref context, Icons.DeathCrystal(), death.x, death.y))
            {
                MouseText(Util.T("DeathTpOf", death.owner), 10);
                if (Released())
                    Teleports.UseDeath(death);
            }
        }
    }
}
