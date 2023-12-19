using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Memory;
using Newtonsoft.Json.Linq; 

namespace Deathmatch;

[MinimumApiVersion(128)]
public partial class DeathmatchCore : BasePlugin, IPluginConfig<DeathmatchConfig>
{
    public override string ModuleName => "Deathmatch Core";
    public override string ModuleAuthor => "Nocky";
    public override string ModuleVersion => "1.0";

    public class ModeInfo
	{
		public string Name { get; set; } = "";
		public int Armor { get; set; } = 1;
		public bool OnlyHS { get; set; } = false;
        public string PrimaryWeapon { get; set; } = "";
        public string SecondaryWeapon { get; set; } = "";
        public bool SelectWeapons { get; set; } = true;
        public string WeaponsType { get; set; } = "all";
        public bool KnifeDamage { get; set; } = true;
        public bool CenterMessage { get; set; } = false;
        public string CenterMessageText { get; set; } = "";
        public string BlockedWeapons { get; set; } = "";
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
    public DeathmatchConfig Config{ get; set; } = null!;
    public static int g_iTotalModes = 0;
    public static int g_iDefaultCTSpawnsTeleported = 0;
    public static int g_iDefaultTSpawnsTeleported = 0;
    public static int g_iTotalCTSpawns = 0;
    public static int g_iTotalTSpawns = 0;
    public static int g_iActiveMode = 0;
    public static bool g_bIsOnlyWeaponSet = false;
    public static bool g_bIsActiveEditor = false;
    Dictionary<string, int> WeaponsTypeMapping = new Dictionary<string, int>
    {
        { "all", 255 },
        { "pistols", 1 },
        { "smgs", 2 },
        { "rifles", 4 },
        { "shotguns", 8 },
        { "snipers", 16 },
        { "heavy", 32 }
    };
    List<string> WeaponsTypesList = new List<string> { "all", "pistols", "smgs", "rifles", "shotguns", "snipers", "heavy" };
    HashSet<string> SecondaryWeaponsList = new HashSet<string> { 
        "weapon_hkp2000", "weapon_cz75a", "weapon_deagle", "weapon_elite", 
        "weapon_fiveseven", "weapon_glock", "weapon_p250", 
        "weapon_revolver", "weapon_tec9", "weapon_usp_silencer" };
    HashSet<string> PrimaryWeaponsList = new HashSet<string> { 
        "weapon_mag7", "weapon_nova", "weapon_sawedoff", "weapon_xm1014",
        "weapon_m249", "weapon_negev", "weapon_mac_10", "weapon_mp5sd", 
        "weapon_mp7", "weapon_mp9", "weapon_p90", "weapon_bizon", 
        "weapon_ump45", "weapon_ak47", "weapon_aug", "weapon_famas", 
        "weapon_galilar", "weapon_m4a1_silencer", "weapon_m4a1", "weapon_sg556", 
        "weapon_awp", "weapon_g3sg1", "weapon_scar20", "weapon_ssg08" };

    HashSet<string> AllWeaponsList = new HashSet<string> { 
        "shotguns_weapon_mag7", "shotguns_weapon_nova", "shotguns_weapon_sawedoff", "shotguns_weapon_xm1014",
        "heavy_weapon_m249", "heavy_weapon_negev", "smgs_weapon_mac_10", "smgs_weapon_mp5sd", 
        "smgs_weapon_mp7", "smgs_weapon_mp9", "smgs_weapon_p90", "smgs_weapon_bizon", 
        "smgs_weapon_ump45", "rifles_weapon_ak47", "rifles_weapon_aug", "rifles_weapon_famas", 
        "rifles_weapon_galilar", "rifles_weapon_m4a1_silencer", "rifles_weapon_m4a1", "rifles_weapon_sg556", 
        "snipers_weapon_awp", "snipers_weapon_g3sg1", "snipers_weapon_scar20", "snipers_weapon_ssg08",
        "pistols_weapon_hkp2000", "pistols_weapon_cz75a", "pistols_weapon_deagle", "pistols_weapon_elite", 
        "pistols_weapon_fiveseven", "pistols_weapon_glock", "pistols_weapon_p250", 
        "pistols_weapon_revolver", "pistols_weapon_tec9", "pistols_weapon_usp_silencer" };

