using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Cvars;
using Newtonsoft.Json.Linq;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Menu;

namespace Deathmatch;

[MinimumApiVersion(178)]
public partial class DeathmatchCore : BasePlugin, IPluginConfig<DeathmatchConfig>
{
    public override string ModuleName => "Deathmatch Core";
    public override string ModuleAuthor => "Nocky";
    public override string ModuleVersion => "1.1.1";
    public static DeathmatchCore Instance { get; set; } = new();
    public class ModeInfo
    {
        public string Name { get; set; } = "";
        public int Interval { get; set; } = 1;
        public int Armor { get; set; } = 1;
        public bool OnlyHS { get; set; } = false;
        public bool KnifeDamage { get; set; } = true;
        public bool RandomWeapons { get; set; } = true;
        public string CenterMessageText { get; set; } = "";
    }

    public enum AcquireResult : int
    {
        Allowed = 0,
        InvalidItem,
        AlreadyOwned,
        AlreadyPurchased,
        ReachedGrenadeTypeLimit,
        ReachedGrenadeTotalLimit,
        NotAllowedByTeam,
        NotAllowedByMap,
        NotAllowedByMode,
        NotAllowedForPurchase,
        NotAllowedByProhibition,
    };

    public enum AcquireMethod : int
    {
        PickUp = 0,
        Buy,
    };

    public static CounterStrikeSharp.API.Modules.Timers.Timer? modeTimer;
    internal static PlayerCache<DeathmatchPlayerData> playerData = new PlayerCache<DeathmatchPlayerData>();
    public static ModeInfo ModeData = new ModeInfo();
    public DeathmatchConfig Config { get; set; } = null!;
    public static int g_iTotalModes = 0;
    public static int g_iTotalCTSpawns = 0;
    public static int g_iTotalTSpawns = 0;
    public static int g_iActiveMode = 0;
    public static int g_iModeTimer = 0;
    public static int g_iRemainingTime = 500;
    public static bool g_bIsPrimarySet = false;
    public static bool g_bIsActiveEditor = false;
    public static bool g_bDefaultMapSpawnDisabled = false;
    public static bool g_bWeaponRestrictGlobal;
    public static bool IsCasualGamemode;

    public MemoryFunctionWithReturn<CCSPlayer_ItemServices, CEconItemView, AcquireMethod, NativeObject, AcquireResult>? CCSPlayer_CanAcquireFunc;
    public MemoryFunctionWithReturn<int, string, CCSWeaponBaseVData>? GetCSWeaponDataFromKeyFunc;

    HashSet<string> RadioMessagesList = new HashSet<string> {
        "coverme", "takepoint", "holdpos", "followme",
        "regroup", "takingfire", "go", "fallback",
        "enemydown", "sticktog", "stormfront", "cheer",
        "compliment", "thanks", "roger", "enemyspot",
        "needbackup", "sectorclear", "inposition", "negative",
        "report", "getout" };

