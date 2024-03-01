using CounterStrikeSharp.API.Core;
using Newtonsoft.Json.Linq;
using System.Text.Json.Serialization;

namespace Deathmatch;

public class DeathmatchConfig : BasePluginConfig
{
    [JsonPropertyName("Gameplay Settings")] public Gameplay Gameplay { get; set; } = new Gameplay();
    [JsonPropertyName("General Settings")] public General General { get; set; } = new General();
    [JsonPropertyName("Sounds Settings")] public SoundSettings SoundSettings { get; set; } = new SoundSettings();
    [JsonPropertyName("Custom Commands")] public CustomCommands CustomCommands { get; set; } = new CustomCommands();
    [JsonPropertyName("Players Gameplay Settings")] public PlayersSettings PlayersSettings { get; set; } = new PlayersSettings();
    [JsonPropertyName("Client Preferences")] public PlayersPreferences PlayersPreferences { get; set; } = new PlayersPreferences();

    // BLOCK WEAPON BUY
    // sounds/ui/weapon_cant_buy.vsnd_c
    // sounds/buttons/button8.vsnd_c
}

public class SoundSettings
{
    [JsonPropertyName("Weapon Cant Equip Sound")] public string CantEquipSound { get; set; } = "sounds/ui/weapon_cant_buy.vsnd_c";
    [JsonPropertyName("New Mode Sound")] public string NewModeSound { get; set; } = "sounds/music/3kliksphilip_01/bombtenseccount.vsnd_c";
    //sounds/music/3kliksphilip_01/bombtenseccount.vsnd_c
    //sounds/music/halflife_alyx_01/bombplanted.vsnd_c
}
public class Gameplay
{
    [JsonPropertyName("Free For All")] public bool IsFFA { get; set; } = true;
    [JsonPropertyName("Custom Modes")] public bool IsCustomModes { get; set; } = true;
    [JsonPropertyName("Random Selection Of Modes")] public bool RandomSelectionOfModes { get; set; } = true;
    [JsonPropertyName("Map Start Custom Mode")] public int MapStartMode { get; set; } = 0;
    [JsonPropertyName("New Mode Countdown")] public int NewModeCountdown { get; set; } = 10;
    [JsonPropertyName("Check Enemies Distance")] public bool CheckDistance { get; set; } = true;
    [JsonPropertyName("Distance From Enemies for Respawn")] public int DistanceRespawn { get; set; } = 500;
    [JsonPropertyName("Default Weapons")] public int DefaultModeWeapons { get; set; } = 2;
    [JsonPropertyName("Switch Weapons")] public bool SwitchWeapons { get; set; } = true;
    [JsonPropertyName("Allow Buymenu")] public bool AllowBuyMenu { get; set; } = true;
    [JsonPropertyName("Use Default Spawns")] public bool DefaultSpawns { get; set; } = false;
    [JsonPropertyName("Respawn Players After New Mode")] public bool RespawnPlayersAtNewMode { get; set; } = false;
}
public class General
{
    [JsonPropertyName("Hide Round Seconds")] public bool HideRoundSeconds { get; set; } = true;
    [JsonPropertyName("Block Radio Messages")] public bool BlockRadioMessage { get; set; } = true;
    [JsonPropertyName("Remove Breakable Entities")] public bool RemoveBreakableEntities { get; set; } = true;
    [JsonPropertyName("Remove Decals After Death")] public bool RemoveDecals { get; set; } = true;
    [JsonPropertyName("Force Map End")] public bool ForceMapEnd { get; set; } = false;
}
public class CustomCommands
{
    [JsonPropertyName("Deatmatch Menu Commands")] public string DeatmatchMenuCmds { get; set; } = "dm,deathmatch";
    [JsonPropertyName("Weapons Select Commands")] public string WeaponSelectCmds { get; set; } = "gun,weapon,w,g";
    [JsonPropertyName("Weapons Select Shortcuts")] public string CustomShortcuts { get; set; } = "weapon_ak47:ak,weapon_m4a1:m4,weapon_awp:awp,weapon_usp_silencer:usp,weapon_glock:glock,weapon_deagle:deagle";
}
public class PlayersPreferences
{
    [JsonPropertyName("Kill Sound")] public KillSound KillSound { get; set; } = new KillSound();
    [JsonPropertyName("Headshot Kill Sound")] public HSKillSound HSKillSound { get; set; } = new HSKillSound();
    [JsonPropertyName("Knife Kill Sound")] public KnifeKillSound KnifeKillSound { get; set; } = new KnifeKillSound();
    [JsonPropertyName("Hit Sound")] public HitSound HitSound { get; set; } = new HitSound();
    [JsonPropertyName("Only Headshot")] public OnlyHS OnlyHS { get; set; } = new OnlyHS();
    [JsonPropertyName("Hud Messages")] public HudMessages HudMessages { get; set; } = new HudMessages();
}
// sounds/ui/beepclear.vsnd_c

