using System;
using Terraria;
using Terraria.IO;

namespace ModDifficultyMod.DifficultySystem;

public static class DifficultyHooks
{
    // The world saves on a thread of its own while the game goes on: only that thread may see
    // the real id, because any other caller of Main.GameMode must keep seeing the vanilla one.
    [ThreadStatic]
    static bool isSaving;

    public static void Initialize()
    {
        On_Main.get_Difficulty += orig =>
        {
            WorldFileData data = Main.ActiveWorldFileData;
            if (data.IsNull)
                return orig();
            int gameMode = data.GameMode;
            if (gameMode >= DifficultyID.Count)
            {
                ModDifficulty? diff = ModDifficulty.GetModDifficulty(gameMode);
                if (diff != null)
                    return diff.GetDifficultyLevel(orig());
            }
            return orig();
        };

        On_Main.get_GameMode += orig =>
        {
            WorldFileData data = Main.ActiveWorldFileData;
            if (data.IsNull)
                return 0;
            int gameMode = data.GameMode;
            if (gameMode < DifficultyID.Count)
                return gameMode;
            ModDifficulty? diff = ModDifficulty.GetModDifficulty(gameMode);
            if (diff == null)
                return 0;
            if (isSaving)
                return diff.Type;
            int vanilla = diff.VanillaEquivalent;
            return vanilla > 0 && vanilla < DifficultyID.Count ? vanilla : 0;
        };

        On_WorldFile.SaveWorldHeader += (orig, writer) =>
        {
            isSaving = true;
            try
            {
                return orig(writer);
            }
            finally
            {
                isSaving = false;
            }
        };
    }
}
