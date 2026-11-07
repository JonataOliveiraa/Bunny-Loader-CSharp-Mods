using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace DevToolMod.Core.Systems;

public enum CameraMode
{
    Default,
    Free,
    Static,
    Count,
}

public static class CameraSystem
{
    public static CameraMode CameraMode = CameraMode.Default;

    public static bool PhotoMode;

    public const float MinZoom = 0.1f;
    public const float MaxZoom = 8.0f;
    public static float ZoomMultiplier = 1.0f;
    static Vector2 CameraPosition;
    static Vector2 CameraVelocity;

    public static void SetCamera(CameraMode mode)
    {
        if (mode != CameraMode.Default)
        {
            CameraPosition = Main.screenPosition;
            CameraVelocity = Vector2.Zero;
        }
        CameraMode = mode;
    }

    static Vector2 GetRadialInput(Player player)
    {
        Vector2 v = Vector2.Zero;
        int x = 0, y = 0;

        if (player.controlLeft) x--;
        if (player.controlRight) x++;
        if (player.controlUp || player.controlJump) y--;
        if (player.controlDown) y++;

        float len = MathF.Sqrt(x * x + y * y);
        if (len != 0)
        {
            v.X = x / len;
            v.Y = y / len;
        }

        return v;
    }

    static void UpdateCameraVelocity(Player player)
    {
        Vector2 dir = GetRadialInput(player);
        float speed = 1.25f;
        if (ZoomMultiplier > 1) speed += ZoomMultiplier * 0.25f;
        else speed -= 1 - ZoomMultiplier;
        CameraVelocity.X += dir.X * speed;
        CameraVelocity.Y += dir.Y * speed;
    }

    public static void UpdateCamera(Player player)
    {
        switch (CameraMode)
        {
            case CameraMode.Static:
                Main.screenPosition = CameraPosition;
                break;
            case CameraMode.Free:
                UpdateCameraVelocity(player);
                CameraPosition.X += CameraVelocity.X;
                CameraPosition.Y += CameraVelocity.Y;
                Main.screenPosition = CameraPosition;
                CameraVelocity.X *= 0.85f;
                if (MathF.Abs(CameraVelocity.X) < 0.05f) CameraVelocity.X = 0;
                CameraVelocity.Y *= 0.85f;
                if (MathF.Abs(CameraVelocity.Y) < 0.05f) CameraVelocity.Y = 0;
                break;
        }
    }

    public static void Clear()
    {
        SetCamera(CameraMode.Default);
        ZoomMultiplier = 1.0f;
    }
}
