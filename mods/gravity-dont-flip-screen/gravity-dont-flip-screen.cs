using System;
using System.Runtime.InteropServices;
using BunnyLoader;
using BunnyLoader.Interop;
using Terraria;
using Terraria.Graphics.Renderers;
using Terraria.ModLoader;

public unsafe class GravityDontFlipScreen : Mod
{
    static GravityDontFlipScreen instance = null!;
    static float gravDir = 1;
    static float? gravDir2;
    static IntPtr* smartCursorOriginal;
    static bool reported;

    public override void Load()
    {
        instance = this;

        On_Player.ChestChangeEvents += (orig, self) =>
        {
            if (self.whoAmI != Main.myPlayer || self.gravDir != -1)
            {
                orig(self);
                return;
            }

            self.gravDir = 1;
            orig(self);
            gravDir2 = -1;
        };

        // As in the JavaScript version, the game's method does not run for the local player.
        On_Player.Update_AdjustTileTargetForDisplayJars += (orig, self, i) =>
        {
            if (i != Main.myPlayer)
            {
                orig(self, i);
                return;
            }

            if (gravDir2 != null)
            {
                self.gravDir = gravDir2.Value;
                gravDir2 = null;
            }
        };

        On_Player.SmartInteractLookup += (orig, self) =>
        {
            if (self.whoAmI != Main.myPlayer || self.gravDir != -1)
            {
                orig(self);
                return;
            }

            self.gravDir = 1;
            orig(self);
            self.gravDir = -1;
        };

        // SmartCursorHelper has no members or On_ hooks in the C# API.
        try
        {
            smartCursorOriginal = Hook.Install(new GameClass("Terraria.GameContent", "SmartCursorHelper"),
                "void SmartCursorLookup(Player player)",
                (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, void>)&SmartCursorLookup).OriginalSlot;
        }
        catch (Exception e)
        {
            Logger.Error("The SmartCursorLookup hook could not be installed: " + e.Message);
        }

        On_Player.ItemCheck += (orig, self) =>
        {
            if (self.whoAmI != Main.myPlayer || self.gravDir != -1)
            {
                orig(self);
                return;
            }

            int mouseY = Main.mouseY;
            Main.mouseY = Main.screenHeight - Main.mouseY;
            orig(self);
            Main.mouseY = mouseY;
        };

        On_Player.QuickGrapple += (orig, self) =>
        {
            if (self.whoAmI != Main.myPlayer || self.gravDir != -1)
            {
                orig(self);
                return;
            }

            self.gravDir = 1;
            orig(self);
            self.gravDir = -1;
        };

        On_LegacyPlayerRenderer.DrawPlayerFull += (orig, self, camera, player) =>
        {
            if (player.whoAmI != Main.myPlayer)
            {
                orig(self, camera, player);
                return;
            }

            player.gravDir = gravDir;
            orig(self, camera, player);
            player.gravDir = 1;
        };

        On_Main.DoDraw += (orig, self, gameTime) =>
        {
            if (Main.gameMenu)
            {
                orig(self, gameTime);
                return;
            }

            Player player = Main.LocalPlayer;
            gravDir = player.gravDir;
            player.gravDir = 1;
            orig(self, gameTime);
            player.gravDir = gravDir;
        };
    }

    static void SmartCursorOriginal(IntPtr player, IntPtr method)
    {
        CallFrame frame = default;
        frame.X[0] = (long)player;
        frame.X[1] = (long)method;
        frame.Ret = RetKind.Int;
        GameInterop.Call(*smartCursorOriginal, &frame);
    }

    // Exceptions must not leave this method: there is no managed caller to catch them.
    [UnmanagedCallersOnly]
    static void SmartCursorLookup(IntPtr pointer, IntPtr method)
    {
        try
        {
            var player = new Player(pointer);
            if (player.whoAmI != Main.myPlayer || player.gravDir != -1)
            {
                SmartCursorOriginal(pointer, method);
                return;
            }

            player.gravDir = 1;
            SmartCursorOriginal(pointer, method);
            player.gravDir = -1;
        }
        catch (Exception e)
        {
            if (!reported)
            {
                reported = true;
                instance.Logger.Error("SmartCursorLookup: " + e.Message);
            }
        }
    }
}
