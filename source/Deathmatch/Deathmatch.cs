using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Core.Capabilities;
using DeathmatchAPI.Helpers;
using static DeathmatchAPI.Events.IDeathmatchEventsAPI;
using static CounterStrikeSharp.API.Core.Listeners;

namespace Deathmatch;

public partial class Deathmatch : BasePlugin, IPluginConfig<DeathmatchConfig>
{
    public override string ModuleName => "Deathmatch Core";
    public override string ModuleAuthor => "Nocky";
    public override string ModuleVersion => "1.2.4";

    public void OnConfigParsed(DeathmatchConfig config)
    {
        Config = config;
        CheckedEnemiesDistance = Config.Gameplay.DistanceRespawn;
    }
    public override void Load(bool hotReload)
    {
        var API = new Deathmatch();
        Capabilities.RegisterPluginCapability(DeathmatchAPI, () => API);
        VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Hook(OnWeaponCanAcquire, HookMode.Pre);
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);

        if (Config.SaveWeapons)
            _ = CreateDatabaseConnection();

        SetupDeathmatchMenus();
        string[] Shortcuts = Config.CustomCommands.CustomShortcuts.Split(',');
        string[] WSelect = Config.CustomCommands.WeaponSelectCmds.Split(',');
        string[] DeathmatchMenus = Config.CustomCommands.DeatmatchMenuCmds.Split(',');
        foreach (var weapon in Shortcuts)
        {
            string[] Value = weapon.Split(':');
            if (Value.Length == 2)
            {
                AddCustomCommands(Value[1], Value[0], 1);
            }
        }
        foreach (var cmd in WSelect)
            AddCustomCommands(cmd, "", 2);
        foreach (var cmd in DeathmatchMenus)
            AddCustomCommands(cmd, "", 3);
        foreach (var preference in Preferences.Where(x => x.CommandShortcuts.Any()))
        {
            foreach (var cmd in preference.CommandShortcuts)
                AddCustomCommands(cmd, preference.Name, 4, preference.vipOnly);
        }
        foreach (string radioName in RadioMessagesList)
            AddCommandListener(radioName, OnPlayerRadioMessage);

        AddCommandListener("playerchatwheel", OnPlayerChatwheel);
        AddCommandListener("player_ping", OnPlayerPing);
        AddCommandListener("autobuy", OnRandomWeapons);

        AddCommandListener("say", OnPlayerSay);
        AddCommandListener("say_team", OnPlayerSay);

