using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace FlatSeedMod.Common;

public static class FlatSeedRegistration
{
    public static readonly string[] SeedCodes =
    {
        "flat", "flatt", "flatworld", "flat world", "superflat", "super flat",
        "plano", "mundo plano", "superplano",
        "plat", "monde plat",
        "flach", "flache welt",
        "piatto", "mondo piatto",
        "płaski", "plaski", "płaski świat", "plaski swiat",
        "плоский", "плоский мир",
        "平坦", "平坦世界", "平面世界",
        "平ら", "平らな世界", "平坦な世界",
        "평평", "평평한 세계",
        "phẳng", "thế giới phẳng",
        "แบน", "โลกแบน",
    };

    static readonly HashSet<string> Codes = BuildCodes();

    static WorldGen.SecretSeed native;

    static HashSet<string> BuildCodes()
    {
        var codes = new HashSet<string>();
        foreach (string code in SeedCodes)
            codes.Add(Util.Normalize(code));
        return codes;
    }

    public static bool Matches(string? text)
    {
        string code = Util.Normalize(text);
        return code.Length > 0 && Codes.Contains(code);
    }

    public static bool IsFlatWorld() => !native.IsNull && native.Enabled;

    public static void RegisterFlatSeed()
    {
        native = WorldGen.SecretSeed.Register("Mods." + Util.ModId + ".SecretSeeds.Flat.Description", SoundID.PalFoxparksJoy, "bl:" + Util.ModId + ":Flat");

        On_WorldGen_SecretSeed.Check += (orig, self, code) =>
        {
            if (self.Pointer != native.Pointer)
                return orig(self, code);
            if (!Matches(code))
                return false;
            self._plaintext = code;
            return true;
        };

        On_WorldGen_SecretSeed.CheckInputForSecretSeed += (orig, worldSeed, out secretSeed) =>
        {
            if (!Matches(worldSeed))
                return orig(worldSeed, out secretSeed);
            native.TextThatWasUsedToUnlock = worldSeed;
            native._plaintext = worldSeed;
            secretSeed = native;
            return true;
        };
    }
}
