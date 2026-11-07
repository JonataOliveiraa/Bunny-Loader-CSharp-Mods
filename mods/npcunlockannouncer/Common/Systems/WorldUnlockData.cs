using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace NPCUnlockAnnouncerMod.Common.Systems;

public class WorldUnlockData : ModSystem
{
    static readonly Dictionary<string, int> counts = new();

    public override void OnWorldLoad() => counts.Clear();

    public override void OnWorldUnload() => counts.Clear();

    public override void SaveWorldData(TagCompound tag)
    {
        var compound = new TagCompound();
        foreach (KeyValuePair<string, int> pair in counts)
            compound.Add(pair.Key, pair.Value);
        tag.Add("NPCAnnouncementCounts", compound);
        tag.Add("UnlockedNPCs", counts.Where(pair => pair.Value > 0).Select(pair => pair.Key).ToList());
    }

    public override void LoadWorldData(TagCompound tag)
    {
        counts.Clear();
        if (tag.ContainsKey("NPCAnnouncementCounts"))
        {
            TagCompound compound = tag.GetCompound("NPCAnnouncementCounts");
            foreach (KeyValuePair<string, object> pair in compound)
                counts[pair.Key] = compound.GetInt(pair.Key);
            return;
        }
        foreach (string key in tag.GetList<string>("UnlockedNPCs"))
            counts[key] = 1;
    }

    public static int Count(string key) => counts.TryGetValue(key, out int count) ? count : 0;

    public static void Increment(string key) => counts[key] = Count(key) + 1;
}