        bool mapLoaded = false;
        RegisterListener<OnMapEnd>(() => { mapLoaded = false; });
        RegisterListener<OnMapStart>(mapName =>
        {
            blockedSpawns.Clear();
            if (!mapLoaded)
            {
                mapLoaded = true;
                bool RoundTerminated = false;
                DefaultMapSpawnDisabled = false;
                playerData.Clear();
                AddTimer(3.0f, () =>
                {
                    LoadCustomConfigFile();
                    SetupCustomMode(Config.Gameplay.MapStartMode.ToString());
                    SetupDeathMatchConfigValues();
                    RemoveEntities();
                    LoadMapSpawns(ModuleDirectory + $"/spawns/{mapName}.json", true);
                });

                if (Config.Gameplay.IsCustomModes)
                {
                    AddTimer(1.0f, () =>
                    {
                        if (!GameRules().WarmupPeriod)
                        {
                            ModeTimer++;
                            RemainingTime = ActiveMode.Interval - ModeTimer;

                            if (RemainingTime == 0)
                            {
                                if (Config.General.ForceMapEnd)
                                {
                                    var timelimit = Config.Gameplay.GameLength * 60;
                                    var gameStart = GameRules().GameStartTime;
                                    var currentTime = Server.CurrentTime;
                                    var timeleft = timelimit - (currentTime - gameStart);
                                    if (timeleft <= 0 && !RoundTerminated)
                                    {
                                        GameRules().TerminateRound(0.1f, RoundEndReason.RoundDraw);
                                    }
                                }
                                SetupCustomMode(NextMode.ToString());
                            }
                            if (!string.IsNullOrEmpty(ActiveMode.CenterMessageText) && Config.CustomModes.TryGetValue(NextMode.ToString(), out var modeData))
                            {
                                var time = TimeSpan.FromSeconds(RemainingTime);
                                var formattedTime = $"{time.Minutes}:{time.Seconds:D2}";//RemainingTime > 60 ? $"{time.Minutes}:{time.Seconds:D2}" : $"{time.Seconds}";

                                ModeCenterMessage = ActiveMode.CenterMessageText.Replace("{REMAININGTIME}", formattedTime);
                                ModeCenterMessage = ModeCenterMessage.Replace("{NEXTMODE}", modeData.Name);
                            }
                        }
                    }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
                }
            }
        });
        RegisterListener<OnTick>(() =>
        {
            if (VisibleHud)
            {
                foreach (var p in Utilities.GetPlayers())
                {
                    if (ActiveEditor == p)
                    {
                        var ctSpawns = DefaultMapSpawnDisabled ? spawnPositionsCT.Count : 0;
                        var tSpawns = DefaultMapSpawnDisabled ? spawnPositionsT.Count : 0;
                        p.PrintToCenterHtml($"<font class='fontSize-l' color='red'>Spawns Editor</font><br><font class='fontSize-m' color='green'>!1</font> Add CT Spawn (Total: <font color='lime'>{ctSpawns}</font>)<br><font class='fontSize-m' color='green'>!2</font> Add T Spawn (Total: <font color='lime'>{tSpawns}</font>)<br><font class='fontSize-m' color='green'>!3</font> Remove the Nearest Spawn<br><br><font class='fontSize-m' color='green'>!4</font> <font class='fontSize-m' color='cyan'>Save Spawns</font><br> ");
                    }
                    else
                    {
                        if ((Config.PlayersPreferences.HudMessages.Enabled && !GetPrefsValue(p.Slot, "HudMessages")) || MenuManager.GetActiveMenu(p) != null)
                            continue;

                        if (!string.IsNullOrEmpty(ActiveMode.CenterMessageText))
                        {
                            if (Config.Gameplay.HudType == 1)
                                p.PrintToCenterHtml(ModeCenterMessage);
                            else
                                p.PrintToCenter(ModeCenterMessage);
                        }
                        if (Config.General.HideModeRemainingTime && RemainingTime <= Config.Gameplay.NewModeCountdown && Config.Gameplay.NewModeCountdown > 0)
                        {
                            if (RemainingTime == 0)
                            {
                                if (Config.Gameplay.HudType == 1)
                                    p.PrintToCenter($"{Localizer["Hud.NewModeStarted"]}");
                                else
                                    p.PrintToCenterHtml($"{Localizer["Hud.NewModeStarted"]}");
                            }
                            else
                            {
                                var NextModeData = Config.CustomModes[NextMode.ToString()];
                                if (Config.Gameplay.HudType == 1)
                                    p.PrintToCenterHtml($"{Localizer["Hud.NewModeStarting", RemainingTime, NextModeData.Name]}");
                                else
                                    p.PrintToCenter($"{Localizer["Hud.NewModeStarting", RemainingTime, NextModeData.Name]}");
                            }
                        }
                    }
                }
            }
        });

        if (Config.General.RemoveDecals)
        {
            HookUserMessage(411, um =>
            {
                um.Recipients.Clear();
                return HookResult.Continue;
            }, HookMode.Pre);
        }

        if (Config.General.RemovePointsMessage)
        {
            HookUserMessage(124, um =>
            {
                if (IsCasualGamemode)
                    return HookResult.Continue;

                for (int i = 0; i < um.GetRepeatedFieldCount("param"); i++)
                {
                    var message = um.ReadString("param", i);
                    foreach (var msg in PointsMessagesArray)
                    {
                        if (message.Contains(msg))
                        {
                            return HookResult.Stop;
                        }
                    }

                }
                return HookResult.Continue;
            }, HookMode.Pre);
        }

        if (hotReload)
        {
            Server.ExecuteCommand($"map {Server.MapName}");
        }
        else
        {
            if (Config.General.RestartMapOnPluginLoad)
                Server.ExecuteCommand($"map {Server.MapName}");
        }
    }

