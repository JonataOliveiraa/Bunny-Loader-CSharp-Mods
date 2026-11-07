using Terraria;
using Terraria.ModLoader;

public class DobroDeDano : Mod
{
    public override void Load()
    {
        // Item.SetDefaults runs once per created item, so this covers crafted,
        // dropped and stored items alike.
        On_Item.SetDefaults += (orig, self, type, variant) =>
        {
            orig(self, type, variant);
            if (self.damage > 0)
                self.damage *= 2;
        };
        Logger.Info("Double Damage: active");
    }
}
