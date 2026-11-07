using System;
using System.Collections.Generic;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using WeaponOutLiteMod.Common.Configs;
using WeaponOutLiteMod.Common.Players;
using WeaponOutLiteMod.Common.Poses;
using G = WeaponOutLiteMod.Common.PoseGroup;
using ID = WeaponOutLiteMod.Common.DrawItemPoseID;

namespace WeaponOutLiteMod.Common;

// What a draw needs, found once per layer so nothing here allocates.
public struct CanDraw
{
    public Player player;
    public WeaponOutPlayer modPlayer;
    public Pose pose;
    public Item item;
    public Texture2D texture;
    public Rectangle rect;
    public bool yoyo;
    public int? spear;
    public bool projSpear;
}

public static class Renderer
{
    const int GravityOffset = -3;

    static int Clamp255(double v) => (int)Math.Max(0, Math.Min(255, Math.Truncate(v)));

    static Color Mul(Color c, double f) => new(Clamp255(c.R * f), Clamp255(c.G * f), Clamp255(c.B * f), Clamp255(c.A * f));

    // An empty handle when the type has no texture.
    static Texture2D ProjectileTexture(int type)
    {
        TextureAssetArray assets = TextureAssets.Projectile;
        return (uint)type < (uint)assets.Length ? assets[type].Value : default;
    }

    static int ProjFrames(int type) => (uint)type < (uint)Main.projFrames.Length ? Math.Max(1, Main.projFrames[type]) : 1;

    static Rectangle SourceRect(Item item, Texture2D texture)
    {
        if ((uint)item.type < (uint)Main.itemAnimations.Length)
        {
            DrawAnimation animation = Main.itemAnimations[item.type];
            if (!animation.IsNull)
                return animation.GetFrame(texture, -1);
        }
        return new Rectangle(0, 0, texture.Width, texture.Height);
    }

    static bool YoyoTexture(Item item, out Texture2D texture, out Rectangle rect)
    {
        texture = default;
        rect = default;
        if (!WeaponOutConfig.Instance.EnableProjYoyos || item.shoot == 0)
            return false;
        texture = ProjectileTexture(item.shoot);
        if (texture.IsNull)
            return false;
        rect = new Rectangle(0, 0, texture.Width, texture.Height / ProjFrames(item.shoot));
        return true;
    }

    static bool SpearTexture(Item item, Texture2D itemTexture, out Texture2D texture, out Rectangle rect)
    {
        rect = default;
        texture = ProjectileTexture(item.shoot);
        if (texture.IsNull)
            return false;
        rect = new Rectangle(0, 0, texture.Width, texture.Height / ProjFrames(item.shoot));
        return rect.Width == rect.Height && (rect.Width > itemTexture.Width || rect.Height > itemTexture.Height);
    }

    // Quarter turns of each spear projectile's sprite, learned from the live projectile.
    static readonly Dictionary<int, int> spearTurns = new();

    static int SpearTurns(int type) => spearTurns.TryGetValue(type, out int turns) ? turns : 1;

    static void LearnSpear(Projectile p)
    {
        double vX = p.velocity.X, vY = p.velocity.Y;
        if (p.spriteDirection != 1 || Math.Abs(vX) + Math.Abs(vY) < 0.01)
            return;
        double tip = Math.Atan2(vY, vX) - p.rotation;
        double turns = PoseMath.RoundHalfUp((-MH.PiOver4 - tip) / MH.PiOver2);
        spearTurns[p.type] = (int)((turns % 4 + 4) % 4);
    }

    public static int DefaultSpriteTurn(Item item)
    {
        int? turn = Overrides.ItemSpriteTurn(item.type);
        if (turn != null)
            return turn.Value;
        if (item.shoot == 0 || !WeaponOutConfig.Instance.EnableProjSpears || Classifier.GetItemPoseGroupData(item).poseGroup != G.Spear)
            return 0;
        Texture2D texture = Classifier.ItemTexture(item.type);
        return !texture.IsNull && SpearTexture(item, texture, out _, out _) ? SpearTurns(item.shoot) : 0;
    }