    public override void Unload(bool hotReload)
    {
        VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Unhook(OnWeaponCanAcquire, HookMode.Pre);
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);
    }

    public void SetupCustomMode(string modeId)
    {
        ActiveMode = Config.CustomModes[modeId];
        bool bNewmode = true;
        if (modeId.Equals(ActiveCustomMode.ToString()))
            bNewmode = false;

        ActiveCustomMode = modeId;
        NextMode = GetModeType();

        if (Config.CustomModes.TryGetValue(NextMode.ToString(), out var modeData) && !string.IsNullOrEmpty(ActiveMode.CenterMessageText))
        {
            ModeCenterMessage = ActiveMode.CenterMessageText.Replace("{NEXTMODE}", modeData.Name);
            ModeCenterMessage = ModeCenterMessage.Replace("{REMAININGTIME}", RemainingTime.ToString());
        }
        SetupDeathmatchConfiguration(ActiveMode, bNewmode);

        Server.NextFrame(() =>
        {
            DeathmatchAPI.Get()?.TriggerEvent(new OnCustomModeStarted(int.Parse(ActiveCustomMode), ActiveMode));
        });
    }

    public void SetupDeathmatchConfiguration(ModeData mode, bool isNewMode)
    {
        ModeTimer = 0;

        if (isNewMode)
            Server.PrintToChatAll($"{Localizer["Chat.Prefix"]} {Localizer["Chat.NewModeStarted", mode.Name]}");

        Server.ExecuteCommand($"mp_free_armor {mode.Armor};mp_damage_headshot_only {mode.OnlyHS};mp_ct_default_primary \"\";mp_t_default_primary \"\";mp_ct_default_secondary \"\";mp_t_default_secondary \"\"");

        if (mode.ExecuteCommands.Any())
        {
            foreach (var cmd in mode.ExecuteCommands)
                Server.ExecuteCommand(cmd);
        }

        foreach (var p in Utilities.GetPlayers().Where(p => p.PawnIsAlive))
        {
            p.RemoveWeapons();
            GivePlayerWeapons(p, true);
            if (mode.Armor != 0)
            {
                string armor = mode.Armor == 1 ? "item_kevlar" : "item_assaultsuit";
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
    public void LoadCustomConfigFile()
    {
        string path = Server.GameDirectory + "/csgo/cfg/deathmatch/";
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        if (!File.Exists(path + "deathmatch.cfg"))
        {
            var content = @"
// Things you can customize and add your own cvars
sv_cheats 1
mp_timelimit 30
mp_maxrounds 0
sv_disable_radar 1
sv_alltalk 1
mp_warmuptime 20
mp_freezetime 1
mp_death_drop_grenade 0
mp_death_drop_gun 0
mp_death_drop_healthshot 0
mp_drop_grenade_enable 0
mp_death_drop_c4 0
mp_death_drop_taser 0
sv_infinite_ammo 0
mp_defuser_allocation 0
mp_solid_teammates 1
mp_give_player_c4 0
mp_playercashawards 0
mp_teamcashawards 0
cash_team_bonus_shorthanded 0
mp_autokick 0
mp_match_restart_delay 10
mp_weapons_allow_zeus 1

//Do not change or delete!!
mp_max_armor 0
mp_weapons_allow_typecount -1
mp_hostages_max 0
mp_buy_allow_grenades 0
sv_cheats 0
            ";

            using (StreamWriter writer = new StreamWriter(path + "deathmatch.cfg"))
            {
                writer.Write(content);
            }
        }
        Server.ExecuteCommand("exec deathmatch/deathmatch.cfg");
    }

    public void SetupDeathMatchConfigValues()
    {
        var gameType = ConVar.Find("game_type")!.GetPrimitiveValue<int>();
        IsCasualGamemode = gameType != 1;
        /*if (!IsLinuxServer && !IsCasualGamemode)
        {
            SendConsoleMessage("======= Deathmatch WARNING =======", ConsoleColor.Red);
            SendConsoleMessage("Your server is running on Windows, the Deathmatch plugin does not work properly if you have deathmatch game mode (game_type 1 and game_mode 2)", ConsoleColor.DarkYellow);
            SendConsoleMessage("Please use game mode Casual!", ConsoleColor.DarkYellow);
            SendConsoleMessage("======= Deathmatch WARNING =======", ConsoleColor.Red);
        }*/

        var iHideSecond = Config.General.HideRoundSeconds ? 1 : 0;
        var iFFA = Config.Gameplay.IsFFA ? 1 : 0;
        Server.ExecuteCommand($"mp_maxrounds 0;mp_timelimit {Config.Gameplay.GameLength};mp_teammates_are_enemies {iFFA};sv_hide_roundtime_until_seconds {iHideSecond};mp_roundtime_defuse {Config.Gameplay.GameLength};mp_roundtime {Config.Gameplay.GameLength};mp_roundtime_deployment {Config.Gameplay.GameLength};mp_roundtime_hostage {Config.Gameplay.GameLength};mp_respawn_on_death_ct 1;mp_respawn_on_death_t 1");

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
            var modeId = int.Parse(ActiveCustomMode);
            if (Config.Gameplay.RandomSelectionOfModes)
            {
                int iRandomMode;
                do
                {
                    iRandomMode = Random.Next(0, Config.CustomModes.Count);
                } while (iRandomMode == modeId);
                return iRandomMode;
            }
            else
            {
                if (modeId + 1 != Config.CustomModes.Count && modeId + 1 < Config.CustomModes.Count)
                    return modeId + 1;
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