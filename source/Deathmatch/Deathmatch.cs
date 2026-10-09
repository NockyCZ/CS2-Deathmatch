using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Core.Capabilities;
using DeathmatchAPI.Helpers;
using static DeathmatchAPI.Events.IDeathmatchEventsAPI;
using static CounterStrikeSharp.API.Core.Listeners;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.UserMessages;
using System.Drawing;
using System.Data;
using DeathmatchAPI;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Deathmatch;

public partial class Deathmatch : BasePlugin, IPluginConfig<DeathmatchConfig>
{
    public override string ModuleName => "Deathmatch";
    public override string ModuleAuthor => "Nocky & Miksen(Forked)";
    public override string ModuleVersion => "1.3.6";

    public void OnConfigParsed(DeathmatchConfig config)
    {
        Config = config;
        CheckedEnemiesDistance = Config.SpawnSystem.DistanceRespawn;
        CheckSpawnVisibility = Config.SpawnSystem.CheckVisible;

        UpdateConfigFile();
    }

    private static readonly JsonSerializerOptions _configEmitOptions = new() { WriteIndented = false };
    private static readonly JsonSerializerOptions _configWriteOptions = new() { WriteIndented = true };

    // Restores any key defined in Configs.cs that is missing from the on-disk Deathmatch.json
    // (CSS only reads the file, it never writes new/missing keys back). Compares the user's file
    // against a fresh default config and adds back anything missing at any depth — including
    // default dictionary entries (modes, weapon restricts) the user removed. Existing user values
    // and key order are preserved; only missing keys are added. JSON comments are dropped.
    private void UpdateConfigFile()
    {
        try
        {
            // ModuleDirectory is somewhere under .../counterstrikesharp/plugins/<name>[/...].
            // Split on the "/plugins/" segment so nesting depth doesn't matter.
            string moduleDir = ModuleDirectory.Replace('\\', '/').TrimEnd('/');
            const string marker = "/plugins/";
            int idx = moduleDir.IndexOf(marker, StringComparison.Ordinal);
            if (idx < 0)
            {
                Logger.LogError($"Config auto-update: '/plugins/' not in ModuleDirectory={moduleDir} — skipped.");
                return;
            }

            string cssRoot = moduleDir.Substring(0, idx);            // .../counterstrikesharp
            string pluginName = new DirectoryInfo(ModuleDirectory).Name; // leaf folder (ignores disabled/ etc.)
            string configPath = Path.Combine(cssRoot, "configs", "plugins", pluginName, pluginName + ".json");

            if (!File.Exists(configPath))
            {
                Logger.LogError($"Config auto-update: file not found at {configPath} — skipped.");
                return;
            }

            JsonNode? root;
            try
            {
                root = JsonNode.Parse(File.ReadAllText(configPath), null, new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                });
            }
            catch (JsonException ex)
            {
                Logger.LogError($"Deathmatch.json is not valid JSON, skipping auto-update: {ex.Message}");
                return;
            }

            if (root is not JsonObject existing)
                return;

            // Full schema with every default from Configs.cs.
            if (JsonNode.Parse(JsonSerializer.Serialize(new DeathmatchConfig(), _configEmitOptions)) is not JsonObject defaults)
                return;

            JsonObject merged = MergeOrdered(existing, defaults, out bool changed);
            if (changed)
            {
                File.WriteAllText(configPath, merged.ToJsonString(_configWriteOptions));
                Logger.LogInformation("Deathmatch config: restored missing keys (existing values preserved).");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to auto-update config file: {ex.Message}");
        }
    }

