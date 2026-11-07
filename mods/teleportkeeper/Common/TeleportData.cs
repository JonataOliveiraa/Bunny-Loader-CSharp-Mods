using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader.IO;

namespace TeleportKeeperMod.Common;

public sealed class TeleportPoint
{
    public int id;
    public string name = "";
    public float x, y;
    public string icon = "Default";
    public string color = "White";
    public bool deleteOnUse, favorite;

    public void Write(BinaryWriter writer)
    {
        writer.Write(id);
        writer.Write(name);
        writer.Write(x);
        writer.Write(y);
        writer.Write(icon);
        writer.Write(color);
        writer.Write(deleteOnUse);
        writer.Write(favorite);
    }

    public static TeleportPoint Read(BinaryReader reader) => Teleports.Clean(new TeleportPoint
    {
        id = reader.ReadInt32(),
        name = reader.ReadString(),
        x = reader.ReadSingle(),
        y = reader.ReadSingle(),
        icon = reader.ReadString(),
        color = reader.ReadString(),
        deleteOnUse = reader.ReadBoolean(),
        favorite = reader.ReadBoolean(),
    });
}

public sealed class DeathPoint
{
    public string owner = "";
    public float x, y;
}

public static class Data
{
    public static List<TeleportPoint> points = new();
    public static List<DeathPoint> deaths = new();
    public static int nextId = 1;
    public static bool loaded;

    // Goes up on every change, so the list screen only rebuilds when there is something new.
    public static int version;
}

public static class Teleports
{
    static readonly List<Action> listeners = new();

    public static void OnDataChanged(Action fn) => listeners.Add(fn);

    static void Changed()
    {
        Data.version++;
        foreach (Action fn in listeners)
            Util.Safe("data listener", fn);
    }

    public static TeleportPoint Clean(TeleportPoint p)
    {
        if (p.name.Length > 40)
            p.name = p.name[..40];
        p.icon = Icons.IconOf(p.icon).key;
        p.color = NameColors.NameColorOf(p.color).key;
        return p;
    }

    static void WriteSnapshot(BinaryWriter writer)
    {
        writer.Write(Data.points.Count);
        foreach (TeleportPoint p in Data.points)
            p.Write(writer);
        writer.Write(Data.deaths.Count);
        foreach (DeathPoint d in Data.deaths)
        {
            writer.Write(d.owner);
            writer.Write(d.x);
            writer.Write(d.y);
        }
    }

    static void Commit()
    {
        Changed();
        if (Util.IsServer)
            Net.ToAll(PacketKind.Sync, WriteSnapshot);
    }

    static bool NameUsed(string name)
    {
        foreach (TeleportPoint p in Data.points)
        {
            if (p.name == name)
                return true;
        }
        return false;
    }

    public static string DefaultName(string? icon)
    {
        string name = Icons.IconName(icon);
        if (!NameUsed(name))
            return name;
        for (int n = 2; ; n++)
        {
            string numbered = name + " " + n;
            if (!NameUsed(numbered))
                return numbered;
        }
    }

    public static string PlayerName(Player player) => player.IsNull ? "" : player.name ?? "";

    public static TeleportPoint? Find(int id)
    {
        foreach (TeleportPoint p in Data.points)
        {
            if (p.id == id)
                return p;
        }
        return null;
    }

    // What happens on the side that owns the data.
    static bool ApplyAdd(string name, string icon, bool deleteOnUse, float x, float y, string color)
    {
        TeleportPoint p = Clean(new TeleportPoint { id = Data.nextId++, name = name, icon = icon, deleteOnUse = deleteOnUse, x = x, y = y, color = color });
        if (p.name.Length == 0)
            p.name = DefaultName(p.icon);
        Data.points.Add(p);
        return true;
    }

    static bool ApplyEdit(int id, bool hasDetails, string name, string icon, bool deleteOnUse, string color, bool hasFavorite, bool favorite)
    {
        TeleportPoint? p = Find(id);
        if (p == null)
            return false;
        if (hasDetails)
        {
            p.name = name;
            p.icon = icon;
            p.deleteOnUse = deleteOnUse;
            p.color = color;
        }
        if (hasFavorite)
            p.favorite = favorite;
        Clean(p);
        if (p.name.Length == 0)
            p.name = DefaultName(p.icon);
        return true;
    }

