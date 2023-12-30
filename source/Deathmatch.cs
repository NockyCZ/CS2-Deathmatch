using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Cvars;
using Newtonsoft.Json.Linq;

namespace Deathmatch;

[MinimumApiVersion(128)]
public partial class DeathmatchCore : BasePlugin, IPluginConfig<DeathmatchConfig>
{
    public override string ModuleName => "Deathmatch Core";
    public override string ModuleAuthor => "Nocky";
    public override string ModuleVersion => "1.2";

    public class ModeInfo
    {
        public string Name { get; set; } = "";
        public int Armor { get; set; } = 1;
        public bool OnlyHS { get; set; } = false;
        public bool KnifeDamage { get; set; } = true;
        public bool RandomWeapons { get; set; } = true;
        public bool CenterMessage { get; set; } = false;
        public string CenterMessageText { get; set; } = "";
    }

    public class deathmatchPlayerData
    {
        public required string primaryWeapon { get; set; }
        public required string secondaryWeapon { get; set; }
        public required bool spawnProtection { get; set; }
        public required int killStreak { get; set; }
        public required bool onlyHS { get; set; }
        public required bool killFeed { get; set; }
        //public required bool showHud { get; set; }
    }

    internal static PlayerCache<deathmatchPlayerData> playerData = new PlayerCache<deathmatchPlayerData>();
    public static ModeInfo ModeData = new ModeInfo();
    public DeathmatchConfig Config { get; set; } = null!;
    public static int g_iTotalModes = 0;
    public static int g_iDefaultCTSpawnsTeleported = 0;
    public static int g_iDefaultTSpawnsTeleported = 0;
    public static int g_iTotalCTSpawns = 0;
    public static int g_iTotalTSpawns = 0;
    public static int g_iActiveMode = 0;
    public static bool g_bIsPrimarySet = false;
    public static bool g_bIsSecondarySet = false;
    public static bool g_bIsActiveEditor = false;

    HashSet<string> SecondaryWeaponsList = new HashSet<string> {
        "weapon_hkp2000", "weapon_cz75a", "weapon_deagle", "weapon_elite",
        "weapon_fiveseven", "weapon_glock", "weapon_p250",
        "weapon_revolver", "weapon_tec9", "weapon_usp_silencer" };

    HashSet<string> PrimaryWeaponsList = new HashSet<string> {
        "weapon_mag7", "weapon_nova", "weapon_sawedoff", "weapon_xm1014",
        "weapon_m249", "weapon_negev", "weapon_mac10", "weapon_mp5sd",
        "weapon_mp7", "weapon_mp9", "weapon_p90", "weapon_bizon",
        "weapon_ump45", "weapon_ak47", "weapon_aug", "weapon_famas",
        "weapon_galilar", "weapon_m4a1_silencer", "weapon_m4a1", "weapon_sg556",
        "weapon_awp", "weapon_g3sg1", "weapon_scar20", "weapon_ssg08" };

    HashSet<string> AllWeaponsList = new HashSet<string> {
        "weapon_ak47", "weapon_m4a1_silencer", "weapon_m4a1", "weapon_sg556",
        "weapon_aug", "weapon_galilar", "weapon_famas",
        "weapon_deagle", "weapon_usp_silencer", "weapon_glock", "weapon_p250",
        "weapon_fiveseven", "weapon_cz75a", "weapon_elite",
        "weapon_revolver", "weapon_tec9", "weapon_hkp2000",
        "weapon_awp", "weapon_g3sg1", "weapon_scar20", "weapon_ssg08",
        "weapon_mac10", "weapon_mp5sd", "weapon_mp7", "weapon_mp9",
        "weapon_p90", "weapon_bizon", "weapon_ump45",
        "weapon_mag7", "weapon_nova", "weapon_sawedoff", "weapon_xm1014",
        "weapon_m249", "weapon_negev" };

    HashSet<string> RadioMessagesList = new HashSet<string> {
        "coverme", "takepoint", "holdpos", "followme",
        "regroup", "takingfire", "go", "fallback",
        "enemydown", "sticktog", "stormfront", "cheer",
        "compliment", "thanks", "roger", "enemyspot",
        "needbackup", "sectorclear", "inposition", "negative",
        "report", "getout" };