    public static bool CanDrawBaseDrawData(in PlayerDrawSet drawInfo, out CanDraw can)
    {
        can = default;
        Player player = drawInfo.drawPlayer;
        WeaponOutPlayer modPlayer = WeaponOutPlayer.Of(player);
        Pose? pose = modPlayer.CurrentDrawItemPose;
        WeaponOutConfig config = WeaponOutConfig.Instance;
        bool gameMenu = Main.gameMenu;
        if ((!modPlayer.DrawHeldItem() && !(gameMenu && config.EnableMenuDisplay)) || pose == null)
            return false;
        Item item = modPlayer.HeldItem();
        if (item.IsNull || item.type == 0 || item.holdStyle != 0)
            return false;

        if (player.sleeping.isSleeping && !gameMenu)
            return false;

        Texture2D texture = Classifier.ItemTexture(item.type);
        Rectangle rect = default;
        bool hasRect = false, yoyo = false, projSpear = false;
        int? spear = null;
        // -1: no group, the item shoots nothing.
        int group = item.shoot != 0 ? (int)Classifier.GetItemPoseGroupData(item).poseGroup : -1;
        if (group == (int)G.Yoyo && config.EnableProjYoyos)
        {
            if (YoyoTexture(item, out Texture2D proj, out Rectangle projRect))
            {
                texture = proj;
                rect = projRect;
                hasRect = true;
                yoyo = true;
            }
        }
        else if (group == (int)G.Spear && config.EnableProjSpears && !texture.IsNull)
        {
            if (SpearTexture(item, texture, out Texture2D proj, out Rectangle projRect))
            {
                texture = proj;
                rect = projRect;
                hasRect = true;
                projSpear = true;
            }
        }
        if (texture.IsNull)
            return false;
        int? turn = Overrides.ItemSpriteTurn(item.type);
        if (turn != null)
            spear = turn;
        else if (projSpear)
            spear = SpearTurns(item.shoot);

        can.player = player;
        can.modPlayer = modPlayer;
        can.pose = pose;
        can.item = item;
        can.texture = texture;
        can.rect = hasRect ? rect : SourceRect(item, texture);
        can.yoyo = yoyo;
        can.spear = spear;
        can.projSpear = projSpear;
        return true;
    }

    static PoseData CreateBaseDrawData(in PlayerDrawSet drawInfo, in CanDraw can)
    {
        Player player = can.player;
        Item item = can.item;
        Rectangle rect = can.rect;
        WeaponOutConfig config = WeaponOutConfig.Instance;
        Vector2 screen = Main.screenPosition;
        Vector2 mounted = player.MountedCenter;

        double drawX = Math.Truncate((double)mounted.X - screen.X);
        double drawY = Math.Truncate((double)mounted.Y - screen.Y + player.gfxOffY) + GravityOffset;
        Vector2 position = drawInfo.Position;
        if (Main.gameMenu && config.EnableMenuDisplay)
        {
            drawX = Math.Truncate((double)position.X - screen.X) + 10;
            drawY = Math.Truncate((double)position.Y - screen.Y) + 18;
        }

        Vector2 center = player.Center;
        int tileX = (int)((double)center.X / 16), tileY = (int)((double)center.Y / 16);
        if (player.sitting.isSitting && PlayerSittingHelper.GetSittingTargetInfo(player, tileX, tileY, out int seatDir, out Vector2 _, out Vector2 seat, out ExtraSeatInfo _))
        {
            drawX += Math.Truncate((double)seat.X * seatDir);
            drawY += Math.Truncate((double)seat.Y * 2 + GravityOffset);
        }
        else if (player.sleeping.isSleeping && PlayerSleepingHelper.GetSleepingTargetInfo(tileX, tileY, out int bedDir, out Vector2 _, out Vector2 bed))
        {
            drawX += Math.Truncate((double)bed.X * bedDir);
            drawY += Math.Truncate((double)bed.Y * 2 + GravityOffset);
        }

        double scale = config.EnableItemScaling ? PoseMath.SnapNearOne(item.scale) : 1;

        Color light = Lighting.GetColor((int)(((double)position.X + player.width / 2.0) / 16), (int)(((double)position.Y + player.height / 2.0) / 16));
        Color colour = item.GetColor(light), alpha = item.GetAlpha(light);
        var mix = new Color(
            Clamp255(Math.Min(1, (colour.R + alpha.R) / 255.0) * 255 + 0.5), Clamp255(Math.Min(1, (colour.G + alpha.G) / 255.0) * 255 + 0.5),
            Clamp255(Math.Min(1, (colour.B + alpha.B) / 255.0) * 255 + 0.5), Clamp255(Math.Min(1, (colour.A + alpha.A) / 255.0) * 255 + 0.5));
        Color lighting = Mul(player.GetImmuneAlpha(mix, 0f), player.stealth);

        SpriteEffects effect = SpriteEffects.None;
        if (player.direction < 0)
            effect = SpriteEffects.FlipVertically;
        if (player.gravDir < 0)
        {
            drawY -= GravityOffset * 2;
            effect |= SpriteEffects.FlipHorizontally;
        }

        double shadowX = 0, shadowY = 0;
        double shadow = drawInfo.shadow;
        if (shadow > 0)
        {
            Vector2 infoCenter = drawInfo.Center;
            shadowX = (double)infoCenter.X - mounted.X + ((double)center.X - mounted.X);
            shadowY = (double)infoCenter.Y - mounted.Y + ((double)center.Y - mounted.Y);
            lighting = Mul(lighting, 1 - shadow);
        }

        return new PoseData(can.texture, new PoseVector(drawX + shadowX, drawY + shadowY), rect, lighting, 0,
            new PoseVector(rect.Width / 2.0, rect.Height / 2.0), new PoseVector(scale, scale), effect);
    }

