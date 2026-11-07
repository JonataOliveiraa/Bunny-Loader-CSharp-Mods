using System;
using System.Collections.Generic;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using topdownmode.Common.Configs;

namespace topdownmode.Common.Players;

// Swings toward the aim: the blade sweeps an arc around the mouse direction, the arm follows,
// and an own hitbox replaces the vanilla melee rectangle.
public static class TopDownCombat
{
    sealed class TextureSize
    {
        public int W, H, Fails;
        public bool Ok;
    }

    struct SwingTest
    {
        public bool Stab;
        public double Cx, Cy, L;
        public double A0, SpanAbs, Sign;
        public double Cos, Sin, Reach, HalfWidth;

        public readonly bool Hits(double px, double py)
        {
            double dx = px - Cx, dy = py - Cy;
            if (Stab)
            {
                double along = dx * Cos + dy * Sin;
                if (along < 0.05 * L || along > Reach)
                    return false;
                return Math.Abs(-dx * Sin + dy * Cos) <= HalfWidth;
            }
            double r = Math.Sqrt(dx * dx + dy * dy);
            if (r < 0.12 * L || r > 1.08 * L)
                return false;
            double rel = WrapPi(Math.Atan2(dy, dx) - A0) * Sign;
            return rel >= -Pad && rel <= SpanAbs + Pad;
        }
    }

    const double Pad = 0.14;
    const int TargetDummy = 488;
    const int MaxNpcSlots = 400;

    public static bool AttackActive;
    public static double AimAngle, Progress;
    public static int Hits;

    static bool enabled, hooked, logged, hitboxFailed;
    static int strikeFails;
    static int forcedNoMeleeType = -1;
    static readonly HashSet<string> warned = new();
    static readonly Dictionary<int, TextureSize> texCache = new();
    static readonly Random random = new();

    // The swing in progress.
    static bool stateActive, stateFrontArm, stateBackArm, stateReverse;
    static int statePrevAnim, stateType = -1, stateDir = 1, stateCount;
    static double stateAim;
    static GameHandle? noMeleeItem;
    static readonly bool[] stateHits = new bool[MaxNpcSlots];
    static readonly List<int> stateCand = new();
    static bool candValid;
    static long candTick;

    static double WrapPi(double a) => a - 2 * Math.PI * Math.Floor((a + Math.PI) / (2 * Math.PI));

    static double Smooth3(double v)
    {
        double x = Math.Max(0, Math.Min(1, v));
        return x * x * (3 - 2 * x);
    }

    static void WarnOnce(string key, string message)
    {
        if (warned.Add(key))
            TopDownMode.Log(message);
    }

    static TextureSize? GetItemTexture(int type)
    {
        texCache.TryGetValue(type, out TextureSize? e);
        if (e != null && e.Ok)
            return e;
        if (e != null && e.Fails >= 40)
            return null;
        try
        {
            if (e == null)
                Main.instance.LoadItem(type);
            Texture2D tex = TextureAssets.Item[type].Value;
            int w = tex.IsNull ? 0 : tex.Width;
            int h = tex.IsNull ? 0 : tex.Height;
            DrawAnimation anim = Main.itemAnimations[type];
            int frames = anim.IsNull ? 1 : anim.FrameCount;
            if (frames > 1)
                h /= frames;
            if (w > 0 && h > 0)
            {
                e = new TextureSize { W = w, H = h, Ok = true };
                texCache[type] = e;
                return e;
            }
        }
        catch (Exception x)
        {
            WarnOnce("tex", "texture of item " + type + " unavailable: " + x);
        }
        e ??= new TextureSize();
        e.Fails++;
        texCache[type] = e;
        return null;
    }

    static bool IsSwingWeapon(Item item)
    {
        if (item.IsNull || item.type <= 0)
            return false;
        if (item.damage <= 0)
            return false;
        int style = item.useStyle;
        if (style != 1 && style != 3)
            return false;
        if (item.noMelee && forcedNoMeleeType != item.type)
            return false;
        if (item.noUseGraphic)
            return false;
        if (item.pick > 0 || item.axe > 0 || item.hammer > 0)
            return false;
        return true;
    }

    static double LocalTipAngle(int w, int h, int dir, int grav) =>
        Math.Atan2(grav < 0 ? h : -h, dir < 0 ? -w : w);

    static void TipOffset(int w, int h, int dir, int grav, double scale, double rot, out double x, out double y)
    {
        double lx = (dir < 0 ? -w : w) * scale;
        double ly = (grav < 0 ? h : -h) * scale;
        double c = Math.Cos(rot), s = Math.Sin(rot);
        x = lx * c - ly * s;
        y = lx * s + ly * c;
    }