    // Rebuilds a new object ordered exactly like 'defaults' (= Configs.cs declaration order),
    // so a restored key lands in its proper position instead of at the end. Existing user
    // values are kept (only missing keys take the default); keys the user has that are not in
    // defaults are appended afterwards. Arrays are treated as a single value. 'changed' is set
    // if any key was missing at any depth.
    private static JsonObject MergeOrdered(JsonObject target, JsonObject defaults, out bool changed)
    {
        changed = false;
        var result = new JsonObject();

        foreach (var kv in defaults)
        {
            if (target[kv.Key] is JsonNode existingNode)
            {
                if (existingNode is JsonObject existingObj && kv.Value is JsonObject defaultObj)
                {
                    result[kv.Key] = MergeOrdered(existingObj, defaultObj, out bool childChanged);
                    if (childChanged) changed = true;
                }
                else
                {
                    target.Remove(kv.Key);          // detach before re-parenting
                    result[kv.Key] = existingNode;  // keep user's value as-is
                }
            }
            else
            {
                result[kv.Key] = kv.Value?.DeepClone();
                changed = true;
            }
        }

        // Preserve any keys the user added that are not part of the schema.
        foreach (var kv in target.ToList())
        {
            if (result.ContainsKey(kv.Key)) continue;
            target.Remove(kv.Key);
            result[kv.Key] = kv.Value;
        }

        return result;
    }
    public override void Load(bool hotReload)
    {
        var API = new Deathmatch();
        Capabilities.RegisterPluginCapability(DeathmatchAPI, () => API);
        VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Hook(OnWeaponCanAcquire, HookMode.Pre);
        //VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
        RegisterListener<OnEntityTakeDamagePre>(OnEntityTakeDamagePre);

        if (Config.SaveWeapons)
            _ = CreateDatabaseConnection();

        string[] WSelect = Config.CustomCommands.WeaponSelectCmds.Split(',');
        string[] DeathmatchMenus = Config.CustomCommands.DeatmatchMenuCmds.Split(',');
        foreach (var weapon in Config.CustomCommands.CustomShortcuts)
            AddCustomCommands(weapon.Key, weapon.Value, 1);
        foreach (var cmd in WSelect)
            AddCustomCommands(cmd, "", 2);
        foreach (var cmd in DeathmatchMenus)
            AddCustomCommands(cmd, "", 3);
        foreach (string radioName in RadioMessagesList)
            RegisterTrackedCommandListener(radioName, OnPlayerRadioMessage);

        RegisterTrackedCommandListener("playerchatwheel", OnPlayerChatwheel);
        RegisterTrackedCommandListener("player_ping", OnPlayerPing);
        RegisterTrackedCommandListener("autobuy", OnRandomWeapons);
        foreach (string cmd in MapChangeCommands)
            RegisterTrackedCommandListener(cmd, OnMapChangeCommand);

        bool mapLoaded = false;
        _onMapEndDelegate = () => { mapLoaded = false; StopNewModeSound(); };
        RegisterListener<OnMapEnd>(_onMapEndDelegate);
        _onMapStartDelegate = mapName =>
        {
            blockedSpawns.Clear();
            savedSpawnsModel.Clear();
            playerData.Clear();
            playersWaitingForRespawn.Clear();
            playersWithSpawnProtection.Clear();

            if (!mapLoaded)
            {
                mapLoaded = true;
                bool RoundTerminated = false;
                DefaultMapSpawnDisabled = false;
                AddTimer(0.2f, () =>
                {
                    LoadCustomConfigFile();
                });

                AddTimer(3.0f, () =>
                {
                    SpawnsPath = ModuleDirectory + $"/spawns/{mapName}.json";
                    SetupCustomMode(Config.Gameplay.MapStartMode.ToString());
                    SetupDeathMatchConfigValues();
                    RemoveEntities();
                    LoadMapSpawns(ModuleDirectory + $"/spawns/{mapName}.json");
                    SetGameRules();
                }, TimerFlags.STOP_ON_MAPCHANGE);

                double secTimer = 0;
                bool wasSupportedGamemode = IsSupportedGamemode();
                double lastUpdate = Server.CurrentTime;
                AddTimer(0.1f, () =>
                {
                    // game_mode / game_type changed to an unsupported mode (game_alias, exec, console) -> cut the music
                    bool supported = IsSupportedGamemode();
                    if (wasSupportedGamemode && !supported)
                        StopNewModeSound();
                    wasSupportedGamemode = supported;

                    if (playersWaitingForRespawn.Count > 0)
                    {
                        List<int>? toRemove = null;
                        foreach (var kv in playersWaitingForRespawn)
                        {
                            var time = Server.CurrentTime - kv.Value.currentTime;
                            if (time < kv.Value.timer)
                                continue;

                            var player = Utilities.GetPlayerFromSlot(kv.Key);
                            if (player != null && player.IsValid && !player.PawnIsAlive && (player.Team == CsTeam.Terrorist || player.Team == CsTeam.CounterTerrorist))
                            {
                                player.Respawn();
                            }
                            (toRemove ??= new List<int>()).Add(kv.Key);
                        }
                        if (toRemove != null)
                            foreach (var slot in toRemove) playersWaitingForRespawn.Remove(slot);
                    }
                    if (playersWithSpawnProtection.Count > 0)
                    {
                        List<int>? toRemove = null;
                        foreach (var kv in playersWithSpawnProtection)
                        {
                            var time = Server.CurrentTime - kv.Value.currentTime;
                            if (time < kv.Value.timer)
                                continue;

                            var player = Utilities.GetPlayerFromSlot(kv.Key);
                            if (player != null && player.IsValid && player.PlayerPawn.Value != null && playerData.TryGetValue(player.Slot, out var pData))
                            {
                                pData.SpawnProtection = false;
                                if (!string.IsNullOrEmpty(Config.Gameplay.SpawnProtectionColor))
                                {
                                    player.PlayerPawn.Value.Render = Color.White;
                                    Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");
                                }
                            }
                            (toRemove ??= new List<int>()).Add(kv.Key);
                        }
                        if (toRemove != null)
                            foreach (var slot in toRemove) playersWithSpawnProtection.Remove(slot);
                    }

                    if (Config.Gameplay.IsCustomModes)
                    {
                        double now = Server.CurrentTime;
                        double timer = now - lastUpdate;
                        lastUpdate = now;

                        secTimer += timer;
                        if (secTimer >= 1)
                        {
                            secTimer = 0;
                            if (VisibleHud)
                            {
                                foreach (var p in Utilities.GetPlayers())
                                {
                                    if (!playerData.TryGetValue(p.Slot, out var data))
                                        continue;

                                    if ((Config.PlayersPreferences.HudMessages.Enabled && !GetPrefsValue(data, "HudMessages", Config.PlayersPreferences.HudMessages.DefaultValue)) || MenuManager.GetActiveMenu(p) != null)
                                        continue;

                                    if (RemainingTime <= Config.Gameplay.NewModeCountdown && Config.Gameplay.NewModeCountdown > 0)
                                    {
                                        if (RemainingTime == 0)
                                        {
                                            if (Config.Gameplay.HudType == 0)
                                                p.PrintToCenter($"{Localizer["Hud.NewModeStarted"]}");
                                        }
                                        else if (!Config.General.HideModeRemainingTime && Config.CustomModes.TryGetValue(NextMode.ToString(), out var NextModeData))
                                        {
                                            if (Config.Gameplay.HudType == 0)
                                                p.PrintToCenter($"{Localizer["Hud.NewModeStarting", RemainingTime, NextModeData.Name]}");
                                        }
                                    }
                                    else if (!string.IsNullOrEmpty(ActiveMode.CenterMessageText) && Server.CurrentTime < ModeMessageHideTime)
                                    {
                                        if (Config.Gameplay.HudType == 0)
                                            p.PrintToCenter(ModeCenterMessage);
                                    }
                                }
                            }

                            if (GameRules == null)
                            {
                                SetGameRules();
                            }
                            else if (!GameRules.WarmupPeriod)
                            {
                                ModeTimer++;
                                RemainingTime = ActiveMode.Interval - ModeTimer;

                                if (RemainingTime <= 0)
                                {
                                    if (Config.General.ForceMapEnd)
                                    {
                                        var timelimit = Config.Gameplay.GameLength * 60;
                                        var gameStart = GameRules.GameStartTime;
                                        var currentTime = Server.CurrentTime;
                                        var timeleft = timelimit - (currentTime - gameStart);
                                        if (timeleft <= 0 && !RoundTerminated)
                                        {
                                            GameRules.TerminateRound(1.0f, RoundEndReason.RoundDraw);
                                            return;
                                        }
                                    }
                                    SetupCustomMode(NextMode.ToString());
                                }
                                if (!string.IsNullOrEmpty(ActiveMode.CenterMessageText) && Config.CustomModes.TryGetValue(NextMode.ToString(), out var modeData))
                                {
                                    var time = TimeSpan.FromSeconds(RemainingTime);
                                    var formattedTime = $"{time.Minutes}:{time.Seconds:D2}";

                                    ModeCenterMessage = ActiveMode.CenterMessageText.Replace("{REMAININGTIME}", formattedTime);
                                    ModeCenterMessage = ModeCenterMessage.Replace("{NEXTMODE}", modeData.Name);
                                }
                            }
                            // Rebuild HUD string cache once per second to keep OnTick alloc-free
                            RebuildHudCache();
                        }
                    }
                }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
            }
        };
        RegisterListener<OnMapStart>(_onMapStartDelegate);
        _onTickDelegate = OnTickHudFast;
        RegisterListener<OnTick>(_onTickDelegate);

        if (Config.General.RemoveDecals)
        {
            _hookDecals = um =>
            {
                um.Recipients.Clear();
                return HookResult.Continue;
            };
            HookUserMessage(411, _hookDecals, HookMode.Pre);
        }

        if (Config.General.RemovePointsMessage)
        {
            _hookPoints = um =>
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
            };
            HookUserMessage(124, _hookPoints, HookMode.Pre);
        }

        if (Config.General.RemoveRespawnSound)
        {
            _hookRespawnSound = um =>
            {
                if (IsCasualGamemode)
                    return HookResult.Continue;

                var soundevent = um.ReadUInt("soundevent_hash");
                if (soundevent == 1734994609)
                    um.Recipients.Clear();

                return HookResult.Continue;
            };
            HookUserMessage(208, _hookRespawnSound, HookMode.Pre);
        }

        _hookHudMessages = um =>
        {
            if (IsCasualGamemode)
                return HookResult.Continue;

            var message = um.ReadString("message");
            message = message.Replace("#", "");
            if (HudMessagesArray.Contains(message))
                return HookResult.Stop;

            return HookResult.Continue;
        };
        HookUserMessage(323, _hookHudMessages, HookMode.Pre);

        if (hotReload)
        {
            Preferences.Categorie.RemoveAllCategories();
            Preferences.Menu.RemoveAllOptions();
            Preferences.Preference.RemoveAllPreferences();
            SetupDeathmatchMenus();
            Server.ExecuteCommand($"map {Server.MapName}");
        }
        else
        {
            SetupDeathmatchMenus();
            if (Config.General.RestartMapOnPluginLoad)
                Server.ExecuteCommand($"map {Server.MapName}");
        }
    }

