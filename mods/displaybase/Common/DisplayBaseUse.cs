using System;
using System.Diagnostics;
using DisplayBaseMod.Content.TileEntities;
using DisplayBaseMod.Content.Tiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DisplayBaseMod.Common;

// A tap on a base puts the held item on it, instead of using the item.
public class DisplayBaseUse : GlobalItem
{
    // The original counted 18 frames at 60 per second.
    const double HoldSeconds = 18 / 60.0;

    sealed class Press
    {
        public int X, Y, Type;
        public long Start;
        public bool Mining;
    }

    static Press? press;
    static bool placedThisPress;

    static bool IsTool(Item item) => item.pick > 0 || item.axe > 0 || item.hammer > 0;

    static Point16? BaseAt(Player player, int x, int y)
    {
        if (!WorldGen.InWorld(x, y))
            return null;
        Tile tile = Main.tile[x, y];
        if (!tile.HasTile || tile.TileType != ModContent.TileType<DisplayBaseTile>())
            return null;
        float cx = player.position.X / 16 + player.width / 32f, cy = player.position.Y / 16 + player.height / 32f;
        if (Math.Abs(x - cx) > Player.tileRangeX + 2 || Math.Abs(y - cy) > Player.tileRangeY + 2)
            return null;
        return TEDisplayBase.TopLeft(x, y);
    }

    static Point16? TargetBase(Player player)
    {
        var mouse = Main.MouseWorld;
        return BaseAt(player, (int)MathF.Floor(mouse.X / 16), (int)MathF.Floor(mouse.Y / 16))
            ?? BaseAt(player, Player.tileTargetX, Player.tileTargetY);
    }

    static void Place(Player player, int x, int y)
    {
        if (TEDisplayBase.Ensure(x, y) == null && Main.netMode != NetmodeID.MultiplayerClient)
            return;
        try
        {
            TEDisplayBase.OnPlayerInteraction(player, x, y);
        }
        catch (Exception e)
        {
            ModContent.GetInstance<DisplayBase>().Logger.Error("error placing the item: " + e);
        }
    }

    public override bool CanUseItem(Item item, Player player)
    {
        if (player.whoAmI != Main.myPlayer || item.favorited)
            return true;
        if (TargetBase(player) is not Point16 top)
        {
            press = null;
            return true;
        }
        if (!IsTool(item))
        {
            if (!placedThisPress && !TEDisplayBase.Recent())
            {
                placedThisPress = true;
                Place(player, top.X, top.Y);
            }
            return false;
        }
        if (press is { Mining: true })
            return true;
        long now = Stopwatch.GetTimestamp();
        if (press == null || press.X != top.X || press.Y != top.Y || press.Type != item.type)
            press = new Press { X = top.X, Y = top.Y, Type = item.type, Start = now };
        if ((now - press.Start) / (double)Stopwatch.Frequency >= HoldSeconds)
        {
            press.Mining = true;
            return true;
        }
        return false;
    }

    // The finger left the screen: a tool's quick tap puts it on display.
    public static void OnReleased(Player player)
    {
        placedThisPress = false;
        if (press == null)
            return;
        Press tap = press;
        press = null;
        if (tap.Mining)
            return;
        Item held = player.inventory[player.selectedItem];
        if (held.IsNull || held.type != tap.Type || held.favorited)
            return;
        Place(player, tap.X, tap.Y);
    }
}

public class DisplayBasePlayer : ModPlayer
{
    public override void PostUpdate()
    {
        if (Player.whoAmI != Main.myPlayer || Player.controlUseItem)
            return;
        DisplayBaseUse.OnReleased(Player);
    }
}
