using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using Newtonsoft.Json;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Memory;

namespace Deathmatch
{
    public partial class Deathmatch
    {
        [GameEventHandler(HookMode.Post)]
        public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            //var spawns = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_counterterrorist").Concat(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_terrorist")).Concat(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_deathmatch_spawn"));
            //SendConsoleMessage($"spawns entities: {spawns.Count()}", ConsoleColor.DarkCyan);
            var player = @event.Userid;
            if (player != null && player.IsValid && !player.IsBot && !player.IsHLTV && player.SteamID.ToString().Length == 17 && !playerData.ContainsKey(player.Slot))
            {
                var data = new DeathmatchPlayerData
                {
                    BlockRandomWeaponsIntegeration = Server.CurrentTime,
                };
                playerData[player.Slot] = data;
                if (Config.SaveWeapons)
                {
                    _ = UpdateOrLoadPlayerData(player, player.SteamID.ToString(), null, true);
                }
                else
                {
                    bool IsVIP = AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag);
                    SetupDefaultWeapons(data, player.Team, IsVIP);
                    SetupDefaultPreferences(data, IsVIP);
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
                    playerData.Remove(player.Slot);
                }
                blockedSpawns.Remove(player.Slot);
                playersWaitingForRespawn.Remove(player);
                playersWithSpawnProtection.Remove(player);
            }

