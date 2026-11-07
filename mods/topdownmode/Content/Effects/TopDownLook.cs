using System;
using BunnyLoader;
using Terraria;
using topdownmode.Common.Configs;
using topdownmode.Common.Systems;

namespace topdownmode.Content.Effects;

public class TopDownLook : ModScreenEffect
{
    static int errors, drawErrors, kills;
    static string lastErr = "";

    bool any, good;

    public static string Status() =>
        "look: errors=" + errors + " draw errors=" + drawErrors + " ended=" + kills + (lastErr != "" ? " last error: " + lastErr : "");

    public override void SetDefaults()
    {
        Fullscreen = true;
        Center = null;
        Key = "topdown:look";
        Order = 10;
    }

    public override void OnKill()
    {
        kills++;
        if (kills <= 5)
            TopDownMode.Log("look: effect ended (" + kills + ")" + (lastErr != "" ? " last error: " + lastErr : ""));
    }

    public override void Update()
    {
        any = false;
        try
        {
            if (Main.gameMenu || !TopDownWorldSystem.IsTopDown)
            {
                good = false;
                return;
            }
            TopDownConfig c = TopDownConfig.Instance;
            float grade = c.Atmosphere ? c.AtmosphereStrength : 0f;
            if (!(grade > 0))
            {
                good = false;
                return;
            }
            Uniforms["uDay"] = (float)TopDownWorldSystem.Daylight();
            Uniforms["uGrade"] = grade;
            good = true;
            any = true;
        }
        catch (Exception e)
        {
            errors++;
            lastErr = e.ToString();
            if (errors <= 3)
                TopDownMode.Log("look (error): " + e);
            any = good;
        }
    }

    public override bool ShouldDraw() => any;

    public override void Draw(ScreenCommands cmd)
    {
        try
        {
            base.Draw(cmd);
        }
        catch (Exception e)
        {
            drawErrors++;
            lastErr = "draw: " + e;
            if (drawErrors <= 3)
                TopDownMode.Log("look (draw error): " + e);
        }
    }
}
