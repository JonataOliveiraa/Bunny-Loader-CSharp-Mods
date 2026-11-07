using System;
using System.Collections.Generic;
using BunnyLoader.Interop;
using ChestPeekMod.Common.Configs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace ChestPeekMod.Lib;

// Chest Peek: when pointing at a chest (or piggy bank, safe, forge, void vault), shows its contents
// near the finger. The idea and the layout come from the "hover over chest" of Better Chests
// (NotLe0n, GPLv3); the code was rewritten for Bunny Loader.
public static class ChestPeekPanel
{
    const int Columns = 10;
    const int Cell = 30;   // side of each slot, in interface pixels (times the config's scale)
    const int NoTarget = int.MinValue;

    // Camera and size of the WORLD's screen, read in the update step (ChestPeekSystem.UpdateUI).
    public static float ViewX, ViewY;
    public static int ViewW, ViewH;
    public static bool ViewOk;

    // What the tile touch writes every frame the finger is over the chest. The target is a
    // Main.chest index, or the same negative numbers the game uses in Player.chest for the
    // player's storages: -2 piggy bank, -3 safe, -4 defender's forge, -5 void vault. An index
    // needs no handle to stay valid between frames.
    static int target = NoTarget;
    static int targetFrame = -100, frames;
    // A fixed point in the WORLD: center of the chest's top, in pixels, and its top and bottom.
    static float worldX, worldTop, worldBottom;
    static bool stale;
    static bool loggedHover, loggedDraw;

    sealed class Entry
    {
        public int Type, Stack, Prefix;
        public GameHandle? Handle;   // keeps an expanded chest item alive between frames
        public Item Item;
        public string StackText = "";
    }

    // Slot -> what is shown for the container being shown.
    static readonly Dictionary<int, Entry> Cache = new();
    static readonly List<Entry> Shown = new();
    static int cacheOwner = NoTarget;

    static int errors;
    static bool failed;
    static int skipLogs;

    public static void Install()
    {
        try
        {
            // Runs every frame with the interface's SpriteBatch open.
            On_Main.DrawInterface_15_InvasionProgressBars += orig =>
            {
                orig();
                Frame();
            };
        }
        catch (Exception e)
        {
            ChestPeek.Instance.Logger.Error("Chest Peek: could not hook the draw: " + e.Message);
        }
        try
        {
            // The tile under the finger or cursor.
            On_Player.TileInteractionsMouseOver += (orig, self, x, y) =>
            {
                orig(self, x, y);
                if (x != Player.tileTargetX || y != Player.tileTargetY)
                    return;
                try
                {
                    PointAt(x, y);
                }
                catch (Exception e)
                {
                    Fail("pointing", e);
                }
            };
        }
        catch (Exception e)
        {
            ChestPeek.Instance.Logger.Error("Chest Peek: could not hook the tile touch: " + e.Message);
        }
    }

    static void Fail(string what, Exception e)
    {
        errors++;
        if (errors <= 5)
            ChestPeek.Instance.Logger.Error("Chest Peek: error (" + what + "): " + e);
        if (errors >= 20 && !failed)
        {
            failed = true;
            ChestPeek.Instance.Logger.Error("Chest Peek: turned off after 20 errors");
        }
    }

    // If the tile at (i, j) is a chest, keeps which one for the draw to show.
    static void PointAt(int i, int j)
    {
        if (failed)
            return;
        Tile tile = Main.tile[i, j];
        if (!tile.active())
            return;
        int type = tile.type;
        int found = NoTarget;
        // Origin tile (top left corner) and size in tiles of the structure pointed at.
        int ox = i, oy = j, w = 2, h = 2;

        if (TileID.Sets.BasicChest[type])
        {
            ox = tile.frameX % 36 != 0 ? i - 1 : i;
            oy = tile.frameY % 36 != 0 ? j - 1 : j;
            found = FindChest(ox, oy);
        }
        else if (type == TileID.Dressers)
        {
            w = 3;
            ox = i - tile.frameX % 54 / 18;
            oy = tile.frameY % 36 != 0 ? j - 1 : j;
            found = FindChest(ox, oy);
        }
        else if (type == TileID.PiggyBank || type == TileID.Safes || type == TileID.VoidVault)
        {
            ox = i - tile.frameX % 36 / 18;
            oy = j - tile.frameY % 36 / 18;
            found = type == TileID.PiggyBank ? -2 : type == TileID.Safes ? -3 : -5;
        }
        else if (type == TileID.DefendersForge)
        {
            w = 3;
            h = 3;
            ox = i - tile.frameX % 54 / 18;
            oy = j - tile.frameY % 54 / 18;
            found = -4;
        }

        if (found == NoTarget)
            return;
        target = found;
        targetFrame = frames;
        // Anchored in the WORLD, on the chest (center of its top, and its base), never on the finger.
        worldX = (ox + w / 2f) * 16;
        worldTop = oy * 16;
        worldBottom = (oy + h) * 16;
        if (!loggedHover)
        {
            loggedHover = true;
            ChestPeek.Instance.Logger.Info("Chest Peek: pointing at a chest (" + ox + "," + oy + ")");
        }
    }

