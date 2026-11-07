using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace GnorpBuilderMod.Common;

public readonly record struct HeldGlobe(Globe Globe, string Name, Asset<Texture2D>? Texture);

// A globe builds its structure where the player taps. Each subclass only says what it builds.
public abstract class Globe : ModItem
{
    const int DoubleTapTicks = 30;
    const int ItemSize = 32;

    public virtual int Value => 2500;
    public virtual int Rare => 2;
    public virtual string? Build => null;
    public virtual string? Biome => null;
    public virtual string? Elevator => null;
    public virtual string? Arena => null;
    public virtual bool Track => false;

    // Null is the Dig sound, which the game only has by number.
    protected virtual LegacySoundStyle? PlaceSound => null;

    public override string Texture => Util.ModName + "/Content/Items/" + Name;

    static long lastTap = -9999;
    static Vector2? lastTapAt;

    BuildRequest? pending;

    public override void SetDefaults()
    {
        Item.width = ItemSize;
        Item.height = ItemSize;
        Item.useStyle = 4;
        Item.useTime = 20;
        Item.useAnimation = 20;
        Item.noMelee = true;
        Item.consumable = true;
        Item.maxStack = 9999;
        Item.rare = Rare;
        Item.value = Value;
        Item.autoReuse = false;
    }

    public string? PreviewBuild(Player player)
    {
        if (Biome != null) return Placement.BiomeBuild(Biome, player);
        if (Elevator == "Biome") return Placement.BiomeElevatorPreview(player);
        if (Elevator != null) return Placement.Elevators[Elevator].Top;
        if (Arena != null) return Arena == "Advanced" ? "LargeAdvancedArena" : "LargeSimpleArena";
        if (Track) return "ElevatorMinecartTrackPlus";
        return Build;
    }

    BuildRequest? RequestFor(Player player, Vector2 world)
    {
        string? preview = PreviewBuild(player);
        if (preview == null) return null;
        Placement.TopLeftAt(preview, world, out int x, out int y);
        if (Elevator != null) return new BuildRequest("elevator", Elevator, x, y);
        if (Arena != null) return new BuildRequest("arena", Arena, x, y);
        if (Track) return new BuildRequest("track", "", x, y);
        return new BuildRequest("build", preview, x, y, true);
    }

    static bool DoubleTapOk(Vector2 world)
    {
        if (!GnorpConfig.Instance.RequireDoubleClickPlacement) return true;
        long now = Main.GameUpdateCount;
        if (lastTapAt is Vector2 at && now - lastTap <= DoubleTapTicks && Vector2.Distance(world, at) < 16f)
        {
            lastTap = -9999;
            lastTapAt = null;
            return true;
        }
        lastTap = now;
        lastTapAt = world;
        return false;
    }

    public override bool CanUseItem(Player player)
    {
        if (player.whoAmI != Main.myPlayer) return true;
        Vector2 world = Main.MouseWorld;
        BuildRequest? request = RequestFor(player, world);
        if (request == null) return false;
        if (request.Check && !Structures.IsInBounds(request.Name, request.X, request.Y)) return false;
        if (!DoubleTapOk(world)) return false;
        pending = request;
        return true;
    }

    public override bool? UseItem(Player player)
    {
        if (player.whoAmI != Main.myPlayer || pending == null) return false;
        BuildRequest request = pending;
        pending = null;
        Placement.Request(request);
        PlaySound(Main.MouseWorld);
        return true;
    }

    void PlaySound(Vector2 position)
    {
        try
        {
            if (PlaceSound is LegacySoundStyle style) SoundEngine.PlaySound(style, position, 0f, 1f);
            else SoundEngine.PlaySound(SoundID.Dig, position, 1, 0f);
        }
        catch (Exception e)
        {
            Mod.Logger.Warn("the placing sound failed: " + e.Message);
        }
    }

    public static HeldGlobe? HeldPreview(Player player)
    {
        Item item = player.HeldItem;
        if (item.IsNull || item.type <= 0 || item.stack <= 0 || item.IsAir) return null;
        if (item.ModItem is not Globe globe || item.type != globe.Type) return null;
        string? name = globe.PreviewBuild(player);
        return name == null ? null : new HeldGlobe(globe, name, Placement.PreviewTexture(name));
    }
}
