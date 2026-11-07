// SPDX-License-Identifier: GPL-3.0-only
// Modified on 2026-11-06: ported from JavaScript to C#.
// Kasen's forms, smoke timing and action frames follow the original Kasen.cs.
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using WildAndHornedMod.Common.Configs;
using static WildAndHornedMod.Common.Pets.Runtime;

namespace WildAndHornedMod.Common.Pets;

public abstract class KasenPet : PetBase
{
    static readonly (int Frame, int Ticks)[] LECTURE = { (3, 24), (4, 14), (3, 24), (4, 14), (3, 40) };
    static readonly (int Frame, int Ticks)[] SNACK = { (5, 50), (6, 18), (7, 10), (6, 10), (7, 10), (5, 40) };

    bool initialized;
    int smokeTimer, spontaneousTicksLeft;

    string TargetForm
    {
        get => Projectile.ai[0] >= 1 ? "Oni" : "Hermit";
        set
        {
            if (!IsOwner || TargetForm == value)
                return;
            Projectile.ai[0] = value == "Oni" ? 1 : 0;
            Projectile.netUpdate = true;
        }
    }

    public override bool IsTransforming => smokeTimer > 0;
    public override string CurrentAction => IsTransforming ? "Transform" : PetState == 3 ? "Lecture" : PetState == 4 ? "Snack" : "";

    int BodyFrame(int frame) => Logic.KasenBodyFrame(frame, DisplayedForm == "Oni", step);

    protected override List<(int Line, float Weight)> RegularLines()
    {
        if (DisplayedForm != "Oni")
            return base.RegularLines();
        var lines = new List<(int Line, float Weight)> { (14, 1), (15, 1), (16, 1), (18, 1) };
        if (Main.bloodMoon)
            lines.Add((17, 2));
        return lines;
    }

    void UpdateForm()
    {
        KasenConfig config = KasenConfig.Instance;
        bool bloodMoon = config.BloodMoonOniForm && Main.bloodMoon;
        if (IsOwner)
        {
            if (!config.SpontaneousShifts || bloodMoon)
                spontaneousTicksLeft = 0;
            else if (spontaneousTicksLeft > 0)
                spontaneousTicksLeft--;
            else if (initialized && PetState == 0 && !IsTransforming && Chance(config.MinutesBetweenShifts * 3600))
                spontaneousTicksLeft = config.ShiftDurationSeconds * 60;
            string wanted = bloodMoon || spontaneousTicksLeft > 0 ? "Oni" : "Hermit";
            if (!initialized)
            {
                TargetForm = wanted;
            }
            else if (wanted != TargetForm && !IsTransforming)
            {
                TargetForm = wanted;
                PetState = 0;
                Speak(wanted == "Oni" ? (bloodMoon ? 21 : 19) : 20, 10);
            }
        }
        if (!initialized)
        {
            initialized = true;
            DisplayedForm = TargetForm;
            return;
        }
        if (smokeTimer == 0 && DisplayedForm != TargetForm)
            smokeTimer = 36;
        if (smokeTimer > 0)
        {
            if (36 - smokeTimer == 16)
            {
                DisplayedForm = TargetForm;
                Projectile.frame = BodyFrame(0);
                Effects.Sound(this, SoundID.Item8, -0.4f, 0.5f);
                Effects.SmokeBurst(this);
            }
            smokeTimer--;
        }
    }

    public override void AI()
    {
        Move();
        UpdateForm();
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
            Projectile.frame = BodyFrame(0);
            if (++timer >= (PetState == 1 ? 14 : 80))
                PetState = 0;
        }
        else
        {
            (int frame, bool done) = Logic.AnimationFrame(PetState == 3 ? LECTURE : SNACK, timer++);
            Projectile.frame = BodyFrame(frame);
            if (done && IsOwner)
            {
                cooldown = 720;
                PetState = 0;
            }
        }
        if (PetState <= 2 && cooldown > 0)
            cooldown--;
    }

    void Idle()
    {
        Projectile.frame = BodyFrame(0);
        if (!IsOwner || IsTransforming)
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
            if (DisplayedForm == "Oni" || Chance(2))
            {
                PetState = 3;
                Speak(DisplayedForm == "Oni" ? (Chance(2) ? 15 : 18) : (Chance(2) ? 1 : 7), 20);
            }
            else
            {
                PetState = 4;
                Speak(Chance(2) ? 4 : 10, 60);
            }
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
            Projectile.frame = BodyFrame(0);
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        DrawCell(this, Projectile.frame, 0, lightColor);
        bool oni = DisplayedForm == "Oni";
        if (PetState == 1 && timer < 14)
            DrawCell(this, (timer >= 4 && timer < 10 ? 1 : 0) + (oni ? 2 : 0), 1, lightColor);
        else if (PetState == 2 && timer < 80)
            DrawCell(this, 4 + (oni ? 1 : 0), 1, lightColor);
        if (IsTransforming)
        {
            int elapsed = 36 - smokeTimer;
            DrawCell(this, elapsed < 8 ? 6 : elapsed < 26 ? 7 : 8, 1, lightColor);
        }
        return false;
    }
}
