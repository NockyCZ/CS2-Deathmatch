using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using DeathmatchAPI.Helpers;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace Deathmatch
{
    public partial class Deathmatch
    {
        public static Dictionary<string, ModeData> CustomModes = new();
        public static Dictionary<string, Dictionary<string, Dictionary<RestrictType, RestrictData>>> RestrictedWeapons = new();
        public static Dictionary<Vector, QAngle> spawnPositionsCT = new();
        public static Dictionary<Vector, QAngle> spawnPositionsT = new();
        public static Dictionary<int, Vector> blockedSpawns = new();
        public static PlayerCache<DeathmatchPlayerData> playerData = new PlayerCache<DeathmatchPlayerData>();
        public static List<PreferencesData> Preferences = new();
        public static List<CDynamicProp> savedSpawnsModel = new();
        public static List<CPointWorldText> savedSpawnsVectorText = new();

        readonly Dictionary<string, string> weaponSelectMapping = new Dictionary<string, string>
        {
            { "m4a4", "weapon_m4a1" },
            { "weapon_m4a1", "weapon_m4a1" },
            { "m4a1_silencer", "weapon_m4a1_silencer" },
            { "m4a1", "weapon_m4a1_silencer" }
        };

        readonly string[] SecondaryWeaponsList =
        {
            "weapon_hkp2000", "weapon_cz75a", "weapon_deagle", "weapon_elite",
            "weapon_fiveseven", "weapon_glock", "weapon_p250",
            "weapon_revolver", "weapon_tec9", "weapon_usp_silencer"
        };

        readonly string[] PrimaryWeaponsList =
        {
            "weapon_mag7", "weapon_nova", "weapon_sawedoff", "weapon_xm1014",
            "weapon_m249", "weapon_negev", "weapon_mac10", "weapon_mp5sd",
            "weapon_mp7", "weapon_mp9", "weapon_p90", "weapon_bizon",
            "weapon_ump45", "weapon_ak47", "weapon_aug", "weapon_famas",
            "weapon_galilar", "weapon_m4a1_silencer", "weapon_m4a1", "weapon_sg556",
            "weapon_awp", "weapon_g3sg1", "weapon_scar20", "weapon_ssg08"
        };

        readonly string[] RadioMessagesList =
        {
            "coverme", "takepoint", "holdpos", "regroup", "followme",
            "takingfire", "go", "fallback", "sticktog", "getinpos",
            "stormfront", "report", "roger", "enemyspot", "needbackup",
            "sectorclear", "inposition", "reportingin", "getout",
            "negative", "enemydown", "sorry", "cheer", "compliment",
            "thanks", "go_a", "go_b", "needrop", "deathcry"
        };
    };

}