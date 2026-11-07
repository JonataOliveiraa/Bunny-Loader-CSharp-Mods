using System;
using System.Collections.Generic;
using Terraria;
using topdownmode.Common.Configs;
using topdownmode.Common.Players;

namespace topdownmode.Common.Systems;

// Gravity is undone from outside each tick: the change in velocity that looks like gravity is
// taken back, and thrown projectiles fall through a simulated height instead.
public static class TopDownProjectiles
{
    const int MaxProj = 1000;
    const double MaxPerUpdate = 0.5, MinPerUpdate = 0.015;
    const int ScanEvery = 3;
    const double Z0 = 21, Vz0 = 0, ZScale = 0.02, RollFriction = 0.9;

    enum LandMode { None, Roll, Kill }

    sealed class Height
    {
        public int Type;
        public double Z, Vz, Base;
        public LandMode Mode;
        public bool Landed;
    }

    static bool on = true;
    static int scanCd, nest;
    static long ticks, seen, fixedCount, landed, rolled, killed;
    static int errors;
    static string lastErr = "";
    static readonly List<string> last = new();

    static readonly bool[] track = new bool[MaxProj];
    static readonly bool[] hasBefore = new bool[MaxProj];
    static readonly double[] beforeVy = new double[MaxProj];
    static readonly int[] beforeExtra = new int[MaxProj], beforeType = new int[MaxProj];
    static readonly Dictionary<int, Height> z = new();
    static readonly List<int> zKeys = new();

    static bool TopDownOn()
    {
        if (!(on && TopDownPlayer.On && TopDownPlayer.Active))
            return false;
        if (Main.gameMenu)
            return false;
        return TopDownConfig.Instance.ProjectilesTopDown;
    }

    static void Fail(Exception e)
    {
        errors++;
        string text = e.ToString();
        lastErr = text.Length > 200 ? text[..200] : text;
        if (errors <= 3)
            TopDownMode.Log("projectiles (error): " + lastErr);
    }

    static void ScanAll()
    {
        Array.Clear(track);
        zKeys.Clear();
        zKeys.AddRange(z.Keys);
        foreach (int i in zKeys)
        {
            Projectile q = Main.projectile[i];
            if (q.IsNull || !q.active)
                z.Remove(i);
        }
        for (int i = 0; i < MaxProj; i++)
        {
            Projectile p = Main.projectile[i];
            if (!p.IsNull && p.active)
                track[i] = true;
        }
    }

