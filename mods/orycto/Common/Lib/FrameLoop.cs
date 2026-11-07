using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using OryctoMod.Common.Configs;
using Terraria;

namespace OryctoMod.Common.Lib;

public static class FrameLoop
{
    const int MaxNPCs = 200;
    const int MaxProjectiles = 1000;
    const int TicksPerApply = 60;
    const int MaxTouchesChecked = 6;
    const long DefaultTickTicks = 166666;
    const float MaxNudge = 64f;

    enum EntityKind : byte { Player, NPC, Projectile }

    struct MovedEntity
    {
        public EntityKind Kind;
        public int Index;
        public Vector2 Position;
    }

    static readonly MovedEntity[] Moved = new MovedEntity[1 + MaxNPCs + MaxProjectiles];
    static int movedCount;

    static int lastDrawTicks = -1;
    static int lastUpdateCount = -1;
    static long lastAt;
    static double interval = 16.7;
    static int updates;
    static int itemAnimationSkips;

    static bool originalsRead;
    static long originalTick, originalClamp, originalIgnore;
    static double runAccumulator;
    static long lastGateAt;

    static bool inputBound;
    static int lastTouchCount;

    // True on a draw with no new world tick since the last one.
    public static bool Between { get; private set; }

    // World ticks that actually ran.
    public static int WorldTicks { get; private set; }

    // The frame rate the game loop runs at, or 0 for the game's own.
    public static int Current { get; private set; }

    static float Clamp(double value, double low, double high) => (float)Math.Min(high, Math.Max(low, value));

    static int FpsOf(BoostConfig config) =>
        int.TryParse(config.Fps, out int fps) && fps >= 60 && fps <= 240 ? fps : 120;

    public static void Install()
    {
        InstallSmoothing();
        InstallItemAnimationGate();
        InstallUpdateGate();
    }

    public static void WorldLoaded()
    {
        updates = 0;
        runAccumulator = 0;
        ApplyAll();
    }

    public static void WorldUnloaded()
    {
        SetLoop(0);
        SetRate(60);
    }

    static void SetRate(int fps)
    {
        try
        {
            Engine.SetTargetFrameRate(fps);
        }
        catch (Exception e)
        {
            BoostLog.WarnOnce("set rate", e);
        }
    }

    static void InstallSmoothing()
    {
        On_Main.DoDraw += (orig, self, gameTime) =>
        {
            BoostConfig config = BoostConfig.Instance;
            Between = Current > 60 && WorldTicks == lastDrawTicks;
            lastDrawTicks = WorldTicks;
            bool undo = config.UnlockFps && config.Smooth && !Main.gameMenu && Shift(config);
            try
            {
                orig(self, gameTime);
            }
            finally
            {
                if (undo)
                    Restore();
            }
        };
    }

    // Ground items (coins, fallen stars, souls...) step their animation inside the draw code, once per
    // DRAWN frame (Main.DrawItem_AnimateSlot). With the frame rate unlocked there are more draws than world
    // ticks, so they ran too fast. On an in-between draw (no new world tick since the last draw) skip it, so
    // they advance once per world tick, like in the inventory.
    static void InstallItemAnimationGate()
    {
        On_Main.DrawItem_AnimateSlot += (orig, self, slot, gameFramesPerSpriteFrame, spriteFramesAmount) =>
        {
            if (Between)
            {
                if (++itemAnimationSkips == 1)
                    BoostLog.Info("ground item animation follows world ticks (first in-between call skipped)");
                return;
            }
            orig(self, slot, gameFramesPerSpriteFrame, spriteFramesAmount);
        };
    }

    static bool Nudged(Vector2 position, Vector2 old, float fraction, out Vector2 moved)
    {
        moved = position;
        float dx = (position.X - old.X) * fraction, dy = (position.Y - old.Y) * fraction;
        if (dx == 0f && dy == 0f)
            return false;
        if (Math.Abs(dx) > MaxNudge || Math.Abs(dy) > MaxNudge)
            return false;
        moved = new Vector2(position.X + dx, position.Y + dy);
        return true;
    }

    static void Remember(EntityKind kind, int index, Vector2 position) =>
        Moved[movedCount++] = new MovedEntity { Kind = kind, Index = index, Position = position };

    // Moves the entities forward by how far this draw is into the next world tick.
    // Returns whether Restore has to run after the draw.
    static bool Shift(BoostConfig config)
    {
        long now = Environment.TickCount64;
        int updateCount = WorldTicks;
        if (updateCount != lastUpdateCount)
        {
            if (lastUpdateCount >= 0 && lastAt > 0)
                interval = Clamp(interval * 0.9 + (now - lastAt) * 0.1, 8, 50);
            lastUpdateCount = updateCount;
            lastAt = now;
            return false;
        }
        float fraction = Clamp((now - lastAt) / interval, 0, 1);
        if (fraction < 0.12f)
            return false;

        movedCount = 0;

        Player me = Main.player[Main.myPlayer];
        if (me.active && Nudged(me.position, me.oldPosition, fraction, out Vector2 moved))
        {
            Remember(EntityKind.Player, Main.myPlayer, me.position);
            me.position = moved;
        }

        if (config.Enemies)
        {
            var npcs = Main.npc;
            for (int i = 0; i < MaxNPCs; i++)
            {
                NPC npc = npcs[i];
                if (npc.active && Nudged(npc.position, npc.oldPosition, fraction, out moved))
                {
                    Remember(EntityKind.NPC, i, npc.position);
                    npc.position = moved;
                }
            }
        }

        if (config.Projectiles)
        {
            var projectiles = Main.projectile;
            for (int i = 0; i < MaxProjectiles; i++)
            {
                Projectile projectile = projectiles[i];
                if (projectile.active && Nudged(projectile.position, projectile.oldPosition, fraction, out moved))
                {
                    Remember(EntityKind.Projectile, i, projectile.position);
                    projectile.position = moved;
                }
            }
        }

        return true;
    }

