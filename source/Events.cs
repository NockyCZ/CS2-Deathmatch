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
            if (IsPlayerValid(player, false))
            {
                if (!playerData.ContainsPlayer(player))
                {
                    DeathmatchPlayerData setupPlayerData = new DeathmatchPlayerData
                    {
                        PrimaryWeapon = "",
                        SecondaryWeapon = "",
                        KillStreak = 0,
                        OnlyHS = false,
                        KillFeed = false,
                        SpawnProtection = false,
                        ShowHud = true,
                        LastSpawn = "0"
                    };
                    playerData[player] = setupPlayerData;
                }
            }
            return HookResult.Continue;
        }

        [GameEventHandler(HookMode.Post)]
        public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            CCSPlayerController player = @event.Userid;
            GivePlayerWeapons(player, false);
            return HookResult.Continue;
        }
        [GameEventHandler]
        public HookResult OnItemPickup(EventItemPickup @event, GameEventInfo info)
        {
            CCSPlayerController player = @event.Userid;
            if (player.IsValid && player.PlayerPawn.IsValid && !player.IsHLTV && player.PawnIsAlive)
            {
                var weaponName = $"weapon_{@event.Item}";
                if (weaponName.Contains("knife"))
                {
                    if (player.IsBot)
                    {
                        player.RemoveItemByDesignerName(weaponName);
                    }
                    return HookResult.Continue;
                }
                else if ((PrimaryWeaponsList.Contains(weaponName) && !AllowedPrimaryWeaponsList.Contains(weaponName)) || (SecondaryWeaponsList.Contains(weaponName) && !AllowedSecondaryWeaponsList.Contains(weaponName)))
                {
                    player.RemoveItemByDesignerName(weaponName);
                    string replacedweaponName = Localizer[weaponName];
                    player.PrintToChat($"{Localizer["Prefix"]} {Localizer["Weapon_Disabled", replacedweaponName]}");
                    AddTimer(0.2f, () =>
                    {
                        int slot = IsHaveWeaponFromSlot(player, 0);
                        player.ExecuteClientCommand($"slot{slot}");
                    });
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
            if (player.IsValid)
            {
                if (playerData.ContainsPlayer(player))
                {
                    playerData[player].KillStreak = 0;
                }

                if (!player.IsBot && AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag))
                {
                    AddTimer(Config.PlayersSettings.RespawnTimeVIP, () =>
                    {
                        string[] spawns = CheckAvaibleSpawns(player, player.TeamNum);
                        if (!string.IsNullOrEmpty(spawns[0]))
                        {
                            if (spawns[0] == "not found")
                            {
                                DMRespawnPlayer(player, spawns, false);
                                SendConsoleMessage($"[Deathmatch] Player {player.PlayerName} was respawned, but no available spawn point was found! Therefore, a random spawn was selected.", ConsoleColor.DarkYellow);
                            }
                            else if (spawns[0] == "default")
                            {
                                DMRespawnPlayer(player, spawns, false);
                            }
                            else
                            {
                                DMRespawnPlayer(player, spawns);
                            }
                        }
                    }, TimerFlags.STOP_ON_MAPCHANGE);
                }
                else
                {
                    AddTimer(Config.PlayersSettings.RespawnTime, () =>
                    {
                        string[] spawns = CheckAvaibleSpawns(player, player.TeamNum);
                        if (!string.IsNullOrEmpty(spawns[0]))
                        {
                            if (spawns[0] == "not found")
                            {
                                DMRespawnPlayer(player, spawns, false);
                                SendConsoleMessage($"[Deathmatch] Player {player.PlayerName} was respawned, but no available spawn point was found! Therefore, a random spawn was selected.", ConsoleColor.DarkYellow);
                            }
                            else if (spawns[0] == "default")
                            {
                                DMRespawnPlayer(player, spawns, false);
                            }
                            else
                            {
                                DMRespawnPlayer(player, spawns);
                            }
                        }
                    }, TimerFlags.STOP_ON_MAPCHANGE);
                }
            }
            if (attacker.IsValid && player.IsValid && attacker != player)
            {
                if (Config.g_bRemoveDecals)
                {
                    var RemoveDecals = NativeAPI.CreateEvent("round_start", false);
                    NativeAPI.FireEventToClient(RemoveDecals, (int)player.Index);
                }
                if (playerData.ContainsPlayer(attacker))
                {
                    playerData[attacker].KillStreak++;
                    if (attacker.Pawn.Value != null)
                    {
                        int giveHP = 0;
                        if (@event.Headshot)
                        {
                            if (AdminManager.PlayerHasPermissions(attacker, Config.PlayersSettings.VIPFlag))
                            {
                                giveHP = 100 >= attacker.Pawn.Value.Health + Config.PlayersSettings.HeadshotHealthVIP ? Config.PlayersSettings.HeadshotHealthVIP : 100 - attacker.Pawn.Value.Health;
                                if (Config.PlayersSettings.RefillAmmoHSVIP)
                                {
                                    var activeWeapon = attacker.Pawn.Value.WeaponServices!.ActiveWeapon.Value;
                                    if (activeWeapon != null)
                                    {
                                        activeWeapon.Clip1 = 250;
                                        activeWeapon.ReserveAmmo[0] = 250;
                                    }
                                }
                            }
                            else
                            {
                                giveHP = 100 >= attacker.Pawn.Value.Health + Config.PlayersSettings.HeadshotHealth ? Config.PlayersSettings.HeadshotHealth : 100 - attacker.Pawn.Value.Health;
                                if (Config.PlayersSettings.RefillAmmoHS)
                                {
                                    var activeWeapon = attacker.Pawn.Value.WeaponServices!.ActiveWeapon.Value;
                                    if (activeWeapon != null)
                                    {
                                        activeWeapon.Clip1 = 250;
                                        activeWeapon.ReserveAmmo[0] = 250;
                                    }
                                }
                            }

                        }
                        else
                        {
                            if (AdminManager.PlayerHasPermissions(attacker, Config.PlayersSettings.VIPFlag))
                            {
                                giveHP = 100 >= attacker.Pawn.Value.Health + Config.PlayersSettings.KillHealthVIP ? Config.PlayersSettings.KillHealthVIP : 100 - attacker.Pawn.Value.Health;
                                if (Config.PlayersSettings.RefillAmmoVIP)
                                {
                                    var activeWeapon = attacker.Pawn.Value.WeaponServices!.ActiveWeapon.Value;
                                    if (activeWeapon != null)
                                    {
                                        activeWeapon.Clip1 = 250;
                                        activeWeapon.ReserveAmmo[0] = 250;
                                    }
                                }
                            }
                            else
                            {
                                giveHP = 100 >= attacker.Pawn.Value.Health + Config.PlayersSettings.KillHealth ? Config.PlayersSettings.KillHealth : 100 - attacker.Pawn.Value.Health;
                                if (Config.PlayersSettings.RefillAmmo)
                                {
                                    var activeWeapon = attacker.Pawn.Value.WeaponServices!.ActiveWeapon.Value;
                                    if (activeWeapon != null)
                                    {
                                        activeWeapon.Clip1 = 250;
                                        activeWeapon.ReserveAmmo[0] = 250;
                                    }
                                }
                            }
                        }

                        if (giveHP != 0)
                        {
                            attacker.Pawn.Value.Health += giveHP;
                            Utilities.SetStateChanged(attacker.Pawn.Value, "CBaseEntity", "m_iHealth");
                        }
                    }
                }
                @event.FireEventToClient(player);
                @event.FireEventToClient(attacker);

                foreach (var p in Utilities.GetPlayers().Where(p => p is { IsBot: false, IsHLTV: false, IsValid: true }))
                {
                    if (playerData.ContainsPlayer(p) && playerData[p].KillFeed && (attacker != p || player != p))
                    {
                        @event.FireEventToClient(p);
                    }
                }
            }
            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            if (Config.g_bRemoveBreakableEntities)
            {
                RemoveBreakableEntities();
            }
            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            g_bIsActiveEditor = false;
            return HookResult.Continue;
        }
        private HookResult OnPlayerBuy(CCSPlayerController? player, CommandInfo info)
        {
            if (player != null && player.IsValid)
            {
                player.PrintToChat($"{Localizer["Prefix"]} {Localizer["Setup_Weapons_By_Command"]}");
                return HookResult.Handled;
            }
            return HookResult.Continue;
        }
        private HookResult OnPlayerRadioMessage(CCSPlayerController? player, CommandInfo info)
        {
            if (Config.g_bBlockRadioMessage)
            {
                return HookResult.Handled;
            }
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
                if (!ModeData.KnifeDamage && damageInfo.Ability.IsValid && damageInfo.Ability.Value!.DesignerName.Contains("knife"))
                {
                    attacker.PrintToCenter(Localizer["Knife_damage_disabled"]);
                    damageInfo.Damage = 0;
                }
            }
            return HookResult.Continue;
        }
    }
}