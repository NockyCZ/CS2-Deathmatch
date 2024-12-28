using DeathmatchAPI.Helpers;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace Deathmatch
{
    public partial class Deathmatch
    {
        public class SpawnData
        {
            public required string team { get; set; }
            public required string pos { get; set; }
            public required string angle { get; set; }
        }

        public class DeathmatchPlayerData
        {
            public Dictionary<string, string> PrimaryWeapon { get; set; } = new();
            public Dictionary<string, string> SecondaryWeapon { get; set; } = new();
            public Dictionary<string, bool> Preferences { get; set; } = new();
            public Dictionary<int, DamageData> DamageInfo { get; set; } = new();
            public bool SpawnProtection { get; set; } = false;
            public int KillStreak { get; set; } = 0;
            public float BlockRandomWeaponsIntegeration { get; set; } = 0;
        }

        public class DamageData
        {
            public int Damage { get; set; } = 0;
            public int Hits { get; set; } = 0;
        }

        public class RestrictData
        {
            public int CT { get; set; }
            public int T { get; set; }
            public int Global { get; set; }
        }
    }
}