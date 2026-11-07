using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BunnyLoader;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using OryctoMod.Common.Configs;
using Terraria;

namespace OryctoMod.Common.Lib;

// Two options of the JavaScript version never worked, because their hooks target methods
// the game does not have, and the error was caught and only logged:
//   ThrottleFarEntities and ThrottleCritters hooked Main.UpdateNPC(int) (the game has NPC.UpdateNPC).
//   ThrottleWiring hooked Wiring.Update() (the game has Wiring.UpdateMech).
// They are kept as options that do nothing, because turning them on for real would change
// the game with defaults nobody has played with.
public static unsafe class Savings
{
    const int MaxNPCs = 200;
    const int MaxProjectiles = 1000;
    const int MaxDustToDraw = 6000;
    const float CullMargin = 150f;
    const int MinimapPeriod = 12;
    const int LiquidPeriod = 2;

    static readonly List<int> CulledProjectiles = new();
    static readonly List<int> CulledNPCs = new();

    static IntPtr* minimapOriginal;
    static IntPtr* liquidOriginal;

    public static void InstallLightingSkip()
    {
        On_Lighting.LightTiles += (orig, firstX, lastX, firstY, lastY) =>
        {
            if (FrameLoop.Between && BoostConfig.Instance.SkipLighting)
                return;
            orig(firstX, lastX, firstY, lastY);
        };
    }

    public static void ApplyDustCap()
    {
        int want = Math.Min(BoostConfig.Instance.DustCap, MaxDustToDraw);
        if (Main.maxDustToDraw != want)
            Main.maxDustToDraw = want;
    }

    static bool OutsideScreen(Vector2 position, int width, int height, Vector2 screen)
    {
        return position.X + width < screen.X - CullMargin || position.X > screen.X + Main.screenWidth + CullMargin
            || position.Y + height < screen.Y - CullMargin || position.Y > screen.Y + Main.screenHeight + CullMargin;
    }

    // Entities off the screen are switched off for the length of the draw.
    public static void InstallCulling()
    {
        On_Main.DrawProjectiles += (orig, self) =>
        {
            if (!BoostConfig.Instance.CullEntities)
            {
                orig(self);
                return;
            }
            Vector2 screen = Main.screenPosition;
            var projectiles = Main.projectile;
            CulledProjectiles.Clear();
            for (int i = 0; i < MaxProjectiles; i++)
            {
                Projectile projectile = projectiles[i];
                if (projectile.active && OutsideScreen(projectile.position, projectile.width, projectile.height, screen))
                {
                    projectile.active = false;
                    CulledProjectiles.Add(i);
                }
            }
            try
            {
                orig(self);
            }
            finally
            {
                foreach (int i in CulledProjectiles)
                    projectiles[i].active = true;
            }
        };

        On_Main.DrawNPCs += (orig, self, behindTiles) =>
        {
            if (!BoostConfig.Instance.CullEntities)
            {
                orig(self, behindTiles);
                return;
            }
            Vector2 screen = Main.screenPosition;
            var npcs = Main.npc;
            CulledNPCs.Clear();
            for (int i = 0; i < MaxNPCs; i++)
            {
                NPC npc = npcs[i];
                if (npc.active && !npc.boss && OutsideScreen(npc.position, npc.width, npc.height, screen))
                {
                    npc.active = false;
                    CulledNPCs.Add(i);
                }
            }
            try
            {
                orig(self, behindTiles);
            }
            finally
            {
                foreach (int i in CulledNPCs)
                    npcs[i].active = true;
            }
        };
    }

    public static void InstallVisualSkips()
    {
        On_Main.DrawRain += (orig, self) =>
        {
            if (!BoostConfig.Instance.NoRain)
                orig(self);
        };

        On_Main.DrawDust += (orig, self) =>
        {
            if (!BoostConfig.Instance.NoParticles)
                orig(self);
        };
    }

    static void CallOriginal(IntPtr* original, IntPtr self, IntPtr method)
    {
        CallFrame frame = default;
        frame.X[0] = (long)self;
        frame.X[1] = (long)method;
        frame.Ret = RetKind.Int;
        GameInterop.Call(*original, &frame);
    }

    // WorldMap and Liquid have no On_ hooks in the C# API, so these two use raw hooks with
    // the signatures the JavaScript version used. A method the game does not have is logged
    // and the option does nothing, as it did there.
    public static void InstallMinimapThrottling()
    {
        try
        {
            minimapOriginal = Hook.Install(new GameClass("Terraria.Map", "WorldMap"), "void Update()",
                (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, void>)&MinimapUpdate).OriginalSlot;
        }
        catch (Exception e)
        {
            BoostLog.WarnOnce("minimap throttle", e);
        }
    }

    [UnmanagedCallersOnly]
    static void MinimapUpdate(IntPtr self, IntPtr method)
    {
        try
        {
            if (BoostConfig.Instance.ThrottleMinimap && Main.GameUpdateCount % MinimapPeriod != 0)
                return;
            CallOriginal(minimapOriginal, self, method);
        }
        catch (Exception e)
        {
            BoostLog.WarnOnce("minimap throttle", e);
        }
    }

    public static void InstallLiquidThrottling()
    {
        try
        {
            liquidOriginal = Hook.Install(new GameClass("Terraria", "Liquid"), "void Update()",
                (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, void>)&LiquidUpdate).OriginalSlot;
        }
        catch (Exception e)
        {
            BoostLog.WarnOnce("liquid throttle", e);
        }
    }

    [UnmanagedCallersOnly]
    static void LiquidUpdate(IntPtr self, IntPtr method)
    {
        try
        {
            if (BoostConfig.Instance.ThrottleLiquid && Main.GameUpdateCount % LiquidPeriod != 0)
                return;
            CallOriginal(liquidOriginal, self, method);
        }
        catch (Exception e)
        {
            BoostLog.WarnOnce("liquid throttle", e);
        }
    }
}