            return HookResult.Continue;
        }

        [GameEventHandler(HookMode.Post)]
        public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            if (GameRules == null)
                SetGameRules();

            var player = @event.Userid;
            if (player != null && player.IsValid)
            {
                //if (Config.SpawnSystem.SpawnsMethod != 2 && (CheckedEnemiesDistance > 100 || CheckSpawnVisibility) && GameRules?.WarmupPeriod == false)
                if (Config.SpawnSystem.SpawnsMethod != 2 && CheckedEnemiesDistance > 100 && GameRules?.WarmupPeriod == false)
                    PerformRespawn(player, player.Team);

                GivePlayerWeapons(player, false);
            }

            return HookResult.Continue;
        }

        [GameEventHandler(HookMode.Pre)]
        public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            var player = @event.Userid;
            var attacker = @event.Attacker;
            if (Config.Gameplay.DisplayAllKillFeed)
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
                    foreach (var p in playerData.Keys)
                    {
                        if (p != attacker?.Slot)
                            playerData[p].DamageInfo.Remove(player.Slot);
                    }

                    if (attacker != null && attacker.IsValid && attacker != player && GetPrefsValue(data, "DamageInfo", Config.PlayersPreferences.DamageInfo.DefaultValue))
                    {
                        if (data.DamageInfo.TryGetValue(attacker.Slot, out var damageInfo))
                        {
                            var givenDamageMessage = Localizer["Chat.GivenDamageVictim", attacker.PlayerName, damageInfo.Damage, damageInfo.Hits];
                            if (!string.IsNullOrEmpty(givenDamageMessage))
                                player.PrintToChat(Localizer["Chat.Prefix"] + " " + givenDamageMessage);
                        }
                        else
                        {
                            var givenDamageMessage = Localizer["Chat.NoDamageGiven", attacker.PlayerName];
                            if (!string.IsNullOrEmpty(givenDamageMessage))
                                player.PrintToChat(Localizer["Chat.Prefix"] + " " + givenDamageMessage);
                        }
                        data.DamageInfo.Clear();
                    }
                }
                timer = IsVIP ? Config.PlayersSettings.VIP.RespawnTime : Config.PlayersSettings.NonVIP.RespawnTime;

                if (Config.Gameplay.DisplayAllKillFeed)
                    @event.FireEventToClient(player);
            }

            playersWaitingForRespawn[player] = (timer, Server.CurrentTime);
            if (attacker != null && attacker.IsValid && attacker != player && playerData.TryGetValue(attacker.Slot, out var attackerData) && attacker.PlayerPawn.Value != null)
            {
                attackerData.KillStreak++;
                if (GetPrefsValue(attackerData, "DamageInfo", Config.PlayersPreferences.DamageInfo.DefaultValue))
                {
                    if (attackerData.DamageInfo.TryGetValue(player.Slot, out var damageInfo))
                    {
                        var givenDamageMessage = Localizer["Chat.GivenDamageAttacker", player.PlayerName, damageInfo.Damage, damageInfo.Hits];
                        if (!string.IsNullOrEmpty(givenDamageMessage))
                            attacker.PrintToChat(Localizer["Chat.Prefix"] + " " + givenDamageMessage);
                        attackerData.DamageInfo.Remove(player.Slot);
                    }
                }

                bool IsHeadshot = @event.Headshot;
                bool IsKnifeKill = @event.Weapon.Contains("knife") || @event.Weapon.Contains("bayonet");

                if (IsHeadshot && GetPrefsValue(attackerData, "HeadshotKillSound", Config.PlayersPreferences.HSKillSound.DefaultValue))
                    attacker.ExecuteClientCommand("play " + Config.PlayersPreferences.HSKillSound.Path);
                else if (IsKnifeKill && GetPrefsValue(attackerData, "KnifeKillSound", Config.PlayersPreferences.KnifeKillSound.DefaultValue))
                    attacker.ExecuteClientCommand("play " + Config.PlayersPreferences.KnifeKillSound.Path);
                else if (GetPrefsValue(attackerData, "KillSound", Config.PlayersPreferences.KillSound.DefaultValue))
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
                                //Utilities.SetStateChanged(weapon.Value.As<CCSWeaponBase>(), "CBasePlayerWeapon", "m_iClip1");
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
                            //Utilities.SetStateChanged(activeWeapon.As<CCSWeaponBase>(), "CBasePlayerWeapon", "m_iClip1");
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

                if (Config.Gameplay.DisplayAllKillFeed)
                    @event.FireEventToClient(attacker);
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

            if (attacker != null && attacker.IsValid && playerData.TryGetValue(attacker.Slot, out var attackerData))
            {
                if (ActiveMode.OnlyHS)
                {
                    if (@event.Hitgroup == 1)
                    {
                        if (Config.PlayersPreferences.DamageInfo.Enabled && GetPrefsValue(attackerData, "DamageInfo", Config.PlayersPreferences.DamageInfo.DefaultValue))
                        {
                            if (!attackerData.DamageInfo.TryGetValue(player.Slot, out var damageInfo))
                            {
                                damageInfo = new DamageData();
                                attackerData.DamageInfo[player.Slot] = damageInfo;
                            }
                            damageInfo.Damage += @event.DmgHealth;
                            damageInfo.Hits++;
                        }
                        if (GetPrefsValue(attackerData, "HitSound", Config.PlayersPreferences.HitSound.DefaultValue) && (!@event.Weapon.Contains("knife") || !@event.Weapon.Contains("bayonet")))
                            attacker.ExecuteClientCommand("play " + Config.PlayersPreferences.HitSound.Path);
                    }
                }
                else
                {
                    if (@event.Hitgroup != 1)
                    {
                        if ((!@event.Weapon.Contains("knife") || !@event.Weapon.Contains("bayonet")) && GetPrefsValue(attackerData, "HitSound", Config.PlayersPreferences.HitSound.DefaultValue))
                        {
                            if (!GetPrefsValue(attackerData, "OnlyHS", Config.PlayersPreferences.OnlyHS.DefaultValue))
                                attacker.ExecuteClientCommand("play " + Config.PlayersPreferences.HitSound.Path);
                        }
                    }
                    else if (GetPrefsValue(attackerData, "HitSound", Config.PlayersPreferences.HitSound.DefaultValue))
                    {
                        attacker.ExecuteClientCommand("play " + Config.PlayersPreferences.HitSound.Path);
                    }

                    if (Config.PlayersPreferences.DamageInfo.Enabled && GetPrefsValue(attackerData, "DamageInfo", Config.PlayersPreferences.DamageInfo.DefaultValue))
                    {
                        if (!attackerData.DamageInfo.TryGetValue(player.Slot, out var damageInfo))
                        {
                            damageInfo = new DamageData();
                            attackerData.DamageInfo[player.Slot] = damageInfo;
                        }
                        damageInfo.Damage += @event.DmgHealth;
                        damageInfo.Hits++;
                    }
                }
            }
            return HookResult.Continue;
        }

        private HookResult OnTakeDamage(DynamicHook hook)
        {
            var entity = hook.GetParam<CEntityInstance>(0);
            if (entity == null || !entity.IsValid || !entity.DesignerName.Equals("player"))
                return HookResult.Continue;

            var pawn = entity.As<CCSPlayerPawn>();
            if (pawn == null || !pawn.IsValid)
                return HookResult.Continue;

            var player = pawn.OriginalController.Get();
            if (player == null || !player.IsValid)
                return HookResult.Continue;

            var damageInfo = hook.GetParam<CTakeDamageInfo>(1);
            if (playerData.TryGetValue(player.Slot, out var victimData) && victimData.SpawnProtection)
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

            if (damageInfo.GetHitGroup() != HitGroup_t.HITGROUP_HEAD && playerData.TryGetValue(attacker.Slot, out var attackerData) && GetPrefsValue(attackerData, "OnlyHS", Config.PlayersPreferences.OnlyHS.DefaultValue))
            {
                damageInfo.Damage = 0;
                return HookResult.Continue;
            }

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
                    return HookResult.Handled;
                }
                return HookResult.Continue;
            }

            if (!IsCasualGamemode && !player.IsBot && IsHaveBlockedRandomWeaponsIntegration(player))
            {
                hook.SetReturn(AcquireResult.AlreadyPurchased);
                return HookResult.Handled;
            }

            if (ActiveMode.RandomWeapons)
            {
                if (!string.IsNullOrEmpty(Config.SoundSettings.CantEquipSound))
                    player.ExecuteClientCommand("play " + Config.SoundSettings.CantEquipSound);
                player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponsSelectIsDisabled"]}");
                hook.SetReturn(AcquireResult.AlreadyPurchased);
                return HookResult.Handled;
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
                return HookResult.Handled;
            }

            if (playerData.TryGetValue(player.Slot, out var data))
            {
                string localizerWeaponName = Localizer[vdata.Name];
                bool IsVIP = AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag);

                bool IsPrimary = PrimaryWeaponsList.Contains(vdata.Name);
                if (CheckIsWeaponRestricted(vdata.Name, IsVIP, player.Team, ActiveMode.PrimaryWeapons, ActiveCustomMode, IsPrimary))
                {
                    if (!string.IsNullOrEmpty(Config.SoundSettings.CantEquipSound))
                        player.ExecuteClientCommand("play " + Config.SoundSettings.CantEquipSound);

                    (int NonVIP, int VIP) restrictInfo = GetRestrictData(vdata.Name, player.Team);
                    player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponIsRestricted", localizerWeaponName, GetWeaponRestrictLozalizer(restrictInfo.NonVIP), GetWeaponRestrictLozalizer(restrictInfo.VIP)]}");
                    hook.SetReturn(AcquireResult.NotAllowedByMode);
                    return HookResult.Handled;
                }

                var pawn = player.PlayerPawn.Value;
                if (IsPrimary)
                {
                    if (data.PrimaryWeapon.TryGetValue(ActiveCustomMode, out var primaryWeapon) && vdata.Name == primaryWeapon)
                    {
                        player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponsIsAlreadySet", localizerWeaponName]}");
                        hook.SetReturn(AcquireResult.AlreadyOwned);
                        return HookResult.Handled;
                    }
                    data.PrimaryWeapon[ActiveCustomMode] = vdata.Name;
                    player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.PrimaryWeaponSet", localizerWeaponName]}");

                    var weapon = pawn?.GetWeaponFromSlot(gear_slot_t.GEAR_SLOT_RIFLE);
                    if (!Config.Gameplay.SwitchWeapons && weapon != null)
                    {
                        hook.SetReturn(AcquireResult.AlreadyOwned);
                        return HookResult.Handled;
                    }
                }
                else
                {
                    if (data.SecondaryWeapon.TryGetValue(ActiveCustomMode, out var secondaryWeapon) && vdata.Name == secondaryWeapon)
                    {
                        player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponsIsAlreadySet", localizerWeaponName]}");
                        hook.SetReturn(AcquireResult.AlreadyOwned);
                        return HookResult.Handled;
                    }
                    data.SecondaryWeapon[ActiveCustomMode] = vdata.Name;
                    player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.SecondaryWeaponSet", localizerWeaponName]}");

                    var weapon = pawn?.GetWeaponFromSlot(gear_slot_t.GEAR_SLOT_PISTOL);
                    if (!Config.Gameplay.SwitchWeapons && weapon != null)
                    {
                        hook.SetReturn(AcquireResult.AlreadyOwned);
                        return HookResult.Handled;
                    }
                }
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
            return HookResult.Handled;
        }
    }
}