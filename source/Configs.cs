using CounterStrikeSharp.API.Core;
using Newtonsoft.Json.Linq;
using System.Text.Json.Serialization;

namespace Deathmatch;

public class DeathmatchConfig : BasePluginConfig
{
    [JsonPropertyName("free_for_all")] public bool g_bFFA { get; set; } = true;
    [JsonPropertyName("custom_modes")] public bool g_bCustomModes { get; set; } = true;
    [JsonPropertyName("random_selection_of_modes")] public bool g_bRandomSelectionOfModes { get; set; } = true;
    [JsonPropertyName("map_start_custom_mode")] public int g_iMapStartMode { get; set; } = 0;
    [JsonPropertyName("new_mode_countdown")] public int NewModeCountdown { get; set; } = 10;
    [JsonPropertyName("check_enemies_distance")] public bool CheckDistance { get; set; } = true;
    [JsonPropertyName("distance_from_enemies_for_respawn")] public int DistanceRespawn { get; set; } = 500;
    [JsonPropertyName("default_weapons")] public int DefaultModeWeapons { get; set; } = 2;
    [JsonPropertyName("respawn_players_after_new_mode")] public bool g_bRespawnPlayersAtNewMode { get; set; } = false;
    [JsonPropertyName("hide_round_seconds")] public bool g_bHideRoundSeconds { get; set; } = true;
    [JsonPropertyName("block_radio_messages")] public bool g_bBlockRadioMessage { get; set; } = true;
    [JsonPropertyName("remove_breakable_entities")] public bool g_bRemoveBreakableEntities { get; set; } = true;
    [JsonPropertyName("remove_decals_after_death")] public bool g_bRemoveDecals { get; set; } = true;
    [JsonPropertyName("weapons_select_shortcuts")] public string CustomShortcuts { get; set; } = "weapon_ak47:ak,weapon_m4a1:m4,weapon_awp:awp,weapon_usp_silencer:usp,weapon_glock:glock,weapon_deagle:deagle";
    [JsonPropertyName("Players Settings")] public PlayersSettings PlayersSettings { get; set; } = new PlayersSettings();
}

public class PlayersSettings
{
    [JsonPropertyName("VIP_flag")] public string VIPFlag { get; set; } = "@css/vip";
    [JsonPropertyName("respawn_time")] public float RespawnTime { get; set; } = 1.5f;
    [JsonPropertyName("VIP_respawn_time")] public float RespawnTimeVIP { get; set; } = 1.1f;
    [JsonPropertyName("spawn_protection_time")] public float ProtectionTime { get; set; } = 1.1f;
    [JsonPropertyName("VIP_spawn_protection_time")] public float ProtectionTimeVIP { get; set; } = 1.2f;
    [JsonPropertyName("reffil_ammo_kill")] public bool RefillAmmo { get; set; } = false;
    [JsonPropertyName("VIP_reffil_ammo_kill")] public bool RefillAmmoVIP { get; set; } = true;
    [JsonPropertyName("reffil_ammo_headshot")] public bool RefillAmmoHS { get; set; } = true;
    [JsonPropertyName("VIP_reffil_ammo_headshot")] public bool RefillAmmoHSVIP { get; set; } = true;
    [JsonPropertyName("refill_health_kill")] public int KillHealth { get; set; } = 20;
    [JsonPropertyName("VIP_refill_health_kill")] public int KillHealthVIP { get; set; } = 25;
    [JsonPropertyName("refill_health_headshot")] public int HeadshotHealth { get; set; } = 40;
    [JsonPropertyName("VIP_refill_health_headshot")] public int HeadshotHealthVIP { get; set; } = 50;
}
public static class Configuration
{
    public static JObject? JsonCustomModes { get; private set; }
    public static List<string> CustomCvarsList = new List<string>();
    public static void CreateOrLoadCvars(string filepath)
    {
        if (!File.Exists(filepath))
        {
            using (StreamWriter writer = new StreamWriter(filepath))
            {
                writer.Write("mp_buy_anywhere 0\nmp_buytime 0\nsv_disable_radar 1\nmp_give_player_c4 0\nmp_playercashawards 0\nmp_teamcashawards 0\nmp_weapons_allow_zeus 0\nmp_buy_allow_grenades 0\nmp_max_armor 0\nmp_freezetime 0\nmp_death_drop_grenade 0\nmp_death_drop_gun 0\nmp_death_drop_healthshot 0\nmp_drop_grenade_enable 0\nmp_death_drop_c4 0\nmp_death_drop_taser 0\nmp_defuser_allocation 0\nmp_solid_teammates 0\nmp_weapons_allow_typecount -1\nmp_hostages_max 0");
            }
            CustomCvarsList = new List<string>(File.ReadLines(filepath));
        }
        else
        {
            CustomCvarsList = new List<string>(File.ReadLines(filepath));
        }
    }