    static void ReadAim(Player player, out double angle, out int direction, out string from)
    {
        int dirNow = player.direction < 0 ? -1 : 1;
        Vector2 mouse = Main.MouseWorld;
        Vector2 center = player.Center;
        double mdx = (double)mouse.X - center.X, mdy = (double)mouse.Y - center.Y;
        if (Math.Sqrt(mdx * mdx + mdy * mdy) >= 24)
        {
            direction = Math.Abs(mdx) < 12 ? dirNow : (mdx < 0 ? -1 : 1);
            angle = Math.Atan2(mdy, mdx);
            from = "mouse";
            return;
        }
        double heading = TopDownPlayer.Heading;
        if (!double.IsNaN(heading))
        {
            angle = heading;
            direction = Math.Cos(heading) < -0.2 ? -1 : (Math.Cos(heading) > 0.2 ? 1 : dirNow);
            from = "heading";
            return;
        }
        angle = dirNow > 0 ? 0 : Math.PI;
        direction = dirNow;
        from = "facing";
    }

    static double SweepAt(double p, double arc, bool reverse)
    {
        double a = (Smooth3(p) - 0.5) * arc;
        return reverse ? -a : a;
    }

    static double StabExtend(double p) => Smooth3(p / 0.40) * (1 - Smooth3((p - 0.62) / 0.38));

    static void SetArm(Player player, bool front, bool on, double rotation)
    {
        if (front)
            player.SetCompositeArmFront(on, Player.CompositeArmStretchAmount.Full, (float)rotation);
        else
            player.SetCompositeArmBack(on, Player.CompositeArmStretchAmount.None, (float)rotation);
    }

