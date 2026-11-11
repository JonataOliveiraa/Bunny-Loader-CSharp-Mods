using System;

namespace ModDifficultyMod.DifficultySystem.Utils;

public static class MenuLayouts
{
    static WorldCreateMenu_Layout Create
    {
        get
        {
            WorldCreateMenu_Layout instance = WorldCreateMenu_Layout.Instance;
            return !instance.IsNull ? instance : throw new InvalidOperationException("WorldCreateMenu_Layout.Instance is null");
        }
    }

    static WorldSelectMenu_Layout Select
    {
        get
        {
            WorldSelectMenu_Layout instance = WorldSelectMenu_Layout.Instance;
            return !instance.IsNull ? instance : throw new InvalidOperationException("WorldSelectMenu_Layout.Instance is null");
        }
    }

    public static TransactionButton_Layout Normal => Create.Normal;

    public static TransactionButton_Layout Expert => Create.Expert;

    public static TransactionButton_Layout Master => Create.Master;

    public static TransactionButton_Layout Journey => Create.Journey;

    public static TransactionButton_Layout DifficultyValue => Create.DifficultyValue;

    public static Panel_Layout Greyout => Create.Greyout;

    public static Panel_Layout DifficultyOptionsBacking => Create.DifficultyOptionsBacking;

    public static String_Layout DifficultyTitle => Create.DifficultyTitle;

    public static String_Layout ItemMode => Select.ItemMode;
}
