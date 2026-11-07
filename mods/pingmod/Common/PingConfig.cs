using System;
using System.ComponentModel;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace PingModPort.Common;

public enum PingIconStyle { BasicPurple, Basic, DSi, Gold, BasicLight, Fancy, Minecraft }

public class PingConfig : ModConfig
{
    public static PingConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [DefaultValue(PingIconStyle.DSi)]
    public PingIconStyle IconStyle;

    [Range(1, 10)]
    [Increment(1)]
    [DefaultValue(3)]
    public int PingSyncTimer;

    [DefaultValue(true)]
    public bool ShowIndicator;

    [DefaultValue(true)]
    public bool AttachToHealthBar;

    public static int StyleCount => SpriteNames.Length;

    public static int StyleIndex => Instance == null ? (int)PingIconStyle.DSi : Math.Max(0, (int)Instance.IconStyle);

    static readonly string[] SpriteNames = { "Purple", "Basic", "DS", "Gold", "BasicLight", "Fancy", "Minecraft" };
    static readonly string[] Levels = { "Full", "Mid", "Low", "Empty" };
    static readonly Asset<Texture2D>?[,] Textures = new Asset<Texture2D>?[SpriteNames.Length, Levels.Length];

    public static Asset<Texture2D> PingTexture(int style, int ping)
    {
        int level = ping < 60 ? 0 : ping < 90 ? 1 : ping < 250 ? 2 : 3;
        int s = Math.Min(Math.Max(style, 0), SpriteNames.Length - 1);
        return Textures[s, level] ??= ModContent.Request<Texture2D>("pingmod/UI/Sprites/PingIndicator" + SpriteNames[s] + Levels[level]);
    }

    public static Color PingColor(int ping)
    {
        float t = Math.Min(Math.Max(ping, 15), 200) / 200f;
        return new Color(Ping.Round(255 * t), Ping.Round(128 * (1 - t)), 0);
    }
}
