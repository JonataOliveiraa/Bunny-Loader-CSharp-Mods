using System;
using System.Collections.Generic;
using BunnyLoader;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using topdownmode.Common.Configs;
using topdownmode.Common.Systems;
using topdownmode.Content.Effects;

namespace topdownmode.Common.Players;

// Eight-way movement with no gravity, driven from the three ModPlayer hooks.
public class TopDownPlayer : ModPlayer
{
    public static bool On = true;
    public static bool Active;

    // The direction of the last real movement, used to aim when there is no mouse. NaN: none yet.
    public static double Heading = double.NaN;

    static int resetCount, moveCount, postCount;
    static string mode = "detecting";
    static bool reported, climbLogged, noStep, stepHooked;
    static long jumpTick = -1;
    static double lastVx, lastVy;
    static double preX = double.NaN, preVx;
    static int snapLogs;
    static long skippedSteps;
    static long fxWindow;
    static int fxSpawns, lookRespawn, waterRespawn;
    static bool lookSpawned, waterSpawned;
    static readonly HashSet<string> seen = new();
    static readonly HashSet<string> warned = new();

    public static long SkippedSteps => skippedSteps;

    public static string Status() =>
        "movement: mode=" + mode + " on=" + On + " active=" + Active + " ResetEffects=" + resetCount + " PreUpdateMovement=" + moveCount
        + " PostUpdate=" + postCount + " steps skipped=" + skippedSteps + " water effect respawns=" + waterRespawn + " look effect respawns=" + lookRespawn;

    static long Now() => Main.GameUpdateCount;

    static void WarnOnce(string key, string message)
    {
        if (warned.Add(key))
            TopDownMode.Log(message);
    }

    static void ReadInput(Player player, TopDownConfig cfg, out double dx, out double dy, out bool sprint)
    {
        bool left = player.controlLeft;
        bool right = player.controlRight;
        bool up = player.controlUp;
        bool down = player.controlDown;
        bool jump = jumpTick >= 0 && Now() - jumpTick <= 1;
        JumpButtonMode jumpMode = cfg.JumpButton;
        if (jumpMode == JumpButtonMode.Up && jump)
            up = true;

        if (cfg.Debug)
        {
            NoteFirst(left, "controlLeft");
            NoteFirst(right, "controlRight");
            NoteFirst(player.controlUp, "controlUp");
            NoteFirst(down, "controlDown");
            NoteFirst(jump, "controlJump");
        }

        dx = (right ? 1 : 0) - (left ? 1 : 0);
        dy = (down ? 1 : 0) - (up ? 1 : 0);
        double len = Math.Sqrt(dx * dx + dy * dy);
        if (len > 0)
        {
            dx /= len;
            dy /= len;
        }
        sprint = jumpMode == JumpButtonMode.Sprint && jump;
    }

    static void NoteFirst(bool flag, string name)
    {
        if (flag && seen.Add(name))
            TopDownMode.Log("first time active: " + name);
    }

    static void NoFall(Player player)
    {
        player.noFallDmg = true;
        player.fallStart = (int)Math.Floor(player.position.Y / 16.0);
    }

    static void NoTreeClimb()
    {
        bool before = Main.tileRope[TileID.Trees];
        if (before)
            Main.tileRope[TileID.Trees] = false;
        if (!climbLogged)
        {
            climbLogged = true;
            TopDownMode.Log("trunk climbing: tileRope was " + before + " (turned off)");
        }
    }

    static void CheckSnap(Player player)
    {
        if (double.IsNaN(preX))
            return;
        double moved = player.position.X - preX;
        double diff = Math.Abs(moved - preVx);
        if (diff > 3 && snapLogs < 8)
        {
            snapLogs++;
            TopDownMode.Log("pull on X: moved " + moved.ToString("0.00") + " with speed " + preVx.ToString("0.00") + " (pulley=" + player.pulley + ", grappling=" + player.grappling[0] + ")");
        }
        preX = double.NaN;
    }

    public static void InstallNoStep()
    {
        noStep = true;
        if (stepHooked)
            return;
        stepHooked = true;
        On_Collision.StepUp += (orig, ref position, ref velocity, width, height, ref stepSpeed, ref gfxOffY, gravDir, holdsMatching, specialChecksMode) =>
        {
            if (noStep && On && Active)
            {
                skippedSteps++;
                return;
            }
            orig(ref position, ref velocity, width, height, ref stepSpeed, ref gfxOffY, gravDir, holdsMatching, specialChecksMode);
        };
        On_Collision.StepDown += (orig, ref position, ref velocity, width, height, ref stepSpeed, ref gfxOffY, gravDir, waterWalk) =>
        {
            if (noStep && On && Active)
            {
                skippedSteps++;
                return;
            }
            orig(ref position, ref velocity, width, height, ref stepSpeed, ref gfxOffY, gravDir, waterWalk);
        };
        On_Collision.TileCollision += (orig, oldPosition, oldVelocity, width, height, fallThrough, fall2, gravDir, ignoreDoors, ignoreAetheriumPlatforms, hoik) =>
        {
            bool suspended = On && Active && TopDownCactus.SuspendBases(oldPosition, width, height);
            if (!suspended)
                return orig(oldPosition, oldVelocity, width, height, fallThrough, fall2, gravDir, ignoreDoors, ignoreAetheriumPlatforms, hoik);
            try
            {
                return orig(oldPosition, oldVelocity, width, height, fallThrough, fall2, gravDir, ignoreDoors, ignoreAetheriumPlatforms, hoik);
            }
            finally
            {
                TopDownCactus.ResumeBases();
            }
        };
        TopDownMode.Log("step magnet off; cactus collision without the invisible base (Collision.TileCollision hooked)");
    }