    static DrawData ToDrawData(in PoseData d) => new(d.texture, new Vector2((float)d.position.X, (float)d.position.Y), d.sourceRect,
        d.color, (float)d.rotation, new Vector2((float)d.origin.X, (float)d.origin.Y), (float)d.scale.X, d.effect, 0);

    static void TurnPoint(double x, double y, double W, double H, int quarters, out double outX, out double outY)
    {
        x -= W / 2;
        y -= H / 2;
        for (int k = 0; k < quarters; k++)
        {
            double rotated = x;
            x = -y;
            y = rotated;
        }
        outX = x + W / 2;
        outY = y + H / 2;
    }

    static void FlipPoint(in PoseData d, double x, double y, double W, double H, out double outX, out double outY)
    {
        outX = d.Has(SpriteEffects.FlipHorizontally) ? W - x : x;
        outY = d.Has(SpriteEffects.FlipVertically) ? H - y : y;
    }

    // The same drawing with the sprite turned a number of quarters: a rotation and origin that put
    // three corners of the turned sprite where they should be. False when no rotation fits.
    static bool Turned(in PoseData d, int quarters, out PoseData result)
    {
        result = d;
        double W = d.sourceRect.Width, H = d.sourceRect.Height;
        Span<double> qx = stackalloc double[3] { 0, W, 0 };
        Span<double> qy = stackalloc double[3] { 0, 0, H };
        Span<double> atX = stackalloc double[3];
        Span<double> atY = stackalloc double[3];
        double cos = Math.Cos(d.rotation), sin = Math.Sin(d.rotation);
        for (int n = 0; n < 3; n++)
        {
            TurnPoint(qx[n], qy[n], W, H, quarters, out double turnedX, out double turnedY);
            FlipPoint(d, turnedX, turnedY, W, H, out double px, out double py);
            double x = (px - d.origin.X) * d.scale.X, y = (py - d.origin.Y) * d.scale.Y;
            atX[n] = d.position.X + x * cos - y * sin;
            atY[n] = d.position.Y + x * sin + y * cos;
        }
        for (int k = 0; k < 4; k++)
        {
            double r = d.rotation + k * MH.PiOver2, c = Math.Cos(r), s = Math.Sin(r);
            FlipPoint(d, qx[0], qy[0], W, H, out double f0X, out double f0Y);
            double dx = atX[0] - d.position.X, dy = atY[0] - d.position.Y;
            double originX = f0X - (dx * c + dy * s) / d.scale.X;
            double originY = f0Y - (-dx * s + dy * c) / d.scale.Y;
            bool fits = true;
            for (int n = 0; n < 3 && fits; n++)
            {
                FlipPoint(d, qx[n], qy[n], W, H, out double fX, out double fY);
                double x = (fX - originX) * d.scale.X, y = (fY - originY) * d.scale.Y;
                fits = Math.Abs(d.position.X + x * c - y * s - atX[n]) < 0.01 && Math.Abs(d.position.Y + x * s + y * c - atY[n]) < 0.01;
            }
            if (fits)
            {
                result.rotation = r;
                result.origin = new PoseVector(PoseMath.RoundHalfUp(originX * 1000) / 1000, PoseMath.RoundHalfUp(originY * 1000) / 1000);
                return true;
            }
        }
        return false;
    }