    static bool ApplyDelete(int id)
    {
        TeleportPoint? p = Find(id);
        return p != null && Data.points.Remove(p);
    }

    static bool ApplyClear()
    {
        if (Data.points.Count == 0)
            return false;
        Data.points = new List<TeleportPoint>();
        return true;
    }

    static bool ApplyDeath(string owner, float x, float y)
    {
        if (owner.Length == 0)
            return false;
        Data.deaths.RemoveAll(d => d.owner == owner);
        Data.deaths.Add(new DeathPoint { owner = owner, x = x, y = y });
        return true;
    }

    static bool ApplyDeleteDeath(string owner) => Data.deaths.RemoveAll(d => d.owner == owner) > 0;

    static bool Run(bool changed)
    {
        if (changed)
            Commit();
        return changed;
    }

    public static bool Add(string name, string icon, bool deleteOnUse, float x, float y, string color = "White")
    {
        if (!Util.IsClient)
            return Run(ApplyAdd(name, icon, deleteOnUse, x, y, color));
        Net.ToServer(PacketKind.Add, w =>
        {
            w.Write(name);
            w.Write(icon);
            w.Write(deleteOnUse);
            w.Write(x);
            w.Write(y);
            w.Write(color);
        });
        return true;
    }

    static bool SendEdit(int id, bool hasDetails, string name, string icon, bool deleteOnUse, string color, bool hasFavorite, bool favorite)
    {
        if (!Util.IsClient)
            return Run(ApplyEdit(id, hasDetails, name, icon, deleteOnUse, color, hasFavorite, favorite));
        Net.ToServer(PacketKind.Edit, w =>
        {
            w.Write(id);
            w.Write(hasDetails);
            w.Write(name);
            w.Write(icon);
            w.Write(deleteOnUse);
            w.Write(color);
            w.Write(hasFavorite);
            w.Write(favorite);
        });
        return true;
    }

    public static bool Edit(int id, string name, string icon, bool deleteOnUse, string color) => SendEdit(id, true, name, icon, deleteOnUse, color, false, false);

    public static bool Favorite(int id, bool favorite) => SendEdit(id, false, "", "", false, "", true, favorite);

    public static bool Delete(int id)
    {
        if (!Util.IsClient)
            return Run(ApplyDelete(id));
        Net.ToServer(PacketKind.Del, w => w.Write(id));
        return true;
    }

    public static bool Clear()
    {
        if (!Util.IsClient)
            return Run(ApplyClear());
        Net.ToServer(PacketKind.Clear, static _ => { });
        return true;
    }

    public static bool Death(string owner, float x, float y)
    {
        if (!Util.IsClient)
            return Run(ApplyDeath(owner, x, y));
        Net.ToServer(PacketKind.Death, w =>
        {
            w.Write(owner);
            w.Write(x);
            w.Write(y);
        });
        return true;
    }

    public static bool DeleteDeath(string owner)
    {
        if (!Util.IsClient)
            return Run(ApplyDeleteDeath(owner));
        Net.ToServer(PacketKind.DeathDel, w => w.Write(owner));
        return true;
    }

    static void TeleportTo(float x, float y)
    {
        Player p = Util.Local;
        if (p.IsNull)
            return;
        Main.mapFullscreen = false;
        p.Teleport(new Vector2(x * 16f, y * 16f), 0, 0);
        Util.TeleportSound();
    }

    public static void UsePoint(TeleportPoint point)
    {
        TeleportTo(point.x, point.y);
        if (point.deleteOnUse)
        {
            Delete(point.id);
            Util.Say("RemovedTp", 255, 100, 100);
        }
    }

    public static void UseDeath(DeathPoint death)
    {
        TeleportTo(death.x, death.y);
        if (death.owner == PlayerName(Util.Local))
        {
            DeleteDeath(death.owner);
            Util.Say("DeathTpRemoved", 255, 0, 0);
        }
    }

