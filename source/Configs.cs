using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;
using static Deathmatch.Deathmatch;

namespace Deathmatch;

public class DeathmatchConfig : BasePluginConfig
{
    [JsonPropertyName("Gameplay Settings")] public Gameplay Gameplay { get; set; } = new Gameplay();
    [JsonPropertyName("General Settings")] public General General { get; set; } = new General();
    [JsonPropertyName("Sounds Settings")] public SoundSettings SoundSettings { get; set; } = new SoundSettings();
    [JsonPropertyName("Custom Commands")] public CustomCommands CustomCommands { get; set; } = new CustomCommands();
    [JsonPropertyName("Players Gameplay Settings")] public PlayersSettings PlayersSettings { get; set; } = new PlayersSettings();
    [JsonPropertyName("Client Preferences")] public PlayersPreferences PlayersPreferences { get; set; } = new PlayersPreferences();
    [JsonPropertyName("Custom Modes")] public CustomModes CustomModes { get; set; } = new CustomModes();
    [JsonPropertyName("Weapons Restrict")] public WeaponsRestrict WeaponsRestrict { get; set; } = new WeaponsRestrict();
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

public class CustomModes
{
    [JsonPropertyName("0")]
    public ModeData _0 { get; set; } = new ModeData
    {
        Name = "Default",
        Interval = 300,
        Armor = 1,
        OnlyHS = false,
        KnifeDamage = true,
        RandomWeapons = false,
        CenterMessageText = "",
        PrimaryWeapons = new List<string> {
                        "weapon_aug", "weapon_sg556", "weapon_xm1014",
                        "weapon_ak47", "weapon_famas", "weapon_galilar",
                        "weapon_m4a1", "weapon_m4a1_silencer", "weapon_mp5sd",
                        "weapon_mp7", "weapon_p90", "weapon_awp"
                    },
        SecondaryWeapons = new List<string> {
                        "weapon_usp_silencer", "weapon_p250", "weapon_glock",
                        "weapon_fiveseven", "weapon_hkp2000", "weapon_deagle"
                    },
        Utilities = new List<string> {
                        "weapon_flashbang"
                    },
        ExecuteCommands = new List<string>()
    };

    [JsonPropertyName("1")]
    public ModeData _1 { get; set; } = new ModeData
    {
        Name = "Only Headshot",
        Interval = 300,
        Armor = 1,
        OnlyHS = true,
        KnifeDamage = false,
        RandomWeapons = false,
        CenterMessageText = "<font class='fontSize-l' color='orange'>Only Headshot</font>",
        PrimaryWeapons = new List<string> {
                        "weapon_aug", "weapon_sg556", "weapon_xm1014",
                        "weapon_ak47", "weapon_famas", "weapon_galilar",
                        "weapon_m4a1", "weapon_m4a1_silencer", "weapon_mp5sd",
                        "weapon_mp7", "weapon_p90"
                    },
        SecondaryWeapons = new List<string> {
                        "weapon_usp_silencer", "weapon_p250", "weapon_glock",
                        "weapon_fiveseven", "weapon_hkp2000", "weapon_deagle"
                    },
        Utilities = new List<string>(),
        ExecuteCommands = new List<string>()
    };

    [JsonPropertyName("2")]
    public ModeData _2 { get; set; } = new ModeData
    {
        Name = "Only Deagle",
        Interval = 120,
        Armor = 2,
        OnlyHS = false,
        KnifeDamage = true,
        RandomWeapons = false,
        CenterMessageText = "<font class='fontSize-l' color='green'>Only Deagle</font>",
        PrimaryWeapons = new List<string>(),
        SecondaryWeapons = new List<string> {
                        "weapon_deagle"
                    },
        Utilities = new List<string> {
                        "weapon_flashbang" , "weapon_healthshot"
                    },
        ExecuteCommands = new List<string>()
    };

    [JsonPropertyName("3")]
    public ModeData _3 { get; set; } = new ModeData
    {
        Name = "Only Pistols",
        Interval = 180,
        Armor = 1,
        OnlyHS = false,
        KnifeDamage = true,
        RandomWeapons = false,
        CenterMessageText = "<font class='fontSize-l' color='blue'>Only Pistols</font>",
        PrimaryWeapons = new List<string>(),
        SecondaryWeapons = new List<string> {
                        "weapon_usp_silencer", "weapon_p250", "weapon_glock",
                        "weapon_cz75a", "weapon_elite", "weapon_fiveseven",
                        "weapon_tec9", "weapon_hkp2000"
                    },
        Utilities = new List<string>(),
        ExecuteCommands = new List<string>()
    };

    [JsonPropertyName("4")]
    public ModeData _4 { get; set; } = new ModeData
    {
        Name = "Only SMG",
        Interval = 200,
        Armor = 2,
        OnlyHS = false,
        KnifeDamage = true,
        RandomWeapons = true,
        CenterMessageText = "<font class='fontSize-l' color='yellow'>Only SMG (Random Weapons)</font>",
        PrimaryWeapons = new List<string> {
                        "weapon_p90", "weapon_bizon", "weapon_mp5sd",
                        "weapon_mp7", "weapon_mp9", "weapon_mac10",
                        "weapon_ump45"
                    },
        SecondaryWeapons = new List<string>(),
        Utilities = new List<string> {
                        "weapon_hegrenade", "weapon_flashbang", "weapon_healthshot"
                    },
        ExecuteCommands = new List<string>()
    };
}

public class WeaponsRestrict
{
    [JsonPropertyName("Global Restrict")] public bool Global { get; set; } = true;

    [JsonPropertyName("Weapons")]
    public Dictionary<string, Dictionary<string, Dictionary<RestrictType, RestrictData>>> DefaultRestrictions { get; set; } = new()
    {
        ["weapon_ak47"] = new Dictionary<string, Dictionary<RestrictType, RestrictData>>()
        {
            ["0"] = new Dictionary<RestrictType, RestrictData>()
            {
                [RestrictType.VIP] = new RestrictData()
                {
                    CT = 6,
                    T = 6,
                    Global = 12
                },
                [RestrictType.NonVIP] = new RestrictData()
                {
                    CT = 5,
                    T = 5,
                    Global = 10
                }
            },
            ["1"] = new Dictionary<RestrictType, RestrictData>()
            {
                [RestrictType.VIP] = new RestrictData()
                {
                    CT = 5,
                    T = 5,
                    Global = 7
                },
                [RestrictType.NonVIP] = new RestrictData()
                {
                    CT = 4,
                    T = 4,
                    Global = 5
                }
            }
        },
        ["weapon_awp"] = new Dictionary<string, Dictionary<RestrictType, RestrictData>>()
        {
            ["0"] = new Dictionary<RestrictType, RestrictData>()
            {
                [RestrictType.VIP] = new RestrictData()
                {
                    CT = 3,
                    T = 3,
                    Global = 4
                },
                [RestrictType.NonVIP] = new RestrictData()
                {
                    CT = 2,
                    T = 2,
                    Global = 3
                }
            }
        }
    };
}