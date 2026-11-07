using System;
using System.Collections.Generic;
using Terraria;
using topdownmode.Common.Configs;
using topdownmode.Common.Players;

namespace topdownmode.Common.NPCs;

// NPCs without gravity. The game's AI still runs for the horizontal side; the vertical side is
// steered here, with slimes hopping along a straight line and a fake height.
public static class TopDownNPC
{
    enum BossMode { NotBoss, Slime, Walker, Free }

    sealed class NpcState
    {
        public int Type;
        public long Tick;
        public bool Slime, Air;
        public double? Lift;
    }

    sealed class Hop
    {
        public int Type, T, Dur;
        public double Ux, Uy, Vs, H;
    }

    sealed class Wander
    {
        public int Type, T, Idle, Stroll, Sx = 1, Still;
        public double Vy, Svy, Px, Py;
    }

    sealed class BossSeen
    {
        public string Name = "";
        public int AiStyle;
        public BossMode Mode;
    }

    static readonly double[] StrollSpeeds = { -0.7, 0, 0, 0.7 };

    static readonly Dictionary<int, Wander> wander = new();
    static readonly Dictionary<int, Hop> hops = new();
    static readonly Dictionary<int, NpcState> state = new();
    static readonly Dictionary<int, BossSeen> bossSeen = new();
    static readonly Random random = new();

    static bool on = true, installed;
    static int cur = -1;
    static bool curMine;

    public static int Ticks, Steered, Skipped, Errors, HopCount, Strolls, Frames, Lifted;
    public static string LastErr = "";

    static double Rnd() => random.NextDouble();

    static long Now() => Main.GameUpdateCount;

    static double Clamp(double v, double a, double b) => Math.Min(b, Math.Max(a, v));

    static bool TopDownOn() => on && TopDownPlayer.On && TopDownPlayer.Active && TopDownConfig.Instance.NpcTopDown;

    static void Fail(Exception e)
    {
        Errors++;
        LastErr = e.ToString();
        if (Errors <= 3)
            TopDownMode.Log("NPCs (error): " + e);
    }

    static BossMode ModeOf(NPC n)
    {
        if (!n.boss)
            return BossMode.NotBoss;
        BossMode m = n.aiStyle switch
        {
            15 => BossMode.Slime,
            3 or 22 or 87 => BossMode.Walker,
            _ => BossMode.Free,
        };
        if (!bossSeen.ContainsKey(n.type))
        {
            var seen = new BossSeen { Name = n.GivenOrTypeName ?? n.type.ToString(), AiStyle = n.aiStyle, Mode = m };
            bossSeen[n.type] = seen;
            TopDownMode.Log("boss seen: " + seen.Name + " (type " + n.type + ", aiStyle " + n.aiStyle + ") -> mode " + m);
        }
        return m;
    }

    static bool TargetOf(NPC n, out Player target)
    {
        int t = n.target;
        target = default;
        if (t >= 0 && t < 255)
            target = Main.player[t];
        if (target.IsNull || !target.active || target.dead)
            target = Main.player[Main.myPlayer];
        return !target.IsNull && target.active && !target.dead;
    }

    static NpcState Remember(NPC n, bool slime)
    {
        int id = n.whoAmI;
        if (!state.TryGetValue(id, out NpcState? s) || s.Type != n.type)
        {
            s = new NpcState { Type = n.type, Slime = slime };
            if (state.Count > 600)
                state.Clear();
            state[id] = s;
        }
        s.Tick = Now();
        s.Slime = slime;
        return s;
    }