    static void ApplyArm(Player player, double angle, int dir)
    {
        double armRot = WrapPi(angle - Math.PI / 2 * dir);
        SetArm(player, true, true, armRot);
        player.itemLocation = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, (float)armRot);
        SetArm(player, false, true, dir >= 0 ? 2.18 : (Math.PI - 2.18));
        stateFrontArm = true;
        stateBackArm = true;
    }

    static void SetNoMelee(Item item, bool on)
    {
        if (on)
        {
            if (!item.IsNull && noMeleeItem == null && !item.noMelee)
            {
                item.noMelee = true;
                noMeleeItem = new GameHandle(item.Pointer);
                forcedNoMeleeType = item.type;
            }
        }
        else if (noMeleeItem != null)
        {
            Item held = new(noMeleeItem.Target);
            held.noMelee = false;
            noMeleeItem.Dispose();
            noMeleeItem = null;
            forcedNoMeleeType = -1;
        }
    }

    static void Release(Player player)
    {
        SetNoMelee(default, false);
        if (stateFrontArm)
        {
            SetArm(player, true, false, 0);
            stateFrontArm = false;
        }
        if (stateBackArm)
        {
            SetArm(player, false, false, 0);
            stateBackArm = false;
        }
    }

    static bool MakeWedgeTest(double cx, double cy, double L, double a0, double a1, out SwingTest test)
    {
        double span = WrapPi(a1 - a0);
        double spanAbs = Math.Abs(span);
        test = new SwingTest { Cx = cx, Cy = cy, L = L, A0 = a0, SpanAbs = spanAbs, Sign = span < 0 ? -1 : 1 };
        return spanAbs >= 0.02;
    }

    static SwingTest MakeStabTest(double cx, double cy, double L, double ang, double ext) => new()
    {
        Stab = true,
        Cx = cx,
        Cy = cy,
        L = L,
        Cos = Math.Cos(ang),
        Sin = Math.Sin(ang),
        Reach = L * (1 + 0.45 * ext),
        HalfWidth = Math.Max(8, 0.11 * L),
    };

    static bool NpcHits(NPC npc, in SwingTest test)
    {
        double x = npc.position.X, y = npc.position.Y;
        int w = npc.width, h = npc.height;
        if (w <= 0 || h <= 0)
            return false;
        int nx = Math.Max(2, Math.Min(6, (int)Math.Ceiling(w / 12.0))), ny = Math.Max(2, Math.Min(6, (int)Math.Ceiling(h / 12.0)));
        for (int i = 0; i <= nx; i++)
        {
            for (int j = 0; j <= ny; j++)
            {
                if (test.Hits(x + (double)w * i / nx, y + (double)h * j / ny))
                    return true;
            }
        }
        return false;
    }

    static bool StrikeNpc(NPC npc, Item item, int dmg, float kb, int dir, bool crit)
    {
        try
        {
            npc.SimpleStrikeNPC(dmg, dir, crit, kb, item.DamageType, true, 0, false);
            return true;
        }
        catch (GameException e)
        {
            WarnOnce("strike", "damage through SimpleStrikeNPC failed: " + e.Message);
        }
        strikeFails++;
        if (strikeFails >= 3 && !hitboxFailed)
        {
            hitboxFailed = true;
            TopDownMode.Log("the damage call does not work: own hitbox off, the vanilla swing deals the damage again.");
        }
        return false;
    }

    static void DebugDust(double cx, double cy, double L, double a0, double a1, bool stab, double ang, double ext)
    {
        var color = new Color(255, 255, 255, 0);
        if (stab)
        {
            for (int k = 1; k <= 8; k++)
            {
                double al = L * (1 + 0.45 * ext) * k / 8;
                OneDust(cx + Math.Cos(ang) * al, cy + Math.Sin(ang) * al, color);
            }
        }
        else
        {
            double span = WrapPi(a1 - a0);
            for (int k = 0; k <= 6; k++)
            {
                double a = a0 + span * k / 6;
                OneDust(cx + Math.Cos(a) * L, cy + Math.Sin(a) * L, color);
            }
        }
    }

    static void OneDust(double x, double y, Color color)
    {
        int idx = Dust.NewDust(new Vector2((float)(x - 2), (float)(y - 2)), 4, 4, 278, 0, 0, 0, color, 0.6f);
        Dust dust = Main.dust[idx];
        dust.noGravity = true;
        dust.velocity.X = 0;
        dust.velocity.Y = 0;
    }

    static void RunHitbox(Player player, Item item, TopDownConfig cfg, double cx, double cy, double L, bool stab, double ang, double ext, double a0, double a1, bool newAttack)
    {
        try
        {
            if (hitboxFailed)
            {
                SetNoMelee(default, false);
                return;
            }
            if (newAttack)
            {
                Array.Clear(stateHits);
                candValid = false;
            }
            SwingTest test;
            if (stab)
            {
                if (ext < 0.08)
                    return;
                test = MakeStabTest(cx, cy, L, ang, ext);
            }
            else if (!MakeWedgeTest(cx, cy, L, a0, a1, out test))
            {
                return;
            }

            if (cfg.ShowHitbox && Main.GameUpdateCount % 3 == 0)
                DebugDust(cx, cy, L, a0, a1, stab, ang, ext);

            int pw = player.whoAmI;
            double pcx = player.Center.X;
            double reach = L * 1.6 + 60;
            long tick = Main.GameUpdateCount;

            if (!candValid || tick - candTick >= 4 || tick < candTick)
            {
                stateCand.Clear();
                int maxN = Math.Max(1, Math.Min(400, Main.maxNPCs));
                for (int i = 0; i < maxN; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.IsNull || !npc.active)
                        continue;
                    Vector2 center = npc.Center;
                    double dx = center.X - cx, dy = center.Y - cy;
                    if (Math.Sqrt(dx * dx + dy * dy) > reach + 240 + Math.Max(npc.width, npc.height))
                        continue;
                    stateCand.Add(i);
                }
                candValid = true;
                candTick = tick;
            }

            bool haveStats = false;
            int statBase = 0, statCrit = 0;
            float statKb = 0;
            double dmgScale = Math.Max(0.1, cfg.HitboxDamageScale);
            for (int c = 0; c < stateCand.Count; c++)
            {
                int i = stateCand[c];
                if (stateHits[i])
                    continue;
                NPC npc = Main.npc[i];
                if (npc.IsNull || !npc.active || npc.life <= 0)
                    continue;
                bool dummy = npc.type == TargetDummy;
                if (!dummy && (npc.friendly || npc.dontTakeDamage || npc.immortal))
                    continue;
                if (dummy && npc.dontTakeDamage)
                    continue;
                Vector2 center = npc.Center;
                double dx = center.X - cx, dy = center.Y - cy;
                if (Math.Sqrt(dx * dx + dy * dy) > reach + Math.Max(npc.width, npc.height))
                    continue;
                if (npc.immune[pw] > 0)
                    continue;
                if (!NpcHits(npc, test))
                    continue;

                if (!haveStats)
                {
                    haveStats = true;
                    statBase = player.GetWeaponDamage(item);
                    statCrit = player.GetWeaponCrit(item);
                    statKb = player.GetWeaponKnockback(item, item.knockBack);
                }
                bool crit = random.NextDouble() * 100 < statCrit;
                int dmg = Math.Max(1, (int)Math.Floor(statBase * dmgScale + 0.5));
                int dir = center.X >= pcx ? 1 : -1;
                stateHits[i] = true;
                if (StrikeNpc(npc, item, dmg, statKb, dir, crit))
                {
                    Hits++;
                    npc.immune[pw] = Math.Max(npc.immune[pw], 4);
                }
            }
        }
        catch (Exception e)
        {
            WarnOnce("hitbox", "error in the hitbox: " + e);
        }
    }

    static void Stop(Player player)
    {
        if (stateActive || stateFrontArm || stateBackArm || noMeleeItem != null)
            Release(player);
        stateActive = false;
        statePrevAnim = 0;
        AttackActive = false;
    }

    static void Apply(Player player, int index)
    {
        if (player.IsNull || index != Main.myPlayer)
            return;
        TopDownConfig cfg = TopDownConfig.Instance;

        if (!cfg.Enabled || !cfg.MouseAim || player.dead)
        {
            Stop(player);
            return;
        }

        int anim = player.itemAnimation;
        if (anim <= 0)
        {
            Stop(player);
            return;
        }

        Item item = player.HeldItem;
        if (!IsSwingWeapon(item))
        {
            Stop(player);
            return;
        }
        int style = item.useStyle;
        int type = item.type;
        int maxAnim = Math.Max(1, player.itemAnimationMax);

        if (noMeleeItem != null && new Item(noMeleeItem.Target).type != type)
            SetNoMelee(default, false);

        bool restarted = !stateActive || stateType != type || (statePrevAnim <= 2 && anim >= maxAnim - 1);
        if (restarted)
        {
            stateActive = true;
            stateType = type;
            stateCount++;
            stateReverse = (stateCount % 2) == 0;
            ReadAim(player, out stateAim, out stateDir, out string from);
            if (cfg.Debug)
                TopDownMode.Log("swing #" + stateCount + ": aim " + (stateAim * 180 / Math.PI).ToString("0") + " degrees (" + from + "), side " + stateDir);
        }
        else if (cfg.AimMode == AimModeKind.Follow)
        {
            ReadAim(player, out stateAim, out stateDir, out _);
        }
        statePrevAnim = anim;
        AttackActive = true;
        AimAngle = stateAim;

        double progress = Math.Max(0, Math.Min(1, 1 - (double)anim / maxAnim));
        Progress = progress;
        int dir = stateDir;
        int grav = player.gravDir < 0 ? -1 : 1;
        player.direction = dir;

        TextureSize? tex = GetItemTexture(type);
        int w = tex != null ? tex.W : 32, h = tex != null ? tex.H : 32;
        double scale = Math.Max(0.1, item.scale);
        double tipLocal = LocalTipAngle(w, h, dir, grav);

        double arc = Math.Max(0.5, Math.Min(4.7, cfg.SwingArc * Math.PI / 180));
        bool stab = style == 3;
        double blade = BladeAt(stab, dir, arc, progress);
        double angle = WrapPi(blade - tipLocal);
        player.itemRotation = (float)angle;

        if (cfg.ArmPose)
            ApplyArm(player, angle, dir);

        double ext = 0;
        if (stab)
        {
            ext = StabExtend(progress);
            Vector2 loc0 = player.itemLocation;
            TipOffset(w, h, dir, grav, scale, angle, out double ox, out double oy);
            double l0 = Math.Sqrt(ox * ox + oy * oy);
            if (l0 > 1 && ext > 0.01)
            {
                double push = 0.30 * l0 * ext;
                player.itemLocation = new Vector2((float)(loc0.X + ox / l0 * push), (float)(loc0.Y + oy / l0 * push));
            }
        }

        if (cfg.CustomHitbox && !hitboxFailed)
        {
            SetNoMelee(item, true);
            Vector2 loc = player.itemLocation;
            double L = Math.Sqrt((double)w * w + (double)h * h) * scale;
            RunHitbox(player, item, cfg, loc.X, loc.Y, L, stab, stateAim, ext, BladeAt(stab, dir, arc, Math.Max(0, progress - 0.30)), blade, restarted);
        }
        else
        {
            SetNoMelee(default, false);
        }

        if (!logged)
        {
            logged = true;
            TopDownMode.Log("swing toward the mouse active (composite arm " + (stateFrontArm ? "ok" : "off") + ").");
        }
    }

    static double BladeAt(bool stab, int dir, double arc, double p) => stab ? stateAim : stateAim + dir * SweepAt(p, arc, stateReverse);

    public static void InstallCombatHook()
    {
        enabled = true;
        if (hooked)
            return;
        On_Player.Update += (orig, self, i) =>
        {
            orig(self, i);
            if (enabled)
                Apply(self, i);
        };
        hooked = true;
        TopDownMode.Log("swing hook installed");
    }

    public static void UninstallCombatHook()
    {
        enabled = false;
        AttackActive = false;
        SetNoMelee(default, false);
    }

    public static string Status() =>
        "combat: active=" + AttackActive + " swings=" + stateCount + " hits=" + Hits + " hitbox failed=" + hitboxFailed;
}
