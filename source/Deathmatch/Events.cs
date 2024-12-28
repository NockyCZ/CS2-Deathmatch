using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using Newtonsoft.Json;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.UserMessages;

namespace Deathmatch
{
    public partial class Deathmatch
    {
        [GameEventHandler(HookMode.Post)]
        public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player != null && player.IsValid && !player.IsBot && !player.IsHLTV && player.SteamID.ToString().Length == 17 && !playerData.ContainsPlayer(player))
            {
                var setupPlayerData = new DeathmatchPlayerData
                {
                    BlockRandomWeaponsIntegeration = Server.CurrentTime,
                };
                playerData[player] = setupPlayerData;
                if (Config.SaveWeapons)
                {
                    _ = UpdateOrLoadPlayerData(player, player.SteamID.ToString(), null, true);
                }
                else
                {
                    SetupDefaultWeapons(player);
                    SetupDefaultPreferences(player);
                }
            }
            return HookResult.Continue;
        }

        [GameEventHandler(HookMode.Pre)]
        public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player != null && player.IsValid)
            {
                if (playerData.TryGetValue(player.Slot, out var data))
                {
                    if (Config.SaveWeapons)
                    {
                        string[] preferences = {
                            JsonConvert.SerializeObject(data.PrimaryWeapon),
                            JsonConvert.SerializeObject(data.SecondaryWeapon),
                            JsonConvert.SerializeObject(data.Preferences),
                        };
                        _ = UpdateOrLoadPlayerData(player, player.SteamID.ToString(), preferences, false);
                    }
                    playerData.RemovePlayer(player);
                }
                blockedSpawns.Remove(player.Slot);
            }

            return HookResult.Continue;
        }

        [GameEventHandler(HookMode.Post)]
        public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player != null && player.IsValid)
            {
                GivePlayerWeapons(player, false);
            }

            return HookResult.Continue;
        }

        [GameEventHandler(HookMode.Pre)]
        public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            var attacker = @event.Attacker;
            var player = @event.Userid;

            if (player == null || !player.IsValid || attacker == player)
                return HookResult.Continue;

            if (attacker != null && attacker.IsValid)
            {
                if (ActiveMode.OnlyHS)
                {
                    if (@event.Hitgroup == 1 && playerData.TryGetValue(attacker.Slot, out var data))
                    {
                        if (Config.PlayersPreferences.DamageInfo.Enabled && GetPrefsValue(attacker.Slot, "DamageInfo"))
                        {
                            if (data.DamageInfo.TryGetValue(player.Slot, out var damageInfo))
                            {
                                damageInfo.Damage += @event.DmgHealth;
                                damageInfo.Hits++;
                            }
                            else
                            {
                                data.DamageInfo[player.Slot] = new DamageData
                                {
                                    Damage = @event.DmgHealth,
                                    Hits = 1
                                };
                            }
                        }
                        if (GetPrefsValue(attacker.Slot, "HitSound") && (!@event.Weapon.Contains("knife") || !@event.Weapon.Contains("bayonet")))
                            attacker!.ExecuteClientCommand("play " + Config.PlayersPreferences.HitSound.Path);
                    }
                }
                else
                {
                    if (@event.Hitgroup != 1)
                    {
                        if ((!@event.Weapon.Contains("knife") || !@event.Weapon.Contains("bayonet")) && GetPrefsValue(attacker.Slot, "HitSound"))
                        {
                            if (!GetPrefsValue(attacker.Slot, "OnlyHS"))
                                attacker.ExecuteClientCommand("play " + Config.PlayersPreferences.HitSound.Path);
                        }
                    }
                    else if (GetPrefsValue(attacker.Slot, "HitSound"))
                    {
                        attacker.ExecuteClientCommand("play " + Config.PlayersPreferences.HitSound.Path);
                    }
                    if (Config.PlayersPreferences.DamageInfo.Enabled && GetPrefsValue(attacker.Slot, "DamageInfo") && playerData.TryGetValue(attacker.Slot, out var data))
                    {
                        if (data.DamageInfo.TryGetValue(player.Slot, out var damageInfo))
                        {
                            damageInfo.Damage += @event.DmgHealth;
                            damageInfo.Hits++;
                        }
                        else
                        {
                            data.DamageInfo[player.Slot] = new DamageData
                            {
                                Damage = @event.DmgHealth,
                                Hits = 1
                            };
                        }
                    }
                }
            }
            return HookResult.Continue;
        }

        [GameEventHandler(HookMode.Pre)]
        public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            var player = @event.Userid;
            var attacker = @event.Attacker;
            info.DontBroadcast = true;

            if (player == null || !player.IsValid)
                return HookResult.Continue;

            var timer = 1.0f;
            bool IsVIP = AdminManager.PlayerHasPermissions(attacker, Config.PlayersSettings.VIPFlag);
            if (playerData.TryGetValue(player.Slot, out var data))
            {
                data.KillStreak = 0;
                if (Config.PlayersPreferences.DamageInfo.Enabled)
                {
                    playerData.Keys.ToList().ForEach(p =>
                    {
                        playerData[p].DamageInfo.Remove(player.Slot);
                    });

                    if (GetPrefsValue(player.Slot, "DamageInfo") && attacker != null && attacker.IsValid)
                    {
                        if (data.DamageInfo.TryGetValue(attacker.Slot, out var damageInfo))
                            player.PrintToChat(Localizer["Chat.Prefix"] + " " + Localizer["Chat.GivenDamageVictim", attacker.PlayerName, damageInfo.Damage, damageInfo.Hits]);
                        else
                            player.PrintToChat(Localizer["Chat.Prefix"] + " " + Localizer["Chat.NoDamageGiven", attacker.PlayerName]);

                        data.DamageInfo.Clear();
                    }
                }
                /*if (Config.General.RemoveDecals)
                {
                    var RemoveDecals = NativeAPI.CreateEvent("round_start", false);
                    NativeAPI.FireEventToClient(RemoveDecals, (int)player.Index);
                }*/
                timer = IsVIP ? Config.PlayersSettings.VIP.RespawnTime : Config.PlayersSettings.NonVIP.RespawnTime;
                @event.FireEventToClient(player);
            }
            AddTimer(timer, () =>
            {
                if (player != null && player.IsValid && !player.PawnIsAlive)
                    PerformRespawn(player, player.Team);
            }, TimerFlags.STOP_ON_MAPCHANGE);

            if (attacker != null && attacker.IsValid && attacker != player && playerData.TryGetValue(attacker.Slot, out var attackerData) && attacker.PlayerPawn.Value != null)
            {
                attackerData.KillStreak++;
                if (GetPrefsValue(attacker.Slot, "DamageInfo") && attackerData.DamageInfo.TryGetValue(player.Slot, out var damageInfo))
                {
                    attacker.PrintToChat(Localizer["Chat.Prefix"] + " " + Localizer["Chat.GivenDamageAttacker", player.PlayerName, damageInfo.Damage, damageInfo.Hits]);
                    attackerData.DamageInfo.Remove(player.Slot);
                }

                bool IsHeadshot = @event.Headshot;
                bool IsKnifeKill = @event.Weapon.Contains("knife") || @event.Weapon.Contains("bayonet");

                if (IsHeadshot && GetPrefsValue(attacker.Slot, "HeadshotKillSound"))
                    attacker.ExecuteClientCommand("play " + Config.PlayersPreferences.HSKillSound.Path);
                else if (IsKnifeKill && GetPrefsValue(attacker.Slot, "KnifeKillSound"))
                    attacker.ExecuteClientCommand("play " + Config.PlayersPreferences.KnifeKillSound.Path);
                else if (GetPrefsValue(attacker.Slot, "KillSound"))
                    attacker.ExecuteClientCommand("play " + Config.PlayersPreferences.KillSound.Path);

                var Health = IsHeadshot
                ? (IsVIP ? Config.PlayersSettings.VIP.HeadshotHealth : Config.PlayersSettings.NonVIP.HeadshotHealth)
                : (IsVIP ? Config.PlayersSettings.VIP.KillHealth : Config.PlayersSettings.NonVIP.KillHealth);

                var refillAmmo = IsHeadshot
                ? (IsVIP ? Config.PlayersSettings.VIP.RefillAmmoHS : Config.PlayersSettings.NonVIP.RefillAmmoHS)
                : (IsVIP ? Config.PlayersSettings.VIP.RefillAmmo : Config.PlayersSettings.NonVIP.RefillAmmo);

                var giveHP = 100 >= attacker.PlayerPawn.Value.Health + Health ? Health : 100 - attacker.PlayerPawn.Value.Health;

                if (refillAmmo)
                {
                    var allWeapons = IsVIP ? Config.PlayersSettings.VIP.ReffilAllWeapons : Config.PlayersSettings.NonVIP.ReffilAllWeapons;
                    if (allWeapons)
                    {
                        var weapons = attacker.PlayerPawn.Value.WeaponServices?.MyWeapons.Where(w => w.Value != null && (ActiveMode.SecondaryWeapons.Contains(w.Value.DesignerName) || ActiveMode.PrimaryWeapons.Contains(w.Value.DesignerName))).ToList();
                        if (weapons != null)
                        {
                            foreach (var weapon in weapons)
                            {
                                if (weapon.Value == null)
                                    continue;

                                weapon.Value.Clip1 = 250;
                                weapon.Value.ReserveAmmo[0] = 250;
                            }
                        }
                    }
                    else
                    {
                        var activeWeapon = attacker.PlayerPawn.Value.WeaponServices?.ActiveWeapon.Value;
                        if (activeWeapon != null)
                        {
                            activeWeapon.Clip1 = 250;
                            activeWeapon.ReserveAmmo[0] = 250;
                        }
                    }
                }
                if (giveHP > 0)
                {
                    attacker.PlayerPawn.Value.Health += giveHP;
                    Utilities.SetStateChanged(attacker.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
                }

                if (ActiveMode.Armor != 0)
                {
                    string armor = ActiveMode.Armor == 1 ? "item_kevlar" : "item_assaultsuit";
                    attacker.GiveNamedItem(armor);
                }
                @event.FireEventToClient(attacker);
            }
            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            if (Config.General.RemoveBreakableEntities)
                RemoveBreakableEntities();

            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnNewMatchBegin(EventBeginNewMatch @event, GameEventInfo info)
        {
            SetupCustomMode(Config.Gameplay.MapStartMode.ToString());
            return HookResult.Continue;
        }

        private HookResult OnPlayerRadioMessage(CCSPlayerController? player, CommandInfo info)
        {
            if (Config.General.BlockRadioMessage)
                return HookResult.Handled;
            return HookResult.Continue;
        }

        private HookResult OnPlayerSay(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null || !player.IsValid || ActiveEditor != player)
                return HookResult.Continue;

            if (!player.PawnIsAlive)
            {
                info.ReplyToCommand($"{Localizer["Chat.Prefix"]} You have to be alive to use Spawns Editor!");
                return HookResult.Continue;
            }

            string msg = info.GetArg(1).ToLower();
            if (msg.StartsWith('!') || msg.StartsWith('/'))
            {
                msg = msg.Replace("!", "").Replace("/", "");
                if (ulong.TryParse(msg, out var number))
                {
                    switch (number)
                    {
                        case 1:
                            AddNewSpawnPoint(player.PlayerPawn.Value!.AbsOrigin!, player.PlayerPawn.Value.AbsRotation!, CsTeam.CounterTerrorist);
                            player.PrintToChat($"{Localizer["Chat.Prefix"]} Spawn for the {ChatColors.DarkBlue}CT team{ChatColors.Default} has been added. (Total: {ChatColors.Green}{spawnPositionsCT.Count}{ChatColors.Default})");
                            break;

                        case 2:
                            AddNewSpawnPoint(player.PlayerPawn.Value!.AbsOrigin!, player.PlayerPawn.Value.AbsRotation!, CsTeam.Terrorist);
                            player.PrintToChat($"{Localizer["Chat.Prefix"]} Spawn for the {ChatColors.Orange}T team{ChatColors.Default} has been added. (Total: {ChatColors.Green}{spawnPositionsT.Count}{ChatColors.Default})");
                            break;

                        case 3:
                            RemoveNearestSpawnPoint(player.PlayerPawn.Value!.AbsOrigin);
                            player.PrintToChat($"{Localizer["Chat.Prefix"]} The nearest spawn point has been removed!");
                            break;

                        case 4:
                            SaveSpawnsFile();
                            LoadMapSpawns(ModuleDirectory + $"/spawns/{Server.MapName}.json", false);
                            player.PrintToChat($"{Localizer["Chat.Prefix"]} Spawns have been successfully saved!");
                            RemoveSpawnModels();
                            ActiveEditor = null;
                            break;
                    }
                }
            }
            return HookResult.Continue;
        }

        private HookResult OnPlayerChatwheel(CCSPlayerController? player, CommandInfo info)
        {
            if (Config.General.BlockPlayerChatWheel)
                return HookResult.Handled;
            return HookResult.Continue;
        }

        private HookResult OnPlayerPing(CCSPlayerController? player, CommandInfo info)
        {
            if (Config.General.BlockPlayerPing)
                return HookResult.Handled;
            return HookResult.Continue;
        }

        private HookResult OnRandomWeapons(CCSPlayerController? player, CommandInfo info)
        {
            return HookResult.Stop;
        }

        private HookResult OnTakeDamage(DynamicHook hook)
        {
            var damageInfo = hook.GetParam<CTakeDamageInfo>(1);
            var player = hook.GetParam<CCSPlayerPawn>(0).Controller.Value?.As<CCSPlayerController>();
            if (player == null || !player.IsValid)
                return HookResult.Continue;

            if (playerData.ContainsPlayer(player) && playerData[player].SpawnProtection)
            {
                damageInfo.Damage = 0;
                return HookResult.Continue;
            }

            var attacker = damageInfo.Attacker.Value?.As<CCSPlayerPawn>().Controller.Value?.As<CCSPlayerController>();
            if (attacker == null || !attacker.IsValid)
                return HookResult.Continue;

            if (!ActiveMode.KnifeDamage && damageInfo.Ability.Value != null && (damageInfo.Ability.Value.DesignerName.Contains("knife") || damageInfo.Ability.Value.DesignerName.Contains("bayonet")))
            {
                attacker.PrintToCenter(Localizer["Hud.KnifeDamageIsDisabled"]);
                damageInfo.Damage = 0;
                return HookResult.Continue;
            }

            if (Config.PlayersPreferences.OnlyHS.Enabled && damageInfo.GetHitGroup() != HitGroup_t.HITGROUP_HEAD && GetPrefsValue(attacker.Slot, "OnlyHS"))
                damageInfo.Damage = 0;

            return HookResult.Continue;
        }

        private HookResult OnWeaponCanAcquire(DynamicHook hook)
        {
            var vdata = VirtualFunctions.GetCSWeaponDataFromKey.Invoke(-1, hook.GetParam<CEconItemView>(1).ItemDefinitionIndex.ToString());
            var player = hook.GetParam<CCSPlayer_ItemServices>(0).Pawn.Value.Controller.Value?.As<CCSPlayerController>();

            if (vdata == null || player == null || !player.IsValid)
                return HookResult.Continue;

            if (hook.GetParam<AcquireMethod>(2) == AcquireMethod.PickUp)
            {
                if (!ActiveMode.PrimaryWeapons.Contains(vdata.Name) && !ActiveMode.SecondaryWeapons.Contains(vdata.Name))
                {
                    if (vdata.Name.Contains("knife") || vdata.Name.Contains("bayonet") || ActiveMode.Utilities.Contains(vdata.Name))
                        return HookResult.Continue;

                    hook.SetReturn(AcquireResult.AlreadyOwned);
                    return HookResult.Stop;
                }
                return HookResult.Continue;
            }

            if (!IsCasualGamemode && IsHaveBlockedRandomWeaponsIntegration(player))
            {
                hook.SetReturn(AcquireResult.AlreadyPurchased);
                return HookResult.Stop;
            }

            if (ActiveMode.RandomWeapons)
            {
                if (!string.IsNullOrEmpty(Config.SoundSettings.CantEquipSound))
                    player.ExecuteClientCommand("play " + Config.SoundSettings.CantEquipSound);
                player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponsSelectIsDisabled"]}");
                hook.SetReturn(AcquireResult.AlreadyPurchased);
                return HookResult.Stop;
            }

            if (!ActiveMode.PrimaryWeapons.Contains(vdata.Name) && !ActiveMode.SecondaryWeapons.Contains(vdata.Name))
            {
                if (!player.IsBot)
                {
                    if (!string.IsNullOrEmpty(Config.SoundSettings.CantEquipSound))
                        player.ExecuteClientCommand("play " + Config.SoundSettings.CantEquipSound);

                    string replacedweaponName = Localizer[vdata.Name];
                    player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponIsDisabled", replacedweaponName]}");
                }
                hook.SetReturn(AcquireResult.AlreadyPurchased);
                return HookResult.Stop;
            }

            if (playerData.ContainsPlayer(player))
            {
                string localizerWeaponName = Localizer[vdata.Name];
                bool IsVIP = AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag);

                bool IsPrimary = PrimaryWeaponsList.Contains(vdata.Name);
                if (CheckIsWeaponRestricted(vdata.Name, IsVIP, player.Team, IsPrimary))
                {
                    if (!string.IsNullOrEmpty(Config.SoundSettings.CantEquipSound))
                        player.ExecuteClientCommand("play " + Config.SoundSettings.CantEquipSound);

                    (int NonVIP, int VIP) restrictInfo = GetRestrictData(vdata.Name, player.Team);
                    player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponIsRestricted", localizerWeaponName, GetWeaponRestrictLozalizer(restrictInfo.NonVIP), GetWeaponRestrictLozalizer(restrictInfo.VIP)]}");
                    hook.SetReturn(AcquireResult.NotAllowedByMode);
                    return HookResult.Stop;
                }

                if (IsPrimary)
                {
                    if (playerData[player].PrimaryWeapon.TryGetValue(ActiveCustomMode, out var primaryWeapon) && vdata.Name == primaryWeapon)
                    {
                        player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponsIsAlreadySet", localizerWeaponName]}");
                        hook.SetReturn(AcquireResult.AlreadyOwned);
                        return HookResult.Stop;
                    }
                    playerData[player].PrimaryWeapon[ActiveCustomMode] = vdata.Name;
                    player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.PrimaryWeaponSet", localizerWeaponName]}");

                    var weapon = GetWeaponFromSlot(player, gear_slot_t.GEAR_SLOT_RIFLE);
                    if (!Config.Gameplay.SwitchWeapons && weapon != null)
                    {
                        hook.SetReturn(AcquireResult.AlreadyOwned);
                        return HookResult.Stop;
                    }
                }
                else
                {
                    if (playerData[player].SecondaryWeapon.TryGetValue(ActiveCustomMode, out var secondaryWeapon) && vdata.Name == secondaryWeapon)
                    {
                        player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponsIsAlreadySet", localizerWeaponName]}");
                        hook.SetReturn(AcquireResult.AlreadyOwned);
                        return HookResult.Stop;
                    }
                    playerData[player].SecondaryWeapon[ActiveCustomMode] = vdata.Name;
                    player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.SecondaryWeaponSet", localizerWeaponName]}");

                    var weapon = GetWeaponFromSlot(player, gear_slot_t.GEAR_SLOT_PISTOL);
                    if (!Config.Gameplay.SwitchWeapons && weapon != null)
                    {
                        hook.SetReturn(AcquireResult.AlreadyOwned);
                        return HookResult.Stop;
                    }
                }
            }
            return HookResult.Continue;
        }
    }
}