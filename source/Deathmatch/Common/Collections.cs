using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using DeathmatchAPI.Helpers;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace Deathmatch
{
    public partial class Deathmatch
    {
        //public static Dictionary<Vector, QAngle> spawnPositionsCT = new();
        //public static Dictionary<Vector, QAngle> spawnPositionsT = new();
        public static List<SpawnData> spawnPoints = new();

        public static Dictionary<int, Vector> blockedSpawns = new();
        public static Dictionary<int, DeathmatchPlayerData> playerData = new();
        public Dictionary<CCSPlayerController, (float timer, float currentTime)> playersWaitingForRespawn = new();
        public Dictionary<CCSPlayerController, (float timer, float currentTime)> playersWithSpawnProtection = new();
        public static HashSet<CBaseEntity> savedSpawnsModel = new();

        readonly Dictionary<string, string> weaponSelectMapping = new()
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

        readonly string[] PointsMessagesArray =
        {
            "Player_Point_Award_Killed_Enemy",
            "Player_Point_Award_Killed_Enemy_Plural",
            "Player_Point_Award_Assist_Enemy",
            "Player_Point_Award_Assist_Enemy_Plural",
            "Player_Point_Award_Killed_Enemy_Noweapon",
            "Player_Point_Award_Killed_Enemy_Noweapon_Plural",
            "Player_Point_Award_Picked_Up_Dogtag",
            "Player_Point_Award_Picked_Up_Dogtag_Plural"
        };

        readonly string[] HudMessagesArray =
        {
            "SFUI_Notice_DM_BuyMenu_RandomON",
            "SFUI_Notice_DM_BuyMenu_RandomOFF",
            "SFUI_Notice_DM_BuyMenuExpire_RandomON",
            "SFUI_Notice_DM_BuyMenuExpire_RandomOFF",
            "SFUI_Notice_DM_InvulnExpire_RandomON",
            "SFUI_Notice_DM_InvulnExpire_RandomOFF",
            "SFUI_DMPoints_BonusWeapon"
        };
    };
}