    static void PreTick()
    {
        if ((scanCd++ % ScanEvery) == 0)
            ScanAll();
        for (int i = 0; i < MaxProj; i++)
        {
            if (!track[i])
                continue;
            try
            {
                Projectile p = Main.projectile[i];
                if (p.IsNull || !p.active)
                {
                    track[i] = false;
                    continue;
                }
                hasBefore[i] = true;
                beforeVy[i] = p.velocity.Y;
                beforeExtra[i] = Math.Max(0, p.extraUpdates);
                beforeType[i] = p.type;
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }
    }

    static LandMode ModeOf(Projectile p) => p.aiStyle switch
    {
        16 => LandMode.Roll,
        2 => LandMode.Kill,
        1 => p.arrow ? LandMode.Kill : LandMode.None,
        _ => LandMode.None,
    };

    static void SetScale(Projectile p, Height s) => p.scale = (float)(s.Base * (1 + Math.Max(0, s.Z) * ZScale));

    static void PostTick()
    {
        for (int i = 0; i < MaxProj; i++)
        {
            if (!hasBefore[i])
                continue;
            hasBefore[i] = false;
            try
            {
                Projectile p = Main.projectile[i];
                if (p.IsNull || !p.active)
                {
                    z.Remove(i);
                    continue;
                }
                if (p.type != beforeType[i])
                {
                    z.Remove(i);
                    continue;
                }
                seen++;
                int k = beforeExtra[i] + 1;
                double vy = p.velocity.Y;
                double dv = vy - beforeVy[i];
                bool grav = dv > MinPerUpdate * k && dv <= MaxPerUpdate * k;
                double posY = p.position.Y;
                if (grav)
                {
                    p.velocity.Y = (float)(vy - dv);
                    posY -= dv * (k + 1) / 2;
                    fixedCount++;
                    if (last.Count < 8 || (fixedCount & 255) == 0)
                    {
                        last.Add("type " + beforeType[i] + " dv=" + dv.ToString("0.000") + " k=" + k);
                        if (last.Count > 8)
                            last.RemoveAt(0);
                    }
                }
                z.TryGetValue(i, out Height? st);
                if (st == null)
                {
                    LandMode mode = ModeOf(p);
                    if (mode != LandMode.None)
                    {
                        double scaleBase = p.scale;
                        if (scaleBase == 0)
                            scaleBase = 1;
                        st = new Height { Type = beforeType[i], Z = Z0, Vz = Vz0, Base = scaleBase, Mode = mode };
                        z[i] = st;
                    }
                }
                if (st != null)
                {
                    if (!st.Landed)
                    {
                        double dz = k * st.Vz - (grav ? dv * (k + 1) / 2 : 0);
                        if (grav)
                            st.Vz -= dv;
                        if (st.Z + dz <= 0)
                        {
                            dz = -st.Z;
                            st.Z = 0;
                            st.Landed = true;
                            landed++;
                            if (st.Mode == LandMode.Roll)
                            {
                                p.velocity.X = (float)(p.velocity.X * 0.5);
                                p.velocity.Y = (float)(p.velocity.Y * 0.5);
                                rolled++;
                            }
                        }
                        else
                        {
                            st.Z += dz;
                        }
                        posY -= dz;
                        if (st.Landed)
                            p.scale = (float)st.Base;
                        else
                            SetScale(p, st);
                        p.position.Y = (float)posY;
                        if (st.Landed && st.Mode == LandMode.Kill)
                        {
                            killed++;
                            z.Remove(i);
                            p.Kill();
                        }
                        continue;
                    }
                    if (st.Mode == LandMode.Roll)
                    {
                        double vx = p.velocity.X * RollFriction, vy2 = p.velocity.Y * RollFriction;
                        if (Math.Abs(vx) < 0.08)
                            vx = 0;
                        if (Math.Abs(vy2) < 0.08)
                            vy2 = 0;
                        p.velocity.X = (float)vx;
                        p.velocity.Y = (float)vy2;
                    }
                }
                if (grav)
                    p.position.Y = (float)posY;
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }
    }

    public static void InstallProjectiles()
    {
        on = true;
        On_Main.DoUpdate += (orig, self, gameTime) =>
        {
            bool ran = false;
            try
            {
                if (TopDownOn())
                {
                    PreTick();
                    ran = true;
                    ticks++;
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
                    try
                    {
                        PostTick();
                    }
                    catch (Exception e)
                    {
                        Fail(e);
                        Array.Clear(hasBefore);
                    }
                }
            }
        };
        On_Projectile.NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float_NewProjectileModifier += (orig, source, x, y, speedX, speedY, type, damage, knockBack, owner, ai0, ai1, ai2, modifier) =>
        {
            if (nest > 0)
                return orig(source, x, y, speedX, speedY, type, damage, knockBack, owner, ai0, ai1, ai2, modifier);
            nest++;
            int idx;
            try
            {
                idx = orig(source, x, y, speedX, speedY, type, damage, knockBack, owner, ai0, ai1, ai2, modifier);
            }
            finally
            {
                nest--;
            }
            NoteNew(idx);
            return idx;
        };
        On_Projectile.NewProjectile_IEntitySource_Vector2_Vector2_int_int_float_int_float_float_float_NewProjectileModifier += (orig, source, position, velocity, type, damage, knockBack, owner, ai0, ai1, ai2, modifier) =>
        {
            if (nest > 0)
                return orig(source, position, velocity, type, damage, knockBack, owner, ai0, ai1, ai2, modifier);
            nest++;
            int idx;
            try
            {
                idx = orig(source, position, velocity, type, damage, knockBack, owner, ai0, ai1, ai2, modifier);
            }
            finally
            {
                nest--;
            }
            NoteNew(idx);
            return idx;
        };
        TopDownMode.Log("projectiles: hooks installed");
    }

    static void NoteNew(int idx)
    {
        try
        {
            if (idx >= 0 && idx < MaxProj && TopDownOn())
            {
                Projectile p = Main.projectile[idx];
                if (!p.IsNull && p.active)
                    track[idx] = true;
            }
        }
        catch (Exception e)
        {
            Fail(e);
        }
    }

    public static void UninstallProjectiles()
    {
        on = false;
        Array.Clear(track);
        Array.Clear(hasBefore);
        z.Clear();
    }

    public static string Status() =>
        "projectiles: ticks=" + ticks + " checked=" + seen + " gravity undone=" + fixedCount + " range=" + MinPerUpdate + ".." + MaxPerUpdate + " z0=" + Z0 + " simulated=" + z.Count
        + " landed=" + landed + " rolled=" + rolled + " stuck=" + killed + " errors=" + errors + (lastErr != "" ? " last error: " + lastErr : "") + (last.Count > 0 ? " | last: " + string.Join(" ; ", last) : "");
}