    public static void CreateOrLoadCustomModes(string filepath)
    {
        if (!File.Exists(filepath))
        {
            JObject exampleData = new JObject
            {
                ["custom_modes"] = new JObject
                {
                    ["0"] = new JObject
                    {
                        ["mode_name"] = "Default",
                        ["mode_interval"] = 300,
                        ["armor"] = 1,
                        ["only_hs"] = false,
                        ["allow_knife_damage"] = true,
                        ["random_weapons"] = false,
                        ["allow_center_message"] = false,
                        ["center_message_text"] = "",
                        ["primary_weapons"] = new JArray { "weapon_aug", "weapon_sg556", "weapon_xm1014", "weapon_ak47", "weapon_famas", "weapon_galilar", "weapon_m4a1", "weapon_m4a1_silencer", "weapon_mp5sd", "weapon_mp7", "weapon_p90" },
                        ["secondary_weapons"] = new JArray { "weapon_usp_silencer", "weapon_p250", "weapon_glock", "weapon_fiveseven", "weapon_hkp2000" }
                    },
                    ["1"] = new JObject
                    {
                        ["mode_name"] = "Only Headshot",
                        ["mode_interval"] = 300,
                        ["armor"] = 1,
                        ["only_hs"] = true,
                        ["allow_knife_damage"] = false,
                        ["random_weapons"] = false,
                        ["allow_center_message"] = true,
                        ["center_message_text"] = "<font class='fontSize-l' color='orange'>Only Headshot</font>",
                        ["primary_weapons"] = new JArray { "weapon_aug", "weapon_sg556", "weapon_xm1014", "weapon_ak47", "weapon_famas", "weapon_galilar", "weapon_m4a1", "weapon_m4a1_silencer", "weapon_mp5sd", "weapon_mp7", "weapon_p90" },
                        ["secondary_weapons"] = new JArray { "weapon_usp_silencer", "weapon_p250", "weapon_glock", "weapon_fiveseven", "weapon_hkp2000" },
                    },
                    ["2"] = new JObject
                    {
                        ["mode_name"] = "Only Deagle",
                        ["mode_interval"] = 120,
                        ["armor"] = 2,
                        ["only_hs"] = false,
                        ["allow_knife_damage"] = true,
                        ["random_weapons"] = false,
                        ["allow_center_message"] = true,
                        ["center_message_text"] = "<font class='fontSize-l' color='green'>Only Deagle</font>",
                        ["primary_weapons"] = new JArray { },
                        ["secondary_weapons"] = new JArray { "weapon_deagle" },
                    },
                    ["3"] = new JObject
                    {
                        ["mode_name"] = "Only Pistols",
                        ["mode_interval"] = 180,
                        ["armor"] = 1,
                        ["only_hs"] = false,
                        ["allow_knife_damage"] = true,
                        ["random_weapons"] = false,
                        ["allow_center_message"] = true,
                        ["center_message_text"] = "<font class='fontSize-l' color='blue'>Only Pistols</font>",
                        ["primary_weapons"] = new JArray { },
                        ["secondary_weapons"] = new JArray { "weapon_usp_silencer", "weapon_p250", "weapon_glock", "weapon_cz75a", "weapon_elite", "weapon_fiveseven", "weapon_tec9", "weapon_hkp2000" }
                    },
                    ["4"] = new JObject
                    {
                        ["mode_name"] = "Only SMG",
                        ["mode_interval"] = 200,
                        ["armor"] = 2,
                        ["only_hs"] = false,
                        ["allow_knife_damage"] = true,
                        ["random_weapons"] = true,
                        ["allow_center_message"] = true,
                        ["center_message_text"] = "<font class='fontSize-l' color='yellow'>Only SMG</font>",
                        ["primary_weapons"] = new JArray { "weapon_p90", "weapon_bizon", "weapon_mp5sd", "weapon_mp7", "weapon_mp9", "weapon_mac10", "weapon_ump45" },
                        ["secondary_weapons"] = new JArray { }
                    }
                }
            };

            File.WriteAllText(filepath, exampleData.ToString());
            var jsonData = File.ReadAllText(filepath);
            JsonCustomModes = JObject.Parse(jsonData);
        }
        else
        {
            var jsonData = File.ReadAllText(filepath);
            JsonCustomModes = JObject.Parse(jsonData);
        }

        if (JsonCustomModes != null && JsonCustomModes["custom_modes"] is JObject customModesObject)
        {
            DeathmatchCore.g_iTotalModes = customModesObject?.Count ?? 0;
            if (DeathmatchCore.g_iTotalModes == 0)
            {
                DeathmatchCore.SendConsoleMessage($"[Deathmatch] Wrong modes setup! (Deathmatch/custom_modes.json.json)", ConsoleColor.Red);
                throw new Exception($"[Deathmatch] Wrong modes setup in custom_modes.json!");
            }
        }
        else
        {
            DeathmatchCore.SendConsoleMessage($"[Deathmatch] Wrong modes setup! (Deathmatch/custom_modes.json.json)", ConsoleColor.Red);
            throw new Exception($"[Deathmatch] Wrong modes setup in custom_modes.json!");
        }
    }
}