    public void OnConfigParsed(DeathmatchConfig config)
    {
        Config = config;
    }
    public override void Load(bool hotReload)
    {
        GetCSWeaponDataFromKeyFunc = new(GameData.GetSignature("GetCSWeaponDataFromKey"));
        CCSPlayer_CanAcquireFunc = new(GameData.GetSignature("CCSPlayer_CanAcquire"));
        CCSPlayer_CanAcquireFunc.Hook(OnWeaponCanAcquire, HookMode.Pre);
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
        Configuration.CreateOrLoadCustomModes(ModuleDirectory + "/custom_modes.json");
        Configuration.CreateOrLoadCvars(ModuleDirectory + "/deathmatch_cvars.txt");
        LoadWeaponsRestrict(ModuleDirectory + "/weapons_restrict.json");

        customShortcuts.Clear();
        string[] Shortcuts = Config.CustomCommands.CustomShortcuts.Split(',');
        string[] WSelect = Config.CustomCommands.WeaponSelectCmds.Split(',');
        string[] DeathmatchMenus = Config.CustomCommands.DeatmatchMenuCmds.Split(',');
        foreach (var weapon in Shortcuts)
        {
            string[] Value = weapon.Split(':');
            if (Value.Length == 2)
            {
                customShortcuts.Add(Value[1], Value[0]);
                AddCustomCommands(Value[1], Value[0], 1);
            }
        }
        foreach (var cmd in WSelect)
            AddCustomCommands(cmd, "", 2);
        foreach (var cmd in DeathmatchMenus)
            AddCustomCommands(cmd, "", 3);
        foreach (string radioName in RadioMessagesList)
            AddCommandListener(radioName, OnPlayerRadioMessage);
        AddCommandListener("autobuy", OnRandomWeapons);

        RegisterListener<Listeners.OnMapEnd>(() => { modeTimer?.Kill(); });
        RegisterListener<Listeners.OnMapStart>(mapName =>
        {
            g_bDefaultMapSpawnDisabled = false;
            Server.NextFrame(() =>
            {
                if (Config.Gameplay.IsCustomModes)
                {
                    modeTimer?.Kill();
                    modeTimer = AddTimer(1.0f, () =>
                    {
                        if (!GameRules().WarmupPeriod)
                        {
                            g_iModeTimer++;
                            g_iRemainingTime = ModeData.Interval - g_iModeTimer;
                            if (g_iRemainingTime == 0)
                            {
                                var mode = GetModeType().ToString();
                                SetupCustomMode(mode);
                            }
                        }
                    }, TimerFlags.REPEAT);
                }
                if (Config.General.ForceMapEnd)
                {
                    bool RoundTerminated = false;
                    var timelimit = ConVar.Find("mp_timelimit")!.GetPrimitiveValue<float>() * 60;
                    AddTimer(10.0f, () =>
                    {
                        var gameStart = GameRules().GameStartTime;
                        var currentTime = Server.CurrentTime;
                        var timeleft = timelimit - (currentTime - gameStart);
                        if (timeleft <= 0 && !RoundTerminated)
                        {
                            RoundTerminated = true;
                            GameRules().TerminateRound(0.1f, RoundEndReason.RoundDraw);
                        }

                    }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
                }
                RemoveEntities();
                SetupDeathMatchConfigValues();
                SetupCustomMode(Config.Gameplay.MapStartMode.ToString());
                SetupDeathmatchMenus();
                LoadMapSpawns(ModuleDirectory + $"/spawns/{mapName}.json", true);
            });
        });
        RegisterListener<Listeners.OnTick>(() =>
        {
            if (g_bIsActiveEditor)
            {
                foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && AdminManager.PlayerHasPermissions(p, "@css/root")))
                {
                    string CTSpawns = $"<font class='fontSize-m' color='cyan'>CT Spawns:</font> <font class='fontSize-m' color='green'>{g_iTotalCTSpawns}</font>";
                    string TSpawns = $"<font class='fontSize-m' color='orange'>T Spawns:</font> <font class='fontSize-m' color='green'>{g_iTotalTSpawns}</font>";
                    p.PrintToCenterHtml($"<font class='fontSize-l' color='red'>Spawns Editor</font><br>{CTSpawns}<br>{TSpawns}<br>");
                }
            }
            else
            {
                foreach (var p in Utilities.GetPlayers().Where(p => playerData.ContainsPlayer(p) && playerData[p].HudMessages))
                {
                    if (!string.IsNullOrEmpty(ModeData.CenterMessageText) && MenuManager.GetActiveMenu(p) == null)
                    {
                        p.PrintToCenterHtml($"{ModeData.CenterMessageText}");
                    }
                    if (g_iRemainingTime <= Config.Gameplay.NewModeCountdown && Config.Gameplay.NewModeCountdown > 0)
                    {
                        if (g_iRemainingTime == 0)
                        {
                            p.PrintToCenter($"{Localizer["New_Mode_Started"]}");
                        }
                        else
                        {
                            p.PrintToCenter($"{Localizer["New_Mode_Starts_In", g_iRemainingTime]}");
                        }
                    }
                }
            }
        });
    }
    public override void Unload(bool hotReload)
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);
        CCSPlayer_CanAcquireFunc?.Unhook(OnWeaponCanAcquire, HookMode.Pre);
        playerData.Clear();
        AllowedPrimaryWeaponsList.Clear();
        AllowedSecondaryWeaponsList.Clear();
    }
    public void SetupCustomMode(string modetype)
    {
        bool bNewmode = true;
        AllowedSecondaryWeaponsList.Clear();
        AllowedPrimaryWeaponsList.Clear();
        RestrictedWeapons.Clear();
        if (modetype == g_iActiveMode.ToString())
            bNewmode = false;

        if (Configuration.JsonCustomModes != null && Configuration.JsonCustomModes.TryGetValue(propertyName: "custom_modes", out var data) && data is JObject dataObject)
        {
            if (dataObject.TryGetValue(modetype, out var modeValue) && modeValue is JObject)
            {
                ModeData.Name = modeValue["mode_name"]?.ToString() ?? $"{modetype}";
                ModeData.Interval = modeValue["mode_interval"]?.Value<int>() ?? 500;
                ModeData.Armor = modeValue["armor"]?.Value<int>() ?? 1;
                ModeData.OnlyHS = modeValue["only_hs"]?.Value<bool>() ?? false;
                ModeData.KnifeDamage = modeValue["allow_knife_damage"]?.Value<bool>() ?? true;
                ModeData.RandomWeapons = modeValue["random_weapons"]?.Value<bool>() ?? false;
                ModeData.CenterMessageText = modeValue["center_message_text"]?.ToString() ?? "";
                g_iActiveMode = int.Parse(modetype);

                if (ModeData.Armor < 0 || ModeData.Armor > 2)
                {
                    SendConsoleMessage($"[Deathmatch] Wrong value in Armor! (Mode ID: {modetype}) | Allowed options: 0 , 1 , 2", ConsoleColor.Red);
                    throw new Exception($"[Deathmatch] Wrong value in Armor! (Mode ID: {modetype}) | Allowed options: 0 , 1 , 2");
                }

                JArray primaryWeaponsArray = (JArray)modeValue["primary_weapons"]!;
                JArray secondaryWeaponsArray = (JArray)modeValue["secondary_weapons"]!;
                if (primaryWeaponsArray != null && primaryWeaponsArray.Count > 0)
                {
                    foreach (string? weapon in primaryWeaponsArray)
                    {
                        AllowedPrimaryWeaponsList.Add(weapon!);
                    }
                    CheckIsValidWeaponsInList(AllowedPrimaryWeaponsList, PrimaryWeaponsList);
                }
                if (secondaryWeaponsArray != null && secondaryWeaponsArray.Count > 0)
                {
                    foreach (string? weapon in secondaryWeaponsArray)
                    {
                        AllowedSecondaryWeaponsList.Add(weapon!);
                    }
                    CheckIsValidWeaponsInList(AllowedSecondaryWeaponsList, SecondaryWeaponsList);
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
        g_iModeTimer = 0;

        if (isNewMode)
            Server.PrintToChatAll($"{Localizer["Prefix"]} {Localizer["New_mode", ModeData.Name]}");

        Server.ExecuteCommand($"mp_free_armor {ModeData.Armor};mp_damage_headshot_only {ModeData.OnlyHS};mp_ct_default_primary \"\";mp_t_default_primary \"\";mp_ct_default_secondary \"\";mp_t_default_secondary \"\"");

        foreach (var p in Utilities.GetPlayers().Where(p => p != null && p.IsValid && p.PawnIsAlive))
        {
            p.RemoveWeapons();
            GivePlayerWeapons(p, true);
            if (ModeData.Armor != 0)
            {
                string armor = ModeData.Armor == 1 ? "item_kevlar" : "item_assaultsuit";
                p.GiveNamedItem(armor);
            }
            if (!p.IsBot)
            {
                if (!string.IsNullOrEmpty(Config.SoundSettings.NewModeSound))
                    p.ExecuteClientCommand("play " + Config.SoundSettings.NewModeSound);
                p.GiveNamedItem("weapon_knife");
            }
            if (Config.Gameplay.RespawnPlayersAtNewMode)
                p.Respawn();
        }
    }
    public void SetupDeathMatchConfigValues()
    {
        var gameType = ConVar.Find("game_type")!.GetPrimitiveValue<int>();
        Server.PrintToConsole($"gametype {gameType}");
        IsCasualGamemode = gameType != 1;

        var iHideSecond = Config.General.HideRoundSeconds ? 1 : 0;
        var time = ConVar.Find("mp_timelimit")!.GetPrimitiveValue<float>();
        var iFFA = Config.Gameplay.IsFFA ? 1 : 0;
        Server.ExecuteCommand($"mp_teammates_are_enemies {iFFA};sv_hide_roundtime_until_seconds {iHideSecond};mp_roundtime_defuse {time};mp_roundtime {time};mp_roundtime_deployment {time};mp_roundtime_hostage {time};mp_respawn_on_death_ct 1;mp_respawn_on_death_t 1");
        foreach (var cvar in Configuration.CustomCvarsList)
        {
            Server.ExecuteCommand(cvar);
        }
        if (Config.Gameplay.AllowBuyMenu)
            Server.ExecuteCommand("mp_buy_anywhere 1;mp_buytime 60000;mp_buy_during_immunity 0");
        else
            Server.ExecuteCommand("mp_buy_anywhere 0;mp_buytime 0;mp_buy_during_immunity 0");

        if (!IsCasualGamemode)
        {
            var TeamMode = Config.Gameplay.IsFFA ? 0 : 1;
            Server.ExecuteCommand($"mp_dm_teammode {TeamMode}; mp_dm_bonus_length_max 0;mp_dm_bonus_length_min 0;mp_dm_time_between_bonus_max 9999;mp_dm_time_between_bonus_min 9999;mp_respawn_immunitytime 0");
        }
    }
    public int GetModeType()
    {
        if (Config.Gameplay.IsCustomModes)
        {
            if (Config.Gameplay.RandomSelectionOfModes)
            {
                Random random = new Random();
                int iRandomMode;
                do
                {
                    iRandomMode = random.Next(0, g_iTotalModes);
                } while (iRandomMode == g_iActiveMode);
                return iRandomMode;
            }
            else
            {
                if (g_iActiveMode + 1 != g_iTotalModes && g_iActiveMode + 1 < g_iTotalModes)
                    return g_iActiveMode + 1;
                return 0;
            }
        }
        return Config.Gameplay.MapStartMode;
    }
    public static void SendConsoleMessage(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }
}