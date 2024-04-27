using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace Deathmatch
{
    public partial class Deathmatch
    {
        public class ModeData
        {
            public required string Name { get; set; }
            public required int Interval { get; set; }
            public required int Armor { get; set; }
            public required bool OnlyHS { get; set; }
            public required bool KnifeDamage { get; set; }
            public required bool RandomWeapons { get; set; }
            public required string CenterMessageText { get; set; }
            public List<string>? PrimaryWeapons { get; set; }
            public List<string>? SecondaryWeapons { get; set; }
            public List<string>? Utilities { get; set; }
            public List<string>? ExecuteCommands { get; set; }
        }

        public class DeathmatchPlayerData
        {
            public required string PrimaryWeapon { get; set; }
            public required string SecondaryWeapon { get; set; }
            public required bool SpawnProtection { get; set; }
            public required int KillStreak { get; set; }
            public required bool KillSound { get; set; }
            public required bool HSKillSound { get; set; }
            public required bool KnifeKillSound { get; set; }
            public required bool HitSound { get; set; }
            public required bool OnlyHS { get; set; }
            public required bool HudMessages { get; set; }
            public required Vector LastSpawn { get; set; }
            public required int OpenedMenu { get; set; }
        }

        public class RestrictData
        {
            public int CT { get; set; }
            public int T { get; set; }
            public int Global { get; set; }
        }
    }
}