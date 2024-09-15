namespace DeathmatchAPI.Helpers;

public enum CategoryType
{
    FUNCTIONS,
    SOUNDS
}

public class ModeData
{
    public string Name { get; set; } = "Defaut Mode";
    public int Interval { get; set; } = 300;
    public int Armor { get; set; } = 100;
    public bool OnlyHS { get; set; } = false;
    public bool KnifeDamage { get; set; } = true;
    public bool RandomWeapons { get; set; } = false;
    public string CenterMessageText { get; set; } = "";
    public List<string> PrimaryWeapons { get; set; } = new();
    public List<string> SecondaryWeapons { get; set; } = new();
    public List<string> Utilities { get; set; } = new();
    public List<string> ExecuteCommands { get; set; } = new();
}

public class PreferencesData
{
    public required string Name { get; set; }
    public required CategoryType Category { get; set; }
    public required bool defaultValue { get; set; }
    public required bool vipOnly { get; set; }
}