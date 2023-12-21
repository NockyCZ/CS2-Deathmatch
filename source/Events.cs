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
                        weaponBuys = 0,
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
            Server.NextFrame(() =>
            {
                if (IsPlayerValid(player, true))
                {
                    player.InGameMoneyServices!.Account = 10000;
                    if (playerData.ContainsPlayer(player))
                    {
                        playerData[player].weaponBuys = 0;
                        playerData[player].spawnProtection = false;
                        AddTimer((float)Config.g_iProtectionTime, () =>
                        {
                            playerData[player].spawnProtection = false;
                        }, TimerFlags.STOP_ON_MAPCHANGE);
                        if (!g_bIsOnlyWeaponSet)
                        {
                            if (!string.IsNullOrEmpty(playerData[player].primaryWeapon))
                            {
                                if (!BlockedWeaponsList.Contains(playerData[player].primaryWeapon))
                                {
                                    player.GiveNamedItem(playerData[player].primaryWeapon);
                                }
                                else
                                {
                                    if (!g_bIsSecondarySet)
                                    {
                                        string replacedweaponName = Localizer[playerData[player].primaryWeapon];
                                        player.PrintToChat($"{Localizer["Prefix"]} {Localizer["PrimaryWeapon_Disabled", replacedweaponName]}");
                                    }
                                }
                            }
                            if (!string.IsNullOrEmpty(playerData[player].secondaryWeapon))
                            {
                                if (!BlockedWeaponsList.Contains(playerData[player].secondaryWeapon))
                                {
                                    player.GiveNamedItem(playerData[player].secondaryWeapon);
                                }
                                else
                                {
                                    if (!g_bIsPrimarySet)
                                    {
                                        string replacedweaponName = Localizer[playerData[player].secondaryWeapon];
                                        player.PrintToChat($"{Localizer["Prefix"]} {Localizer["SecondaryWeapon_Disabled", replacedweaponName]}");
                                    }
                                }
                            }
                        }
                    }
                }
                else if (player.IsValid && player.IsBot && !player.IsHLTV)
                {
                    player.InGameMoneyServices!.Account = 10000;
                    /*Random random = new Random();
                    int iPrimary = random.Next(0, BOTsPrimaryWeaponsList.Count);
                    int iSecondary = random.Next(0, BOTsSecondaryWeaponsList.Count);*/
                    if (BOTsPrimaryWeaponsList.Count != 0 && !g_bIsOnlyWeaponSet)
                    {
                        string weapon = GetRandomWeaponFromList(BOTsPrimaryWeaponsList);
                        player.GiveNamedItem(weapon);
                        //Console.WriteLine($"{player.PlayerName} primary: {weapon}");
                    }
                    /*else{
                        Console.WriteLine($" primary: {g_bIsOnlyWeaponSet} {BOTsPrimaryWeaponsList.Count}");
                    }*/
                    if (BOTsSecondaryWeaponsList.Count != 0 && !g_bIsOnlyWeaponSet)
                    {
                        string weapon = GetRandomWeaponFromList(BOTsSecondaryWeaponsList);
                        player.GiveNamedItem(weapon);
                        //Console.WriteLine($"{player.PlayerName} secondary: {weapon}");
                    }
                    /*else{
                        Console.WriteLine($" secondary: {g_bIsOnlyWeaponSet} {BOTsSecondaryWeaponsList.Count}");
                    }*/
                }
            });
            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnItemPurchase(EventItemPurchase @event, GameEventInfo info)
        {
            CCSPlayerController player = @event.Userid;
            if (IsPlayerValid(player, true))
            {
                if (playerData.ContainsPlayer(player))
                {
                    player.InGameMoneyServices!.Account = 10000;
                    playerData[player].weaponBuys++;
                    string weaponName = @event.Weapon;
                    if (PrimaryWeaponsList.Contains(weaponName) && !BlockedWeaponsList.Contains(weaponName))
                    {
                        playerData[player].primaryWeapon = weaponName;
                        string replacedweaponName = Localizer[weaponName];
                        player.PrintToChat($"{Localizer["Prefix"]} {Localizer["PrimaryWeapon_Set", replacedweaponName]}");
                    }
                    else if (SecondaryWeaponsList.Contains(weaponName) && !BlockedWeaponsList.Contains(weaponName))
                    {
                        playerData[player].secondaryWeapon = weaponName;
                        string replacedweaponName = Localizer[weaponName];
                        player.PrintToChat($"{Localizer["Prefix"]} {Localizer["SecondaryWeapon_Set", replacedweaponName]}");
                    }
                }
            }
            return HookResult.Continue;
        }
        [GameEventHandler]
        public HookResult OnItemPickup(EventItemPickup @event, GameEventInfo info)
        {
            CCSPlayerController player = @event.Userid;
            if (IsPlayerValid(player, true))
            {
                var weaponName = $"weapon_{@event.Item}";
                if (BlockedWeaponsList.Contains(weaponName))
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
        public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            if (!ModeData.KnifeDamage)
            {
                if (@event.Weapon == "knife")
                {
                    @event.Attacker.PrintToCenter(Localizer["Knife_damage_disabled"]);
                    if (@event.Userid!.PlayerPawn!.Value!.Health + @event.DmgHealth <= 100)
                    {
                        @event.Userid.PlayerPawn.Value.Health = @event.Userid.PlayerPawn.Value.Health + @event.DmgHealth;
                        @event.Userid.PlayerPawn.Value.ArmorValue = @event.Userid.PlayerPawn.Value.ArmorValue + @event.DmgArmor;
                    }
                    else
                    {
                        @event.Userid.PlayerPawn.Value.Health = 100;
                        @event.Userid.PlayerPawn.Value.ArmorValue = 100;
                    }
                    @event.Userid.PlayerPawn.Value.VelocityModifier = 1;
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

            Utilities.GetPlayers().ForEach(player =>
            {
                if (player.IsValid && player.PawnIsAlive)
                {
                    player.RemoveWeapons();
                }
            });
            return HookResult.Continue;
        }
        private HookResult OnPlayerBuy(CCSPlayerController? player, CommandInfo info)
        {
            if (IsPlayerValid(player!) && playerData.ContainsPlayer(player!)){
                if(playerData[player!].weaponBuys >= Config.g_iMaxWeaponBuys){
                    player!.PrintToChat($"{Localizer["Prefix"]} {Localizer["Maximum_Weapons_Buys", Config.g_iMaxWeaponBuys]}");
                    return HookResult.Handled;
                }
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

            if (IsPlayerValid(player, true))
            {
                if (playerData.ContainsPlayer(player) && playerData[player].spawnProtection)
                    hook.GetParam<CTakeDamageInfo>(1).Damage = 0;
            }
            return HookResult.Continue;
        }
    }
}