    // The world's chest at the top left corner (x, y); a locked one shows nothing.
    static int FindChest(int x, int y)
    {
        int id = Chest.FindChest(x, y);
        if (id == -1)
            return NoTarget;
        Chest chest = Main.chest[id];
        if (chest.IsNull || Chest.IsLocked(chest.x, chest.y))
            return NoTarget;
        return id;
    }

    static void Why(string message)
    {
        if (skipLogs < 8)
        {
            skipLogs++;
            ChestPeek.Instance.Logger.Info("Chest Peek: did not draw: " + message);
        }
    }

    static void Frame()
    {
        frames++;
        if (failed || Main.gameMenu || target == NoTarget)
            return;
        if (frames - targetFrame > 3)
        {
            if (targetFrame >= 0 && !stale)
            {
                stale = true;
                Why("the touch stopped (frames since the last touch: " + (frames - targetFrame) + ")");
            }
            return;
        }
        stale = false;
        try
        {
            Draw();
        }
        catch (Exception e)
        {
            Fail("drawing", e);
        }
    }

    static void ClearCache()
    {
        foreach (Entry entry in Cache.Values)
            entry.Handle?.Dispose();
        Cache.Clear();
    }

    static void RemoveEntry(int slot)
    {
        if (Cache.Remove(slot, out Entry? entry))
            entry.Handle?.Dispose();
    }

    static Entry Remember(int slot, int type, int stack, int prefix, out bool isNew)
    {
        isNew = !Cache.TryGetValue(slot, out Entry? entry) || entry.Type != type || entry.Stack != stack || entry.Prefix != prefix;
        if (isNew)
        {
            entry?.Handle?.Dispose();
            entry = new Entry { Type = type, Stack = stack, Prefix = prefix, StackText = stack.ToString() };
            Cache[slot] = entry;
        }
        return entry!;
    }

    // World chests keep ChestItem (a compact struct: type, stack, prefix). The icon needs a real
    // Item (ExpandItem), which is expensive, so it is only rebuilt when the slot changes.
    static void CollectChest(Chest chest)
    {
        GameArray<ChestItem> all = chest.item;
        for (int i = 0; i < all.Length; i++)
        {
            ChestItem slot = all[i];
            int type = slot.type, stack = slot.stack;
            if (!(type > 0 && stack > 0))
            {
                RemoveEntry(i);
                continue;
            }
            Entry entry = Remember(i, type, stack, slot.prefix, out bool isNew);
            if (isNew)
            {
                Item expanded = slot.ExpandItem();
                entry.Handle = new GameHandle(expanded.Pointer);
                entry.Item = expanded;
            }
            Shown.Add(entry);
        }
    }

    // The player's storages keep real Items, so these are drawn as they are.
    static void CollectStorage(InventoryStorage storage)
    {
        GameArray<Item> all = storage.item;
        for (int i = 0; i < all.Length; i++)
        {
            Item slot = all[i];
            int type = slot.IsNull ? 0 : slot.type, stack = slot.IsNull ? 0 : slot.stack;
            if (!(type > 0 && stack > 0))
            {
                RemoveEntry(i);
                continue;
            }
            Entry entry = Remember(i, type, stack, slot.prefix, out _);
            entry.Item = slot;
            Shown.Add(entry);
        }
    }

    static int Round(double value) => (int)Math.Floor(value + 0.5);

