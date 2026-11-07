using System.Collections.Generic;
using FlatSeedMod.Common.Configs;
using FlatSeedMod.Common.Gen;
using Terraria;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace FlatSeedMod.Common.Systems;

public class FlattenWorldPass : GenPass
{
    readonly FlatSettings settings;

    public FlattenWorldPass(FlatSettings settings) : base("Flatten World", 100) => this.settings = settings;

    protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
    {
        progress.Message = Util.T("WorldGen.FlatteningWorld");
        Surface.FlattenSurface(progress, settings);
    }
}

public class FlattenDesertPass : GenPass
{
    readonly FlatSettings settings;

    public FlattenDesertPass(FlatSettings settings) : base("Flatten Desert", 20) => this.settings = settings;

    protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
    {
        progress.Message = Util.T("WorldGen.FlatteningWorld");
        Surface.FlattenDesert(progress, settings);
    }
}

public class FlattenCleanupPass : GenPass
{
    readonly FlatSettings settings;

    public FlattenCleanupPass(FlatSettings settings) : base("Flatten World Cleanup", 100) => this.settings = settings;

    protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
    {
        progress.Message = Util.T("WorldGen.FlatteningWorld");
        Surface.TrimSurface(progress, settings, Islands.All());
    }
}

public class FlatWorldSystem : ModSystem
{
    static readonly HashSet<string> Removed = new() { "Tunnels", "Mount Caves", "Mountain Caves", "Lakes" };

    static void Insert(List<GenPass> tasks, string after, GenPass pass)
    {
        int index = tasks.FindIndex(task => task.Name == after);
        int final = tasks.FindIndex(task => task.Name == "Final Cleanup");
        if (index >= 0)
            tasks.Insert(index + 1, pass);
        else if (final >= 0)
            tasks.Insert(final, pass);
        else
            tasks.Add(pass);
    }

    public override void ModifyWorldGenTasks(List<GenPass> tasks)
    {
        if (!FlatSeedRegistration.IsFlatWorld() || WorldGen.SecretSeed.noSurface.Enabled)
            return;
        FlatSeedConfig config = FlatSeedConfig.Instance;
        var settings = new FlatSettings(config.SurfaceHeight, config.FillChasms, config.SmoothBeaches);

        for (int i = tasks.Count - 1; i >= 0; i--)
        {
            if (Removed.Contains(tasks[i].Name))
                tasks.RemoveAt(i);
        }

        Insert(tasks, "Surface Caves", new FlattenWorldPass(settings));
        Insert(tasks, "Full Desert", new FlattenDesertPass(settings));
        Insert(tasks, "Dungeon", new FlattenCleanupPass(settings));
    }
}
