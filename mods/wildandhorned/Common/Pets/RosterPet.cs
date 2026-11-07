// SPDX-License-Identifier: GPL-3.0-only
// Modified on 2026-11-06: ported from JavaScript to C#.
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using static WildAndHornedMod.Common.Pets.Runtime;

namespace WildAndHornedMod.Common.Pets;

public abstract class RosterPet : PetBase
{
    // The front layer's row for each idle step (4 + this in column 2).
    static readonly int[] FrontSway = { 0, 1, 2, 1 };

    PetAction? ActionAt(int state)
    {
        PetAction[] actions = Definition.Actions;
        return state >= 3 && state - 3 < actions.Length ? actions[state - 3] : null;
    }

    public override string CurrentAction => ActionAt(PetState)?.Name ?? "";

    bool Alerted => Definition.Name == "Aunn" ? enemyNear : treasureNear;

    float WeightOf(PetAction action) => Alerted && action.alertWeight.HasValue ? action.alertWeight.Value : action.Weight;

    int[] LinesOf(PetAction action) => Alerted && action.alertLines != null ? action.alertLines : action.Lines;

    public override void AI()
    {
        Move();
        Effects.UpdateEffects(this);
        if (PetState != lastState)
        {
            lastState = PetState;
            timer = 0;
        }
        if (PetState == 0)
        {
            Idle();
        }
        else if (PetState == 1 || PetState == 2)
        {
            if (++timer >= (PetState == 1 ? 14 : 80))
                PetState = 0;
        }
        else
        {
            PetAction? action = ActionAt(PetState);
            if (action == null)
            {
                PetState = 0;
            }
            else
            {
                (int frame, bool done) = Logic.AnimationFrame(action.Frames, timer++);
                Projectile.frame = frame;
                if (done && IsOwner)
                {
                    cooldown = 720;
                    PetState = 0;
                }
            }
        }
        if (PetState <= 2 && cooldown > 0)
            cooldown--;
    }

    void Idle()
    {
        if (!IsOwner)
            return;
        if (mainTimer % 270 == 0)
        {
            PetState = 1;
            return;
        }
        if (mainTimer % 400 == 150 && Chance(2))
        {
            PetState = 2;
            return;
        }
        if (mainTimer > 0 && mainTimer % 360 == 0 && replyKey.Length == 0 && chatLife == 0 && cooldown <= 0 && Chance(3))
        {
            PetAction[] actions = Definition.Actions;
            var weights = new float[actions.Length];
            for (int i = 0; i < weights.Length; i++)
                weights[i] = WeightOf(actions[i]);
            int index = Logic.WeightedIndex(weights, Main.rand.NextFloat());
            PetState = index + 3;
            PetAction action = actions[index];
            int[] lines = LinesOf(action);
            if (lines.Length > 0)
                Speak(lines[Main.rand.Next(lines.Length)], action.lag);
        }
    }

    public override void PostAI()
    {
        if (++stepCounter >= 14)
        {
            stepCounter = 0;
            step = (step + 1) % 4;
        }
        if (PetState <= 2)
            Projectile.frame = Definition.Columns == 3 ? 0 : Logic.IDLE_LOOP[step];
        Effects.AddPetLight(this);
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        bool layers = Definition.Columns == 3 && !Logic.HidesLayers(Definition.Name, Projectile.frame);
        if (layers)
            DrawCell(this, step, 2, lightColor);
        DrawCell(this, Projectile.frame, 0, lightColor);
        if (PetState == 1 && timer < 14)
            DrawCell(this, timer >= 4 && timer < 10 ? 1 : 0, 1, lightColor);
        else if (PetState == 2 && timer < 80)
            DrawCell(this, 2, 1, lightColor);
        if (layers)
            DrawCell(this, 4 + FrontSway[step], 2, lightColor);
        return false;
    }
}
