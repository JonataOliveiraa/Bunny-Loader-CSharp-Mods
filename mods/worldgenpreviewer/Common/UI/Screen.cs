using System;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using WorldGenPreviewerMod.Common.Configs;
using WorldGenPreviewerMod.Common.Gen;
using WorldGenPreviewerMod.Common.Map;

namespace WorldGenPreviewerMod.Common.UI;

public static class Screen
{
    const double SAMPLE_MS = 6;
    const double CANCEL_WAIT_MS = 1500;
    const int MAX_ERRORS = 3;

    static MenuInterface? menu;
    static PreviewScreen? screen;
    static int seenSession;
    static bool wasActive;
    static double endedAt;
    static int errors;
    static bool ticked;

    static WorldGenPreviewerConfig Cfg => WorldGenPreviewerConfig.Instance;

    static void WithBatch(MenuInterface interfaces)
    {
        SpriteBatch sb = Main.spriteBatch;
        bool open = sb._beginCalled;
        if (open)
            sb.End();
        sb.Begin(SpriteSortMode.Deferred, default, SamplerState.PointClamp, default, default, default, default, true);
        try
        {
            interfaces.Frame(sb);
        }
        finally
        {
            sb.End();
            if (open)
                sb.Begin(SpriteSortMode.Deferred, default, default, default, default, default, default, true);
        }
    }

    static void Tick(double now)
    {
        if (State.session != seenSession)
        {
            seenSession = State.session;
            Preview.Stop();
            View.Reset();
            ScreenLayout.scrolledAt = -1e9;
            menu?.Reset();
            errors = 0;
            Util.ClearTexts();
        }
        if (State.active)
        {
            wasActive = true;
        }
        else if (wasActive)
        {
            wasActive = false;
            endedAt = now;
            Preview.Stop();
        }
        if (State.cleanupPending && !State.active && now - endedAt >= CANCEL_WAIT_MS)
        {
            State.cleanupPending = false;
            Control.DeleteCancelledWorld();
            if (Main.menuMode == 10)
                Main.menuMode = 6;
            Main.LoadWorlds();
        }
    }

    static void Show(string where)
    {
        if (errors >= MAX_ERRORS)
            return;
        try
        {
            if (Cfg.LivePreview)
                Preview.Work(State.finishing ? SAMPLE_MS * 2 : SAMPLE_MS);
            menu ??= new MenuInterface();
            screen ??= new PreviewScreen();
            if (menu.CurrentState != screen)
                menu.SetState(screen);
            WithBatch(menu);
            errors = 0;
            if (Pointer.seen)
                State.tapSeen = true;
            Volatile.Write(ref State.lastDraw, Util.NowMs);
            State.drawnSession = State.session;
        }
        catch (Exception e)
        {
            errors++;
            Util.Log("error drawing the preview (" + where + "): " + e);
            if (errors >= MAX_ERRORS)
            {
                Util.Log("preview screen turned off; the generation goes on as usual");
                Preview.Stop();
                Commands.Play();
            }
        }
    }

    // True when the game's own generation screen should draw.
    public static bool BeginGenerateMenu()
    {
        Tick(Util.NowMs);
        ticked = true;
        if (State.active && Main.gameMenu && errors < MAX_ERRORS)
        {
            Show("GUIWorldGenerateMenu");
            return false;
        }
        return true;
    }

    public static void EndGenerateMenu() => ticked = false;

    public static void AfterStatusMenu()
    {
        if (State.active && !ticked)
        {
            Tick(Util.NowMs);
            Show("GUIStatusMenu");
        }
    }

    public static void InstallScreen()
    {
        GameBindings.InstallDrawHooks();
        On_GUILogo.Draw += (orig, self) =>
        {
            if (!State.active)
                orig(self);
        };
    }
}
