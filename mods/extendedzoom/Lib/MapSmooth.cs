using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ExtendedZoomMod.Lib;

// Smooth zoom and drag on the fullscreen map.
// The game moves the map's scale and position (in the update and, for dragging, during the draw
// itself). This keeps a TARGET and draws the map with a smoothed value that walks toward it.
// Position writes made DURING the draw become a shift of the target (they do not reach the screen).
public static class MapSmooth
{
    const double Speed = 12;      // same as the player zoom (~90% in 0.2 s)
    const double Snap = 0.002;    // log difference below which the scale snaps to the target
    const double SnapPos = 0.05;

    static double S, PX, PY;
    static bool valid, broken;
    static long last;
    static bool inDraw, swallow, bypass;
    static double accX, accY;

    static void SetPos(double x, double y)
    {
        bypass = true;
        try
        {
            Main.mapFullscreenPos = new Vector2((float)x, (float)y);
        }
        finally
        {
            bypass = false;
        }
    }

    public static void Install()
    {
        On_Main.set_mapFullscreenPos += (orig, value) =>
        {
            if (bypass || !inDraw || !swallow)
            {
                orig(value);
                return;
            }
            // Swallowed: the screen position does not change now. The target moves and the screen follows it.
            Vector2 current = Main.mapFullscreenPos;
            accX += value.X - current.X;
            accY += value.Y - current.Y;
        };

        On_Main.DoDraw += (orig, self, gameTime) =>
        {
            if (broken || !ZoomState.SmoothMap || !Main.mapFullscreen)
            {
                valid = false;
                orig(self, gameTime);
                return;
            }

            double tS, tx, ty, eS, ex, ey;
            bool wasValid = valid;
            try
            {
                tS = Main.mapFullscreenScale;
                Vector2 target = Main.mapFullscreenPos;
                tx = target.X;
                ty = target.Y;
                long now = Environment.TickCount64;
                double dt = Math.Min(0.1, Math.Max(0, (now - last) / 1000.0));
                last = now;
                if (!valid || !(tS > 0) || !double.IsFinite(tS) || !double.IsFinite(tx) || !double.IsFinite(ty))
                {
                    S = tS;
                    PX = tx;
                    PY = ty;
                    valid = tS > 0 && double.IsFinite(tS) && double.IsFinite(tx) && double.IsFinite(ty);
                    wasValid = false;
                }
                else
                {
                    double diff = Math.Log(tS / S);
                    double k = 1 - Math.Exp(-dt * Speed);
                    S = Math.Abs(diff) < Snap ? tS : S * Math.Exp(diff * k);
                    if (Math.Abs(tx - PX) + Math.Abs(ty - PY) < SnapPos)
                    {
                        PX = tx;
                        PY = ty;
                    }
                    else
                    {
                        PX += (tx - PX) * k;
                        PY += (ty - PY) * k;
                    }
                }
                eS = S;
                ex = PX;
                ey = PY;
                Main.mapFullscreenScale = (float)eS;
                SetPos(ex, ey);
            }
            catch (Exception e)
            {
                broken = true;
                ExtendedZoom.Instance.Logger.Error("Extended Zoom Out: smooth map zoom turned off after an error: " + e.Message);
                orig(self, gameTime);
                return;
            }

            accX = 0;
            accY = 0;
            swallow = wasValid;
            inDraw = true;
            try
            {
                orig(self, gameTime);
            }
            finally
            {
                inDraw = false;
                try
                {
                    double aS = Main.mapFullscreenScale;
                    Vector2 after = Main.mapFullscreenPos;
                    double nS = tS * (aS / eS);
                    double nx, ny;
                    if (swallow)
                    {
                        // Dragging and limits move the target.
                        nx = tx + accX;
                        ny = ty + accY;
                    }
                    else
                    {
                        nx = tx + (after.X - ex);
                        ny = ty + (after.Y - ey);
                        PX = after.X;
                        PY = after.Y;
                    }
                    swallow = false;
                    Main.mapFullscreenScale = (float)nS;
                    SetPos(nx, ny);
                    S = aS;
                }
                catch (Exception e)
                {
                    broken = true;
                    ExtendedZoom.Instance.Logger.Error("Extended Zoom Out: smooth map zoom (restoring): " + e.Message);
                }
            }
        };
        ExtendedZoom.Instance.Logger.Info("Extended Zoom Out: smooth map zoom installed");
    }
}
