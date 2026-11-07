using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

public class HelloMod : Mod
{
    public override void Load()
    {
        On_Item.SetDefaults += (orig, self, type, variant) =>
        {
            orig(self, type, variant);
            if (type == ItemID.Minishark)
            {
                self.useTime = 4;
                self.useAnimation = 4;
                self.shootSpeed = 10f;
            }
        };
    }
}
