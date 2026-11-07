using System;
using System.Collections.Generic;
using BunnyLoader.Interop;
using LootBeamsMod.Common.Configs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace LootBeamsMod.Common;

public class LootBeamItem : GlobalItem
{
    const int SlotLifetimeFrames = 10;

    static readonly Dictionary<int, Rgb> Rarity = new()
    {
        [-11] = new(255, 175, 0), [-1] = new(130, 130, 130),
        [1] = new(150, 150, 255), [2] = new(150, 255, 150), [3] = new(255, 200, 150), [4] = new(255, 150, 150),
        [5] = new(255, 150, 255), [6] = new(210, 160, 255), [7] = new(150, 255, 10), [8] = new(255, 255, 10),
        [9] = new(5, 200, 255), [10] = new(255, 40, 100), [11] = new(180, 40, 255),
    };
    static readonly Rgb White = new(255, 255, 255);
    static readonly Rgb ModRarityFallback = new(180, 40, 255);

    static readonly Rectangle ArrowTip = new(0, 0, 24, 13);
    static readonly Rectangle ArrowTail = new(0, 28, 24, 13);
    static readonly Vector2 ArrowOrigin = new(12, 13);

    sealed class BeamTexture
    {
        public Texture2D Tex;
        public Vector2 Center;
    }

    static readonly Dictionary<string, BeamTexture> Textures = new();

    static BeamTexture Tex(string path)
    {
        if (!Textures.TryGetValue(path, out BeamTexture? texture))
        {
            Texture2D tex = ModContent.Request<Texture2D>("lootbeams/Assets/" + path).Value;
            texture = new BeamTexture { Tex = tex, Center = new Vector2(tex.Width / 2f, tex.Height / 2f) };
            Textures[path] = texture;
        }
        return texture;
    }

    static readonly Dictionary<int, (int Width, int Height)> ItemSizes = new();

    static (int Width, int Height) ItemSize(int type)
    {
        if (ItemSizes.TryGetValue(type, out var size))
            return size;
        Asset<Texture2D>? asset = type < TextureAssets.Item.Length ? TextureAssets.Item[type] : null;
        if (asset == null)
            return (24, 24);
        Texture2D tex = asset.Value;
        if (tex.IsNull)
            return (24, 24);
        int frames = 1;
        if (type < Main.itemAnimations.Length)
        {
            DrawAnimation animation = Main.itemAnimations[type];
            if (!animation.IsNull && animation.FrameCount > 0)
                frames = animation.FrameCount;
        }
        size = (tex.Width, Math.Max(1, tex.Height / frames));
        ItemSizes[type] = size;
        return size;
    }

    static Rgb ModRarityColor(int rare)
    {
        ModRarity? rarity = RarityLoader.GetRarity(rare);
        if (rarity == null)
            return ModRarityFallback;
        try
        {
            Color color = rarity.RarityColor;
            return new Rgb(color.R, color.G, color.B);
        }
        catch (Exception)
        {
            return ModRarityFallback;
        }
    }

    // Null means the color changes every frame: see DynamicColor.
    static Rgb? RarityColor(Item item)
    {
        if (item.expert || item.rare == -12 || item.rare == -13)
            return null;
        if (item.rare > 11)
            return ModRarityColor(item.rare);
        return Rarity.TryGetValue(item.rare, out Rgb color) ? color : White;
    }

    static Rgb? ColorOf(Item item, LootBeamConfig config, ListLookup user)
    {
        int type = item.type;
        if (user.Colors.TryGetValue(type, out Rgb color))
            return color;
        if (config.UseDefaultLists && Lists.DefaultColors.TryGetValue(type, out color))
            return color;
        return RarityColor(item);
    }

    public static bool HasBeam(Item item, LootBeamConfig config, ListLookup user)
    {
        int type = item.type;
        if (user.Black.Contains(type))
            return false;
        if (user.White.Contains(type))
            return true;
        if (config.UseDefaultLists && Lists.DefaultBlacklist.Contains(type))
            return false;
        if (config.UseDefaultLists && Lists.DefaultWhitelist.Contains(type))
            return true;
        int rare = item.rare;
        bool quest = item.questItem || rare == -11;
        bool expert = item.expert || rare == -12;
        bool master = rare == -13;
        if (config.HighlightQuest && quest)
            return true;
        if (config.HighlightExpert && expert)
            return true;
        if (config.HighlightMaster && master)
            return true;
        return config.MinRarity > -2 && rare >= config.MinRarity && item.value >= config.MinCopper() && !quest && !expert && !master;
    }

