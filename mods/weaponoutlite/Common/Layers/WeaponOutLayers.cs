using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using WeaponOutLiteMod.Common.Configs;
using WeaponOutLiteMod.Common.Players;

namespace WeaponOutLiteMod.Common.Layers;

internal static class LayerHelpers
{
    public static bool Visible(in PlayerDrawSet drawInfo, int depth)
    {
        if (!WeaponOutConfig.Instance.EnableWeaponOutVisuals)
            return false;
        Player player = drawInfo.drawPlayer;
        if (WeaponOutPlayer.Of(player).DrawDepth() != depth)
            return false;
        return Renderer.CanDrawBaseDrawData(drawInfo, out _);
    }

    public static void Draw(ref PlayerDrawSet drawInfo)
    {
        if (WeaponOutConfig.Instance.EnableWeaponOutVisuals)
            Renderer.DrawPlayerItem(ref drawInfo);
    }
}

public class WeaponOutBackLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.JimsCloak);
    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => LayerHelpers.Visible(drawInfo, DrawDepthID.Back);
    protected override void Draw(ref PlayerDrawSet drawInfo) => LayerHelpers.Draw(ref drawInfo);
}

public class WeaponOutOffHandLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Skin);
    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => LayerHelpers.Visible(drawInfo, DrawDepthID.OffHand);
    protected override void Draw(ref PlayerDrawSet drawInfo) => LayerHelpers.Draw(ref drawInfo);
}

public class WeaponOutHandLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.SolarShield);
    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => LayerHelpers.Visible(drawInfo, DrawDepthID.Hand);
    protected override void Draw(ref PlayerDrawSet drawInfo) => LayerHelpers.Draw(ref drawInfo);
}

public class WeaponOutFrontLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.HandOnAcc);
    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => LayerHelpers.Visible(drawInfo, DrawDepthID.Front);
    protected override void Draw(ref PlayerDrawSet drawInfo) => LayerHelpers.Draw(ref drawInfo);
}