    static void Draw()
    {
        ChestPeekConfig config = ChestPeekConfig.Instance;
        if (!config.Enabled)
        {
            Why("turned off in the config");
            return;
        }
        Player player = Main.player[Main.myPlayer];
        if (player.chest != -1)
        {
            Why("a chest is open");   // with the chest open its contents are already on screen
            return;
        }

        if (target != cacheOwner)
        {
            cacheOwner = target;
            ClearCache();
        }
        Shown.Clear();
        if (target >= 0)
        {
            Chest chest = Main.chest[target];
            if (chest.IsNull)
                return;
            CollectChest(chest);
        }
        else
        {
            CollectStorage(target == -2 ? player.bank : target == -3 ? player.bank2 : target == -4 ? player.bank3 : player.bank4);
        }

        float scale = config.Scale / 100f;
        if (!loggedDraw)
            ChestPeek.Instance.Logger.Info("Chest Peek: items=" + Shown.Count + " scale=" + scale);
        float cell = Cell * scale;
        float pad = 6 * scale;
        SpriteBatch batch = Main.spriteBatch;
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        Texture2D back = TextureAssets.InventoryBack.Value;

        int count = Shown.Count;
        int columns = Math.Max(1, Math.Min(Columns, count));
        int rows = (int)Math.Ceiling(count / (double)Columns);
        float width = count > 0 ? columns * cell + pad * 2 : 64 * scale;
        float height = count > 0 ? rows * cell + pad * 2 : 26 * scale;

        // Still, above the chest (centered on it); if it does not fit at the top of the screen, it
        // goes below the chest. The chest's point comes from the world and goes through the
        // current camera, never through the finger.
        int sw = Main.screenWidth, sh = Main.screenHeight;
        // World -> interface: fraction of the world's screen times the interface's screen.
        float camX, camY;
        int vw, vh;
        if (ViewOk && ViewW > 0 && ViewH > 0)
        {
            camX = ViewX;
            camY = ViewY;
            vw = ViewW;
            vh = ViewH;
        }
        else
        {
            Vector2 c = Main.screenPosition;
            camX = c.X;
            camY = c.Y;
            vw = sw;
            vh = sh;
        }
        float cx = (worldX - camX) / vw * sw;
        float topY = (worldTop - camY) / vh * sh;
        float bottomY = (worldBottom - camY) / vh * sh;
        float gap = 6 * scale;
        float px = cx - width / 2, py = topY - height - gap;
        if (py < 4)
            py = bottomY + gap;
        int roundedWidth = Round(width), roundedHeight = Round(height);
        int x = Math.Max(4, Math.Min(Round(px), sw - roundedWidth - 4));
        int y = Math.Max(4, Math.Min(Round(py), sh - roundedHeight - 4));
        if (!loggedDraw)
        {
            loggedDraw = true;
            ChestPeek.Instance.Logger.Info("Chest Peek: drawing the panel at " + x + "," + y + " screen " + sw + "x" + sh + " world " + vw + "x" + vh);
        }

        batch.Draw(pixel, new Rectangle(x - 1, y - 1, roundedWidth + 2, roundedHeight + 2), Color.Multiply(new Color(110, 130, 200), 0.9f));
        batch.Draw(pixel, new Rectangle(x, y, roundedWidth, roundedHeight), Color.Multiply(new Color(22, 30, 70), 0.92f));

        if (count == 0)
        {
            Utils.DrawBorderString(batch, "Empty", new Vector2(x + width / 2, y + height / 2 - 9 * scale), Color.White, 0.8f * scale, 0.5f, 0f, -1);
            return;
        }

        Color slotColor = Color.Multiply(Color.White, 0.85f);
        for (int i = 0; i < count; i++)
        {
            Entry entry = Shown[i];
            float left = x + pad + i % Columns * cell;
            float top = y + pad + i / Columns * cell;
            batch.Draw(back, new Rectangle(Round(left) + 1, Round(top) + 1, Round(cell) - 2, Round(cell) - 2), slotColor);
            ItemSlot.DrawItemIcon(entry.Item, 0, batch, new Vector2(left + cell / 2, top + cell / 2), 1f, cell - 8 * scale, Color.White, 1f, false);
            if (entry.Stack > 1)
                Utils.DrawBorderString(batch, entry.StackText, new Vector2(left + 3 * scale, top + cell - 16 * scale), Color.White, 0.55f * scale, 0f, 0f, -1);
        }
    }
}