    public static bool IsMini(int type, LootBeamConfig config, ListLookup user) =>
        user.Mini.Contains(type) || (config.UseDefaultLists && Lists.DefaultMini.Contains(type));

    // The state of the frame being drawn. The items draw in whoAmI order, so a whoAmI that
    // does not grow means a new frame.
    static int lastWhoAmI = int.MaxValue;
    static int frameCount;
    static LootBeamConfig frameConfig = null!;
    static ListLookup frameUser = null!;
    static (int, string, bool, bool, bool, bool, int) frameFilters;
    static int filterVersion;
    static float screenX, screenY, screenWidth, screenHeight, frameFps = 1f;

    static void BeginFrame()
    {
        frameCount++;
        LootBeamConfig config = frameConfig = LootBeamConfig.Instance;
        if (!config.Enabled)
            return;

        frameUser = Lists.Lookup();
        var filters = (config.MinRarity, config.MinValue, config.HighlightQuest, config.HighlightExpert, config.HighlightMaster, config.UseDefaultLists, Lists.Version);
        if (filters != frameFilters)
        {
            frameFilters = filters;
            filterVersion++;
        }

        Vector2 screen = Main.screenPosition;
        screenX = screen.X;
        screenY = screen.Y;
        screenWidth = Main.screenWidth;
        screenHeight = Main.screenHeight;
        frameFps = Math.Max(Main.frameRate / 60f, 0.017f);
    }

    static Rgb DynamicColor(bool master) =>
        master ? new Rgb(255, Util.Round(Main.masterColor * 200), 0) : new Rgb(Main.DiscoR, Main.DiscoG, Main.DiscoB);

    // What each dropped item remembers between frames.
    sealed class Slot
    {
        public int Type, Version, Seen, Time;
        public float Fade;
        public bool Beam, Master, Mini;
        public int Width, Height, TexW, TexH;
        public Rgb? Rgb;
    }

    static Slot?[] slots = new Slot?[401];

    static Slot SlotOf(int whoAmI, WorldItem world, Item item, int type)
    {
        Slot? slot = slots[whoAmI];
        if (slot != null && slot.Type == type && slot.Version == filterVersion && frameCount - slot.Seen <= SlotLifetimeFrames)
        {
            slot.Seen = frameCount;
            return slot;
        }

        bool fresh = slot == null || slot.Type != type || frameCount - slot.Seen > SlotLifetimeFrames;
        slot ??= slots[whoAmI] = new Slot();
        if (fresh)
        {
            slot.Fade = 0f;
            slot.Time = 0;
        }
        slot.Type = type;
        slot.Version = filterVersion;
        slot.Seen = frameCount;
        slot.Beam = HasBeam(item, frameConfig, frameUser);
        if (!slot.Beam)
            return slot;

        var size = ItemSize(type);
        slot.Width = world.width;
        slot.Height = world.height;
        slot.TexW = size.Width;
        slot.TexH = size.Height;
        slot.Rgb = ColorOf(item, frameConfig, frameUser);
        slot.Master = !item.expert && item.rare == -13;
        slot.Mini = IsMini(type, frameConfig, frameUser);
        return slot;
    }

    // Brightness has 64 steps. With alpha 0 the color adds up instead of blending.
    static Color Paint(Rgb rgb, float k, bool additive)
    {
        int level = Util.Round(Clamp01(k) * 63);
        double f = level / 63.0;
        return new Color(Util.Round(rgb.R * f), Util.Round(rgb.G * f), Util.Round(rgb.B * f), additive ? 0 : Util.Round(255 * f));
    }

    static float Clamp01(float value) => Math.Max(0f, Math.Min(1f, value));

    static void DrawTex(SpriteBatch batch, Texture2D tex, float x, float y, GameNullable<Rectangle> source, Color color, Vector2 origin, float scale) =>
        batch.Draw(tex, new Vector2(x, y), source, color, 0f, origin, scale, SpriteEffects.None, 0f);

    static void DrawBeam(SpriteBatch batch, LootBeamConfig config, float x, float bottom, float h, float scale, Rgb rgb, float fade, float alpha, bool additive, int time)
    {
        float k = fade * (0.75f + alpha * 0.25f) * config.BeamOpacity;
        if (k <= 0.01f)
            return;
        if (config.BeamStyle == BeamStyleMode.Simple)
        {
            BeamTexture beam = Tex(additive ? "Beams/SimpleBeamAdditive" : "Beams/SimpleBeam");
            DrawTex(batch, beam.Tex, x, bottom - (h * 0.5f + 56f * scale), default, Paint(rgb, k, additive), beam.Center, scale);
            return;
        }
        BeamTexture arrow = Tex("Beams/Arrow");
        float bob = config.BeamStyle == BeamStyleMode.AnimatedArrow ? MathF.Sin(time * 0.08f) * 6f : 0f;
        float s = scale * 1.25f;
        float tip = bottom - h - 10f + bob;
        Color color = Paint(rgb, k, false);
        DrawTex(batch, arrow.Tex, x, tip, new GameNullable<Rectangle>(ArrowTip), color, ArrowOrigin, s);
        DrawTex(batch, arrow.Tex, x, tip - 13f * s, new GameNullable<Rectangle>(ArrowTail), color, ArrowOrigin, s);
    }

