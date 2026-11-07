using System.Collections.Generic;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using WorldGenPreviewerMod.Common.Gen;

namespace WorldGenPreviewerMod.Common.Systems;

// A pass that runs first and does nothing, only to get the generation's progress object.
public class SetupPass : GenPass
{
    public SetupPass() : base(State.SETUP_PASS, 0) { }

    protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration) => State.progress = progress;
}

public class PreviewerSystem : ModSystem
{
    public override void ModifyWorldGenTasks(List<GenPass> tasks) => tasks.Insert(0, new SetupPass());
}
