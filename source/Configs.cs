using CounterStrikeSharp.API.Core;
using Newtonsoft.Json.Linq;
using System.Text.Json.Serialization;

namespace Deathmatch;

public class DeathmatchConfig : BasePluginConfig
{
    [JsonPropertyName("free_for_all")] public bool g_bFFA { get; set; } = true;
    [JsonPropertyName("custom_modes")] public bool g_bCustomModes { get; set; } = true;
    [JsonPropertyName("custom_modes_interval")] public int g_iCustomModesInterval { get; set; } = 5;
    [JsonPropertyName("random_selection_of_modes")] public bool g_bRandomSelectionOfModes { get; set; } = true;
    [JsonPropertyName("map_start_custom_mode")] public int g_iMapStartMode { get; set; } = 0;
    //[JsonPropertyName("max_weapon_buys")] public int g_iMaxWeaponBuys { get; set; } = 3;
    [JsonPropertyName("spawn_protection_time")] public int g_iProtectionTime { get; set; } = 1;
    [JsonPropertyName("round_restart_time")] public int g_iRoundRestartTime { get; set; } = 1;
    [JsonPropertyName("hide_round_seconds")] public bool g_bHideRoundSeconds { get; set; } = true;
    [JsonPropertyName("block_radio_messages")] public bool g_bBlockRadioMessage { get; set; } = true;
    [JsonPropertyName("remove_breakable_entities")] public bool g_bRemoveBreakableEntities { get; set; } = true;
    [JsonPropertyName("reffil_ammo_kill")] public bool g_bRefillAmmoKill { get; set; } = false;
    [JsonPropertyName("reffil_ammo_headshot")] public bool g_bRefillAmmoHeadshot { get; set; } = true;
    [JsonPropertyName("refill_health_kill")] public int g_iKillHealth { get; set; } = 25;
    [JsonPropertyName("refill_health_headshot")] public int g_iHeadshotHealth { get; set; } = 50;
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
                writer.Write("mp_buy_anywhere 0\nmp_buytime 0\nmp_respawn_on_death_t 1\nmp_respawn_on_death_ct 1\nsv_disable_radar 1\nmp_give_player_c4 0\nmp_playercashawards 0\nmp_teamcashawards 0\nmp_weapons_allow_zeus 0\nmp_buy_allow_grenades 0\nmp_max_armor 0\nmp_freezetime 0\nmp_death_drop_grenade 0\nmp_death_drop_gun 0\nmp_death_drop_healthshot 0\nmp_drop_grenade_enable 0\nmp_death_drop_c4 0\nmp_death_drop_taser 0\nmp_defuser_allocation 0\nmp_solid_teammates 0\nmp_weapons_allow_typecount -1\nmp_hostages_max 0");
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
                        ["armor"] = 1,
                        ["only_hs"] = false,
                        ["allow_knife_damage"] = true,
                        ["random_weapons"] = false,
                        ["allow_center_message"] = false,
                        ["center_message_text"] = "",
                        ["primary_weapons"] = new JArray{ "weapon_aug", "weapon_sg556", "weapon_xm1014", "weapon_ak47", "weapon_famas", "weapon_galilar", "weapon_m4a1", "weapon_mp5sd", "weapon_p90" },
                        ["secondary_weapons"] = new JArray{ "weapon_usp_silencer", "weapon_p250", "weapon_glock", "weapon_fiveseven" }
                    },
                    ["1"] = new JObject
                    {
                        ["mode_name"] = "Only Headshot",
                        ["armor"] = 1,
                        ["only_hs"] = true,
                        ["allow_knife_damage"] = false,
                        ["random_weapons"] = false,
                        ["allow_center_message"] = true,
                        ["center_message_text"] = "<font class='fontSize-l' color='orange'>Only Headshot</font>",
                        ["primary_weapons"] = new JArray{ "weapon_aug", "weapon_sg556", "weapon_xm1014", "weapon_ak47", "weapon_famas", "weapon_galilar", "weapon_m4a1", "weapon_mp5sd", "weapon_p90" },
                        ["secondary_weapons"] = new JArray{ "weapon_usp_silencer", "weapon_p250", "weapon_glock", "weapon_fiveseven" },
                    },
                    ["2"] = new JObject
                    {
                        ["mode_name"] = "Only Deagle",
                        ["armor"] = 2,
                        ["only_hs"] = false,
                        ["allow_knife_damage"] = true,
                        ["random_weapons"] = false,
                        ["allow_center_message"] = true,
                        ["center_message_text"] = "<font class='fontSize-l' color='green'>Only Deagle</font>",
                        ["primary_weapons"] = new JArray{ },
                        ["secondary_weapons"] = new JArray{ "weapon_deagle" },
                    },
                    ["3"] = new JObject
                    {
                        ["mode_name"] = "Only Pistols",
                        ["armor"] = 1,
                        ["only_hs"] = false,
                        ["allow_knife_damage"] = true,
                        ["random_weapons"] = false,
                        ["allow_center_message"] = true,
                        ["center_message_text"] = "<font class='fontSize-l' color='blue'>Only Pistols</font>",
                        ["primary_weapons"] = new JArray{ },
                        ["secondary_weapons"] = new JArray{ "weapon_usp_silencer", "weapon_p250", "weapon_glock", "weapon_cz75a", "weapon_elite", "weapon_fiveseven", "weapon_tec9" }
                    },
                    ["4"] = new JObject
                    {
                        ["mode_name"] = "Only SMG",
                        ["armor"] = 2,
                        ["only_hs"] = false,
                        ["allow_knife_damage"] = true,
                        ["random_weapons"] = true,
                        ["allow_center_message"] = true,
                        ["center_message_text"] = "<font class='fontSize-l' color='yellow'>Only SMG</font>",
                        ["primary_weapons"] = new JArray{ "weapon_p90", "weapon_bizon", "weapon_mp5sd", "weapon_mp7", "weapon_mp9", "weapon_mac10", "weapon_ump45" },
                        ["secondary_weapons"] = new JArray{ }
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
            DeathmatchCore.g_iTotalModes = (int)(customModesObject?.Count ?? 0);
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
