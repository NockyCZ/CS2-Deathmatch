namespace DeathmatchAPI.Helpers;

public class ModeData
{
    public required string Name { get; set; }
    public required int Interval { get; set; }
    public required int Armor { get; set; }
    public required bool OnlyHS { get; set; }
    public required bool KnifeDamage { get; set; }
    public required bool RandomWeapons { get; set; }
    public required string CenterMessageText { get; set; }
    public List<string> PrimaryWeapons { get; set; } = new();
    public List<string> SecondaryWeapons { get; set; } = new();
    public List<string> Utilities { get; set; } = new();
    public List<string> ExecuteCommands { get; set; } = new();
}