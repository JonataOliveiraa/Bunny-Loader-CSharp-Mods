namespace DevToolMod.TL;

public static class ModLocalization
{
    public static string Translate(string key) => DevTool.Instance.GetLocalization(key).Value ?? key;
}