    static void Add(ref PlayerDrawSet drawInfo, in PoseData data, int? spear = null)
    {
        if (data.texture.IsNull)
            return;
        if (spear is { } quarters && quarters != 0 && Turned(data, quarters, out PoseData turned))
            drawInfo.DrawDataCache.Add(ToDrawData(turned));
        else
            drawInfo.DrawDataCache.Add(ToDrawData(data));
    }

    // A melee weapon whose projectile is out (a spear thrust, a flail) is not drawn on the player.
    static bool OwnMeleeProjectileOut(Player player, Item item)
    {
        if (item.shoot == 0 || !item.CountsAsClass(DamageClass.Melee) || !item.noMelee)
            return false;
        GameArray<Projectile> projectiles = Main.projectile;
        int n = projectiles.Length;
        for (int i = 0; i < n; i++)
        {
            Projectile p = projectiles[i];
            if (p.IsNull || !p.active)
                continue;
            if (p.owner == player.whoAmI && p.type == item.shoot && p.CountsAsClass(DamageClass.Melee))
            {
                LearnSpear(p);
                return true;
            }
        }
        return false;
    }

    public static void DrawPlayerItem(ref PlayerDrawSet drawInfo)
    {
        if (!CanDrawBaseDrawData(drawInfo, out CanDraw can))
            return;
        try
        {
            AddItemToDrawInfoCache(ref drawInfo, CreateBaseDrawData(drawInfo, can), can);
        }
        catch (Exception e)
        {
            State.LogOnce("render:" + can.item.type, "error while drawing the item " + can.item.type + ": " + e);
        }
    }

    static void AddItemToDrawInfoCache(ref PlayerDrawSet drawInfo, PoseData itemData, in CanDraw can)
    {
        Player player = can.player;
        WeaponOutPlayer modPlayer = can.modPlayer;
        Item item = can.item;
        WeaponOutConfig config = WeaponOutConfig.Instance;
        if (OwnMeleeProjectileOut(player, item))
            return;

        double height = itemData.sourceRect.Height * itemData.scale.X;
        double width = itemData.sourceRect.Width * itemData.scale.Y;
        int bodyFrame = PoseMath.BodyFrameNum(player);
        double itemScale = player.GetAdjustedItemScale(player.HeldItem);
        double scale = itemScale * PoseMath.GetGiantTextureScale(width * itemScale, height * itemScale);
        if (config.YoyoHalfScale && Classifier.GetItemPoseGroupData(item).poseGroup == G.Yoyo)
        {
            bool applyScale = !config.EnableProjYoyos || !can.yoyo;
            if (applyScale)
                scale *= 0.66;
        }
        itemData.scale = new PoseVector(scale, scale);
        itemData.SetOrigin(0.5, 0.5, player);
        double centreX = itemData.position.X, centreY = itemData.position.Y;
        height = itemData.sourceRect.Height * itemData.scale.Y;
        width = itemData.sourceRect.Width * itemData.scale.X;

        Pose pose = can.pose;
        double timer = modPlayer.CombatDelayTimer;
        itemData = pose.CalculateDrawData(itemData, player, height, width, bodyFrame, timer);

        if (pose.GetID() == ID.Custom)
        {
            var funcs = State.customPreDrawDataFuncs;
            for (int k = 0; k < funcs.Count; k++)
            {
                try
                {
                    itemData = funcs[k](player, item, itemData, height, width, bodyFrame, timer);
                }
                catch (Exception e)
                {
                    State.LogOnce("custom:" + k, "error in RegisterCustomPreDrawData function: " + e);
                }
            }
        }
        if (itemData.texture.IsNull)
            return;

        double direction = player.direction, gravDir = player.gravDir;
        itemData.position.X = (itemData.position.X - centreX) * direction + centreX;
        itemData.position.Y = (itemData.position.Y - centreY) * gravDir + centreY;
        double flipX = itemData.Has(SpriteEffects.FlipHorizontally) ? -1 : 1;
        double flipY = itemData.Has(SpriteEffects.FlipVertically) ? -1 : 1;
        flipX *= direction;
        flipY *= gravDir;
        if (flipX < 0)
            itemData.origin.X = (itemData.origin.X - itemData.sourceRect.Width) * -1;
        if (flipY < 0)
            itemData.origin.Y = (itemData.origin.Y - itemData.sourceRect.Height) * -1;
        itemData.rotation += -MH.PiOver2 * direction * gravDir + MH.PiOver2;
        itemData.rotation *= direction * gravDir;

        Add(ref drawInfo, itemData, can.spear);
        if (!can.projSpear && GlowLayer(player, item, itemData, can.yoyo, out PoseData glow))
            Add(ref drawInfo, glow);

        if (config.BowDrawAmmo && item.useAmmo == AmmoID.Arrow)
        {
            Item ammo = FindAmmo(player, item.useAmmo);
            int projectileType = ApplyVanillaAmmoReplacement(item.type, !ammo.IsNull ? ammo.shoot : 0);
            if (!ammo.IsNull && projectileType > 0 && ArrowDrawData(player, projectileType, itemData, out PoseData arrow))
            {
                Add(ref drawInfo, arrow);
                if (ProjectileGlowLayer(player, projectileType, arrow, out PoseData arrowGlow))
                    Add(ref drawInfo, arrowGlow);
            }
        }

        if (bodyFrame == 5 && player.compositeFrontArm.enabled)
        {
            int r100 = (int)(Math.Truncate((double)drawInfo.compositeFrontArmRotation * 100 * player.direction) % 314);
            bool front = r100 < 0 || r100 > 235;
            bool underHalfPi = Math.Abs(r100) < 160;
            if (front || underHalfPi)
            {
                drawInfo.hideCompositeShoulders = false;
                drawInfo.compShoulderOverFrontArm = true;
            }
        }
    }