    public override void Unload(bool hotReload)
    {
        VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Unhook(OnWeaponCanAcquire, HookMode.Pre);
        //VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);
        RemoveListener<OnEntityTakeDamagePre>(OnEntityTakeDamagePre);

        // Remove engine listeners — without this they accumulate every hotreload
        if (_onMapStartDelegate != null) { RemoveListener<OnMapStart>(_onMapStartDelegate); _onMapStartDelegate = null; }
        if (_onMapEndDelegate   != null) { RemoveListener<OnMapEnd>(_onMapEndDelegate);     _onMapEndDelegate   = null; }
        if (_onTickDelegate     != null) { RemoveListener<OnTick>(_onTickDelegate);         _onTickDelegate     = null; }

        // Unhook user messages — same accumulation problem
        if (_hookDecals       != null) { UnhookUserMessage(411, _hookDecals,       HookMode.Pre); _hookDecals       = null; }
        if (_hookPoints       != null) { UnhookUserMessage(124, _hookPoints,       HookMode.Pre); _hookPoints       = null; }
        if (_hookRespawnSound != null) { UnhookUserMessage(208, _hookRespawnSound, HookMode.Pre); _hookRespawnSound = null; }
        if (_hookHudMessages  != null) { UnhookUserMessage(323, _hookHudMessages,  HookMode.Pre); _hookHudMessages  = null; }

        // Remove tracked command listeners
        foreach (var (cmd, cb) in _registeredCommandListeners)
            RemoveCommandListener(cmd, cb, HookMode.Pre);
        _registeredCommandListeners.Clear();

        // Drop references so native CHandle wrappers + lambdas captured by closures can be GC'd
        playersWaitingForRespawn.Clear();
        playersWithSpawnProtection.Clear();
        playerData.Clear();
        blockedSpawns.Clear();
        savedSpawnsModel.Clear();
        spawnPoints.Clear();

        HudCenterHtml = "";
        HudCenterMsg = "";
        HudHasContent = false;
        _hudPlayerCache.Clear();

        // Clear DeathmatchAPI static caches — they're held across plugin reloads otherwise,
        // retaining captured closures (Action<CCSPlayerController, Menu>) and stale Preference instances.
        Preferences.Categorie.RemoveAllCategories();
        Preferences.Menu.RemoveAllOptions();
        Preferences.Preference.RemoveAllPreferences();
    }

