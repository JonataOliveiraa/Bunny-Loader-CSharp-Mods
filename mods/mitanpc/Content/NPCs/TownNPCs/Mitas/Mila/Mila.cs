using mitanpc.Common;
using mitanpc.Content.Items.PermanentBoosters;
using mitanpc.Content.Items.Weapons.Magic;
using Terraria.ModLoader;

namespace mitanpc.Content.NPCs.TownNPCs.Mitas.Mila;

[AutoloadHead]
public class Mila : TownNPCBase
{
    public override void AddShops()
    {
        new NPCShop(Type, "Shop")
            .Add<MilasBook>()
            .Add<MartialArtsManual>()
            .Add<ShootersManual>()
            .Add<PokemonManual>()
            .Add<MagiciansManual>()
            .Register();
    }
}
