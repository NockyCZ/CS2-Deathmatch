using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace Deathmatch
{
    public partial class Deathmatch
    {
        public class DeathmatchPlayerData
        {
            public string PrimaryWeapon { get; set; } = "";
            public string SecondaryWeapon { get; set; } = "";
            public string LastPrimaryWeapon { get; set; } = "";
            public string LastSecondaryWeapon { get; set; } = "";
            public bool SpawnProtection { get; set; } = false;
            public int KillStreak { get; set; } = 0;
            public required bool KillSound { get; set; }
            public required bool HSKillSound { get; set; }
            public required bool KnifeKillSound { get; set; }
            public required bool HitSound { get; set; }
            public required bool OnlyHS { get; set; }
            public required bool HudMessages { get; set; }
            public Vector LastSpawn { get; set; } = new Vector();
            public int OpenedMenu { get; set; } = 0;
        }

        public class RestrictData
        {
            public int CT { get; set; }
            public int T { get; set; }
            public int Global { get; set; }
        }
    }
}