using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;

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
                bool IsVIP = AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag);
                DeathmatchPlayerData setupPlayerData = new DeathmatchPlayerData
                {
                    KillSound = Config.PlayersPreferences.KillSound.Enabled ? ((Config.PlayersPreferences.KillSound.OnlyVIP && IsVIP ? Config.PlayersPreferences.KillSound.DefaultValue : false) || (!Config.PlayersPreferences.KillSound.OnlyVIP ? Config.PlayersPreferences.KillSound.DefaultValue : false)) : false,
                    HSKillSound = Config.PlayersPreferences.HSKillSound.Enabled ? ((Config.PlayersPreferences.HSKillSound.OnlyVIP && IsVIP ? Config.PlayersPreferences.HSKillSound.DefaultValue : false) || (!Config.PlayersPreferences.HSKillSound.OnlyVIP ? Config.PlayersPreferences.HSKillSound.DefaultValue : false)) : false,
                    KnifeKillSound = Config.PlayersPreferences.KnifeKillSound.Enabled ? ((Config.PlayersPreferences.KnifeKillSound.OnlyVIP && IsVIP ? Config.PlayersPreferences.KnifeKillSound.DefaultValue : false) || (!Config.PlayersPreferences.KnifeKillSound.OnlyVIP ? Config.PlayersPreferences.KnifeKillSound.DefaultValue : false)) : false,
                    HitSound = Config.PlayersPreferences.HitSound.Enabled ? ((Config.PlayersPreferences.HitSound.OnlyVIP && IsVIP ? Config.PlayersPreferences.HitSound.DefaultValue : false) || (!Config.PlayersPreferences.HitSound.OnlyVIP ? Config.PlayersPreferences.HitSound.DefaultValue : false)) : false,
                    OnlyHS = Config.PlayersPreferences.OnlyHS.Enabled ? ((Config.PlayersPreferences.OnlyHS.OnlyVIP && IsVIP ? Config.PlayersPreferences.OnlyHS.DefaultValue : false) || (!Config.PlayersPreferences.OnlyHS.OnlyVIP ? Config.PlayersPreferences.OnlyHS.DefaultValue : false)) : false,
                    HudMessages = Config.PlayersPreferences.HudMessages.Enabled ? ((Config.PlayersPreferences.HudMessages.OnlyVIP && IsVIP ? Config.PlayersPreferences.HudMessages.DefaultValue : false) || (!Config.PlayersPreferences.HudMessages.OnlyVIP ? Config.PlayersPreferences.HudMessages.DefaultValue : false)) : false,
                    BlockRandomWeaponsIntegeration = Server.CurrentTime,
                };
                playerData[player] = setupPlayerData;
            }
            return HookResult.Continue;
        }
        [GameEventHandler(HookMode.Pre)]
        public HookResult OnPlayerConnectDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player != null && player.IsValid)
            {
                if (playerData.ContainsPlayer(player))
                    playerData.RemovePlayer(player);
                if (blockedSpawns.ContainsKey(player.Slot))
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

            if (ActiveMode != null && playerData.ContainsPlayer(attacker))
            {
                if (ActiveMode.OnlyHS)
                {
                    if (@event.Hitgroup == 1 && playerData[attacker].HitSound && (!@event.Weapon.Contains("knife") || !@event.Weapon.Contains("bayonet")))
                        attacker!.ExecuteClientCommand("play " + Config.PlayersPreferences.HitSound.Path);
                }
                else
                {
                    if (@event.Hitgroup != 1 && player.PlayerPawn.IsValid)
                    {
                        if (playerData[attacker].OnlyHS)
                        {
                            player.PlayerPawn.Value!.Health = player.PlayerPawn.Value.Health >= 100 ? 100 : player.PlayerPawn.Value.Health + @event.DmgHealth;
                            player.PlayerPawn.Value.ArmorValue = player.PlayerPawn.Value.ArmorValue >= 100 ? 100 : player.PlayerPawn.Value.ArmorValue + @event.DmgArmor;
                        }
                        else if (playerData[attacker].HitSound && (!@event.Weapon.Contains("knife") || !@event.Weapon.Contains("bayonet")))
                            attacker!.ExecuteClientCommand("play " + Config.PlayersPreferences.HitSound.Path);
                    }
                }

                if (!IsLinuxServer)
                {
                    if (playerData.ContainsPlayer(player) && playerData[player].SpawnProtection)
                    {
                        player!.PlayerPawn.Value!.Health = player.PlayerPawn.Value.Health >= 100 ? 100 : player.PlayerPawn.Value.Health + @event.DmgHealth;
                        player.PlayerPawn.Value.ArmorValue = player.PlayerPawn.Value.ArmorValue >= 100 ? 100 : player.PlayerPawn.Value.ArmorValue + @event.DmgArmor;
                        return HookResult.Continue;
                    }
                    if (!ActiveMode.KnifeDamage && (@event.Weapon.Contains("knife") || @event.Weapon.Contains("bayonet")))
                    {
                        attacker!.PrintToCenter(Localizer["Hud.KnifeDamageIsDisabled"]);
                        player!.PlayerPawn.Value!.Health = player.PlayerPawn.Value.Health >= 100 ? 100 : player.PlayerPawn.Value.Health + @event.DmgHealth;
                        player.PlayerPawn.Value.ArmorValue = player.PlayerPawn.Value.ArmorValue >= 100 ? 100 : player.PlayerPawn.Value.ArmorValue + @event.DmgArmor;
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

            var timer = Config.PlayersSettings.RespawnTime;
            var IsBot = true;
            if (playerData.ContainsPlayer(player))
            {
                IsBot = false;
                playerData[player].KillStreak = 0;
                if (Config.General.RemoveDecals)
                {
                    var RemoveDecals = NativeAPI.CreateEvent("round_start", false);
                    NativeAPI.FireEventToClient(RemoveDecals, (int)player.Index);
                }
                timer = AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag) ? Config.PlayersSettings.RespawnTimeVIP : Config.PlayersSettings.RespawnTime;
                @event.FireEventToClient(player);
            }
            AddTimer(timer, () =>
            {
                if (player != null && player.IsValid && !player.PawnIsAlive)
                    PerformRespawn(player, player.Team, IsBot);
            }, TimerFlags.STOP_ON_MAPCHANGE);

            if (attacker != player && playerData.ContainsPlayer(attacker) && attacker!.PlayerPawn.Value != null)
            {
                playerData[attacker].KillStreak++;
                bool IsVIP = AdminManager.PlayerHasPermissions(attacker, Config.PlayersSettings.VIPFlag);
                bool IsHeadshot = @event.Headshot;
                bool IsKnifeKill = @event.Weapon.Contains("knife");

                if (IsHeadshot && playerData[attacker].HSKillSound)
                    attacker.ExecuteClientCommand("play " + Config.PlayersPreferences.HSKillSound.Path);
                else if (IsKnifeKill && playerData[attacker].KnifeKillSound)
                    attacker.ExecuteClientCommand("play " + Config.PlayersPreferences.KnifeKillSound.Path);
                else if (playerData[attacker].KillSound)
                    attacker.ExecuteClientCommand("play " + Config.PlayersPreferences.KillSound.Path);

                var Health = IsHeadshot
                ? (IsVIP ? Config.PlayersSettings.HeadshotHealthVIP : Config.PlayersSettings.HeadshotHealth)
                : (IsVIP ? Config.PlayersSettings.KillHealthVIP : Config.PlayersSettings.KillHealth);

                var refillAmmo = IsHeadshot
                ? (IsVIP ? Config.PlayersSettings.RefillAmmoHSVIP : Config.PlayersSettings.RefillAmmoHS)
                : (IsVIP ? Config.PlayersSettings.RefillAmmoVIP : Config.PlayersSettings.RefillAmmo);

                var giveHP = 100 >= attacker.PlayerPawn.Value.Health + Health ? Health : 100 - attacker.PlayerPawn.Value.Health;

                if (refillAmmo)
                {
                    var activeWeapon = attacker.PlayerPawn.Value.WeaponServices?.ActiveWeapon.Value;
                    if (activeWeapon != null)
                    {
                        activeWeapon.Clip1 = 250;
                        activeWeapon.ReserveAmmo[0] = 250;
                    }
                }
                if (giveHP > 0)
                {
                    attacker.PlayerPawn.Value.Health += giveHP;
                    Utilities.SetStateChanged(attacker.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
                }

                if (ActiveMode != null && ActiveMode.Armor != 0)
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
        public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            IsActiveEditor = false;
            return HookResult.Continue;
        }

        [GameEventHandler(HookMode.Pre)]
        public HookResult OnItemPurcharsed(EventItemPurchase @event, GameEventInfo info)
        {
            if (IsLinuxServer || ActiveMode == null)
                return HookResult.Continue;

            var player = @event.Userid;
            if (player != null && player.IsValid)
            {
                var weaponName = @event.Weapon;
                bool IsPrimary = PrimaryWeaponsList.Contains(weaponName);

                if (player.IsBot)
                {
                    RemovePlayerWeapon(player, weaponName);
                    var weapon = GetRandomWeaponFromList(IsPrimary ? ActiveMode.PrimaryWeapons : ActiveMode.SecondaryWeapons, false, player.Team, false);
                    if (!string.IsNullOrEmpty(weapon))
                        player.GiveNamedItem(weapon);
                    return HookResult.Continue;
                }

                /*if (!IsCasualGamemode && IsHaveBlockedRandomWeaponsIntegration(player))
                {
                    RemovePlayerWeapon(player, weaponName);
                    return HookResult.Continue;
                }*/

                if (ActiveMode.RandomWeapons && playerData.ContainsPlayer(player))
                {
                    if (!string.IsNullOrEmpty(Config.SoundSettings.CantEquipSound))
                        player.ExecuteClientCommand("play " + Config.SoundSettings.CantEquipSound);

                    player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponsSelectIsDisabled"]}");
                    RemovePlayerWeapon(player, weaponName);

                    var weapon = IsPrimary ? playerData[player].LastPrimaryWeapon : playerData[player].LastSecondaryWeapon;

                    if (string.IsNullOrEmpty(weapon))
                    {
                        weapon = GetRandomWeaponFromList(IsPrimary ? ActiveMode.PrimaryWeapons : ActiveMode.SecondaryWeapons, false, player.Team, false);
                        if (!string.IsNullOrEmpty(weapon))
                        {
                            player.GiveNamedItem(weapon);
                            if (IsPrimary)
                                playerData[player].LastPrimaryWeapon = weapon;
                            else
                                playerData[player].LastSecondaryWeapon = weapon;
                        }
                    }
                    else
                    {
                        player.GiveNamedItem(weapon);
                    }
                    return HookResult.Continue;
                }

                string replacedweaponName = Localizer[weaponName];
                if (!ActiveMode.PrimaryWeapons.Contains(weaponName) && !ActiveMode.SecondaryWeapons.Contains(weaponName))
                {
                    if (!string.IsNullOrEmpty(Config.SoundSettings.CantEquipSound))
                        player.ExecuteClientCommand("play " + Config.SoundSettings.CantEquipSound);

                    player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponIsDisabled", replacedweaponName]}");
                    player.RemoveWeapons();
                    GivePlayerWeapons(player, false, false, true);

                    return HookResult.Continue;
                }

                bool IsVIP = AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag);
                if (CheckIsWeaponRestricted(weaponName, IsVIP, player.Team, IsPrimary))
                {
                    if (!string.IsNullOrEmpty(Config.SoundSettings.CantEquipSound))
                        player.ExecuteClientCommand("play " + Config.SoundSettings.CantEquipSound);

                    var restrictInfo = GetRestrictData(weaponName, player.Team);
                    player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponIsRestricted", replacedweaponName, restrictInfo.Item1, restrictInfo.Item2]}");
                    player.RemoveWeapons();
                    GivePlayerWeapons(player, false, false, true);

                    return HookResult.Continue;
                }

                if (playerData.ContainsPlayer(player))
                {
                    if (IsPrimary)
                    {
                        playerData[player].PrimaryWeapon = weaponName;
                        player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.PrimaryWeaponSet", replacedweaponName]}");
                    }
                    else
                    {
                        playerData[player].SecondaryWeapon = weaponName;
                        player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.SecondaryWeaponSet", replacedweaponName]}");
                    }
                }
            }
            return HookResult.Continue;
        }

        [GameEventHandler(HookMode.Post)]
        public HookResult OnItemPickup(EventItemPickup @event, GameEventInfo info)
        {
            if (IsLinuxServer || ActiveMode == null)
                return HookResult.Continue;

            var player = @event.Userid;

            if (player != null && player.IsValid)
            {
                var weaponName = $"weapon_{@event.Item}";
                if (weaponName.Contains("knife") || weaponName.Contains("bayonet") || weaponName.Contains("healthshot") || ActiveMode.Utilities.Contains(weaponName))
                    return HookResult.Continue;

                if (ActiveMode.RandomWeapons)
                {
                    return HookResult.Continue;
                }

                if (!ActiveMode.PrimaryWeapons.Contains(weaponName) && !ActiveMode.SecondaryWeapons.Contains(weaponName))
                {
                    if (!player.IsBot)
                    {
                        if (!string.IsNullOrEmpty(Config.SoundSettings.CantEquipSound))
                            player.ExecuteClientCommand("play " + Config.SoundSettings.CantEquipSound);

                        string replacedweaponName = Localizer[weaponName];
                        player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponIsDisabled", replacedweaponName]}");
                    }
                    player.RemoveWeapons();
                    GivePlayerWeapons(player, false, false, true);

                    return HookResult.Continue;
                }
            }
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
            return HookResult.Stop;
        }

        private HookResult OnTakeDamage(DynamicHook hook)
        {
            if (ActiveMode == null)
                return HookResult.Continue;

            var damageInfo = hook.GetParam<CTakeDamageInfo>(1);
            var playerPawn = hook.GetParam<CCSPlayerPawn>(0);
            if (playerPawn.Controller.Value == null)
                return HookResult.Continue;
            var player = playerPawn.Controller.Value.As<CCSPlayerController>();
            if (playerData.ContainsPlayer(player) && playerData[player].SpawnProtection)
            {
                damageInfo.Damage = 0;
                return HookResult.Continue;
            }

            if (!ActiveMode.KnifeDamage && damageInfo.Ability.Value != null && (damageInfo.Ability.Value.DesignerName.Contains("knife") || damageInfo.Ability.Value.DesignerName.Contains("bayonet")))
            {
                var attackerHandle = damageInfo.Attacker;
                if (attackerHandle.Value == null)
                    return HookResult.Continue;

                var attacker = attackerHandle.Value.As<CCSPlayerController>();
                attacker.PrintToCenter(Localizer["Hud.KnifeDamageIsDisabled"]);
                damageInfo.Damage = 0;
            }
            return HookResult.Continue;
        }

        private HookResult OnWeaponCanAcquire(DynamicHook hook)
        {
            if (ActiveMode == null)
                return HookResult.Continue;

            var vdata = GetCSWeaponDataFromKeyFunc?.Invoke(-1, hook.GetParam<CEconItemView>(1).ItemDefinitionIndex.ToString());
            var controller = hook.GetParam<CCSPlayer_ItemServices>(0).Pawn.Value.Controller.Value;

            if (vdata == null)
                return HookResult.Continue;

            if (controller == null)
                return HookResult.Continue;

            var player = controller.As<CCSPlayerController>();

            if (player == null || !player.IsValid)
                return HookResult.Continue;

            if (hook.GetParam<AcquireMethod>(2) == AcquireMethod.PickUp)
            {
                if (!ActiveMode.PrimaryWeapons.Contains(vdata.Name) && !ActiveMode.SecondaryWeapons.Contains(vdata.Name))
                {
                    if (vdata.Name.Contains("knife") || vdata.Name.Contains("bayonet") || (ActiveMode != null && ActiveMode.Utilities.Contains(vdata.Name)))
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

                    var restrictInfo = GetRestrictData(vdata.Name, player.Team);
                    player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponIsRestricted", localizerWeaponName, restrictInfo.Item1, restrictInfo.Item2]}");
                    hook.SetReturn(AcquireResult.NotAllowedByMode);
                    return HookResult.Stop;
                }

                if (IsPrimary)
                {
                    if (vdata.Name == playerData[player].PrimaryWeapon)
                    {
                        player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponsIsAlreadySet", localizerWeaponName]}");
                        hook.SetReturn(AcquireResult.AlreadyOwned);
                        return HookResult.Stop;
                    }
                    playerData[player].PrimaryWeapon = vdata.Name;
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
                    if (vdata.Name == playerData[player].SecondaryWeapon)
                    {
                        player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponsIsAlreadySet", localizerWeaponName]}");
                        hook.SetReturn(AcquireResult.AlreadyOwned);
                        return HookResult.Stop;
                    }
                    playerData[player].SecondaryWeapon = vdata.Name;
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