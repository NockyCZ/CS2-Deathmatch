using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;

namespace Deathmatch
{
    public partial class DeathmatchCore
    {
        [GameEventHandler]
        public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            CCSPlayerController player = @event.Userid;
            if (IsPlayerValid(player) && !playerData.ContainsPlayer(player))
            {
                bool IsVIP = AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag);
                DeathmatchPlayerData setupPlayerData = new DeathmatchPlayerData
                {
                    PrimaryWeapon = "",
                    SecondaryWeapon = "",
                    KillStreak = 0,
                    KillSound = Config.PlayersPreferences.KillSound.Enabled ? ((Config.PlayersPreferences.KillSound.OnlyVIP && IsVIP ? Config.PlayersPreferences.KillSound.DefaultValue : false) || (!Config.PlayersPreferences.KillSound.OnlyVIP ? Config.PlayersPreferences.KillSound.DefaultValue : false)) : false,
                    HSKillSound = Config.PlayersPreferences.HSKillSound.Enabled ? ((Config.PlayersPreferences.HSKillSound.OnlyVIP && IsVIP ? Config.PlayersPreferences.HSKillSound.DefaultValue : false) || (!Config.PlayersPreferences.HSKillSound.OnlyVIP ? Config.PlayersPreferences.HSKillSound.DefaultValue : false)) : false,
                    KnifeKillSound = Config.PlayersPreferences.KnifeKillSound.Enabled ? ((Config.PlayersPreferences.KnifeKillSound.OnlyVIP && IsVIP ? Config.PlayersPreferences.KnifeKillSound.DefaultValue : false) || (!Config.PlayersPreferences.KnifeKillSound.OnlyVIP ? Config.PlayersPreferences.KnifeKillSound.DefaultValue : false)) : false,
                    HitSound = Config.PlayersPreferences.HitSound.Enabled ? ((Config.PlayersPreferences.HitSound.OnlyVIP && IsVIP ? Config.PlayersPreferences.HitSound.DefaultValue : false) || (!Config.PlayersPreferences.HitSound.OnlyVIP ? Config.PlayersPreferences.HitSound.DefaultValue : false)) : false,
                    OnlyHS = Config.PlayersPreferences.OnlyHS.Enabled ? ((Config.PlayersPreferences.OnlyHS.OnlyVIP && IsVIP ? Config.PlayersPreferences.OnlyHS.DefaultValue : false) || (!Config.PlayersPreferences.OnlyHS.OnlyVIP ? Config.PlayersPreferences.OnlyHS.DefaultValue : false)) : false,
                    HudMessages = Config.PlayersPreferences.HudMessages.Enabled ? ((Config.PlayersPreferences.HudMessages.OnlyVIP && IsVIP ? Config.PlayersPreferences.HudMessages.DefaultValue : false) || (!Config.PlayersPreferences.HudMessages.OnlyVIP ? Config.PlayersPreferences.HudMessages.DefaultValue : false)) : false,
                    SpawnProtection = false,
                    OpenedMenu = 0,
                    LastSpawn = "0"
                };
                playerData[player] = setupPlayerData;
            }
            return HookResult.Continue;
        }

        [GameEventHandler(HookMode.Post)]
        public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            CCSPlayerController player = @event.Userid;
            if (player != null && player.IsValid)
                GivePlayerWeapons(player, false);

            return HookResult.Continue;
        }
        [GameEventHandler(HookMode.Pre)]
        public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            if (@event.Userid == null || !@event.Userid.IsValid)
                return HookResult.Continue;

            CCSPlayerController attacker = @event.Attacker;
            CCSPlayerController player = @event.Userid;
            if (playerData.ContainsPlayer(attacker))
            {
                if (ModeData.OnlyHS)
                {
                    if (@event.Hitgroup == 1 && (!@event.Weapon.Contains("knife") || !@event.Weapon.Contains("bayonet")) && playerData[attacker].HitSound)
                        attacker.ExecuteClientCommand("play " + Config.PlayersPreferences.HitSound.Path);
                }
                else
                {
                    if (@event.Hitgroup != 1 && player.IsValid && player != null && player.PlayerPawn.IsValid)
                    {
                        if (playerData[attacker].OnlyHS)
                        {
                            player.PlayerPawn.Value!.Health = player.PlayerPawn.Value.Health >= 100 ? 100 : player.PlayerPawn.Value.Health + @event.DmgHealth;
                            player.PlayerPawn.Value.ArmorValue = player.PlayerPawn.Value.ArmorValue >= 100 ? 100 : player.PlayerPawn.Value.ArmorValue + @event.DmgArmor;
                        }
                        else if (playerData[attacker].HitSound && (!@event.Weapon.Contains("knife") || !@event.Weapon.Contains("bayonet")))
                            attacker.ExecuteClientCommand("play " + Config.PlayersPreferences.HitSound.Path);
                    }
                }
            }
            return HookResult.Continue;
        }
        [GameEventHandler(HookMode.Pre)]
        public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController player = @event.Userid;
            CCSPlayerController attacker = @event.Attacker;
            info.DontBroadcast = true;

            if (player == null && !player!.IsValid)
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
                if (player != null && player.IsValid)
                {
                    string[] spawns = CheckAvaibleSpawns(player, player.TeamNum, IsBot);
                    if (!string.IsNullOrEmpty(spawns[0]))
                    {
                        switch (spawns[0])
                        {
                            case "not found":
                                RespawnPlayer(player, spawns, false);
                                SendConsoleMessage($"[Deathmatch] Player {player.PlayerName} was respawned, but no available spawn point was found! Therefore, a random spawn was selected.", ConsoleColor.DarkYellow);
                                break;
                            case "default":
                                RespawnPlayer(player, spawns, false);
                                break;
                            default:
                                RespawnPlayer(player, spawns);
                                break;
                        }
                    }
                }
            }, TimerFlags.STOP_ON_MAPCHANGE);

            if (attacker != player && playerData.ContainsPlayer(attacker) && attacker.PlayerPawn.Value != null)
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
            g_bIsActiveEditor = false;
            return HookResult.Continue;
        }
        private HookResult OnPlayerRadioMessage(CCSPlayerController? player, CommandInfo info)
        {
            if (Config.General.BlockRadioMessage)
                return HookResult.Handled;

            return HookResult.Continue;
        }
        private HookResult OnTakeDamage(DynamicHook hook)
        {
            var p = hook.GetParam<CEntityInstance>(0).Index;
            if (p == 0)
                return HookResult.Continue;

            var playerPawn = Utilities.GetEntityFromIndex<CCSPlayerPawn>((int)p);
            if (playerPawn.OriginalController.Value is not { } player)
                return HookResult.Continue;

            var damageInfo = hook.GetParam<CTakeDamageInfo>(1);
            var a = damageInfo.Attacker.Index;

            if (a == 0)
                return HookResult.Continue;

            var attackerPawn = Utilities.GetEntityFromIndex<CCSPlayerPawn>((int)a);
            if (attackerPawn.OriginalController.Value is not { } attacker)
                return HookResult.Continue;

            if (player != null && player.IsValid && attacker != null && attacker.IsValid)
            {
                if (playerData.ContainsPlayer(player) && playerData[player].SpawnProtection)
                {
                    damageInfo.Damage = 0;
                }
                if (!ModeData.KnifeDamage && damageInfo.Ability.IsValid && (damageInfo.Ability.Value!.DesignerName.Contains("knife") || damageInfo.Ability.Value!.DesignerName.Contains("bayonet")))
                {
                    attacker.PrintToCenter(Localizer["Knife_damage_disabled"]);
                    damageInfo.Damage = 0;
                }
            }
            return HookResult.Continue;
        }
        private HookResult OnWeaponCanAcquire(DynamicHook hook)
        {
            var vdata = GetCSWeaponDataFromKeyFunc?.Invoke(-1, hook.GetParam<CEconItemView>(1).ItemDefinitionIndex.ToString());
            var player = hook.GetParam<CCSPlayer_ItemServices>(0).Pawn.Value!.Controller.Value!.As<CCSPlayerController>();

            if (player == null || !player.IsValid || !player.PawnIsAlive)
                return HookResult.Continue;

            if (hook.GetParam<AcquireMethod>(2) == AcquireMethod.PickUp)
            {
                if (!AllowedPrimaryWeaponsList.Contains(vdata!.Name!) && !AllowedSecondaryWeaponsList.Contains(vdata.Name))
                {
                    if (vdata.Name.Contains("knife") || vdata.Name.Contains("bayonet"))
                        return HookResult.Continue;

                    hook.SetReturn(AcquireResult.AlreadyOwned);
                    return HookResult.Stop;
                }
                return HookResult.Continue;
            }

            if (ModeData.RandomWeapons)
            {
                if (!string.IsNullOrEmpty(Config.SoundSettings.CantEquipSound))
                    player.ExecuteClientCommand("play " + Config.SoundSettings.CantEquipSound);
                player.PrintToChat($"{Localizer["Prefix"]} {Localizer["Weapon_Select_Is_Disabled"]}");
                hook.SetReturn(AcquireResult.AlreadyPurchased);
                return HookResult.Stop;
            }

            if (!AllowedPrimaryWeaponsList.Contains(vdata!.Name!) && !AllowedSecondaryWeaponsList.Contains(vdata.Name))
            {
                /*if (vdata.Name.Contains("knife") || vdata.Name.Contains("bayonet"))
                {
                    if (player.IsBot)
                    {
                        hook.SetReturn(AcquireResult.AlreadyPurchased);
                        return HookResult.Stop;
                    }
                    return HookResult.Continue;
                }*/
                if (!player.IsBot)
                {
                    if (!string.IsNullOrEmpty(Config.SoundSettings.CantEquipSound))
                        player.ExecuteClientCommand("play " + Config.SoundSettings.CantEquipSound);

                    string replacedweaponName = Localizer[vdata.Name];
                    player.PrintToChat($"{Localizer["Prefix"]} {Localizer["Weapon_Disabled", replacedweaponName]}");
                }
                hook.SetReturn(AcquireResult.AlreadyPurchased);
                return HookResult.Stop;
            }

            if (playerData.ContainsPlayer(player))
            {
                string localizerWeaponName = Localizer[vdata.Name];
                if (CheckIsWeaponRestricted(vdata.Name, AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag), player.TeamNum))
                {
                    if (!string.IsNullOrEmpty(Config.SoundSettings.CantEquipSound))
                        player.ExecuteClientCommand("play " + Config.SoundSettings.CantEquipSound);

                    RestrictedWeaponsInfo restrict = RestrictedWeapons[vdata.Name];
                    player.PrintToChat($"{Localizer["Prefix"]} {Localizer["Weapon_Is_Restricted", localizerWeaponName, restrict.nonVIPRestrict, restrict.VIPRestrict]}");
                    hook.SetReturn(AcquireResult.NotAllowedByMode);
                    return HookResult.Stop;
                }

                bool IsPrimary = AllowedPrimaryWeaponsList.Contains(vdata.Name);
                if (IsPrimary)
                {
                    if (vdata.Name == playerData[player].PrimaryWeapon)
                    {
                        player.PrintToChat($"{Localizer["Prefix"]} {Localizer["Weapon_Is_Already_Set", localizerWeaponName]}");
                        hook.SetReturn(AcquireResult.AlreadyOwned);
                        return HookResult.Stop;
                    }
                    playerData[player].PrimaryWeapon = vdata.Name;
                    player.PrintToChat($"{Localizer["Prefix"]} {Localizer["PrimaryWeapon_Set", localizerWeaponName]}");
                    if (!Config.Gameplay.SwitchWeapons && IsHaveWeaponFromSlot(player, 1) == 1)
                    {
                        hook.SetReturn(AcquireResult.AlreadyOwned);
                        return HookResult.Stop;
                    }
                }
                else
                {
                    if (vdata.Name == playerData[player].SecondaryWeapon)
                    {
                        player.PrintToChat($"{Localizer["Prefix"]} {Localizer["Weapon_Is_Already_Set", localizerWeaponName]}");
                        hook.SetReturn(AcquireResult.AlreadyOwned);
                        return HookResult.Stop;
                    }
                    playerData[player].SecondaryWeapon = vdata.Name;
                    player.PrintToChat($"{Localizer["Prefix"]} {Localizer["SecondaryWeapon_Set", localizerWeaponName]}");
                    if (!Config.Gameplay.SwitchWeapons && IsHaveWeaponFromSlot(player, 2) == 2)
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