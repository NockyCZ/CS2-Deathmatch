using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace Deathmatch
{
    public partial class Deathmatch
    {
        public Dictionary<string, ModeData> CustomModes = new();
        public Dictionary<string, Dictionary<string, Dictionary<RestrictType, RestrictData>>> RestrictedWeapons = new();
        public static Dictionary<Vector, QAngle> spawnPositionsCT = new();
        public static Dictionary<Vector, QAngle> spawnPositionsT = new();
        internal static PlayerCache<DeathmatchPlayerData> playerData = new PlayerCache<DeathmatchPlayerData>();
        public List<(string, bool, int)> PrefsMenuSounds = new();
        public List<(string, bool, int)> PrefsMenuFunctions = new();
        public List<string> AllowedPrimaryWeaponsList = new List<string>();
        public List<string> AllowedSecondaryWeaponsList = new List<string>();
        public List<CCSPlayerController> blockRandomWeaponsIntegeration = new List<CCSPlayerController>();

        readonly Dictionary<string, string> weaponSelectMapping = new Dictionary<string, string>
        {
            { "m4a4", "weapon_m4a1" },
            { "weapon_m4a1", "weapon_m4a1" },
            { "m4a1_silencer", "weapon_m4a1_silencer" },
            { "m4a1", "weapon_m4a1_silencer" }
        };

        readonly HashSet<string> SecondaryWeaponsList = new HashSet<string>
        {
            "weapon_hkp2000", "weapon_cz75a", "weapon_deagle", "weapon_elite",
            "weapon_fiveseven", "weapon_glock", "weapon_p250",
            "weapon_revolver", "weapon_tec9", "weapon_usp_silencer"
        };

        readonly HashSet<string> PrimaryWeaponsList = new HashSet<string>
        {
            "weapon_mag7", "weapon_nova", "weapon_sawedoff", "weapon_xm1014",
            "weapon_m249", "weapon_negev", "weapon_mac10", "weapon_mp5sd",
            "weapon_mp7", "weapon_mp9", "weapon_p90", "weapon_bizon",
            "weapon_ump45", "weapon_ak47", "weapon_aug", "weapon_famas",
            "weapon_galilar", "weapon_m4a1_silencer", "weapon_m4a1", "weapon_sg556",
            "weapon_awp", "weapon_g3sg1", "weapon_scar20", "weapon_ssg08"
        };

        readonly HashSet<string> RadioMessagesList = new HashSet<string>
        {
            "coverme", "takepoint", "holdpos", "followme",
            "regroup", "takingfire", "go", "fallback",
            "enemydown", "sticktog", "stormfront", "cheer",
            "compliment", "thanks", "roger", "enemyspot",
            "needbackup", "sectorclear", "inposition", "negative",
            "report", "getout"
        };
    }
}