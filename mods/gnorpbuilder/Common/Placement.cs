using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GnorpBuilderMod.Common;

// What a globe asks for. A multiplayer client sends it to the server, which builds and syncs the area.
public sealed record BuildRequest(string Kind, string Name, int X, int Y, bool Check = false);

public sealed record ElevatorSpec(string Top, int First, int Clear, int MainOffset, int Bottom);

public static class Placement
{
    public static readonly Dictionary<string, ElevatorSpec> Elevators = new()
    {
        ["Forest"] = new("ForestElevatorTop", 17, 1, 1, -1),
        ["Tundra"] = new("TundraElevatorTop", 17, 2, 2, 0),
        ["Desert"] = new("DesertElevatorTop", 16, 1, 1, -1),
        ["Jungle"] = new("JungleElevatorTop", 17, 1, 1, -1),
        ["Ocean"] = new("OceanElevatorTop", 18, 1, 1, -1),
        ["Mushroom"] = new("MushroomElevatorTop", 16, 2, 2, 0),
        ["Hallow"] = new("HallowElevatorTop", 19, 1, 1, -1),
    };

    const int Segment = 20;
    const int ClearWidth = 9;
    const int ArenaRight = 129;
    const int TrackStep = 45;

    public static AnchorPoint CycleAnchor()
    {
        GnorpConfig config = GnorpConfig.Instance;
        var next = (AnchorPoint)(((int)config.Anchor + 1) % Enum.GetValues<AnchorPoint>().Length);
        var pending = (GnorpConfig)config.Clone();
        pending.Anchor = next;
        config.SaveChanges(pending);
        return next;
    }

    static readonly Dictionary<string, Asset<Texture2D>?> PreviewTextures = new(StringComparer.Ordinal);

    public static Asset<Texture2D>? PreviewTexture(string name)
    {
        if (Main.netMode == NetmodeID.Server) return null;
        string key = Structures.BuildName(name);
        if (PreviewTextures.TryGetValue(key, out Asset<Texture2D>? asset)) return asset;
        string path = Util.ModName + "/Builds/" + key;
        asset = ModContent.HasAsset(path) ? ModContent.Request<Texture2D>(path) : null;
        PreviewTextures[key] = asset;
        return asset;
    }

    static void PixelSize(string name, out int w, out int h)
    {
        try
        {
            Asset<Texture2D>? asset = PreviewTexture(name);
            if (asset != null)
            {
                Texture2D texture = asset.Value;
                w = texture.Width;
                h = texture.Height;
                return;
            }
        }
        catch (Exception e)
        {
            // The index still gives the size, for anything but the two full arena previews.
            GnorpBuilder.Instance.Logger.Warn("preview of " + name + " failed: " + e.Message);
        }
        if (Structures.Dimensions(name, out int tilesW, out int tilesH))
        {
            w = tilesW * 16;
            h = tilesH * 16;
            return;
        }
        w = h = 16;
    }

    static void AnchorOffsetTiles(string name, out int x, out int y)
    {
        PixelSize(name, out int w, out int h);
        AnchorPoint anchor = GnorpConfig.Instance.Anchor;
        int tilesW = Math.Max(1, (int)Math.Floor(w / 16.0 + 0.5)), tilesH = Math.Max(1, (int)Math.Floor(h / 16.0 + 0.5));
        x = 0;
        y = 0;
        if (anchor == AnchorPoint.BottomCenter) x = -(tilesW / 2);
        else if (anchor == AnchorPoint.TopRight || anchor == AnchorPoint.BottomRight) x = -(tilesW - 1);
        if (anchor == AnchorPoint.BottomCenter || anchor == AnchorPoint.BottomLeft || anchor == AnchorPoint.BottomRight) y = -(tilesH - 1);
    }

    public static void TopLeftAt(string name, Vector2 world, out int x, out int y)
    {
        AnchorOffsetTiles(name, out int offsetX, out int offsetY);
        x = (int)Math.Floor(world.X / 16.0) + offsetX;
        y = (int)Math.Floor(world.Y / 16.0) + offsetY;
    }

