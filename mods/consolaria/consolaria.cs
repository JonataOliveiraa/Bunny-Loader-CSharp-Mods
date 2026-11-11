using BunnyLoader.Interop;

using Consolaria.Common;
using Consolaria.Content.Items.Placeable;
using Consolaria.Content.Items.Weapons.Melee;
using Consolaria.Content.Projectiles.Friendly;
using Consolaria.Content.Projectiles.Friendly.Pets;
using Consolaria.Content.Tiles;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using System;
using System.Collections.Generic;

using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.WorldBuilding;


namespace Consolaria {
    public partial class Consolaria : Mod {
        public override void Load() {
            if (Main.dedServ) {
                return;
            }

            On_Player.DropTombstone += On_Player_DropTombstone;
            On_WorldGen.dropXmasTree += On_WorldGen_dropXmasTree;

            On_TileDrawing.DrawMultiTileVinesInWind += On_TileDrawing_DrawMultiTileVinesInWind;
        }

        public override void PostSetupContent() {
            // Textures load on the game thread only, so this waits until here. Upstream does it in Load.
            TextureAssets.XmasTree[3] = ModContent.Request<Texture2D>("consolaria/Assets/Textures/Tiles/Xmas_3");

            if (ModContent.GetInstance<ConsolariaConfig>().tizonaZenithIntegrationEnabled) {
                try {
                    AddZenithProfile(ModContent.ItemType<Tizona>(), 70f, new Color(132, 122, 224)); //Color up for debate
                }
                catch (Exception e) {
                    Logger.Error($"Tizona was not added to the Zenith: {e.Message}");
                }
            }

            // What the phone's package did with the mods of its own catalog.
            if (ModLoader.TryGetMod("wikithis", out Mod wikithis)) {
                List<int> itemTypes = new();
                for (int type = ItemID.Count; type < ItemLoader.ItemCount; type++) {
                    if (ItemLoader.GetItem(type)?.Mod == this) itemTypes.Add(type);
                }
                wikithis.Call(0, "https://terrariamods.wiki.gg/wiki/Consolaria/", itemTypes.ToArray());
            }
            if (ModLoader.TryGetMod("weaponoutlite", out Mod weaponOut)) {
                weaponOut.Call("RegisterItemSpriteTurn", ModContent.ItemType<Tonbogiri>(), -360);
            }
        }

        private static void AddZenithProfile(int itemType, float fullBladeLength, Color color)
            => new GameDictionary(FinalFractalHelper._fractalProfiles).Set(itemType, FinalFractalHelper.FinalFractalProfile.New(fullBladeLength, color));

        //We reflect this hook so we are able to list the length/width of our blowing wind tile, Chandiliers, haning bottles, banners and more use this (Vines, double tall grass, ect do NOT use this. Dont attempt to fit them here)
        private void On_TileDrawing_DrawMultiTileVinesInWind(On_TileDrawing.orig_DrawMultiTileVinesInWind orig, TileDrawing self, Vector2 screenPosition, Vector2 offSet, int topLeftX, int topLeftY, int sizeX, int sizeY) {
            int tileType = Main.tile[topLeftX, topLeftY].TileType;
            if (tileType == ModContent.TileType<Banners>()) //Visit each of these tiles to see how predraw, draws them blowing in the wind
            {
                sizeX = 1;
                sizeY = 3;
            }
            else if (tileType == ModContent.TileType<SanctumLantern>()) {
                sizeX = 1;
                sizeY = 2;
            }
            else if (tileType == ModContent.TileType<SoulOfBlightInABottleTile>()) {
                sizeX = 1;
                sizeY = 2;
            }
            orig.Invoke(self, screenPosition, offSet, topLeftX, topLeftY, sizeX, sizeY);
        }

        private void On_Player_DropTombstone(On_Player.orig_DropTombstone orig, Player self, long coinsOwned, NetworkText deathText, int hitDirection) {
            if (Main.netMode == NetmodeID.MultiplayerClient) {
                return;
            }
            if (self.GetModPlayer<WormData>().IsWormPetActive) {
                Vector2 GetRandomTombstoneVelocity(int hitDirection) {
                    float num;
                    for (num = Main.rand.Next(-35, 36) * 0.1f; num < 2f && num > -2f; num += Main.rand.Next(-30, 31) * 0.1f) {
                    }
                    return new Vector2(Main.rand.Next(10, 30) * 0.1f * hitDirection + num,
                                       Main.rand.Next(-40, -20) * 0.1f);
                }

                int projectile = Projectile.NewProjectile(self.GetSource_FromThis("Death"), self.Center, GetRandomTombstoneVelocity(hitDirection), ModContent.ProjectileType<WormTombstone>(), 0, 0f, Main.myPlayer);
                DateTime now = DateTime.Now;
                string str = now.ToString("D");
                if (GameCulture.FromCultureName(GameCulture.CultureName.English).IsActive) {
                    str = now.ToString("MMMM d, yyy");
                }
                string miscText = deathText.ToString() + "\n" + str;
                Main.projectile[projectile].miscText = miscText;
                return;
            }
            orig(self, coinsOwned, deathText, hitDirection);
        }

