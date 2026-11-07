// Hina keeps your luck from going below zero; Tenshi adds half a point.
using Terraria.ModLoader;
using TouhouPetsMod.Content.Buffs.PetBuffs;

namespace TouhouPetsMod.Common;

public class LuckPlayer : ModPlayer
{
    public override void ModifyLuck(ref float luck)
    {
        if (ModUtils.HasBuff<HinaBuff>(Player) && luck < 0)
            luck = 0;
        if (ModUtils.HasBuff<TenshiBuff>(Player))
            luck += 0.5f;
    }
}
