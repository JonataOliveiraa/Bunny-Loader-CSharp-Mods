using System;
using ImprovedMovementMod.Common.Configs;
using Terraria;
using Terraria.ModLoader;

namespace ImprovedMovementMod.Common.GlobalNPCs;

public class LeaningNPC : GlobalNPC
{
    enum Kind { None, Slime, Zombie, Critter, Town, Other }

    float offset;
    float? applied;

    public override bool InstancePerEntity => true;

    static Kind KindOf(NPC npc, MovementConfig config)
    {
        if (Lists.SlimesWhitelist.Contains(npc.type))
            return config.EnableSlimeLeaning ? Kind.Slime : Kind.None;
        if (npc.aiStyle == 3)
            return config.EnableZombieLeaning ? Kind.Zombie : Kind.None;
        if (npc.aiStyle == 7 && !npc.townNPC)
            return config.EnableCritterLeaning ? Kind.Critter : Kind.None;
        if (npc.townNPC)
            return Kind.Town;
        return config.EnableAllNPCLeaning ? Kind.Other : Kind.None;
    }

    static float SpinStep(NPC npc) => (npc.velocity.X - npc.velocity.Y * -npc.direction) * 0.05f;

    void LeanOther(NPC npc, MovementConfig config)
    {
        float baseRotation = applied == npc.rotation ? applied.Value - offset : npc.rotation;
        if (config.NPCLeaningType == NPCLeaningMode.Spin)
            offset = (offset + SpinStep(npc)) % (MathF.PI * 2f);
        else
            offset = Math.Clamp(npc.velocity.X / 4f, -0.145f, 0.145f);
        npc.rotation = baseRotation + offset;
        applied = npc.rotation;
    }

    public override void PostAI(NPC npc)
    {
        MovementConfig config = MovementConfig.Instance;
        Kind kind = KindOf(npc, config);
        if (kind != Kind.Other && applied != null)
        {
            if (npc.rotation == applied)
                npc.rotation = applied.Value - offset;
            applied = null;
            offset = 0f;
        }
        if (kind == Kind.None)
            return;
        if (kind == Kind.Other)
        {
            LeanOther(npc, config);
            return;
        }
        if (kind == Kind.Town && !config.EnableTownNPCLeaning)
        {
            npc.rotation *= 0.75f;
            if (MathF.Abs(npc.rotation) < 0.001f)
                npc.rotation = 0f;
            return;
        }
        if (config.NPCLeaningType == NPCLeaningMode.Spin)
        {
            npc.rotation = (npc.rotation + SpinStep(npc)) % (MathF.PI * 2f);
            return;
        }
        switch (kind)
        {
            case Kind.Slime:
                npc.rotation = Math.Clamp(npc.velocity.Y * npc.direction / 4f, -0.3f, 0.3f);
                break;
            case Kind.Zombie:
                npc.rotation = Math.Clamp(npc.velocity.X / 4f, -0.145f, 0.145f);
                break;
            case Kind.Critter:
                npc.rotation = Math.Clamp(npc.velocity.X / 4f, -0.1f, 0.1f);
                break;
            default:
                float target = npc.velocity.Y == 0f ? Math.Clamp(npc.velocity.X * 0.1f, -0.12f, 0.12f) : 0f;
                npc.rotation += (target - npc.rotation) * 0.25f;
                if (MathF.Abs(npc.rotation) < 0.001f)
                    npc.rotation = 0f;
                break;
        }
    }
}