    static Hop StartHop(NPC n, double vxAI, double vyAI)
    {
        double px = (double)n.position.X + n.width / 2.0;
        double py = (double)n.position.Y + n.height / 2.0;
        double ux = 0, uy = 0, scale = 1;
        bool aimed = false;
        if (!(n.friendly || n.townNPC || n.damage <= 0) && TargetOf(n, out Player p))
        {
            double dx = ((double)p.position.X + p.width / 2.0) - px;
            double dy = ((double)p.position.Y + p.height / 2.0) - py;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1400)
            {
                double a = Math.Atan2(dy, dx) + (Rnd() - 0.5) * 0.5;
                ux = Math.Cos(a);
                uy = Math.Sin(a);
                scale = Clamp(len / 90 + 0.3, 0.3, 1);
                aimed = true;
            }
        }
        if (!aimed)
        {
            int d = n.direction >= 0 ? 1 : -1;
            double a = Math.Atan2((Rnd() * 2 - 1) * 0.9, d * (0.4 + Rnd() * 0.6));
            ux = Math.Cos(a);
            uy = Math.Sin(a);
        }
        double jump = Math.Abs(vyAI);
        var h = new Hop
        {
            Type = n.type,
            T = 0,
            Dur = jump > 7 ? 36 : 30,
            Ux = ux,
            Uy = uy,
            Vs = Clamp(Math.Abs(vxAI), 2, 6) * scale,
            H = Clamp(jump * 2.4, 12, 30) * (n.boss ? 1.6 : 1),
        };
        if (hops.Count > 600)
            hops.Clear();
        hops[n.whoAmI] = h;
        HopCount++;
        return h;
    }

    static void SlimeStep(NPC n, double mem)
    {
        int id = n.whoAmI;
        NpcState s = Remember(n, true);
        hops.TryGetValue(id, out Hop? h);
        if (h != null && h.Type != n.type)
        {
            hops.Remove(id);
            h = null;
        }

        double vxAI = n.velocity.X, vyAI = n.velocity.Y;
        if (h == null && vyAI < -1)
            h = StartHop(n, vxAI, vyAI);

        if (h != null)
        {
            double u = (h.T + 0.5) / h.Dur;
            double arc = 4 * u * (1 - u);
            double sp = h.Vs * (0.45 + 0.75 * arc);
            n.velocity.X = (float)(h.Ux * sp);
            n.velocity.Y = (float)(h.Uy * sp);
            if (Math.Abs(h.Ux) > 0.25)
            {
                int d = h.Ux > 0 ? 1 : -1;
                n.direction = d;
                n.spriteDirection = d;
            }
            s.Air = u < 0.8;
            s.Lift = -h.H * arc;
            h.T++;
            if (h.T >= h.Dur)
            {
                hops.Remove(id);
                n.velocity.X = 0;
                n.velocity.Y = 0;
                s.Air = false;
                s.Lift = 0;
            }
        }
        else
        {
            double vy = mem * 0.85;
            if (Math.Abs(vy) < 0.05)
                vy = 0;
            n.velocity.Y = (float)vy;
            s.Air = false;
            if (s.Lift != null && s.Lift != 0)
                s.Lift = 0;
        }
        Steered++;
    }

    static bool TalkingTo(NPC n) => Main.player[Main.myPlayer].talkNPC == n.whoAmI;

    static void WalkerStep(NPC n, double mem)
    {
        int id = n.whoAmI;
        Remember(n, false);
        double vx = n.velocity.X;
        double want = 0;

        bool friendly = n.friendly || n.townNPC || n.damage <= 0;
        if (!friendly && TargetOf(n, out Player p))
        {
            double dx = ((double)p.position.X + p.width / 2.0) - ((double)n.position.X + n.width / 2.0);
            double dy = ((double)p.position.Y + p.height / 2.0) - ((double)n.position.Y + n.height / 2.0);
            if (Math.Sqrt(dx * dx + dy * dy) < 900 && Math.Abs(dy) > 6)
            {
                double s = Math.Min(4, Math.Max(1.4, Math.Abs(vx)));
                want = Math.Sign(dy) * Math.Min(s, Math.Abs(dy) / 10);
            }
        }
        else if (friendly)
        {
            if (!wander.TryGetValue(id, out Wander? w) || w.Type != n.type)
            {
                w = new Wander { Type = n.type };
                if (wander.Count > 600)
                    wander.Clear();
                wander[id] = w;
            }
            bool town = n.townNPC;
            double ai0 = n.ai[0];
            bool walking = Math.Abs(vx) > 0.05;

            if (town && TalkingTo(n))
            {
                w.Stroll = 0;
                w.Idle = 0;
                want = 0;
            }
            else if (w.Stroll > 0)
            {
                double px = n.position.X, py = n.position.Y;
                w.Still = (Math.Abs(px - w.Px) < 0.01 && Math.Abs(py - w.Py) < 0.01) ? w.Still + 1 : 0;
                w.Px = px;
                w.Py = py;
                if (ai0 > 1 || w.Still > 20)
                {
                    w.Stroll = 0;
                    w.Idle = 0;
                }
                else
                {
                    w.Stroll--;
                    vx += (w.Sx * 0.9 - vx) * 0.25;
                    n.velocity.X = (float)vx;
                    n.direction = w.Sx;
                    n.spriteDirection = w.Sx;
                    want = w.Svy;
                    if (w.Stroll <= 0)
                        w.Idle = 0;
                }
            }
            else
            {
                if (walking)
                    w.Idle = 0;
                else if (ai0 == 0)
                    w.Idle++;
                if (town && w.Idle > 300 && ai0 == 0)
                {
                    w.Stroll = 90 + (int)Math.Floor(Rnd() * 120);
                    w.Sx = Rnd() < 0.5 ? -1 : 1;
                    w.Svy = StrollSpeeds[(int)Math.Floor(Rnd() * 4)];
                    w.Still = 0;
                    w.Idle = 0;
                    Strolls++;
                }
                else if (walking)
                {
                    if (--w.T <= 0)
                    {
                        w.T = 60 + (int)Math.Floor(Rnd() * 120);
                        w.Vy = StrollSpeeds[(int)Math.Floor(Rnd() * 4)];
                    }
                    want = w.Vy;
                }
            }

            if (town && !n.homeless && !Main.dayTime && w.Stroll <= 0 && ai0 <= 1 && !TalkingTo(n))
            {
                int hy = n.homeTileY;
                if (hy > 0)
                {
                    double dyh = hy * 16 - ((double)n.position.Y + n.height);
                    if (Math.Abs(dyh) > 20)
                        want = Math.Sign(dyh) * Math.Min(0.9, Math.Abs(dyh) / 20);
                }
            }
        }

        double vy = mem + (want - mem) * 0.25;
        if (want == 0 && Math.Abs(vy) < 0.05)
            vy = 0;
        n.velocity.Y = (float)vy;
        Steered++;
    }

    static void Steer(NPC n, double mem)
    {
        BossMode bm = ModeOf(n);
        if (bm == BossMode.Slime || (bm == BossMode.NotBoss && n.aiStyle == 1 && !n.townNPC))
            SlimeStep(n, mem);
        else
            WalkerStep(n, mem);
    }

    public static void InstallNpcTopDown()
    {
        if (installed)
            return;
        installed = true;
        On_NPC.UpdateNPC += UpdateNpc;
        On_NPC.AI += Ai;
        On_NPC.FindFrame += FindFrame;
        TopDownMode.Log("top-down NPCs: hooks installed");
    }

    public static void UninstallNpcTopDown()
    {
        on = false;
        wander.Clear();
        hops.Clear();
        state.Clear();
    }

    static void UpdateNpc(On_NPC.orig_UpdateNPC orig, NPC self, int i)
    {
        bool mine = false;
        try
        {
            if (TopDownOn() && self.active && !self.noGravity)
            {
                self.noGravity = true;
                mine = true;
                cur = self.whoAmI;
                curMine = true;
                Ticks++;
            }
        }
        catch (Exception e)
        {
            Fail(e);
        }
        try
        {
            orig(self, i);
        }
        finally
        {
            if (mine)
            {
                try
                {
                    self.noGravity = false;
                    if (state.TryGetValue(self.whoAmI, out NpcState? s) && s.Type == self.type && s.Lift != null && s.Tick == Now())
                    {
                        self.gfxOffY = (float)s.Lift.Value;
                        Lifted++;
                        if (s.Lift == 0)
                            s.Lift = null;
                    }
                }
                catch (Exception e)
                {
                    Fail(e);
                }
            }
            cur = -1;
            curMine = false;
        }
    }

    static void Ai(On_NPC.orig_AI orig, NPC self)
    {
        bool current = curMine && cur == self.whoAmI;
        if (!current || self.aiStyle == 6 || ModeOf(self) == BossMode.Free)
        {
            if (current)
                Skipped++;
            orig(self);
            return;
        }
        double mem = self.velocity.Y;
        self.velocity.Y = 0;
        try
        {
            orig(self);
        }
        catch
        {
            self.velocity.Y = (float)mem;
            throw;
        }
        try
        {
            Steer(self, mem);
        }
        catch (Exception e)
        {
            Fail(e);
            self.velocity.Y = (float)mem;
        }
    }

    static void FindFrame(On_NPC.orig_FindFrame orig, NPC self)
    {
        bool swapped = false;
        float ovx = 0, ovy = 0;
        try
        {
            if (on && state.TryGetValue(self.whoAmI, out NpcState? s) && s.Type == self.type && Now() - s.Tick <= 2)
            {
                ovx = self.velocity.X;
                ovy = self.velocity.Y;
                if (s.Slime)
                {
                    self.velocity.Y = s.Air ? -1 : 0;
                }
                else
                {
                    double sp = Math.Sqrt((double)ovx * ovx + (double)ovy * ovy);
                    int d = ovx != 0 ? Math.Sign(ovx) : (self.direction >= 0 ? 1 : -1);
                    self.velocity.X = sp > 0.1 ? (float)(d * sp) : 0;
                    self.velocity.Y = 0;
                }
                swapped = true;
                Frames++;
            }
        }
        catch (Exception e)
        {
            Fail(e);
        }
        try
        {
            orig(self);
        }
        finally
        {
            if (swapped)
            {
                self.velocity.X = ovx;
                self.velocity.Y = ovy;
            }
        }
    }

    public static string Status() =>
        "NPCs: on=" + on + " ticks=" + Ticks + " steered=" + Steered + " hops=" + HopCount + " lifts=" + Lifted + " strolls=" + Strolls
        + " frames=" + Frames + " skipped=" + Skipped + " bosses seen=" + bossSeen.Count + " errors=" + Errors + (LastErr != "" ? " last error: " + LastErr : "");
}