    static void Restore()
    {
        for (int i = 0; i < movedCount; i++)
        {
            ref MovedEntity entity = ref Moved[i];
            switch (entity.Kind)
            {
                case EntityKind.Player:
                    Main.player[entity.Index].position = entity.Position;
                    break;
                case EntityKind.NPC:
                    Main.npc[entity.Index].position = entity.Position;
                    break;
                default:
                    Main.projectile[entity.Index].position = entity.Position;
                    break;
            }
        }
    }

    static void SetLoop(int fps)
    {
        Current = fps;
        try
        {
            if (!originalsRead)
            {
                originalTick = Engine.FrameTickTicks;
                originalClamp = Engine.FrameClampTicks;
                originalIgnore = Engine.FrameIgnoreTicks;
                originalsRead = true;
            }
            if (fps == 0)
            {
                Engine.FrameTickTicks = originalTick;
                Engine.FrameClampTicks = originalClamp;
                Engine.FrameIgnoreTicks = originalIgnore;
            }
            else
            {
                double baseTicks = originalTick != 0 ? originalTick : DefaultTickTicks;
                double tick = 10000000.0 / fps;
                double clamp = originalClamp / baseTicks * tick;
                Engine.FrameTickTicks = (long)Math.Floor(tick + 0.5);
                Engine.FrameClampTicks = (long)Math.Floor(clamp + 0.5);
            }
        }
        catch (Exception e)
        {
            // Without the faster loop, the update gate below would slow the world down.
            Current = 0;
            BoostLog.WarnOnce("set loop", e);
        }
    }

    // Gates logic and UI to 60 Hz.
    static void InstallUpdateGate()
    {
        On_Main.Update += (orig, self, gameTime) =>
        {
            if (++updates % TicksPerApply == 0)
                ApplyAll();

            // A finger going down or up is only reported by the engine for ONE frame (the touch "began" / "ended" phase).
            // If that frame's update is the one skipped, the tap is lost: a button such as Target Lock then worked only
            // half of the time. Every frame in which a touch starts or ends is therefore run in full.
            bool edge = TouchEdge();
            if (Current > 60)
            {
                // Measured in real time, not as 60 / Current: a screen held at 60 Hz (Samsung does this to
                // games on its list) never reaches the requested rate, and counting frames would run the
                // world at half speed there.
                long now = Stopwatch.GetTimestamp();
                if (lastGateAt != 0)
                    runAccumulator += (now - lastGateAt) * 60.0 / Stopwatch.Frequency;
                lastGateAt = now;
                if (runAccumulator < 0.999)
                {
                    if (!edge)
                        return;
                }
                else
                {
                    // A slow frame runs one tick, as in the base game, without a burst of catch-up ticks later.
                    runAccumulator = Math.Min(runAccumulator - 1, 1);
                }
            }
            else
            {
                lastGateAt = 0;
            }
            WorldTicks++;
            orig(self, gameTime);
        };
        BoostLog.Info("Update gate ready");
    }

    // Did a touch (or the mouse, on an emulator) begin or end in this frame?
    static bool TouchEdge()
    {
        if (!inputBound)
        {
            inputBound = true;
            Engine.BindInput();
        }

        bool edge = false;
        try
        {
            if (Engine.TouchCountReady)
            {
                int count = Engine.TouchCount();
                if (count != lastTouchCount)
                    edge = true;
                lastTouchCount = count;
                if (Engine.TouchReady)
                {
                    for (int i = 0; i < count && i < MaxTouchesChecked; i++)
                    {
                        int phase = Engine.TouchPhase(i);
                        if (phase == Engine.PhaseBegan || phase == Engine.PhaseEnded || phase == Engine.PhaseCanceled)
                            edge = true;
                    }
                }
            }
            if (Engine.MouseReady && Engine.MouseButtonChanged(0))
                edge = true;
        }
        catch (Exception e)
        {
            BoostLog.WarnOnce("touch edge", e);
        }
        return edge;
    }

    static void ApplyAll()
    {
        BoostConfig config = BoostConfig.Instance;
        bool battery = !Main.Setting_Target60FPS;

        bool inMenuAllowed = config.UnlockMenu || !Main.gameMenu;
        bool on = config.UnlockFps && inMenuAllowed && !battery;

        if (on)
        {
            int fps = FpsOf(config);
            SetRate(fps);
            SetLoop(fps > 60 ? fps : 0);
        }
        else
        {
            SetLoop(0);
            if (!battery)
                SetRate(60);
        }
    }
}