    // Tracks AddCommandListener invocations so Unload can match-and-remove
    private void RegisterTrackedCommandListener(string command, CommandInfo.CommandListenerCallback callback)
    {
        AddCommandListener(command, callback);
        _registeredCommandListeners.Add((command, callback));
    }

    // Rebuilt at most ~1×/sec from the REPEAT timer's 1-second branch.
    // OnTickHudFast reads these without allocating to keep the per-tick path cheap.
    private void RebuildHudCache()
    {
        HudHasContent = false;
        HudCenterHtml = "";
        HudCenterMsg = "";

        if (RemainingTime <= Config.Gameplay.NewModeCountdown && Config.Gameplay.NewModeCountdown > 0)
        {
            if (RemainingTime == 0)
            {
                string s = Localizer["Hud.NewModeStarted"];
                HudCenterHtml = s;
                HudCenterMsg = s;
                HudHasContent = true;
            }
            else if (!Config.General.HideModeRemainingTime && Config.CustomModes.TryGetValue(NextMode.ToString(), out var nextModeData))
            {
                string s = Localizer["Hud.NewModeStarting", RemainingTime, nextModeData.Name];
                HudCenterHtml = s;
                HudCenterMsg = s;
                HudHasContent = true;
            }
        }
        else if (!string.IsNullOrEmpty(ActiveMode.CenterMessageText) && Server.CurrentTime < ModeMessageHideTime)
        {
            HudCenterHtml = ModeCenterMessage;
            HudCenterMsg = ModeCenterMessage;
            HudHasContent = true;
        }

        // Refresh cached player list at the same 1Hz cadence; avoids Utilities.GetPlayers() alloc per tick
        float now = Server.CurrentTime;
        if (now - _lastHudPlayerCacheTime >= 1.0f)
        {
            _hudPlayerCache.Clear();
            foreach (var p in Utilities.GetPlayers())
                _hudPlayerCache.Add(p);
            _lastHudPlayerCacheTime = now;
        }
    }

