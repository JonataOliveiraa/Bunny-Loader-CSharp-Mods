using System.Collections.Generic;
using System.IO;
using BestiaryDisplaysMod.Common;
using BestiaryDisplaysMod.Content.Tiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace BestiaryDisplaysMod.Content.TileEntities;

public class HologramTileEntity : ModTileEntity
{
    // The loader's table of placed entities is not public, and the drawing has to reach them
    // all, since a hologram can sit far from its projector. Every entity is built through
    // this constructor, and Collect drops the ones the world no longer has.
    static readonly List<HologramTileEntity> instances = new();

    public string BestiaryCreditId = "";
    public bool Animate = true;
    public bool HologramFx;
    public bool AlternateRender;
    public float Scale = 1;
    public int OffsetX;
    public int OffsetY;
    public float Rotation;
    public int AnimationSpeed = 8;
    public bool TileInvisible;

    public HologramView? view;

    public HologramTileEntity()
    {
        lock (instances)
            instances.Add(this);
    }

    public static HologramTileEntity Template => ModContent.GetInstance<HologramTileEntity>();

    public static HologramTileEntity? At(int i, int j) => TileEntity.TryGet(i, j, out HologramTileEntity entity) ? entity : null;

    public bool StillPlaced => TileEntity.TryGet(Position, out HologramTileEntity entity) && entity == this;

    // Fills the list with the placed entities that show a creature.
    public static void Collect(List<HologramTileEntity> into)
    {
        into.Clear();
        lock (instances)
        {
            for (int i = instances.Count - 1; i >= 0; i--)
            {
                HologramTileEntity entity = instances[i];
                // The template has no ID and stays; it is never in a world.
                if (entity.ID < 0)
                    continue;
                if (!entity.StillPlaced)
                {
                    entity.ReleaseView();
                    instances.RemoveAt(i);
                    continue;
                }
                if (entity.BestiaryCreditId.Length > 0)
                    into.Add(entity);
            }
        }
    }

    void ReleaseView()
    {
        view?.Release();
        view = null;
    }

    public override bool IsTileValidForEntity(int x, int y)
    {
        Tile tile = Main.tile[x, y];
        if (!tile.HasTile)
            return false;
        int type = tile.TileType;
        if (type != ModContent.TileType<HologramProjectorTile>() && type != ModContent.TileType<AdamantiteHologramProjectorTile>() && type != ModContent.TileType<TitaniumHologramProjectorTile>())
            return false;
        return tile.frameX % 54 == 0 && tile.frameY % 36 == 0;
    }

    public bool Advanced
    {
        get
        {
            int type = Main.tile[Position.X, Position.Y].TileType;
            return type == ModContent.TileType<AdamantiteHologramProjectorTile>() || type == ModContent.TileType<TitaniumHologramProjectorTile>();
        }
    }

    void WriteSettings(BinaryWriter writer)
    {
        writer.Write(BestiaryCreditId);
        writer.Write(Animate);
        writer.Write(HologramFx);
        writer.Write(AlternateRender);
        writer.Write(Scale);
        writer.Write(OffsetX);
        writer.Write(OffsetY);
        writer.Write(Rotation);
        writer.Write(AnimationSpeed);
        writer.Write(TileInvisible);
    }

    void ReadSettings(BinaryReader reader)
    {
        BestiaryCreditId = reader.ReadString();
        Animate = reader.ReadBoolean();
        HologramFx = reader.ReadBoolean();
        AlternateRender = reader.ReadBoolean();
        Scale = Finite(reader.ReadSingle(), 1);
        OffsetX = reader.ReadInt32();
        OffsetY = reader.ReadInt32();
        Rotation = Finite(reader.ReadSingle(), 0);
        AnimationSpeed = ValidSpeed(reader.ReadInt32());
        TileInvisible = reader.ReadBoolean();
        ReleaseView();
    }

    static float Finite(float value, float fallback) => float.IsFinite(value) ? value : fallback;

    static int ValidSpeed(int value) => value == 0 ? 8 : value < 1 ? 1 : value;

    public override void SaveData(TagCompound tag)
    {
        tag.Add("creditId", BestiaryCreditId);
        tag.Add("animate", Animate);
        tag.Add("fx", HologramFx);
        tag.Add("altRender", AlternateRender);
        tag.Add("scale", Scale);
        tag.Add("ox", OffsetX);
        tag.Add("oy", OffsetY);
        tag.Add("rot", Rotation);
        tag.Add("animSpeed", AnimationSpeed);
        tag.Add("invis", TileInvisible);
    }

    public override void LoadData(TagCompound tag)
    {
        BestiaryCreditId = tag.GetString("creditId");
        Animate = !tag.ContainsKey("animate") || tag.GetBool("animate");
        HologramFx = tag.GetBool("fx");
        AlternateRender = tag.GetBool("altRender");
        Scale = tag.ContainsKey("scale") ? Finite(tag.GetFloat("scale"), 1) : 1;
        OffsetX = tag.GetInt("ox");
        OffsetY = tag.GetInt("oy");
        Rotation = Finite(tag.GetFloat("rot"), 0);
        AnimationSpeed = tag.ContainsKey("animSpeed") ? ValidSpeed(tag.GetInt("animSpeed")) : 8;
        TileInvisible = tag.GetBool("invis");
        ReleaseView();
    }

    public override void NetSend(BinaryWriter writer) => WriteSettings(writer);

    public override void NetReceive(BinaryReader reader) => ReadSettings(reader);

    public override void OnNetPlace() => Sync();

    public void Sync()
    {
        if ((Main.netMode & NetmodeID.Server) != 0)
            NetMessage.SendData(MessageID.TileEntitySharing, number: ID, number2: Position.X, number3: Position.Y);
    }

    public void Changed()
    {
        ReleaseView();
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            ModPacket packet = ModContent.GetInstance<BestiaryDisplays>().GetPacket();
            packet.Write((int)Position.X);
            packet.Write((int)Position.Y);
            WriteSettings(packet);
            packet.Send(-1, -1);
        }
        else
        {
            Sync();
        }
    }

    public static void HandlePacket(BinaryReader reader)
    {
        int x = reader.ReadInt32(), y = reader.ReadInt32();
        HologramTileEntity? entity = At(x, y);
        if (entity == null)
            return;
        entity.ReadSettings(reader);
        entity.Sync();
    }
}
