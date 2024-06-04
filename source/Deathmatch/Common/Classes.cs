using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace Deathmatch
{
    public partial class Deathmatch
    {
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