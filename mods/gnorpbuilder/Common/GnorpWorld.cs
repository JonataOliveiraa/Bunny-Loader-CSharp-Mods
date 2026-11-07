using System;
using System.Collections.Generic;
using GnorpBuilderMod.Content.NPCs;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace GnorpBuilderMod.Common;

// Gnorp lives in every new world from the first day.
public class GnorpWorldSystem : ModSystem
{
    public override void ModifyWorldGenTasks(List<GenPass> tasks)
    {
        int index = tasks.FindIndex(task => task.Name == "Spawn Point");
        if (index == -1) return;

        tasks.Insert(index + 1, new PassLegacy("Gnorp Spawn Pass", (progress, configuration) =>
        {
            try
            {
                progress.Message = Util.T("WorldGen.SpawningGnorp");
            }
            catch (Exception e)
            {
                Mod.Logger.Warn("the generation message failed: " + e.Message);
            }

            try
            {
                NPC.NewNPC(Projectile.GetNoneSource(), Main.spawnTileX * 16, Main.spawnTileY * 16, ModContent.NPCType<Gnorp>(), 0, 0f, 0f, 0f, 0f, 255);
            }
            catch (Exception e)
            {
                Mod.Logger.Error("Gnorp could not spawn: " + e.Message);
            }
        }));
    }
}