    static void DrawGlow(SpriteBatch batch, LootBeamConfig config, float x, float y, float glowScale, int texW, int texH, Rgb rgb, float fade, float alpha, bool additive)
    {
        if (config.GlowStyle == GlowStyleMode.Off)
            return;
        float k = fade * config.GlowOpacity;
        if (k <= 0.01f)
            return;
        BeamTexture center = Tex(additive ? "Glows/CenterAdditive" : "Glows/Center");
        DrawTex(batch, center.Tex, x, y, default, Paint(rgb, k * (0.75f + alpha * 0.25f), additive), center.Center, glowScale);
        float size = Math.Max(0.25f, Math.Min(5f, (texW / 16 + texH / 16) / 2));
        float wide = (0.3f + alpha * 0.05f * size) * glowScale;
        bool pixel = config.GlowStyle == GlowStyleMode.Pixel;
        BeamTexture glow = Tex(pixel
            ? additive ? "Glows/PixelGlowAdditive" : "Glows/PixelGlow"
            : additive ? "Glows/SimpleGlowAdditive" : "Glows/SimpleGlow");
        if (pixel)
            wide *= 150f / 45f;
        DrawTex(batch, glow.Tex, x, y, default, Paint(rgb, k * (0.5f + alpha * 0.5f), additive), glow.Center, wide);
    }

    static void DrawItemBeam(WorldItem world, SpriteBatch batch, int whoAmI)
    {
        if (whoAmI <= lastWhoAmI)
            BeginFrame();
        lastWhoAmI = whoAmI;
        LootBeamConfig config = frameConfig;
        if (!config.Enabled)
            return;

        if (whoAmI >= slots.Length)
            Array.Resize(ref slots, whoAmI + 1);

        Vector2 position = world.position;
        float left = position.X - screenX, top = position.Y - screenY;
        if (left < -264f || left > screenWidth + 200f || top < -164f || top > screenHeight + 400f)
        {
            if (slots[whoAmI] is Slot offscreen)
                offscreen.Seen = frameCount;
            return;
        }

        Item item = world.inner;
        int type = item.type;
        if (type <= 0)
            return;

        Slot state = SlotOf(whoAmI, world, item, type);
        if (!state.Beam)
            return;

        float x = left + state.Width / 2f;
        float bottom = top + state.Height;
        float h = state.TexH;
        Rgb rgb = state.Rgb ?? DynamicColor(state.Master);

        bool grabbed = config.FadeOnPickup && world.beingGrabbed;
        state.Fade = Clamp01(state.Fade + (grabbed ? -0.125f : 0.025f) / frameFps);

        float alpha = config.Pulse ? Clamp01((MathF.Sin(state.Time * 2f * MathF.PI / 180f) + 1f) * 0.5f) : 1f;
        float beamScale = config.BeamScale, glowScale = config.GlowScale;

        if (state.Mini)
            beamScale = glowScale = 0.5f;
        else
            glowScale *= config.FlashScale - state.Fade * (config.FlashScale - 1f);

        bool additive = config.DrawAdditive;
        if (config.BeamStyle != BeamStyleMode.Off)
            DrawBeam(batch, config, x, bottom, h, beamScale, rgb, state.Fade, alpha, additive, state.Time);

        DrawGlow(batch, config, x, bottom - h * 0.5f, glowScale, state.TexW, state.TexH, rgb, state.Fade, alpha, additive);
        state.Time++;

        if (config.EmitLight > 0f)
        {
            float k = config.EmitLight * state.Fade / 255f;
            int i = (int)MathF.Floor((position.X + state.Width / 2f) / 16f);
            int j = (int)MathF.Floor((position.Y + state.Height - h * 0.5f) / 16f);
            Lighting.AddLight(i, j, rgb.R * k, rgb.G * k, rgb.B * k);
        }
    }

    public override bool PreDrawInWorld(WorldItem item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        DrawItemBeam(item, spriteBatch, whoAmI);
        return true;
    }
}
