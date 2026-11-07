using System;
using System.Collections;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using WikithisMod.Common.UI;

public class Wikithis : Mod
{
    public override void Load()
    {
        ModInterface.Register(new WikiUI());

        On_WorldGen.SaveAndQuit += orig =>
        {
            ModInterface.ClearAll();
            orig();
        };

        // The slot is drawn right after the inventory's split stack control.
        GameBindings.InstallSplitStackHook(() => WikiUI.Instance.Draw());
    }

    public override void PostSetupContent() => ModInterface.SetupAll();

    static bool IsNumber(object? value) =>
        value is int or short or long or byte or sbyte or ushort or uint or ulong or float or double;

    // Call(0, string url, int[] itemTypes) or Call(1, int itemType, string url).
    public override object? Call(params object?[] args)
    {
        string command = (args.Length > 0 ? Convert.ToString(args[0]) ?? "" : "").ToLowerInvariant();
        int count = Math.Max(0, args.Length - 1);
        switch (command)
        {
            case "0":
            case "addmodurl":
            case "addurl":
            case "url":
                if (count != 2)
                    throw new ArgumentException($"Failed to add wiki: {(count > 2 ? "many" : "insufficient")} parameters at Wikithis.Call(); The method requires 3 parameters: Call(string: command, string: url, Array: itemTypes)");
                if (args[1] is not string url)
                    throw new ArgumentException("Failed to add wiki: the URL must be a string");
                if (args[2] is not IEnumerable types || args[2] is string)
                    throw new ArgumentException("Failed to add wiki: the third parameter must be an array of integers");
                foreach (object? type in types)
                {
                    if (!IsNumber(type))
                        throw new ArgumentException("Failed to add wiki: the third parameter must be an array of integers");
                }

                string oldLanguage = Language.ActiveCulture.Name!;
                LanguageManager.Instance.SetLanguage("en-US");
                foreach (object? type in types)
                {
                    int itemType = Convert.ToInt32(type);
                    WikiUI.ItemUrl[itemType] = WikiUI.DefaultSearchStr(Lang.GetItemName(itemType).Value ?? "", url);
                }
                LanguageManager.Instance.SetLanguage(oldLanguage);
                return null;

            case "1":
            case "replace":
                if (count != 2)
                    throw new ArgumentException($"Failed to replace entry: {(count > 2 ? "many" : "insufficient")} parameters at Wikithis.Call(); The method requires 3 parameters: Call(string: command, number: itemType, string: url)");
                if (!IsNumber(args[1]) || Convert.ToDouble(args[1]) < 0)
                    throw new ArgumentException("Failed to replace entry: the itemType must be a positive number");
                if (args[2] is not string newUrl)
                    throw new ArgumentException("Failed to replace entry: the url must be a string");

                int replaced = Convert.ToInt32(args[1]);
                if (!WikiUI.CheckUrl(replaced))
                    throw new ArgumentException("Failed to replace entry: item not found, you must call addmodurl before using replace");
                WikiUI.ItemUrl[replaced] = newUrl;
                return null;

            default:
                throw new ArgumentException("Failed to add wiki: unknown command; Accepted values: \"0\", \"addmodurl\", \"addurl\", \"url\", \"1\", \"replace\"");
        }
    }
}