    HashSet<string> BlockedWeaponsList = new HashSet<string>();
    
    public void OnConfigParsed(DeathmatchConfig config)
    { 
        Config = config; 
    }
    public override void Load(bool hotReload)
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
        Configuration.CreateOrLoadCustomModes(ModuleDirectory + "/custom_modes.json");
        Configuration.CreateOrLoadBlockedWeapons(ModuleDirectory + "/blocked_weapons.json");
        Configuration.CreateOrLoadCvars(ModuleDirectory + "/deathmatch_cvars.txt");

        RegisterListener<Listeners.OnMapStart>(mapName =>
		{
            Server.NextFrame(() =>{
                AddTimer(1.0f, () => {
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
            if(g_bIsActiveEditor){
                foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && AdminManager.PlayerHasPermissions(p, "@css/root"))){
                    string CTSpawns = g_iTotalCTSpawns >= g_iDefaultCTSpawnsTeleported ? $"<font class='fontSize-m' color='cyan'>CT Spawns:</font> <font class='fontSize-m' color='green'>{g_iTotalCTSpawns}</font>" : $"<font class='fontSize-m' color='cyan'>CT Spawns:</font> <font class='fontSize-m' color='yellow'>{g_iTotalCTSpawns} / {g_iDefaultCTSpawnsTeleported} </font> <font class='fontSize-s' color='white'>(Not enough)</font>";
                    string TSpawns = g_iTotalTSpawns >= g_iDefaultTSpawnsTeleported ? $"<font class='fontSize-m' color='orange'>T Spawns:</font> <font class='fontSize-m' color='green'>{g_iTotalTSpawns}</font>" : $"<font class='fontSize-m' color='orange'>T Spawns:</font> <font class='fontSize-m' color='yellow'>{g_iTotalTSpawns} / {g_iDefaultTSpawnsTeleported} </font> <font class='fontSize-s' color='white'>(Not enough)</font>";
                    p.PrintToCenterHtml($"<font class='fontSize-l' color='red'>Spawns Editor</font><br>{CTSpawns}<br>{TSpawns}<br>");
                }
            }
            else if (ModeData.CenterMessage && !string.IsNullOrEmpty(ModeData.CenterMessageText)){
                foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV)){
                    p.PrintToCenterHtml($"{ModeData.CenterMessageText}");
                }
            }
        });
    }
    public override void Unload(bool hotReload)
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);
        playerData.Clear();
        BlockedWeaponsList.Clear();
    }
    public void SetupCustomMode(string modetype)
    {
        bool bNewmode = true;
        if(modetype == g_iActiveMode.ToString()){
            bNewmode = false;
        }
		if (Configuration.JsonCustomModes != null && Configuration.JsonCustomModes.TryGetValue("custom_modes", out var tags) && tags is JObject tagsObject)
		{
            if (tagsObject.TryGetValue(modetype, out var modeValue) && modeValue is JObject)
			{
                ModeData.Name = modeValue?["mode_name"]?.ToString() ?? $"{modetype}";
                string armor = modeValue?["armor"]?.ToString() ?? "1";
                ModeData.OnlyHS = modeValue?["only_hs"]?.Value<bool>() ?? false;
                ModeData.PrimaryWeapon = modeValue?["primary_weapon"]?.ToString() ?? "";
                ModeData.SecondaryWeapon = modeValue?["secondary_weapon"]?.ToString() ?? "";
                ModeData.SelectWeapons = modeValue?["allow_select_weapons"]?.Value<bool>() ?? true;
                ModeData.WeaponsType = modeValue?["weapons_type"]?.ToString() ?? "all";
                ModeData.KnifeDamage = modeValue?["allow_knife_damage"]?.Value<bool>() ?? true;
                ModeData.CenterMessage = modeValue?["allow_center_message"]?.Value<bool>() ?? false;
                ModeData.CenterMessageText = modeValue?["center_message_text"]?.ToString() ?? "";
                ModeData.BlockedWeapons = modeValue?["blocked_weapons"]?.ToString() ?? "";
                g_iActiveMode = int.Parse(modetype);;

                if(string.IsNullOrEmpty(ModeData.BlockedWeapons)){
                    BlockedWeaponsList.Clear();
                }
                else{
                    if (Configuration.JsonBlockedWeapons!["blocked_weapons"]?[ModeData.BlockedWeapons] != null){
                        JArray blockedWeaponsArray = (JArray)Configuration.JsonBlockedWeapons!["blocked_weapons"]![ModeData.BlockedWeapons]!;
                        BlockedWeaponsList = blockedWeaponsArray.ToObject<HashSet<string>>()!;
                    }
                    else{
                        SendConsoleMessage($"[Deathmatch] Invalid blocked_weapons list! (Mode ID: {modetype})", ConsoleColor.Red);
                        throw new Exception($"[Deathmatch] Invalid blocked_weapons list! (Mode ID: {modetype})");
                    }
                }
                if (int.TryParse(armor, out int armorValue)){
                    if (armorValue >= 0 && armorValue <= 2){
                        ModeData.Armor = armorValue;
                    }
                    else{
                        SendConsoleMessage($"[Deathmatch] Wrong value in Armor! (Mode ID: {modetype}) | Allowed options: 0 , 1 , 2", ConsoleColor.Red);
                        throw new Exception($"[Deathmatch] Wrong value in Armor! (Mode ID: {modetype}) | Allowed options: 0 , 1 , 2");
                    }
                }
                else{
                    SendConsoleMessage($"[Deathmatch] Wrong value in Armor! (Mode ID: {modetype}) | Allowed options: 0 , 1 , 2", ConsoleColor.Red);
                    throw new Exception($"[Deathmatch] Wrong value in Armor! (Mode ID: {modetype}) | Allowed options: 0 , 1 , 2");
                }
                if((!string.IsNullOrEmpty(ModeData.PrimaryWeapon) || !string.IsNullOrEmpty(ModeData.SecondaryWeapon)) && !WeaponsTypesList.Contains(ModeData.WeaponsType)){
                    SendConsoleMessage($"[Deathmatch] Wrong value in Weapons Type! (Mode ID: {modetype})", ConsoleColor.Red);
                    throw new Exception($"[Deathmatch] Wrong value in Weapons Type! (Mode ID: {modetype})");
                }
                if(!string.IsNullOrEmpty(ModeData.PrimaryWeapon) && !PrimaryWeaponsList.Contains(ModeData.PrimaryWeapon)){
                    SendConsoleMessage($"[Deathmatch] Wrong primary weapon name! (Mode ID: {modetype})", ConsoleColor.Red);
                    throw new Exception($"[Deathmatch] Wrong primary weapon name! (Mode ID: {modetype})");
                }
                if(!string.IsNullOrEmpty(ModeData.SecondaryWeapon) && !SecondaryWeaponsList.Contains(ModeData.SecondaryWeapon)){
                    SendConsoleMessage($"[Deathmatch] Wrong secondary weapon name! (Mode ID: {modetype})", ConsoleColor.Red);
                    throw new Exception($"[Deathmatch] Wrong secondary weapon name! (Mode ID: {modetype})");
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
    public void SetupBlockedWeapons(string type)
    {
        string[] weaponsListToReplace = { "pistols_", "smgs_", "rifles_", "shotguns_", "snipers_", "heavy_" };
        foreach (string weaponName in AllWeaponsList)
        {
            string weapon = weaponName;
            if(!weapon.Contains(type)){
                foreach (string str in weaponsListToReplace)
                {
                    weapon = weapon.Replace(str, string.Empty);
                }
                BlockedWeaponsList.Add(weapon);
                //Console.WriteLine($"{weapon} added to blocked list");
            }
        }
    }
    public void SetupDeathmatchConfiguration(bool isNewMode)
    {
        if(isNewMode){
            Server.PrintToChatAll($"{Localizer["Prefix"]} {Localizer["New_mode", ModeData.Name]}");
        }

        bool isPrimary = false;
        bool isSecondary = false;
        g_bIsOnlyWeaponSet = false;
        Server.ExecuteCommand($"mp_free_armor {ModeData.Armor};mp_damage_headshot_only {ModeData.OnlyHS}");
        if(!string.IsNullOrEmpty(ModeData.WeaponsType) && ModeData.WeaponsType != "all"){
            SetupBlockedWeapons(ModeData.WeaponsType);
        }
        if(ModeData.SelectWeapons){
            Server.ExecuteCommand("mp_buy_anywhere 1;mp_buytime 6000");
        }
        else{
            Server.ExecuteCommand("mp_buy_anywhere 0;mp_buytime 0");
        }
        if(string.IsNullOrEmpty(ModeData.PrimaryWeapon)){
            Server.ExecuteCommand("mp_ct_default_primary \"\";mp_t_default_primary \"\"");
        }
        else{
            Server.ExecuteCommand($"mp_ct_default_primary {ModeData.PrimaryWeapon};mp_t_default_primary {ModeData.PrimaryWeapon}");
            isPrimary = true;
        }
        if(string.IsNullOrEmpty(ModeData.SecondaryWeapon)){
            Server.ExecuteCommand("mp_ct_default_secondary \"\";mp_t_default_secondary \"\"");
        }
        else{
            Server.ExecuteCommand($"mp_ct_default_secondary {ModeData.SecondaryWeapon};mp_t_default_secondary {ModeData.SecondaryWeapon}");
            isSecondary = true;
        }
        if(isSecondary || isPrimary){
            g_bIsOnlyWeaponSet = true;
        }
        if (WeaponsTypeMapping.TryGetValue(ModeData.WeaponsType, out int type)){
            Server.ExecuteCommand($"mp_buy_allow_guns {type}");
        }
        else{
            Server.ExecuteCommand($"mp_buy_allow_guns 255");
        }
    }
    public void SetupDeathMatchConfigValues()
    {
        var iHideSecond = Config.g_bHideRoundSeconds ? 1 : 0;
        Server.ExecuteCommand($"sv_hide_roundtime_until_seconds {iHideSecond};mp_round_restart_delay {Config.g_iRoundRestartTime}");
        if(Config.g_bCustomModes){
            Server.ExecuteCommand($"mp_roundtime_defuse {Config.g_iCustomModesInterval};mp_roundtime {Config.g_iCustomModesInterval};mp_roundtime_deployment {Config.g_iCustomModesInterval};mp_roundtime_hostage {Config.g_iCustomModesInterval}");
        }
        else{
            Server.ExecuteCommand($"mp_roundtime_defuse 60;mp_roundtime 60;mp_roundtime_deployment 60;mp_roundtime_hostage 60");
        }
        //var iAmmo = Config.g_bUnlimitedAmmo ? 1 : 2;
        var iFFA = Config.g_bFFA ? 1 : 0;
        Server.ExecuteCommand($"mp_teammates_are_enemies {iFFA}");
    }
    public void SetupDeathMatchCvars()
    {
        foreach (var cvar in Configuration.CustomCvarsList){
            Server.ExecuteCommand(cvar);
        }
    }

    /*[ConsoleCommand("css_modeinfo", "Disable or Enable Admin tag")]
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

    [ConsoleCommand("css_dm_blockedweapons", "Show all blocked weapons for current mod")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnBlockedList_CMD(CCSPlayerController? player, CommandInfo info)
    {
        string blockedWeapons = "";
        if (BlockedWeaponsList.Count != 0){
            foreach (string weaponName in BlockedWeaponsList){
                blockedWeapons = $"{blockedWeapons}{weaponName} | ";
            }
        }
        else{
            blockedWeapons = "None blocked weapons...";
        }
        info.ReplyToCommand($"{Localizer["Prefix"]} {ChatColors.Green}BLOCKED WEAPONS FOR {ModeData.Name} (ID: {g_iActiveMode})");
        info.ReplyToCommand(blockedWeapons);
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
        info.ReplyToCommand($"{Localizer["Prefix"]} Spawn Editor has been {ChatColors.Green}{(g_bIsActiveEditor ? "Enabled":"Disabled")}");
        if(g_bIsActiveEditor){
            ShowAllSpawnPoints();
        }
        else{
            RemoveBeams();
        }
        LoadMapSpawns(ModuleDirectory + $"/spawns/{Server.MapName}.json", false);
    }

    [ConsoleCommand("css_dm_addspawn_ct", "Add the new CT spawn point")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnAddSpawnCT_CMD(CCSPlayerController? player, CommandInfo info)
    {
        if(!g_bIsActiveEditor){
            info.ReplyToCommand($"{Localizer["Prefix"]} Spawn Editor is disabled!");
            return;
        }
        if(!player!.PawnIsAlive){
            info.ReplyToCommand($"{Localizer["Prefix"]} You have to be alive to add a new spawn!");
            return;
        }
        var position = player!.PlayerPawn!.Value!.AbsOrigin!;
        var angle = player.PlayerPawn.Value.AbsRotation!;
        //Server.PrintToChatAll($"{position} | {angle}");
        AddNewSpawnPoint(ModuleDirectory + $"/spawns/{Server.MapName}.json", $"{position}", $"{angle}", "ct");
    }
    [ConsoleCommand("css_dm_addspawn_t", "Add the new T spawn point")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnAddSpawnT_CMD(CCSPlayerController? player, CommandInfo info)
    {
        if(!g_bIsActiveEditor){
            info.ReplyToCommand($"{Localizer["Prefix"]} Spawn Editor is disabled!");
            return;
        }
        if(!player!.PawnIsAlive){
            info.ReplyToCommand($"{Localizer["Prefix"]} You have to be alive to add a new spawn!");
            return;
        }
        var position = player!.PlayerPawn!.Value!.AbsOrigin!;
        var angle = player.PlayerPawn.Value.AbsRotation!;
        //Server.PrintToChatAll($"{position} | {angle}");
        AddNewSpawnPoint(ModuleDirectory + $"/spawns/{Server.MapName}.json", $"{position}", $"{angle}", "t");
    }
    [ConsoleCommand("css_dm_removespawn", "Remove the nearest spawn point")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnRemoveSpawn_CMD(CCSPlayerController? player, CommandInfo info)
    {
        if(!g_bIsActiveEditor){
            info.ReplyToCommand($"{Localizer["Prefix"]} Spawn Editor is disabled!");
            return;
        }
        if(!player!.PawnIsAlive){
            info.ReplyToCommand($"{Localizer["Prefix"]} You have to be alive to remove a spawn!");
            return;
        }
        if(g_iTotalCTSpawns < 1 && g_iTotalTSpawns < 1 ){
            info.ReplyToCommand($"{Localizer["Prefix"]} No spawns found!");
            return;
        }
        var position = player!.PlayerPawn!.Value!.AbsOrigin!;

        string nearest = GetNearestSpawnPoint(position[0], position[1], position[2]);
        player.PrintToChat($"{Localizer["Prefix"]} {ChatColors.Default}{nearest}");
    }   
}