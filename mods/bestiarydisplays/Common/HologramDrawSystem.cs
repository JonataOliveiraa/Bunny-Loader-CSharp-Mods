using System;
using System.Collections.Generic;
using BestiaryDisplaysMod.Common.UI;
using BestiaryDisplaysMod.Content.TileEntities;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace BestiaryDisplaysMod.Common;

public class HologramDrawSystem : ModSystem
{
    static readonly List<HologramTileEntity> holograms = new();
    static readonly Action drawAll = DrawAll;

    static void DrawAll()
    {
        if (Main.gameMenu || Main.netMode == 2)
            return;
        HologramTileEntity.Collect(holograms);
        if (holograms.Count == 0)
            return;
        SpriteBatch sb = Main.spriteBatch;
        bool open = sb._beginCalled;
        if (!open)
        {
            GameNullable<Matrix> matrix = new(Main.GameViewMatrix.TransformationMatrix);
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, default, matrix, true);
        }
        try
        {
            foreach (HologramTileEntity entity in holograms)
            {
                try
                {
                    Hologram.DrawHologram(entity, sb);
                }
                catch (Exception e)
                {
                    Util.LogOnce("hologram:" + Util.Message(e), "hologram failed: " + Util.Message(e));
                }
            }
        }
        finally
        {
            if (!open)
                sb.End();
        }
    }

    public override void OnModLoad()
    {
        On_Main.DoDraw_WallsTilesNPCs += static (orig, self) =>
        {
            orig(self);
            Util.Safe("hologram drawing", drawAll);
        };
        HologramMenu.Install();
    }
}