    // An empty handle when there is no such glow.
    static Texture2D GlowMask(int index)
    {
        var assets = TextureAssets.GlowMask;
        if ((uint)index >= (uint)assets.Length)
            return default;
        var asset = assets[index];
        return asset != null ? asset.Value : default;
    }

    static Color GlowColor(Player player, Color alpha, in PoseData data)
    {
        Color glow = Mul(Mul(alpha, player.stealth), data.color.A);
        return player.GetImmuneAlpha(glow, 0f);
    }

    static bool GlowLayer(Player player, Item item, in PoseData data, bool yoyo, out PoseData glow)
    {
        if (yoyo && ProjectileGlowLayer(player, item.shoot, data, out glow))
            return true;
        glow = default;
        if (item.glowMask > 0)
        {
            Texture2D texture = GlowMask(item.glowMask);
            if (texture.IsNull)
                return false;
            glow = data;
            glow.texture = texture;
            glow.color = GlowColor(player, item.GetAlpha(new Color(250, 250, 250, item.alpha)), data);
            return true;
        }
        return false;
    }

    static bool ProjectileGlowLayer(Player player, int type, in PoseData data, out PoseData glow)
    {
        glow = default;
        Projectile p = Classifier.GetProjectile(type);
        if (p.glowMask == -1)
            return false;
        Texture2D texture = GlowMask(p.glowMask);
        if (texture.IsNull)
            return false;
        glow = data;
        glow.texture = texture;
        glow.color = GlowColor(player, p.GetAlpha(new Color(250, 250, 250, p.alpha)), data);
        return true;
    }

    // An empty handle when the player has none.
    static Item FindAmmo(Player player, int useAmmo)
    {
        GameArray<Item> inventory = player.inventory;
        for (int i = 54; i < 58; i++)
        {
            Item it = inventory[i];
            if (!it.IsNull && it.ammo == useAmmo && it.stack > 0)
                return it;
        }
        for (int i = 0; i < 54; i++)
        {
            Item it = inventory[i];
            if (!it.IsNull && it.ammo == useAmmo && it.stack > 0)
                return it;
        }
        return default;
    }

    static int ApplyVanillaAmmoReplacement(int itemType, int projectileType)
    {
        bool wooden = projectileType == ProjectileID.WoodenArrowFriendly;
        switch (itemType)
        {
            case ItemID.DD2BetsyBow: return ProjectileID.DD2BetsyArrow;
            case ItemID.BloodRainBow: return ProjectileID.BloodArrow;
            case ItemID.FairyQueenRangedItem: return wooden ? ProjectileID.FairyQueenRangedItemShot : projectileType;
            case ItemID.HellwingBow: return wooden ? 0 : projectileType;
            case ItemID.Marrow: return ProjectileID.BoneArrow;
            case ItemID.MoltenFury: return wooden ? ProjectileID.FireArrow : projectileType;
            case ItemID.IceBow: return ProjectileID.FrostArrow;
            case ItemID.DD2PhoenixBow: return wooden ? ProjectileID.FireArrow : projectileType;
            case ItemID.PulseBow: return ProjectileID.MartianWalkerLaser;
            case ItemID.ShadowFlameBow: return ProjectileID.ShadowFlameArrow;
            case ItemID.BeesKnees: return wooden ? ProjectileID.BeeArrow : projectileType;
        }
        return projectileType;
    }

