using System;
using System.Collections.Generic;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using topdownmode.Common.Configs;
using topdownmode.Common.Players;
using topdownmode.Common.Utils;

namespace topdownmode.Common.Systems;

// Items, gore and dust without gravity. Items get ItemNoGravity while the game updates them, and
// new drops scatter in a random direction. Wood and gore from a tree fall to the tree's foot.
public static class TopDownDrops
{
    const int MaxItem = 400;
    const int MaxGore = 600;
    const int MaxDust = 6000;

    sealed class Fall
    {
        public int Type, Age;
        public double Y, Vy;
    }

    static bool on = true, inTick;
    static long fallen, landedCount, goreTick, goreScattered, goreCancelled, goreTree, goreLanded, items, scattered, cancelled, dusts, tickCount;
    static int errors, nest, gnest, scanCd, gscanCd;
    static string lastErr = "";

    static readonly bool[] track = new bool[MaxItem];
    static readonly bool[] hasBefore = new bool[MaxItem];
    static readonly double[] beforeVy = new double[MaxItem];
    static readonly List<int> flagged = new();
    static readonly Dictionary<int, Fall> fall = new();
    static readonly HashSet<int> woodTypes = new()
    {
        ItemID.Wood, ItemID.Ebonwood, ItemID.RichMahogany, ItemID.Pearlwood, ItemID.Shadewood, ItemID.SpookyWood, ItemID.BorealWood, ItemID.PalmWood, ItemID.AshWood, ItemID.Acorn,
    };

    static readonly bool[] gtrack = new bool[MaxGore];
    static readonly bool[] gHasBefore = new bool[MaxGore];
    static readonly double[] gBeforeVy = new double[MaxGore];
    static readonly Dictionary<int, Fall> gfall = new();
    static readonly List<int> keys = new();
    static readonly Random random = new();

    static bool TopDownOn() => on && TopDownPlayer.On && TopDownPlayer.Active && TopDownConfig.Instance.DropsTopDown;

    static void Fail(Exception e)
    {
        errors++;
        lastErr = e.ToString();
        if (errors <= 3)
            TopDownMode.Log("drops (error): " + e);
    }

    static void ScanAll()
    {
        Array.Clear(track);
        for (int i = 0; i < MaxItem; i++)
        {
            WorldItem it = Main.item[i];
            if (!it.IsNull && it.active)
                track[i] = true;
        }
    }

    static void Prepare(WorldItem it, int i)
    {
        if (fall.ContainsKey(i))
        {
            items++;
            return;
        }
        int t = it.type;
        if (!ItemID.Sets.ItemNoGravity[t])
        {
            ItemID.Sets.ItemNoGravity[t] = true;
            flagged.Add(t);
        }
        hasBefore[i] = true;
        beforeVy[i] = it.velocity.Y;
        items++;
    }

    // The y of the tree's foot under a tile, in pixels, or null when there is no tree.
    static double? FindFoot(int tx, int ty)
    {
        Dictionary<long, TileBase> known = TopDownTreeBase.Known;
        for (int j = ty; j <= ty + 90; j++)
        {
            if (known.ContainsKey(TileScan.Key(tx, j)) || known.ContainsKey(TileScan.Key(tx - 1, j)) || known.ContainsKey(TileScan.Key(tx + 1, j)))
                return j * 16;
        }
        int last = -1;
        for (int j = ty; j <= ty + 90; j++)
        {
            int t = TileScan.TypeAt(tx, j);
            if (t == TileID.Trees)
                last = j;
            else if (j > ty)
                break;
        }
        return last >= 0 ? (last + 1) * 16 : null;
    }

    static bool StartFall(WorldItem it, int i)
    {
        if (!woodTypes.Contains(it.type))
            return false;
        double w = it.width, h = it.height;
        double px = it.position.X, py = it.position.Y;
        double? foot = FindFoot((int)Math.Floor((px + w / 2) / 16), (int)Math.Floor((py + h / 2) / 16));
        if (foot == null)
            return false;
        it.velocity.X = (float)Math.Max(-0.45, Math.Min(0.45, it.velocity.X * 0.15));
        it.velocity.Y = 0;
        if (py + h >= foot.Value - 0.5)
        {
            it.position.Y = (float)(foot.Value - h);
            landedCount++;
            return true;
        }
        fall[i] = new Fall { Type = it.type, Y = foot.Value };
        fallen++;
        return true;
    }