        private void On_WorldGen_dropXmasTree(On_WorldGen.orig_dropXmasTree orig, int x, int y, int obj) {
            int num = x;
            int num2 = y;
            if (Main.tile[x, y].TileFrameX < 10) {
                num -= Main.tile[x, y].TileFrameX;
                num2 -= Main.tile[x, y].TileFrameY;
            }

            int num3 = 0;
            if ((Main.tile[num, num2].TileFrameY & 1) == 1)
                num3++;

            if ((Main.tile[num, num2].TileFrameY & 2) == 2)
                num3 += 2;

            if ((Main.tile[num, num2].TileFrameY & 4) == 4)
                num3 += 4;

            int num4 = 0;
            if ((Main.tile[num, num2].TileFrameY & 8) == 8)
                num4++;

            if ((Main.tile[num, num2].TileFrameY & 0x10) == 16)
                num4 += 2;

            if ((Main.tile[num, num2].TileFrameY & 0x20) == 32)
                num4 += 4;

            int num5 = 0;
            if ((Main.tile[num, num2].TileFrameY & 0x40) == 64)
                num5++;

            if ((Main.tile[num, num2].TileFrameY & 0x80) == 128)
                num5 += 2;

            if ((Main.tile[num, num2].TileFrameY & 0x100) == 256)
                num5 += 4;

            if ((Main.tile[num, num2].TileFrameY & 0x200) == 512)
                num5 += 8;

            int num6 = 0;
            if ((Main.tile[num, num2].TileFrameY & 0x400) == 1024)
                num6++;

            if ((Main.tile[num, num2].TileFrameY & 0x800) == 2048)
                num6 += 2;

            if ((Main.tile[num, num2].TileFrameY & 0x1000) == 4096)
                num6 += 4;

            if ((Main.tile[num, num2].TileFrameY & 0x2000) == 8192)
                num6 += 8;

            if (obj == 0 && num3 > 0) {
                int number = Item.NewItem(WorldGen.GetItemSource_FromTileBreak(x, y), new Rectangle(x * 16, y * 16, 16, 16), num3 == 5 ? ModContent.ItemType<Topper505>() : num3 == 6 ? ModContent.ItemType<StarTopper4>() : 1874 + num3 - 1);
                if (Main.netMode == 1)
                    NetMessage.SendData(21, -1, -1, default, number, 1f);
            }
            else if (obj == 1 && num4 > 0) {
                int number2 = Item.NewItem(WorldGen.GetItemSource_FromTileBreak(x, y), new Rectangle(x * 16, y * 16, 16, 16), 1878 + num4 - 1);
                if (Main.netMode == 1)
                    NetMessage.SendData(21, -1, -1, default, number2, 1f);
            }
            else if (obj == 2 && num5 > 0) {
                int number3 = Item.NewItem(WorldGen.GetItemSource_FromTileBreak(x, y), new Rectangle(x * 16, y * 16, 16, 16), 1884 + num5 - 1);
                if (Main.netMode == 1)
                    NetMessage.SendData(21, -1, -1, default, number3, 1f);
            }
            else if (obj == 3 && num6 > 0) {
                int number4 = Item.NewItem(WorldGen.GetItemSource_FromTileBreak(x, y), new Rectangle(x * 16, y * 16, 16, 16), 1895 + num6 - 1);
                if (Main.netMode == 1)
                    NetMessage.SendData(21, -1, -1, default, number4, 1f);
            }
        }
    }

    // Upstream hooks SceneMetrics.ExportTileCountsToMain and walks every tile around the player on
    // each scan. The phone has no such method, and the scan has already counted these tiles.
    internal sealed class WormTombstoneSceneSystem : ModSystem {
        public override void TileCountsAvailable(ReadOnlySpan<int> tileCounts) {
            int extraTombstones = tileCounts[ModContent.TileType<WormTombstoneTile>()];
            if (extraTombstones == 0) {
                return;
            }

            SceneMetrics metrics = Main.PlayerSceneMetrics;
            metrics.GraveyardTileCount += extraTombstones;

            if (metrics.GraveyardTileCount > SceneMetrics.GraveyardTileMin) {
                metrics.HasSunflower = false;
            }
        }
    }
}