    HashSet<string> AllowedPrimaryWeaponsList = new HashSet<string>();
    HashSet<string> AllowedSecondaryWeaponsList = new HashSet<string>();

    public void OnConfigParsed(DeathmatchConfig config)
    {
        Config = config;
    }
    public override void Load(bool hotReload)
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
        Configuration.CreateOrLoadCustomModes(ModuleDirectory + "/custom_modes.json");
        Configuration.CreateOrLoadCvars(ModuleDirectory + "/deathmatch_cvars.txt");

        AddCommandListener("buy", OnPlayerBuy);
        AddCommandListener("autobuy", OnPlayerBuy);
        AddCommandListener("buymenu", OnPlayerBuy);
        foreach (string radioName in RadioMessagesList)
        {
            AddCommandListener(radioName, OnPlayerRadioMessage);
        }
        RegisterListener<Listeners.OnMapStart>(mapName =>
        {
            Server.NextFrame(() =>
            {
                AddTimer(1.0f, () =>
                {
                    RemoveEntities();
                    LoadMapSpawns(ModuleDirectory + $"/spawns/{mapName}.json", true);
                    SetupDeathMatchConfigValues();
                    SetupDeathMatchCvars();
                    SetupCustomMode(modetype: Config.g_iMapStartMode.ToString());
                });
            });
        });
        RegisterListener<Listeners.OnTick>(() =>
        {
            if (g_bIsActiveEditor)
            {
                foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && AdminManager.PlayerHasPermissions(p, "@css/root")))
                {
                    string CTSpawns = g_iTotalCTSpawns >= g_iDefaultCTSpawnsTeleported ? $"<font class='fontSize-m' color='cyan'>CT Spawns:</font> <font class='fontSize-m' color='green'>{g_iTotalCTSpawns}</font>" : $"<font class='fontSize-m' color='cyan'>CT Spawns:</font> <font class='fontSize-m' color='yellow'>{g_iTotalCTSpawns} / {g_iDefaultCTSpawnsTeleported} </font> <font class='fontSize-s' color='white'>(Not enough)</font>";
                    string TSpawns = g_iTotalTSpawns >= g_iDefaultTSpawnsTeleported ? $"<font class='fontSize-m' color='orange'>T Spawns:</font> <font class='fontSize-m' color='green'>{g_iTotalTSpawns}</font>" : $"<font class='fontSize-m' color='orange'>T Spawns:</font> <font class='fontSize-m' color='yellow'>{g_iTotalTSpawns} / {g_iDefaultTSpawnsTeleported} </font> <font class='fontSize-s' color='white'>(Not enough)</font>";
                    p.PrintToCenterHtml($"<font class='fontSize-l' color='red'>Spawns Editor</font><br>{CTSpawns}<br>{TSpawns}<br>");
                }
            }
            else if (ModeData.CenterMessage && !string.IsNullOrEmpty(ModeData.CenterMessageText))
            {
                foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV))
                {
                    p.PrintToCenterHtml($"{ModeData.CenterMessageText}");
                }
            }
        });
    }
    public override void Unload(bool hotReload)
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);
        playerData.Clear();
        AllowedPrimaryWeaponsList.Clear();
        AllowedSecondaryWeaponsList.Clear();
    }
    public void SetupCustomMode(string modetype)
    {
        bool bNewmode = true;
        AllowedSecondaryWeaponsList.Clear();
        AllowedPrimaryWeaponsList.Clear();
        if (modetype == g_iActiveMode.ToString())
        {
            bNewmode = false;
        }
        if (Configuration.JsonCustomModes != null && Configuration.JsonCustomModes.TryGetValue("custom_modes", out var tags) && tags is JObject tagsObject)
        {
            if (tagsObject.TryGetValue(modetype, out var modeValue) && modeValue is JObject)
            {
                ModeData.Name = modeValue?["mode_name"]?.ToString() ?? $"{modetype}";
                string armor = modeValue?["armor"]?.ToString() ?? "1";
                ModeData.OnlyHS = modeValue?["only_hs"]?.Value<bool>() ?? false;
                ModeData.KnifeDamage = modeValue?["allow_knife_damage"]?.Value<bool>() ?? true;
                ModeData.RandomWeapons = modeValue?["random_weapons"]?.Value<bool>() ?? false;
                ModeData.CenterMessage = modeValue?["allow_center_message"]?.Value<bool>() ?? false;
                ModeData.CenterMessageText = modeValue?["center_message_text"]?.ToString() ?? "";
                g_iActiveMode = int.Parse(modetype);

                if (int.TryParse(armor, out int armorValue))
                {
                    if (armorValue >= 0 && armorValue <= 2)
                    {
                        ModeData.Armor = armorValue;
                    }
                    else
                    {
                        SendConsoleMessage($"[Deathmatch] Wrong value in Armor! (Mode ID: {modetype}) | Allowed options: 0 , 1 , 2", ConsoleColor.Red);
                        throw new Exception($"[Deathmatch] Wrong value in Armor! (Mode ID: {modetype}) | Allowed options: 0 , 1 , 2");
                    }
                }
                else
                {
                    SendConsoleMessage($"[Deathmatch] Wrong value in Armor! (Mode ID: {modetype}) | Allowed options: 0 , 1 , 2", ConsoleColor.Red);
                    throw new Exception($"[Deathmatch] Wrong value in Armor! (Mode ID: {modetype}) | Allowed options: 0 , 1 , 2");
                }

                JArray primaryWeaponsArray = (JArray)modeValue!["primary_weapons"]!;
                JArray secondaryWeaponsArray = (JArray)modeValue["secondary_weapons"]!;
                if (primaryWeaponsArray != null && primaryWeaponsArray.Count > 0)
                {
                    foreach (string? weapon in primaryWeaponsArray)
                    {
                        AllowedPrimaryWeaponsList.Add(weapon!);
                    }
                    CheckIsValidWeaponsInList(AllowedPrimaryWeaponsList, AllWeaponsList);
                }
                if (secondaryWeaponsArray != null && secondaryWeaponsArray.Count > 0)
                {
                    foreach (string? weapon in secondaryWeaponsArray)
                    {
                        AllowedSecondaryWeaponsList.Add(weapon!);
                    }
                    CheckIsValidWeaponsInList(AllowedSecondaryWeaponsList, AllWeaponsList);
                }
                SetupDeathmatchConfiguration(bNewmode);
                return;
            }
            else
            {
                SendConsoleMessage($"[Deathmatch] Mode with id {modetype} is not found!", ConsoleColor.Red);
                throw new Exception($"[Deathmatch] Mode with id {modetype} is not found!");
            }
        }
        else
        {
            SendConsoleMessage($"[Deathmatch] Wrong code in custom_modes.json!", ConsoleColor.Red);
            throw new Exception($"[Deathmatch] Wrong code in custom_modes.json!");
        }
    }

    public void SetupDeathmatchConfiguration(bool isNewMode)
    {
        if (isNewMode)
        {
            Server.PrintToChatAll($"{Localizer["Prefix"]} {Localizer["New_mode", ModeData.Name]}");
        }
        g_bIsPrimarySet = false;
        g_bIsSecondarySet = false;
        Server.ExecuteCommand($"mp_free_armor {ModeData.Armor};mp_damage_headshot_only {ModeData.OnlyHS};mp_ct_default_primary \"\";mp_t_default_primary \"\";mp_ct_default_secondary \"\";mp_t_default_secondary \"\"");
        foreach (var p in Utilities.GetPlayers().Where(p => p is { IsValid: true, PawnIsAlive: true }))
        {
            p.RemoveWeapons();
            SetupPlayerWeapons(p, true);
        }
    }
    public void SetupDeathMatchConfigValues()
    {
        var iHideSecond = Config.g_bHideRoundSeconds ? 1 : 0;
        Server.ExecuteCommand($"sv_hide_roundtime_until_seconds {iHideSecond};mp_round_restart_delay {Config.g_iRoundRestartTime}");
        if (Config.g_bCustomModes)
        {
            Server.ExecuteCommand($"mp_roundtime_defuse {Config.g_iCustomModesInterval};mp_roundtime {Config.g_iCustomModesInterval};mp_roundtime_deployment {Config.g_iCustomModesInterval};mp_roundtime_hostage {Config.g_iCustomModesInterval}");
        }
        else
        {
            var time = ConVar.Find("mp_timelimit");
            Server.ExecuteCommand($"mp_roundtime_defuse {time};mp_roundtime {time};mp_roundtime_deployment {time};mp_roundtime_hostage {time}");
        }
        //var iAmmo = Config.g_bUnlimitedAmmo ? 1 : 2;
        var iFFA = Config.g_bFFA ? 1 : 0;
        Server.ExecuteCommand($"mp_teammates_are_enemies {iFFA}");
    }
    public void SetupDeathMatchCvars()
    {
        foreach (var cvar in Configuration.CustomCvarsList)
        {
            Server.ExecuteCommand(cvar);
        }
    }

    /*[ConsoleCommand("css_modeinfo", "Mode info")]
    public void OnModeInfoCommand(CCSPlayerController? caller, CommandInfo info)
    {
        info.ReplyToCommand($"{g_iTotalModes}");
        info.ReplyToCommand("===============================");
        info.ReplyToCommand($"Mode name: {ModeData.Name}");
        info.ReplyToCommand($"Armor: {ModeData.Armor}");
        info.ReplyToCommand($"OnlyHS: {ModeData.OnlyHS}");
        info.ReplyToCommand($"Primary Weapon: {ModeData.PrimaryWeapon}");
        info.ReplyToCommand($"Secondary Weapon: {ModeData.SecondaryWeapon}");
        info.ReplyToCommand($"Select Weapons: {ModeData.SelectWeapons}");
        info.ReplyToCommand($"Weapons Type: {ModeData.WeaponsType}");
        info.ReplyToCommand($"Knife Damage: {ModeData.KnifeDamage}");
        info.ReplyToCommand($"Blocked weapons: {ModeData.BlockedWeapons}");
        info.ReplyToCommand("===============================");
    }*/
    [ConsoleCommand("css_gun", "Select a weapon")]
    [ConsoleCommand("css_guns", "Select a weapon")]
    [ConsoleCommand("css_weapons", "Select a weapon")]
    [ConsoleCommand("css_weapon", "Select a weapon")]
    [ConsoleCommand("css_w", "Select a weapon")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnSelectGun_CMD(CCSPlayerController? player, CommandInfo info)
    {
        string weaponName = info.GetArg(1).ToLower();
        if (!IsPlayerValid(player!, false) || !playerData.ContainsPlayer(player!))
        {
            return;
        }
        if (ModeData.RandomWeapons)
        {
            info.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["Weapon_Select_Is_Disabled"]}");
            return;
        }
        if (string.IsNullOrEmpty(weaponName))
        {
            string primaryWeapons = "";
            string secondaryWeapons = "";
            if (AllowedPrimaryWeaponsList.Count != 0)
            {
                foreach (string weapon in AllowedPrimaryWeaponsList)
                {
                    if (string.IsNullOrEmpty(primaryWeapons))
                    {
                        primaryWeapons = $"{ChatColors.Green} {weapon.Replace("weapon_", "")}";
                    }
                    else
                    {
                        primaryWeapons = $"{primaryWeapons}{ChatColors.Default}, {ChatColors.Green}{weapon.Replace("weapon_", "")}";
                    }
                }
                info.ReplyToCommand($"{Localizer["Allowed_Primary_Weapons"]}");
                info.ReplyToCommand($"{ChatColors.Darkred}• {primaryWeapons}");
            }
            if (AllowedSecondaryWeaponsList.Count != 0)
            {
                foreach (string weapon in AllowedSecondaryWeaponsList)
                {
                    if (string.IsNullOrEmpty(secondaryWeapons))
                    {
                        secondaryWeapons = $"{ChatColors.Green} {weapon.Replace("weapon_", "")}";
                    }
                    else
                    {
                        secondaryWeapons = $"{secondaryWeapons}{ChatColors.Default}, {ChatColors.Green}{weapon.Replace("weapon_", "")}";
                    }
                }
                info.ReplyToCommand($"{Localizer["Allowed_Secondary_Weapons"]}");
                info.ReplyToCommand($"{ChatColors.Darkred}• {secondaryWeapons}");
            }
            info.ReplyToCommand($"{Localizer["Prefix"]} /gun <weapon name>");
            return;
        }
        if (!g_bIsPrimarySet && !g_bIsSecondarySet)
        {
            string replacedweaponName = "";
            string matchingValues = "";
            int matchingCount = 0;
            if (weaponName.Contains("m4a4"))
            {
                weaponName = "weapon_m4a1";
                matchingCount = 1;
            }
            else if (weaponName.Contains("m4a1"))
            {
                weaponName = "weapon_m4a1_silencer";
                matchingCount = 1;
            }
            else
            {
                foreach (string weapon in AllWeaponsList)
                {
                    if (weapon.Contains(weaponName))
                    {
                        replacedweaponName = weapon;
                        matchingCount++;
                        string localizerWeaponName = Localizer[replacedweaponName];
                        if (matchingCount == 1)
                        {
                            matchingValues = localizerWeaponName;
                        }
                        else if (matchingCount == 2)
                        {
                            matchingValues = $"{ChatColors.Green}{matchingValues}{ChatColors.Default}, {ChatColors.Green}{localizerWeaponName}";
                        }
                        else if (matchingCount > 2)
                        {
                            matchingValues = $"{matchingValues}{ChatColors.Default}, {ChatColors.Green}{localizerWeaponName}";
                        }
                    }
                }
                if (matchingCount != 0)
                {
                    weaponName = replacedweaponName;
                }
            }

            if (matchingCount > 1)
            {
                info.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["Multiple_Weapon_Select"]} {ChatColors.Default}( {matchingValues} {ChatColors.Default})");
                return;
            }
            else if (matchingCount == 0)
            {
                info.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["Weapon_Name_Not_Found", weaponName]}");
                return;
            }

            if (AllowedPrimaryWeaponsList.Contains(weaponName))
            {
                string localizerWeaponName = Localizer[weaponName];
                if (weaponName == playerData[player!].primaryWeapon)
                {
                    info.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["Weapon_Is_Already_Set", localizerWeaponName]}");
                    return;
                }
                playerData[player!].primaryWeapon = weaponName;
                info.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["PrimaryWeapon_Set", localizerWeaponName]}");
                if (IsHaveWeaponFromSlot(player!, 1) != 1)
                {
                    player!.GiveNamedItem(weaponName);
                }
                return;
            }
            else if (AllowedSecondaryWeaponsList.Contains(weaponName))
            {
                string localizerWeaponName = Localizer[weaponName];
                if (weaponName == playerData[player!].secondaryWeapon)
                {
                    info.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["Weapon_Is_Already_Set", localizerWeaponName]}");
                    return;
                }
                playerData[player!].secondaryWeapon = weaponName;
                info.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["SecondaryWeapon_Set", localizerWeaponName]}");
                if (IsHaveWeaponFromSlot(player!, 2) != 2)
                {
                    player!.GiveNamedItem(weaponName);
                }
                return;
            }
            else
            {
                string localizerWeaponName = Localizer[weaponName];
                info.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["Weapon_Disabled", localizerWeaponName]}");
                return;
            }
        }
        else
        {
            info.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["Weapon_Select_Is_Disabled"]}");
        }
    }

    [ConsoleCommand("css_dm_allowedweapons", "Show all allowed weapons for current mod")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnBlockedList_CMD(CCSPlayerController? player, CommandInfo info)
    {
        string primary = "";
        if (AllowedPrimaryWeaponsList.Count != 0)
        {
            foreach (string weaponName in AllowedPrimaryWeaponsList)
            {
                primary = $"{primary}{weaponName} | ";
            }
        }
        else
        {
            primary = "None allowed weapons...";
        }
        info.ReplyToCommand($"{Localizer["Prefix"]} {ChatColors.Green}PRIMARY ALLOWED WEAPONS {ModeData.Name} (ID: {g_iActiveMode})");
        info.ReplyToCommand(primary);

        string secondary = "";
        if (AllowedSecondaryWeaponsList.Count != 0)
        {
            foreach (string weaponName in AllowedSecondaryWeaponsList)
            {
                secondary = $"{secondary}{weaponName} | ";
            }
        }
        else
        {
            secondary = "None allowed weapons...";
        }
        info.ReplyToCommand($"{Localizer["Prefix"]} {ChatColors.Green}SECONDARY ALLOWED WEAPONS {ModeData.Name} (ID: {g_iActiveMode})");
        info.ReplyToCommand(secondary);
    }

    [ConsoleCommand("css_dm_startmode", "Start Custom Mode")]
    [CommandHelper(1, "<mode id>")]
    [RequiresPermissions("@css/root")]
    public void OnStartMode_CMD(CCSPlayerController? caller, CommandInfo info)
    {
        string modeid = info.GetArg(1);
        SetupCustomMode(modeid);
    }

    [ConsoleCommand("css_dm_editor", "Enable or Disable spawn points editor")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnEditor_CMD(CCSPlayerController? player, CommandInfo info)
    {
        g_bIsActiveEditor = !g_bIsActiveEditor;
        info.ReplyToCommand($"{Localizer["Prefix"]} Spawn Editor has been {ChatColors.Green}{(g_bIsActiveEditor ? "Enabled" : "Disabled")}");
        if (g_bIsActiveEditor)
        {
            ShowAllSpawnPoints();
        }
        else
        {
            RemoveBeams();
        }
        LoadMapSpawns(ModuleDirectory + $"/spawns/{Server.MapName}.json", false);
    }

    [ConsoleCommand("css_dm_addspawn_ct", "Add the new CT spawn point")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnAddSpawnCT_CMD(CCSPlayerController? player, CommandInfo info)
    {
        if (!g_bIsActiveEditor)
        {
            info.ReplyToCommand($"{Localizer["Prefix"]} Spawn Editor is disabled!");
            return;
        }
        if (player!.IsValid && !player!.PawnIsAlive)
        {
            info.ReplyToCommand($"{Localizer["Prefix"]} You have to be alive to add a new spawn!");
            return;
        }
        var position = player!.PlayerPawn!.Value!.AbsOrigin!;
        var angle = player.PlayerPawn.Value.AbsRotation!;
        AddNewSpawnPoint(ModuleDirectory + $"/spawns/{Server.MapName}.json", $"{position}", $"{angle}", "ct");
        info.ReplyToCommand($"{Localizer["Prefix"]} Spawn for the CT team has been added. (Total: {ChatColors.Green}{g_iTotalCTSpawns}{ChatColors.Default})");
    }
    [ConsoleCommand("css_dm_addspawn_t", "Add the new T spawn point")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnAddSpawnT_CMD(CCSPlayerController? player, CommandInfo info)
    {
        if (!g_bIsActiveEditor)
        {
            info.ReplyToCommand($"{Localizer["Prefix"]} Spawn Editor is disabled!");
            return;
        }
        if (player!.IsValid && !player!.PawnIsAlive)
        {
            info.ReplyToCommand($"{Localizer["Prefix"]} You have to be alive to add a new spawn!");
            return;
        }
        var position = player!.PlayerPawn!.Value!.AbsOrigin!;
        var angle = player.PlayerPawn.Value.AbsRotation!;
        AddNewSpawnPoint(ModuleDirectory + $"/spawns/{Server.MapName}.json", $"{position}", $"{angle}", "t");
        info.ReplyToCommand($"{Localizer["Prefix"]} Spawn for the T team has been added. (Total: {ChatColors.Green}{g_iTotalTSpawns}{ChatColors.Default})");
    }
    [ConsoleCommand("css_dm_removespawn", "Remove the nearest spawn point")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnRemoveSpawn_CMD(CCSPlayerController? player, CommandInfo info)
    {
        if (!g_bIsActiveEditor)
        {
            info.ReplyToCommand($"{Localizer["Prefix"]} Spawn Editor is disabled!");
            return;
        }
        if (player!.IsValid && !player!.PawnIsAlive)
        {
            info.ReplyToCommand($"{Localizer["Prefix"]} You have to be alive to remove a spawn!");
            return;
        }
        if (g_iTotalCTSpawns < 1 && g_iTotalTSpawns < 1)
        {
            info.ReplyToCommand($"{Localizer["Prefix"]} No spawns found!");
            return;
        }
        var position = player!.PlayerPawn!.Value!.AbsOrigin!;

        string deleted = GetNearestSpawnPoint(position[0], position[1], position[2]);
        player.PrintToChat($"{Localizer["Prefix"]} {ChatColors.Default}{deleted}");
    }
}