    static void LandFalling()
    {
        keys.Clear();
        keys.AddRange(fall.Keys);
        foreach (int i in keys)
        {
            Fall f = fall[i];
            try
            {
                WorldItem it = Main.item[i];
                if (it.IsNull || !it.active || it.type != f.Type)
                {
                    fall.Remove(i);
                    continue;
                }
                f.Age++;
                double vy = it.velocity.Y;
                double h = it.height;
                double bottom = it.position.Y + h;
                if (bottom >= f.Y - 0.5)
                {
                    it.position.Y = (float)(f.Y - h);
                    it.velocity.X = 0;
                    it.velocity.Y = 0;
                    fall.Remove(i);
                    landedCount++;
                }
                else if ((f.Age > 3 && f.Vy > 0.2 && vy < 0.01) || f.Age > 600)
                {
                    it.velocity.X = 0;
                    it.velocity.Y = 0;
                    fall.Remove(i);
                    landedCount++;
                }
                else
                {
                    f.Vy = vy;
                }
            }
            catch (Exception e)
            {
                Fail(e);
                fall.Remove(i);
            }
        }
    }

    static void ScanGore()
    {
        for (int i = 0; i < MaxGore; i++)
        {
            Gore g = Main.gore[i];
            if (!g.IsNull && g.active)
                gtrack[i] = true;
        }
    }

    static double? GoreFoot(int tx, int ty)
    {
        Dictionary<long, TileBase> known = TopDownTreeBase.Known;
        for (int j = ty; j <= ty + 40; j++)
        {
            for (int dx = 0; dx <= 5; dx++)
            {
                if (known.ContainsKey(TileScan.Key(tx + dx, j)) || (dx != 0 && known.ContainsKey(TileScan.Key(tx - dx, j))))
                    return j * 16;
            }
        }
        return null;
    }

    static bool StartGoreFall(Gore g, int i)
    {
        double h = g.Height;
        double px = g.position.X, py = g.position.Y;
        double? foot = GoreFoot((int)Math.Floor(px / 16), (int)Math.Floor(py / 16));
        if (foot == null)
            return false;
        if (py + h >= foot.Value)
            return false;
        g.velocity.X = (float)Math.Max(-0.8, Math.Min(0.8, g.velocity.X * 0.3));
        gfall[i] = new Fall { Type = g.type, Y = foot.Value, Vy = g.velocity.Y };
        goreTree++;
        return true;
    }

    static void ScatterGore(Gore g)
    {
        double vx = g.velocity.X, vy = g.velocity.Y;
        double sp = Math.Sqrt(vx * vx + vy * vy);
        if (sp < 0.3)
            return;
        double a = random.NextDouble() * Math.PI * 2;
        double nsp = Math.Min(Math.Max(sp, 1), 4);
        g.velocity.X = (float)(Math.Cos(a) * nsp);
        g.velocity.Y = (float)(Math.Sin(a) * nsp);
        goreScattered++;
    }