    public static void UninstallNoStep() => noStep = false;

    static void EnsureWaterFx()
    {
        try
        {
            long now = Now();
            if (now - fxWindow >= 60)
            {
                fxWindow = now;
                fxSpawns = 0;
            }
            if (fxSpawns >= 20)
                return;
            if (ScreenEffects.Find("topdown:look") == null)
            {
                ScreenEffects.Spawn<TopDownLook>();
                fxSpawns++;
                if (lookSpawned)
                    lookRespawn++;
                lookSpawned = true;
                if (lookRespawn > 0 && lookRespawn <= 5)
                    TopDownMode.Log("look: effect recreated (" + lookRespawn + ")");
            }
            if (ScreenEffects.Find("topdown:water") == null)
            {
                ScreenEffects.Spawn<TopDownWater>();
                fxSpawns++;
                if (!waterSpawned)
                {
                    waterSpawned = true;
                    TopDownMode.Log("moving water: screen effect created");
                }
                else
                {
                    waterRespawn++;
                    if (waterRespawn <= 5)
                        TopDownMode.Log("water: effect recreated (" + waterRespawn + ")");
                }
            }
        }
        catch (Exception e)
        {
            WarnOnce("waterfx", "could not create the water effect: " + e);
        }
    }

    public override void ResetEffects() => Phase(Player, 0);

    public override void PreUpdateMovement() => Phase(Player, 1);

    public override void PostUpdate() => Phase(Player, 2);

    void Phase(Player player, int phase)
    {
        if (player.whoAmI != Main.myPlayer)
            return;
        if (phase == 0) resetCount++;
        else if (phase == 1) moveCount++;
        else postCount++;
        if (phase == 2)
        {
            EnsureWaterFx();
            TopDownNoHeat.SuppressHeat();
        }

        TopDownConfig cfg = TopDownConfig.Instance;
        bool active = On && cfg.Enabled && !player.dead;
        Active = active;
        if (!active)
            return;

        if (player.controlJump)
            jumpTick = Now();
        player.controlJump = false;
        player.jump = 0;

        if (phase == 0)
        {
            if ((resetCount & 127) == 1)
                NoTreeClimb();
            try
            {
                TopDownTreeBase.RefreshBases(player);
            }
            catch (Exception e)
            {
                WarnOnce("bases", "error in the tree bases: " + e);
            }
            player.pulley = false;
            player.gravity = 0;
            player.noFallDmg = true;
            return;
        }

        if (phase == 1)
        {
            Drive(player, cfg, false);
            preX = player.position.X;
            preVx = player.velocity.X;
            return;
        }

        CheckSnap(player);
        if (!cfg.Noclip)
        {
            if (TopDownTreeBase.OverlapFix(player, out double fixX, out double fixY))
                player.position = new Vector2((float)(player.position.X + fixX), (float)(player.position.Y + fixY));
        }
        NoFall(player);
        bool moveWorks = moveCount > 0;
        if (postCount == 120)
            Report(cfg, moveWorks);
        if (!moveWorks)
            Drive(player, cfg, true);
    }

    static void Drive(Player player, TopDownConfig cfg, bool fallback)
    {
        ReadInput(player, cfg, out double dx, out double dy, out bool sprint);
        bool attacking = TopDownCombat.AttackActive;
        double speed = cfg.Speed * (sprint ? cfg.SprintMultiplier : 1);
        if (attacking)
            speed *= Math.Min(1, Math.Max(0.1, cfg.AttackMoveMultiplier));
        double a = Math.Min(1, Math.Max(0.05, cfg.Smoothing));

        double vx = player.velocity.X, vy = player.velocity.Y;

        double g = fallback ? player.gravity : 0;

        vx += (dx * speed - vx) * a;
        vy += (dy * speed - vy) * a;
        if (dx == 0 && Math.Abs(vx) < 0.05)
            vx = 0;
        if (dy == 0 && Math.Abs(vy) < 0.05)
            vy = 0;
        if (!cfg.Noclip)
            TopDownTreeBase.AdjustVelocity(player, ref vx, ref vy);
        lastVx = vx;
        lastVy = vy;

        if (Math.Sqrt(vx * vx + vy * vy) > 0.5)
            Heading = Math.Atan2(vy, vx);
        if (dx != 0 && !attacking)
            player.direction = dx > 0 ? 1 : -1;
        player.jump = 0;

        if (cfg.Noclip)
        {
            player.position = new Vector2((float)(player.position.X + vx), (float)(player.position.Y + vy));
            player.velocity = new Vector2(0, 0);
        }
        else
        {
            player.velocity = new Vector2((float)vx, (float)(vy - g));
        }
        NoFall(player);
    }

    static void Report(TopDownConfig cfg, bool moveWorks)
    {
        if (reported)
            return;
        reported = true;
        mode = moveWorks ? "PreUpdateMovement" : "plan B (PostUpdate)";
        if (cfg.Debug)
            TopDownMode.Log("hooks fired in 120 ticks: ResetEffects=" + resetCount + ", PreUpdateMovement=" + moveCount + ", PostUpdate=" + postCount + " -> mode: " + mode);
    }
}
