// Daiyousei: shy, scared of graveyards, and Cirno's best friend.
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using TouhouPetsMod.Common;
using TouhouPetsMod.Content.Buffs.PetBuffs;

namespace TouhouPetsMod.Content.Projectiles.Pets;

public class Daiyousei : BasicTouhouPet
{
    protected override Color InitialColor => new Color(71, 228, 63);

    int wingFrame;
    int wingFrameCounter;

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 7;
        Main.projPet[Type] = true;
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        PetDrawInfo d = DrawInfo();
        Color color = Projectile.GetAlpha(lightColor);
        DrawFrame(d, wingFrame, color);
        DrawFrame(d, Projectile.frame, color);
        return false;
    }

    void Blink()
    {
        if (++Projectile.frameCounter > 3)
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;
        }
        if (Projectile.frame > 2)
        {
            Projectile.frame = 0;
            PetState = 0;
        }
    }

    void UpdateWingFrame()
    {
        if (wingFrame < 3)
            wingFrame = 3;
        if (++wingFrameCounter > 4)
        {
            wingFrameCounter = 0;
            wingFrame++;
        }
        if (wingFrame > 6)
            wingFrame = 3;
    }

    protected override List<ChatLine> ChatLines(Player player)
    {
        List<ChatLine> lines = [new(1, "1")];
        if (Owns<Cirno>())
            lines.AddRange([new(2, "2", 5), new(3, "3", 5)]);
        if (player.ZoneGraveyard)
            lines.Add(new(5, "5", 5));
        return lines;
    }

    void UpdateTalking()
    {
        if (FindChatIndex<Cirno>(4, 0, 0) != null || FindChatIndex<Cirno>(7, 0, 0) != null)
            ChatCD = 1;

        BasicTouhouPet? p;
        if ((p = FindChatIndex<Cirno>(4)) != null)
        {
            SetChatWithOtherOne(p, Text("ToCirno1"), myColor, 0, 360);
            p.ChatIndex = 0;
        }
        else if ((p = FindChatIndex<Cirno>(7)) != null)
        {
            SetChatWithOtherOne(p, Text("ToCirno2"), myColor, 6, 600);
        }
        else if ((p = FindChatIndex<Cirno>(9, 0, 1, true)) != null)
        {
            SetChatWithOtherOne(p, Text("ToCirno3"), myColor, 9, 360);
            p.ChatIndex = 0;
        }
        if (mainTimer % 720 == 0 && Main.rand.NextBool(6) && mainTimer > 0)
            SetChat(myColor);
    }

    protected override void VisualEffectForPreview()
    {
        UpdateWingFrame();
    }

    public override void AI()
    {
        ModUtils.SetPetActive<DaiyouseiBuff>(Projectile, player);

        UpdateTalking();
        var withCirno = Owns<Cirno>();
        Projectile.tileCollide = false;
        Projectile.rotation = Projectile.velocity.X * 0.032f;

        ChangeDir(player, !withCirno);
        MoveToPoint((withCirno ? 80 : -40) * player.direction, -30 + player.gfxOffY, 9);

        if (mainTimer % 270 == 0 && PetState != 2)
            PetState = 1;
        if (PetState == 0)
        {
            Projectile.frame = 0;
            if (extraAI[0] >= 1)
                extraAI[0]--;
        }
        else if (PetState == 1)
        {
            Blink();
        }
    }
}