    static void GorePre()
    {
        if ((gscanCd++ % 15) == 0)
            ScanGore();
        for (int i = 0; i < MaxGore; i++)
        {
            if (!gtrack[i])
                continue;
            try
            {
                Gore g = Main.gore[i];
                if (g.IsNull || !g.active)
                {
                    gtrack[i] = false;
                    gfall.Remove(i);
                    continue;
                }
                if (gfall.ContainsKey(i))
                    continue;
                gHasBefore[i] = true;
                gBeforeVy[i] = g.velocity.Y;
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }
        goreTick++;
    }

    static void GorePost()
    {
        keys.Clear();
        keys.AddRange(gfall.Keys);
        foreach (int i in keys)
        {
            Fall f = gfall[i];
            try
            {
                Gore g = Main.gore[i];
                if (g.IsNull || !g.active || g.type != f.Type)
                {
                    gfall.Remove(i);
                    continue;
                }
                f.Age++;
                double vy = g.velocity.Y;
                if ((double)g.position.Y + g.Height >= f.Y)
                {
                    g.position.Y = (float)(f.Y - g.Height);
                    g.velocity.X = 0;
                    g.velocity.Y = 0;
                    gfall.Remove(i);
                    goreLanded++;
                }
                else if (f.Age > 600)
                {
                    g.velocity.X = 0;
                    g.velocity.Y = 0;
                    gfall.Remove(i);
                    goreLanded++;
                }
                else
                {
                    f.Vy = vy;
                }
            }
            catch (Exception e)
            {
                Fail(e);
                gfall.Remove(i);
            }
        }
        for (int i = 0; i < MaxGore; i++)
        {
            if (!gHasBefore[i])
                continue;
            gHasBefore[i] = false;
            try
            {
                Gore g = Main.gore[i];
                if (g.IsNull || !g.active)
                    continue;
                double vy = g.velocity.Y;
                double d = vy - gBeforeVy[i];
                if (d > 0.01 && d < 0.3)
                {
                    g.position.Y = (float)(g.position.Y - d);
                    double nx = g.velocity.X * 0.96;
                    double ny = gBeforeVy[i] * 0.96;
                    if (Math.Abs(nx) < 0.05)
                        nx = 0;
                    if (Math.Abs(ny) < 0.05)
                        ny = 0;
                    g.velocity.X = (float)nx;
                    g.velocity.Y = (float)ny;
                    goreCancelled++;
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }
    }

    static void PreTick()
    {
        if ((scanCd++ % 30) == 0)
            ScanAll();
        for (int i = 0; i < MaxItem; i++)
        {
            if (!track[i])
                continue;
            try
            {
                WorldItem it = Main.item[i];
                if (it.IsNull || !it.active)
                {
                    track[i] = false;
                    continue;
                }
                Prepare(it, i);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }
    }

    static void PostTick()
    {
        foreach (int t in flagged)
            ItemID.Sets.ItemNoGravity[t] = false;
        flagged.Clear();
        if (fall.Count > 0)
            LandFalling();
        for (int i = 0; i < MaxItem; i++)
        {
            if (!hasBefore[i])
                continue;
            hasBefore[i] = false;
            double vy0 = beforeVy[i];
            try
            {
                WorldItem it = Main.item[i];
                if (it.IsNull || !it.active)
                    continue;
                if (it.beingGrabbed)
                    continue;
                double vy = it.velocity.Y;
                double d = vy - vy0;
                if (Math.Abs(d - 0.1) < 0.015 || Math.Abs(d - 0.08) < 0.012)
                {
                    double vx = it.velocity.X * 0.95;
                    double ny = vy0 * 0.95;
                    if (Math.Abs(vx) < 0.1)
                        vx = 0;
                    if (Math.Abs(ny) < 0.1)
                        ny = 0;
                    it.velocity.X = (float)vx;
                    it.velocity.Y = (float)ny;
                    cancelled++;
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }
    }

    static void ScatterNew(WorldItem it, bool explicitVelocity)
    {
        if (explicitVelocity)
            return;
        double vx = it.velocity.X, vy = it.velocity.Y;
        double sp = Math.Sqrt(vx * vx + vy * vy);
        if (sp < 0.3)
            return;
        double a = random.NextDouble() * Math.PI * 2;
        double nsp = Math.Min(Math.Max(sp, 1.2), 3.2);
        it.velocity.X = (float)(Math.Cos(a) * nsp);
        it.velocity.Y = (float)(Math.Sin(a) * nsp);
        scattered++;
    }

    static bool ExplicitVelocity(GameNullable<Vector2> velocity)
    {
        if (!velocity.HasValue)
            return false;
        Vector2 value = velocity.Value;
        return Math.Abs(value.X) + Math.Abs(value.Y) > 0.001;
    }

    static void AfterNewItem(int idx, bool explicitVelocity)
    {
        try
        {
            if (idx >= 0 && idx < MaxItem && TopDownOn())
            {
                WorldItem it = Main.item[idx];
                if (!it.IsNull && it.active)
                {
                    track[idx] = true;
                    if (!StartFall(it, idx))
                        ScatterNew(it, explicitVelocity);
                    if (inTick)
                        Prepare(it, idx);
                }
            }
        }
        catch (Exception e)
        {
            Fail(e);
        }
    }

    static void InstallItem()
    {
        On_Main.DoUpdate += (orig, self, gameTime) =>
        {
            bool ran = false;
            try
            {
                if (TopDownOn())
                {
                    inTick = true;
                    PreTick();
                    GorePre();
                    ran = true;
                    tickCount++;
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
            try
            {
                orig(self, gameTime);
            }
            finally
            {
                if (ran)
                {
                    inTick = false;
                    try
                    {
                        PostTick();
                    }
                    catch (Exception e)
                    {
                        Fail(e);
                    }
                    try
                    {
                        GorePost();
                    }
                    catch (Exception e)
                    {
                        Fail(e);
                    }
                }
            }
        };
        On_Item.NewItem_IEntitySource_int_int_int_int_int_int_bool_int_NewItemOwnership_Nullable_NewItemModifier += (orig, source, x, y, width, height, type, stack, noBroadcast, prefix, ownership, velocity, modifier) =>
        {
            if (nest > 0)
                return orig(source, x, y, width, height, type, stack, noBroadcast, prefix, ownership, velocity, modifier);
            nest++;
            int idx;
            try
            {
                idx = orig(source, x, y, width, height, type, stack, noBroadcast, prefix, ownership, velocity, modifier);
            }
            finally
            {
                nest--;
            }
            AfterNewItem(idx, ExplicitVelocity(velocity));
            return idx;
        };
        On_Item.NewItem_IEntitySource_Vector2_int_int_int_NewItemOwnership_Nullable_NewItemModifier_bool += (orig, source, center, type, stack, prefix, ownership, velocity, modifier, noBroadcast) =>
        {
            if (nest > 0)
                return orig(source, center, type, stack, prefix, ownership, velocity, modifier, noBroadcast);
            nest++;
            int idx;
            try
            {
                idx = orig(source, center, type, stack, prefix, ownership, velocity, modifier, noBroadcast);
            }
            finally
            {
                nest--;
            }
            AfterNewItem(idx, ExplicitVelocity(velocity));
            return idx;
        };
    }

    static void InstallGore()
    {
        On_Gore.NewGore += (orig, position, velocity, type, scale) =>
        {
            if (gnest > 0)
                return orig(position, velocity, type, scale);
            gnest++;
            int idx;
            try
            {
                idx = orig(position, velocity, type, scale);
            }
            finally
            {
                gnest--;
            }
            try
            {
                if (idx >= 0 && idx < MaxGore && TopDownOn())
                {
                    Gore g = Main.gore[idx];
                    if (!g.IsNull && g.active)
                    {
                        gtrack[idx] = true;
                        gHasBefore[idx] = false;
                        gfall.Remove(idx);
                        if (!StartGoreFall(g, idx))
                            ScatterGore(g);
                    }
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
            return idx;
        };
    }

    static void InstallDust()
    {
        On_Dust.NewDust += (orig, position, width, height, type, speedX, speedY, alpha, newColor, scale) =>
        {
            int idx = orig(position, width, height, type, speedX, speedY, alpha, newColor, scale);
            try
            {
                if (TopDownOn() && idx >= 0 && idx < MaxDust)
                {
                    Dust d = Main.dust[idx];
                    if (!d.IsNull)
                    {
                        d.noGravity = true;
                        dusts++;
                    }
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
            return idx;
        };
    }

    public static void InstallDropsTopDown()
    {
        on = true;
        InstallItem();
        InstallGore();
        InstallDust();
    }

    public static void UninstallDropsTopDown()
    {
        on = false;
        foreach (int t in flagged)
            ItemID.Sets.ItemNoGravity[t] = false;
        flagged.Clear();
        Array.Clear(track);
        Array.Clear(hasBefore);
        fall.Clear();
        Array.Clear(gtrack);
        Array.Clear(gHasBefore);
        gfall.Clear();
    }

    public static string Status() =>
        "drops: ticks=" + tickCount + " items prepared=" + items + " scattered=" + scattered + " wood: fell=" + fallen + " landed=" + landedCount + " falling now=" + fall.Count
        + " gore: ticks=" + goreTick + " scattered=" + goreScattered + " gravity undone=" + goreCancelled + " from trees=" + goreTree + " landed=" + goreLanded
        + " | gravity undone (plan B)=" + cancelled + " dust without gravity=" + dusts + " errors=" + errors + (lastErr != "" ? " last error: " + lastErr : "");
}