public class OnlyHS
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("Default value")] public bool DefaultValue { get; set; } = false;
    [JsonPropertyName("Only for VIP")] public bool OnlyVIP { get; set; } = false;
}

public class HudMessages
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("Default value")] public bool DefaultValue { get; set; } = true;
    [JsonPropertyName("Only for VIP")] public bool OnlyVIP { get; set; } = false;
}
public class HitSound
{
    // sounds/ui/animations/foley_general_grab.vsnd_c
    // sounds/common/talk.vsnd_c
    // sounds/ui/csgo_ui_contract_type2.vsnd_c BEST
    // sounds/ui/buttonrollover.vsnd_c
    // sounds/ui/xp_remaining.vsnd_c
    // sounds/player/taunt_clap_01.vsnd_c

    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("Sound path")] public string Path { get; set; } = "sounds/ui/csgo_ui_contract_type2.vsnd_c";
    [JsonPropertyName("Default value")] public bool DefaultValue { get; set; } = false;
    [JsonPropertyName("Only for VIP")] public bool OnlyVIP { get; set; } = false;
}
public class KnifeKillSound
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("Sound path")] public string Path { get; set; } = "sounds/ui/armsrace_final_kill_knife.vsnd_c";
    [JsonPropertyName("Default value")] public bool DefaultValue { get; set; } = false;
    [JsonPropertyName("Only for VIP")] public bool OnlyVIP { get; set; } = false;
}
public class KillSound
{
    //sounds/training/bell_normal.vsnd_c
    //sounds/buttons/bell1.vsnd_c
    //sounds/ui/armsrace_kill_01.vsnd_c
    //sounds/ui/deathmatch_kill_bonus.vsnd_c
    //sounds/music/kill_01.vsnd_c
    //sounds/music/kill_02.vsnd_c
    //sounds/music/kill_03.vsnd_c
    //sounds/music/kill_bonus.vsnd_c
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("Sound path")] public string Path { get; set; } = "sounds/ui/armsrace_kill_01.vsnd_c";
    [JsonPropertyName("Default value")] public bool DefaultValue { get; set; } = false;
    [JsonPropertyName("Only for VIP")] public bool OnlyVIP { get; set; } = false;
}
public class HSKillSound
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("Sound path")] public string Path { get; set; } = "sounds/buttons/bell1.vsnd_c";
    [JsonPropertyName("Default value")] public bool DefaultValue { get; set; } = false;
    [JsonPropertyName("Only for VIP")] public bool OnlyVIP { get; set; } = false;
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
                writer.Write("sv_disable_radar 1\nmp_give_player_c4 0\nmp_playercashawards 0\nmp_teamcashawards 0\nmp_weapons_allow_zeus 0\nmp_buy_allow_grenades 0\nmp_max_armor 0\nmp_freezetime 0\nmp_death_drop_grenade 0\nmp_death_drop_gun 0\nmp_death_drop_healthshot 0\nmp_drop_grenade_enable 0\nmp_death_drop_c4 0\nmp_death_drop_taser 0\nmp_defuser_allocation 0\nmp_solid_teammates 1\nmp_weapons_allow_typecount -1\nmp_hostages_max 0");
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
                        ["secondary_weapons"] = new JArray { "weapon_usp_silencer", "weapon_p250", "weapon_glock", "weapon_fiveseven", "weapon_hkp2000", "weapon_deagle" }
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
                        ["secondary_weapons"] = new JArray { "weapon_usp_silencer", "weapon_p250", "weapon_glock", "weapon_fiveseven", "weapon_hkp2000", "weapon_deagle" },
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
                        ["center_message_text"] = "<font class='fontSize-l' color='yellow'>Only SMG (Random Weapons)</font>",
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