    public static void InstallNet()
    {
        Net.On(PacketKind.Sync, static (r, _) =>
        {
            if (!Util.IsClient)
                return;
            int count = r.ReadInt32();
            var points = new List<TeleportPoint>(count);
            for (int i = 0; i < count; i++)
                points.Add(TeleportPoint.Read(r));
            count = r.ReadInt32();
            var deaths = new List<DeathPoint>(count);
            for (int i = 0; i < count; i++)
                deaths.Add(new DeathPoint { owner = r.ReadString(), x = r.ReadSingle(), y = r.ReadSingle() });
            Data.points = points;
            Data.deaths = deaths;
            Data.loaded = true;
            Changed();
        });
        Net.On(PacketKind.Hello, static (_, from) =>
        {
            if (Util.IsServer)
                Net.ToClient(from, PacketKind.Sync, WriteSnapshot);
        });
        Net.On(PacketKind.Add, static (r, _) =>
        {
            if (Util.IsServer)
                Run(ApplyAdd(r.ReadString(), r.ReadString(), r.ReadBoolean(), r.ReadSingle(), r.ReadSingle(), r.ReadString()));
        });
        Net.On(PacketKind.Edit, static (r, _) =>
        {
            if (Util.IsServer)
                Run(ApplyEdit(r.ReadInt32(), r.ReadBoolean(), r.ReadString(), r.ReadString(), r.ReadBoolean(), r.ReadString(), r.ReadBoolean(), r.ReadBoolean()));
        });
        Net.On(PacketKind.Del, static (r, _) =>
        {
            if (Util.IsServer)
                Run(ApplyDelete(r.ReadInt32()));
        });
        Net.On(PacketKind.Clear, static (_, _) =>
        {
            if (Util.IsServer)
                Run(ApplyClear());
        });
        Net.On(PacketKind.Death, static (r, _) =>
        {
            if (Util.IsServer)
                Run(ApplyDeath(r.ReadString(), r.ReadSingle(), r.ReadSingle()));
        });
        Net.On(PacketKind.DeathDel, static (r, _) =>
        {
            if (Util.IsServer)
                Run(ApplyDeleteDeath(r.ReadString()));
        });
    }

    public static void SaveTo(TagCompound tag)
    {
        var list = new List<TagCompound>(Data.points.Count);
        foreach (TeleportPoint p in Data.points)
        {
            Clean(p);
            list.Add(new TagCompound
            {
                ["id"] = p.id,
                ["name"] = p.name,
                ["x"] = p.x,
                ["y"] = p.y,
                ["icon"] = p.icon,
                ["color"] = p.color,
                ["deleteOnUse"] = p.deleteOnUse,
                ["favorite"] = p.favorite,
            });
        }
        tag["teleportPoints"] = list;
        tag["nextId"] = Data.nextId;
    }

    public static void LoadFrom(TagCompound tag)
    {
        Data.points = new List<TeleportPoint>();
        Data.nextId = Math.Max(tag.GetInt("nextId"), 1);
        if (tag.TryGet("teleportPoints", out List<TagCompound> list))
        {
            foreach (TagCompound c in list)
            {
                TeleportPoint p = Clean(new TeleportPoint
                {
                    id = c.TryGet("id", out int id) ? id : 0,
                    name = c.GetString("name"),
                    x = c.TryGet("x", out float x) ? x : 0,
                    y = c.TryGet("y", out float y) ? y : 0,
                    icon = c.GetString("icon"),
                    color = c.GetString("color"),
                    deleteOnUse = c.TryGet("deleteOnUse", out bool deleteOnUse) && deleteOnUse,
                    favorite = c.TryGet("favorite", out bool favorite) && favorite,
                });
                Data.points.Add(p);
                Data.nextId = Math.Max(Data.nextId, p.id + 1);
            }
        }
        Data.deaths = new List<DeathPoint>();
        Data.loaded = true;
        Changed();
    }

    public static void EnterWorld()
    {
        if (Util.IsClient)
        {
            Net.ToServer(PacketKind.Hello, static _ => { });
            return;
        }
        Data.loaded = true;
        Changed();
    }

    public static void Reset()
    {
        Data.points = new List<TeleportPoint>();
        Data.deaths = new List<DeathPoint>();
        Data.nextId = 1;
        Data.loaded = false;
        Changed();
    }
}