    private void OnTickHudFast()
    {
        if (!VisibleHud || !HudHasContent || _hudPlayerCache.Count == 0)
            return;

        // Iterate cached player list to avoid per-tick allocation from Utilities.GetPlayers()
        for (int i = 0; i < _hudPlayerCache.Count; i++)
        {
            var p = _hudPlayerCache[i];
            if (p == null || !p.IsValid)
                continue;

            if (!playerData.TryGetValue(p.Slot, out var data))
                continue;

            if ((Config.PlayersPreferences.HudMessages.Enabled && !GetPrefsValue(data, "HudMessages", Config.PlayersPreferences.HudMessages.DefaultValue)) || MenuManager.GetActiveMenu(p) != null)
                continue;

            if (Config.Gameplay.HudType == 1)
                p.PrintToCenterHtml(HudCenterHtml);
        }
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
        ModeMessageHideTime = Config.Gameplay.ModeMessageDuration > 0 ? Server.CurrentTime + Config.Gameplay.ModeMessageDuration : float.MaxValue;
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

        // Cut the previous New Mode Sound so a quick mode change doesn't stack two tracks
        StopNewModeSound();
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
                if (Config.PlayersPreferences.NewModeSound.Enabled && IsSupportedGamemode() && playerData.TryGetValue(p.Slot, out var pData) && GetPrefsValue(pData, "NewModeSound", Config.PlayersPreferences.NewModeSound.DefaultValue))
                {
                    var guid = PlaySound(p, Config.SoundSettings.NewModeSound);
                    if (guid != 0)
                        _newModeSoundGuids[p.Slot] = guid;
                }
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

    // New Mode Sound only plays in supported modes: Casual (0/0), Deathmatch (1/2), Custom (3/0).
    // Read live so a switch to competitive (game_alias / exec) is picked up the same frame.
    private bool IsSupportedGamemode()
    {
        _gameTypeCvar ??= ConVar.Find("game_type");
        _gameModeCvar ??= ConVar.Find("game_mode");
        if (_gameTypeCvar == null || _gameModeCvar == null)
            return true;

        return (_gameTypeCvar.GetPrimitiveValue<int>(), _gameModeCvar.GetPrimitiveValue<int>()) is (0, 0) or (1, 2) or (3, 0);
    }

    private static readonly string[] MapChangeCommands = { "changelevel", "map", "host_workshop_map", "ds_workshop_changelevel", "game_alias" };

    private HookResult OnMapChangeCommand(CCSPlayerController? player, CommandInfo info)
    {
        StopNewModeSound();
        return HookResult.Continue;
    }

    // Stops a New Mode Sound that was emitted as a soundevent (CMsgSosStopSoundEvent).
    // A .vsnd_c path is played client-side via `play` and cannot be stopped by the server.
    private void StopNewModeSound()
    {
        if (_newModeSoundGuids.Count == 0)
            return;

        foreach (var (slot, guid) in _newModeSoundGuids)
        {
            var p = Utilities.GetPlayerFromSlot(slot);
            if (p == null || !p.IsValid)
                continue;

            var msg = UserMessage.FromId(209);
            msg.SetUInt("soundevent_guid", guid);
            msg.Recipients.Add(p);
            msg.Send();
        }
        _newModeSoundGuids.Clear();
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
        Server.ExecuteCommand($"mp_buy_anywhere 1;mp_maxrounds 0;mp_timelimit {Config.Gameplay.GameLength};mp_teammates_are_enemies {iFFA};sv_hide_roundtime_until_seconds {iHideSecond};mp_roundtime_defuse {Config.Gameplay.GameLength};mp_roundtime {Config.Gameplay.GameLength};mp_roundtime_deployment {Config.Gameplay.GameLength};mp_roundtime_hostage {Config.Gameplay.GameLength};mp_respawn_on_death_ct 1;mp_respawn_on_death_t 1");

        if (Config.Gameplay.AllowBuyMenu)
            Server.ExecuteCommand("mp_buytime 60000;mp_buy_during_immunity 0");
        else
            Server.ExecuteCommand("mp_buytime 0;mp_buy_during_immunity 0");

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
            if (Config.CustomModes.Count <= 1)
                return 0;

            var modeId = int.Parse(ActiveCustomMode);
            if (Config.Gameplay.RandomSelectionOfModes)
            {
                int iRandomMode;
                do
                {
                    iRandomMode = Random.Shared.Next(0, Config.CustomModes.Count);
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
