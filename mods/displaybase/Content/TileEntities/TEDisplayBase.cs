using System.Diagnostics;
using System.IO;
using BunnyLoader.Interop;
using DisplayBaseMod.Content.Tiles;
using DisplayBaseMod.Lib;
using ItemUtils = DisplayBaseMod.Lib.Items;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;

namespace DisplayBaseMod.Content.TileEntities;

public class TEDisplayBase : ModTileEntity
{
    // The original counted 10 frames at 60 per second.
    const double RecentSeconds = 10 / 60.0;

    // The item on display, as plain data. The game item is only made for drawing.
    public int ItemType;
    public int ItemPrefix;
    public int ItemStack;

    Item drawItem;
    GameHandle? drawItemHandle;

    public bool IsEmpty => ItemType <= 0 || ItemStack <= 0;

    public static TEDisplayBase Template => ModContent.GetInstance<TEDisplayBase>();

    // The tile entity whose top left is exactly this cell.
    public static TEDisplayBase? At(int i, int j) => TileEntity.TryGet(new Point16(i, j), out TEDisplayBase entity) ? entity : null;

    public static Point16 TopLeft(int i, int j) => TileObjectData.TopLeft(i, j);

    public static TEDisplayBase? Ensure(int x, int y)
    {
        TEDisplayBase? entity = At(x, y);
        if (entity != null || Main.netMode == NetmodeID.MultiplayerClient)
            return entity;
        TEDisplayBase template = Template;
        if (!template.IsTileValidForEntity(x, y))
            return null;
        template.Place(x, y);
        return At(x, y);
    }

    void SetItem(int type, int prefix, int stack)
    {
        ItemType = type;
        ItemPrefix = prefix;
        ItemStack = stack;
        drawItemHandle?.Dispose();
        drawItemHandle = null;
        drawItem = default;
    }

    // The game's collector does not see C# fields, so a handle keeps this item alive.
    public Item DrawItem()
    {
        if (drawItemHandle == null)
        {
            drawItem = ItemUtils.NewItem(ItemType, ItemPrefix, ItemStack);
            drawItemHandle = new GameHandle(drawItem.Pointer);
        }
        return drawItem;
    }

    public override void SaveData(TagCompound tag)
    {
        if (IsEmpty)
            return;
        var data = new TagCompound();
        data.Add("type", ItemUtils.ItemKey(ItemType));
        data.Add("prefix", ItemPrefix);
        data.Add("stack", ItemStack);
        tag.Add("Item", data);
    }

    public override void LoadData(TagCompound tag)
    {
        if (!tag.ContainsKey("Item"))
        {
            SetItem(0, 0, 0);
            return;
        }
        TagCompound data = tag.GetCompound("Item");
        int type = data.ContainsKey("type") ? ItemUtils.ItemFromKey(data["type"]) : 0;
        if (type > 0)
            SetItem(type, data.GetInt("prefix"), System.Math.Max(1, data.GetInt("stack")));
        else
            SetItem(0, 0, 0);
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(IsEmpty ? 0 : ItemType);
        writer.Write(IsEmpty ? 0 : ItemPrefix);
        writer.Write(IsEmpty ? 0 : ItemStack);
    }

    public override void NetReceive(BinaryReader reader)
    {
        int type = reader.ReadInt32(), prefix = reader.ReadInt32(), stack = reader.ReadInt32();
        if (type > 0)
            SetItem(type, prefix, stack);
        else
            SetItem(0, 0, 0);
    }

    public override bool IsTileValidForEntity(int x, int y)
    {
        if (!WorldGen.InWorld(x, y))
            return false;
        Tile tile = Main.tile[x, y];
        if (!tile.HasTile || tile.TileType != ModContent.TileType<DisplayBaseTile>())
            return false;
        return tile.TileFrameX % 54 == 0 && tile.TileFrameY % 54 == 0;
    }

    public override void OnNetPlace() => Sync();

    public override void OnKill()
    {
        if (!IsEmpty)
            DropItem();
    }

    void Sync()
    {
        if ((Main.netMode & NetmodeID.Server) != 0)
            NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, default, ID, Position.X, Position.Y, 0f, 0, 0, 0);
    }

    public void DropItem()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            ItemUtils.Drop(Position.X, Position.Y, ItemType, ItemPrefix, 1);
        SetItem(0, 0, 0);
        Sync();
    }

    public static void TryPlacing(int x, int y, int type, int prefix, int stack)
    {
        WorldGen.RangeFrame(x, y, x + 3, y + 3);
        TEDisplayBase? entity = Ensure(x, y);
        if (entity == null)
        {
            ItemUtils.Drop(x, y, type, prefix, stack);
            return;
        }
        if (!entity.IsEmpty)
            entity.DropItem();
        entity.SetItem(type, prefix, stack);
        entity.Sync();
    }

    static long lastInteraction;

    public static bool Recent() =>
        lastInteraction != 0 && (Stopwatch.GetTimestamp() - lastInteraction) / (double)Stopwatch.Frequency <= RecentSeconds;

    public static void OnPlayerInteraction(Player player, int clickX, int clickY)
    {
        if (Recent())
            return;
        lastInteraction = Stopwatch.GetTimestamp();
        Item held = player.inventory[player.selectedItem];
        if (!held.IsNull && held.type > 0 && held.stack > 0 && !held.favorited)
        {
            player.GamepadEnableGrappleCooldown();
            PlaceItemOnBase(player, clickX, clickY);
            Recipe.FindRecipes(false);
            return;
        }
        Point16 top = TopLeft(clickX, clickY);
        TEDisplayBase? entity = Ensure(top.X, top.Y);
        if (entity != null && !entity.IsEmpty)
        {
            player.GamepadEnableGrappleCooldown();
            Hit(top.X, top.Y);
        }
    }

    static void Hit(int x, int y)
    {
        WorldGen.KillTile(x, y, true, false, false);
        if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.TileManipulation, -1, -1, default, 0, x, y, 1f, 0, 0, 0);
    }

    static void PlaceItemOnBase(Player player, int clickX, int clickY)
    {
        Point16 top = TopLeft(clickX, clickY);
        int x = top.X, y = top.Y;
        TEDisplayBase? entity = Ensure(x, y);
        if (entity == null && Main.netMode != NetmodeID.MultiplayerClient)
            return;
        if (entity != null && !entity.IsEmpty)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                WorldGen.KillTile(x, y, true, false, false);
            else
                Hit(x, y);
        }

        int slot = player.selectedItem;
        Item held = player.inventory[slot];
        if (Main.netMode == NetmodeID.MultiplayerClient)
            Net.SendPlace(x, y, held.type, held.prefix, 1);
        else
            TryPlacing(x, y, held.type, held.prefix, 1);

        held.stack--;
        if (held.stack <= 0)
        {
            held.TurnToAir();
            if (slot == 58)
                Main.mouseItem.TurnToAir();
        }
        player.releaseUseItem = false;
        player.mouseInterface = true;
        player.PlayDroppedItemAnimation(20);
        WorldGen.RangeFrame(x, y, x + 3, y + 3);
    }
}
