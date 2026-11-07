using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace terrarchitect.Content.Tiles;

public class Doorbell : ModTile
{
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileNoAttach[Type] = true;
        Main.tileLavaDeath[Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.StyleTorch);
        TileObjectData.newTile.StyleHorizontal = true;
        TileObjectData.newTile.Width = 2;
        TileObjectData.newTile.Height = 2;
        TileObjectData.newTile.CoordinateWidth = 16;
        TileObjectData.newTile.CoordinateHeights = new[] { 16, 16 };
        TileObjectData.newTile.UsesCustomCanPlace = true;
        TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
        TileObjectData.newTile.AnchorTop = AnchorData.Empty;
        TileObjectData.newTile.AnchorLeft = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide, 2, 0);
        TileObjectData.newTile.AnchorRight = AnchorData.Empty;
        TileObjectData.newAlternate.CopyFrom(TileObjectData.StyleTorch);
        TileObjectData.newAlternate.Width = 2;
        TileObjectData.newAlternate.Height = 2;
        TileObjectData.newAlternate.CoordinateWidth = 16;
        TileObjectData.newAlternate.CoordinateHeights = new[] { 16, 16 };
        TileObjectData.newAlternate.UsesCustomCanPlace = true;
        TileObjectData.newAlternate.AnchorBottom = AnchorData.Empty;
        TileObjectData.newAlternate.AnchorTop = AnchorData.Empty;
        TileObjectData.newAlternate.AnchorLeft = AnchorData.Empty;
        TileObjectData.newAlternate.AnchorRight = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide, 2, 0);
        TileObjectData.addAlternate(1);
        TileObjectData.addTile(Type);
        TileID.Sets.DisableSmartCursor[Type] = true;

        RegisterItemDrop(ModContent.ItemType<Items.DoorbellItem>());
    }
}
