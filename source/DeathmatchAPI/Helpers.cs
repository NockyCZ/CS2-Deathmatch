using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace DeathmatchAPI.Helpers;

public class ModeData
{
    public string Name { get; set; } = "";
    public int Interval { get; set; } = 300;
    public int Armor { get; set; } = 1;
    public bool OnlyHS { get; set; } = false;
    public bool KnifeDamage { get; set; } = true;
    public bool RandomWeapons { get; set; } = false;
    public string CenterMessageText { get; set; } = "";
    public List<string> PrimaryWeapons { get; set; } = new();
    public List<string> SecondaryWeapons { get; set; } = new();
    public List<string> Utilities { get; set; } = new();
    public List<string> ExecuteCommands { get; set; } = new();
}

public class SpawnData
{
    public required CsTeam Team { get; set; }
    public required Vector Position { get; set; }
    public required QAngle Angle { get; set; }
}