    static string? Biome(Player player)
    {
        if (player.ZoneHallow) return "Hallow";
        if (player.ZoneGlowshroom) return "Mushroom";
        if (player.ZoneJungle) return "Jungle";
        if (player.ZoneSnow) return "Tundra";
        if (player.ZoneBeach) return "Ocean";
        if (player.ZoneDesert) return "Desert";
        return null;
    }

    // The elevators ask in another order, so a biome overlap can differ from the preview's.
    static string? ElevatorBiome(Player player)
    {
        if (player.ZoneJungle) return "Jungle";
        if (player.ZoneDesert) return "Desert";
        if (player.ZoneSnow) return "Tundra";
        if (player.ZoneBeach) return "Ocean";
        if (player.ZoneGlowshroom) return "Mushroom";
        if (player.ZoneHallow) return "Hallow";
        return null;
    }

    // The empty key is the forest, the answer when no biome matches.
    static readonly Dictionary<string, Dictionary<string, string>> Houses = new()
    {
        ["Small"] = new() { ["Hallow"] = "SmallHallowHouse", ["Mushroom"] = "SmallMushroomHouse", ["Jungle"] = "SmallJungleHouse", ["Tundra"] = "SmallTundraHouse", ["Ocean"] = "SmallOceanHouse", ["Desert"] = "SmallSandstoneHouse", [""] = "OldCabin" },
        ["Medium"] = new() { ["Hallow"] = "MediumHallowHouse", ["Mushroom"] = "MediumMushroomHouse", ["Jungle"] = "MediumJungleHouse", ["Tundra"] = "MediumTundraHouse", ["Ocean"] = "MediumOceanHouse", ["Desert"] = "MediumSandstoneHouse", [""] = "OldCabinMedium" },
        ["Large"] = new() { ["Hallow"] = "LargeHallowHouse", ["Mushroom"] = "LargeMushroomHouse", ["Jungle"] = "LargeJungleHouse", ["Tundra"] = "LargeTundraHouse", ["Ocean"] = "LargeOceanHouseF", ["Desert"] = "LargeSandstoneHouse", [""] = "OldCabinBig" },
        ["LampPost"] = new() { ["Hallow"] = "HallowLampPost", ["Mushroom"] = "MushroomLampPost", ["Jungle"] = "JungleLampPost", ["Tundra"] = "TundraLampPost", ["Ocean"] = "OceanLampPost", ["Desert"] = "SandstoneLampPost", [""] = "WoodenLampPost" },
    };

    public static string BiomeBuild(string kind, Player player)
    {
        string? biome = Biome(player);
        if (kind == "Pylon")
        {
            if (biome != null) return biome + "Pylon";
            return player.ZoneRockLayerHeight ? "CavernPylon" : "ForestPylon";
        }
        return Houses[kind][biome ?? ""];
    }

    public static string BiomeElevator(Player player) => ElevatorBiome(player) ?? "Forest";

    public static string BiomeElevatorPreview(Player player) => (Biome(player) ?? "Forest") + "ElevatorTop";

    static bool PlaceElevator(string biome, int x, int topY)
    {
        if (!Elevators.TryGetValue(biome, out ElevatorSpec? e)) return false;
        int limit = Main.maxTilesY - 200 - 21;
        Structures.GenerateStructure(e.Top, x, topY);
        int y = topY + e.First;
        bool surface = false, cavern = false;
        int count = 0;

        void PlaceSegment()
        {
            Structures.GenerateStructure("ForestElevatorMain", x + e.MainOffset, y);
            y += Segment;
        }

        while (y < limit)
        {
            PlaceSegment();
            count++;
            if (!surface && y >= Main.worldSurface)
            {
                PlaceSegment();
                surface = true;
                count = 0;
                continue;
            }
            if (!cavern && y >= Main.rockLayer)
            {
                PlaceSegment();
                cavern = true;
                count = 0;
                continue;
            }
            if (count >= 7 && y > topY + e.First + 80 && y < limit - 20)
            {
                PlaceSegment();
                count = 0;
            }
        }
        PlaceSegment();
        PlaceSegment();
        Structures.GenerateStructure("ForestElevatorBottom", x + e.Bottom, y);
        DrainColumn(x + e.Clear, topY, ClearWidth);
        return true;
    }