    static readonly HashSet<int> glowingArrows = new()
    {
        ProjectileID.JestersArrow, ProjectileID.HolyArrow, ProjectileID.FireArrow, ProjectileID.FrostburnArrow,
        ProjectileID.CursedArrow, ProjectileID.IchorArrow, ProjectileID.ShimmerArrow,
    };

    static bool ArrowDrawData(Player player, int projectileType, in PoseData itemData, out PoseData arrow)
    {
        arrow = default;
        Texture2D texture = ProjectileTexture(projectileType);
        if (texture.IsNull)
            return false;
        double direction = player.direction, gravDir = player.gravDir;
        arrow = itemData;
        arrow.texture = texture;
        arrow.sourceRect = new Rectangle(0, 0, texture.Width, texture.Height / ProjFrames(projectileType));

        arrow.origin = new PoseVector(texture.Width / 2, Math.Truncate(texture.Height * (0.5 - 0.175 * direction) / 2) * 2 + 1);

        double directionGrav = direction * gravDir;
        const bool isTextureBackwards = false;
        int flipOriginY = itemData.Has(SpriteEffects.FlipVertically) ? -1 : 1;
        int flippedSprite = ((flipOriginY != player.direction) != isTextureBackwards) ? -1 : 1;
        if (flippedSprite < 0)
            arrow.effect = arrow.Has(SpriteEffects.FlipVertically) ? SpriteEffects.None : SpriteEffects.FlipVertically;
        arrow.effect |= SpriteEffects.FlipHorizontally;

        Texture2D itemTexture = itemData.texture;
        PoseVector raw = PoseMath.RoundDir(new PoseVector(
            itemTexture.Width * itemData.scale.X * (0.5 + 0.375 * gravDir),
            itemTexture.Height * itemData.scale.Y * 0.5), gravDir, 1);
        double offsetX = -itemData.origin.X * itemData.scale.X + raw.X;
        double offsetY = -itemData.origin.Y * itemData.scale.Y + raw.Y;
        arrow.scale = new PoseVector(1, 1);
        Rectangle rect = itemData.sourceRect;
        if (rect.Width < rect.Height)
        {
            if (rect.Height % 4 == 0)
                offsetY += directionGrav;
            if (rect.Width % 4 != offsetX % 2)
                offsetX += gravDir;
        }
        else
        {
            offsetY -= 2 * direction * flippedSprite;
            if (rect.Height % 4 == 0)
                offsetY -= directionGrav;
        }
        double r = arrow.rotation;
        arrow.position.X += Math.Cos(r) * offsetX + Math.Sin(-r) * offsetY;
        arrow.position.Y += Math.Sin(r) * offsetX + Math.Cos(r) * offsetY;
        arrow.rotation += MH.PiOver2 * directionGrav;

        Projectile p = Classifier.GetProjectile(projectileType);
        if (p.alpha > 0)
            arrow.color = new Color(arrow.color.R, arrow.color.G, arrow.color.B, Clamp255(p.alpha));
        if (glowingArrows.Contains(projectileType))
            arrow.color = new Color(255, 255, 255, Clamp255(p.alpha));
        if (projectileType == ProjectileID.FairyQueenRangedItemShot)
        {
            arrow.origin = new PoseVector(texture.Width / 2.0, texture.Height / 2.0);
            arrow.rotation += MH.PiOver2 * directionGrav;
            p.ai[1] = player.miscCounterNormalized * 12;
            Color c = p.GetFairyQueenWeaponsColor(1, 0, null);
            arrow.color = new Color(c.R / 4 + 192, c.G / 4 + 192, c.B / 4 + 192, 0);
        }
        if (projectileType == ProjectileID.MartianWalkerLaser)
            arrow.color = new Color(192, 192, 255, 0);
        return true;
    }
}
