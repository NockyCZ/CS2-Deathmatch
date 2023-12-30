using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Commands;

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
                    deathmatchPlayerData setupPlayerData = new deathmatchPlayerData
                    {
                        primaryWeapon = "",
                        secondaryWeapon = "",
                        killStreak = 0,
                        onlyHS = false,
                        killFeed = false,
                        spawnProtection = false
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
            SetupPlayerWeapons(player, false);
            /*Server.NextFrame(() =>
            {
                SetupPlayerWeapons(player, false);
            });*/
            return HookResult.Continue;
        }
        [GameEventHandler]
        public HookResult OnItemPickup(EventItemPickup @event, GameEventInfo info)
        {
            CCSPlayerController player = @event.Userid;
            if (player.IsValid || player.PlayerPawn.IsValid || !player.IsHLTV)
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
            if (attacker.IsValid && player.IsValid && attacker != player)
            {
                if (!attacker.IsBot && playerData.ContainsPlayer(attacker))
                {
                    playerData[attacker].killStreak++;
                    if (attacker.Pawn.Value != null)
                    {
                        if (@event.Headshot)
                        {
                            var giveHP = 100 >= attacker.Pawn.Value.Health + Config.g_iHeadshotHealth ? Config.g_iHeadshotHealth : 100 - attacker.Pawn.Value.Health;
                            if (giveHP != 0)
                            {
                                attacker.Pawn.Value.Health += giveHP;
                                Utilities.SetStateChanged(attacker.Pawn.Value, "CBaseEntity", "m_iHealth");
                            }
                            if (Config.g_bRefillAmmoHeadshot)
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
                            var giveHP = 100 >= attacker.Pawn.Value.Health + Config.g_iKillHealth ? Config.g_iKillHealth : 100 - attacker.Pawn.Value.Health;
                            if (giveHP != 0)
                            {
                                attacker.Pawn.Value.Health += giveHP;
                            }
                            if (Config.g_bRefillAmmoKill)
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
                }
                if (!player.IsBot && playerData.ContainsPlayer(player))
                {
                    playerData[player].killStreak = 0;
                }
                @event.FireEventToClient(player);
                @event.FireEventToClient(attacker);

                foreach (var p in Utilities.GetPlayers().Where(p => p is { IsBot: false, IsHLTV: false }))
                {
                    if (p.IsValid && playerData.ContainsPlayer(p) && playerData[p].killFeed && (attacker != p || player != p))
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
            if (GameRules().WarmupPeriod)
            {
                return HookResult.Continue;
            }

            var mode = GetModeType().ToString();
            SetupCustomMode(mode);
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
            var entindex = hook.GetParam<CEntityInstance>(0).Index;
            if (entindex == 0)
            {
                return HookResult.Continue;
            }

            var pawn = Utilities.GetEntityFromIndex<CCSPlayerPawn>((int)entindex);
            if (pawn.OriginalController.Value is not { } player)
            {
                return HookResult.Continue;
            }
            
            if (player != null && player.IsValid && player.PawnIsAlive)
            {
                var damageInfo = hook.GetParam<CTakeDamageInfo>(1);
                if (!player.IsBot && playerData.ContainsPlayer(player) && playerData[player].spawnProtection)
                {
                    damageInfo.Damage = 0;
                }
                if (!ModeData.KnifeDamage && damageInfo.Ability.Value!.DesignerName.Contains("knife"))
                {
                    player.PrintToCenter(Localizer["Knife_damage_disabled"]);
                    damageInfo.Damage = 0;
                }
            }
            return HookResult.Continue;
        }
    }
}