    static void DrainColumn(int left, int top, int width)
    {
        int bottom = Main.maxTilesY - 200;
        int minY = -1, maxY = -1;
        for (int x = Math.Max(0, left); x < Math.Min(Main.maxTilesX, left + width); x++)
        {
            for (int y = Math.Max(0, top); y < Math.Min(Main.maxTilesY, bottom); y++)
            {
                Tile tile = Main.tile[x, y];
                if (tile.liquid == 0) continue;
                tile.SetLiquid(0);
                tile.liquidType(0);
                try
                {
                    WorldGen.SquareTileFrame(x, y, true);
                }
                catch (Exception)
                {
                    // Framing is cosmetic here, and the liquid is already gone.
                }
                if (minY < 0 || y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }
        if (minY >= 0 && (Main.netMode & NetmodeID.Server) != 0)
            Structures.SyncArea(left, minY, width, maxY - minY + 1);
    }

    static bool PlaceArena(string kind, int x, int y)
    {
        string left = kind == "Advanced" ? "LargeAdvancedArenaLeft" : "LargeSimpleArenaLeft";
        string right = kind == "Advanced" ? "LargeAdvancedArenaRight" : "LargeSimpleArenaRight";
        if (!Structures.IsInBounds(left, x, y) || !Structures.IsInBounds(right, x + ArenaRight, y)) return false;
        Structures.GenerateStructure(left, x, y);
        Structures.GenerateStructure(right, x + ArenaRight, y);
        return true;
    }

    static bool PlaceFullTrack(int x, int y)
    {
        if (!Structures.IsInBounds("ElevatorMinecartTrackPlus", x, y)) return false;
        Structures.GenerateStructure("ElevatorMinecartTrackPlus", x, y);
        int left = x - TrackStep;
        while (left > 200)
        {
            Structures.GenerateStructure("ElevatorMinecartTrack", left, y + 4);
            left -= TrackStep;
        }
        Structures.GenerateStructure("ElevatorLeftEndMineTrack", left + 30, y + 4);
        int right = x + 28;
        for (; right < Main.maxTilesX - 200; right += TrackStep)
            Structures.GenerateStructure("ElevatorMinecartTrack", right, y + 4);
        Structures.GenerateStructure("ElevatorRightEndMineTrack", right, y + 4);
        return true;
    }

    public static bool Execute(BuildRequest request)
    {
        try
        {
            switch (request.Kind)
            {
                case "build": return Structures.GenerateStructure(request.Name, request.X, request.Y);
                case "arena": return PlaceArena(request.Name, request.X, request.Y);
                case "elevator": return PlaceElevator(request.Name, request.X, request.Y);
                case "track": return PlaceFullTrack(request.X, request.Y);
                default: return false;
            }
        }
        catch (Exception e)
        {
            GnorpBuilder.Instance.Logger.Error(request.Kind + " " + request.Name + " failed: " + e.Message);
            return false;
        }
    }

    static bool Allowed(BuildRequest request)
    {
        switch (request.Kind)
        {
            case "build": return Structures.Dimensions(request.Name, out _, out _);
            case "arena": return request.Name == "Simple" || request.Name == "Advanced";
            case "elevator": return request.Name == "Biome" || Elevators.ContainsKey(request.Name);
            default: return request.Kind == "track";
        }
    }

    public static bool Request(BuildRequest request)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            ModPacket packet = GnorpBuilder.Instance.GetPacket();
            packet.Write(request.Kind);
            packet.Write(request.Name);
            packet.Write(request.X);
            packet.Write(request.Y);
            packet.Send();
            return true;
        }
        if (request.Kind == "elevator" && request.Name == "Biome")
            request = request with { Name = BiomeElevator(Main.LocalPlayer) };
        return Execute(request);
    }

    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        var request = new BuildRequest(reader.ReadString(), reader.ReadString(), reader.ReadInt32(), reader.ReadInt32());
        if ((Main.netMode & NetmodeID.Server) == 0 || !Allowed(request)) return;
        if (request.Kind == "elevator" && request.Name == "Biome")
            request = request with { Name = BiomeElevator(Main.player[whoAmI]) };
        Execute(request);
    }
}
