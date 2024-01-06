using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace Deathmatch
{
    public partial class DeathmatchCore
    {
        public class deathmatchPlayerData
        {
            public required string primaryWeapon { get; set; }
            public required string secondaryWeapon { get; set; }
            public required bool spawnProtection { get; set; }
            public required int killStreak { get; set; }
            public required bool onlyHS { get; set; }
            public required bool killFeed { get; set; }
            public required bool showHud { get; set; }
            public required string lastSpawn { get; set; }
        }
        public void SetupPlayerWeapons(CCSPlayerController player, string weaponName, CommandInfo info)
        {
            if (string.IsNullOrEmpty(weaponName))
            {
                string primaryWeapons = "";
                string secondaryWeapons = "";
                if (AllowedPrimaryWeaponsList.Count != 0)
                {
                    foreach (string weapon in AllowedPrimaryWeaponsList)
                    {
                        if (string.IsNullOrEmpty(primaryWeapons))
                        {
                            primaryWeapons = $"{ChatColors.Green} {weapon.Replace("weapon_", "")}";
                        }
                        else
                        {
                            primaryWeapons = $"{primaryWeapons}{ChatColors.Default}, {ChatColors.Green}{weapon.Replace("weapon_", "")}";
                        }
                    }
                    info.ReplyToCommand($"{Localizer["Allowed_Primary_Weapons"]}");
                    info.ReplyToCommand($"{ChatColors.Darkred}• {primaryWeapons}");
                }
                if (AllowedSecondaryWeaponsList.Count != 0)
                {
                    foreach (string weapon in AllowedSecondaryWeaponsList)
                    {
                        if (string.IsNullOrEmpty(secondaryWeapons))
                        {
                            secondaryWeapons = $"{ChatColors.Green} {weapon.Replace("weapon_", "")}";
                        }
                        else
                        {
                            secondaryWeapons = $"{secondaryWeapons}{ChatColors.Default}, {ChatColors.Green}{weapon.Replace("weapon_", "")}";
                        }
                    }
                    info.ReplyToCommand($"{Localizer["Allowed_Secondary_Weapons"]}");
                    info.ReplyToCommand($"{ChatColors.Darkred}• {secondaryWeapons}");
                }
                info.ReplyToCommand($"{Localizer["Prefix"]} /gun <weapon name>");
                return;
            }

            string replacedweaponName = "";
            string matchingValues = "";
            int matchingCount = 0;
            if (weaponName.Contains("m4a4"))
            {
                weaponName = "weapon_m4a1";
                matchingCount = 1;
            }
            else if (weaponName.Contains("m4a1"))
            {
                weaponName = "weapon_m4a1_silencer";
                matchingCount = 1;
            }
            else
            {
                foreach (string weapon in PrimaryWeaponsList)
                {
                    if (weapon.Contains(weaponName))
                    {
                        replacedweaponName = weapon;
                        matchingCount++;
                        string localizerWeaponName = Localizer[replacedweaponName];
                        if (matchingCount == 1)
                        {
                            matchingValues = localizerWeaponName;
                        }
                        else if (matchingCount == 2)
                        {
                            matchingValues = $"{ChatColors.Green}{matchingValues}{ChatColors.Default}, {ChatColors.Green}{localizerWeaponName}";
                        }
                        else if (matchingCount > 2)
                        {
                            matchingValues = $"{matchingValues}{ChatColors.Default}, {ChatColors.Green}{localizerWeaponName}";
                        }
                    }
                }
                if (matchingCount == 0)
                {
                    foreach (string weapon in SecondaryWeaponsList)
                    {
                        if (weapon.Contains(weaponName))
                        {
                            replacedweaponName = weapon;
                            matchingCount++;
                            string localizerWeaponName = Localizer[replacedweaponName];
                            if (matchingCount == 1)
                            {
                                matchingValues = localizerWeaponName;
                            }
                            else if (matchingCount == 2)
                            {
                                matchingValues = $"{ChatColors.Green}{matchingValues}{ChatColors.Default}, {ChatColors.Green}{localizerWeaponName}";
                            }
                            else if (matchingCount > 2)
                            {
                                matchingValues = $"{matchingValues}{ChatColors.Default}, {ChatColors.Green}{localizerWeaponName}";
                            }
                        }
                    }
                }
                if (matchingCount != 0)
                {
                    weaponName = replacedweaponName;
                }
            }

            if (matchingCount > 1)
            {
                info.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["Multiple_Weapon_Select"]} {ChatColors.Default}( {matchingValues} {ChatColors.Default})");
                return;
            }
            else if (matchingCount == 0)
            {
                info.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["Weapon_Name_Not_Found", weaponName]}");
                return;
            }

            if (AllowedPrimaryWeaponsList.Contains(weaponName))
            {
                string localizerWeaponName = Localizer[weaponName];
                if (weaponName == playerData[player].primaryWeapon)
                {
                    info.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["Weapon_Is_Already_Set", localizerWeaponName]}");
                    return;
                }
                if (CheckIsWeaponRestricted(weaponName, AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag), player.TeamNum))
                {
                    RestrictedWeaponsInfo restrict = RestrictedWeapons[weaponName];
                    info.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["Weapon_Is_Restricted", localizerWeaponName, restrict.nonVIPRestrict, restrict.VIPRestrict]}");
                    return;
                }
                playerData[player].primaryWeapon = weaponName;
                info.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["PrimaryWeapon_Set", localizerWeaponName]}");
                if (player.PawnIsAlive && IsHaveWeaponFromSlot(player, 1) != 1)
                {
                    player.GiveNamedItem(weaponName);
                }
                return;
            }
            else if (AllowedSecondaryWeaponsList.Contains(weaponName))
            {
                string localizerWeaponName = Localizer[weaponName];
                if (weaponName == playerData[player].secondaryWeapon)
                {
                    info.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["Weapon_Is_Already_Set", localizerWeaponName]}");
                    return;
                }
                if (CheckIsWeaponRestricted(weaponName, AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag), player.TeamNum))
                {
                    RestrictedWeaponsInfo restrict = RestrictedWeapons[weaponName];
                    info.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["Weapon_Is_Restricted", localizerWeaponName, restrict.nonVIPRestrict, restrict.VIPRestrict]}");
                    return;
                }
                playerData[player].secondaryWeapon = weaponName;
                info.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["SecondaryWeapon_Set", localizerWeaponName]}");
                if (player.PawnIsAlive && IsHaveWeaponFromSlot(player, 2) != 2)
                {
                    player.GiveNamedItem(weaponName);
                }
                return;
            }
            else
            {
                string localizerWeaponName = Localizer[weaponName];
                info.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["Weapon_Disabled", localizerWeaponName]}");
                return;
            }
        }
        public void GivePlayerWeapons(CCSPlayerController player, bool bNewMode)
        {
            if (playerData.ContainsPlayer(player))
            {
                player.InGameMoneyServices!.Account = 0;
                if (!bNewMode)
                {
                    playerData[player].spawnProtection = true;
                    if (AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag))
                    {
                        AddTimer(Config.PlayersSettings.ProtectionTimeVIP, () =>
                        {
                            playerData[player].spawnProtection = false;
                        }, TimerFlags.STOP_ON_MAPCHANGE);
                    }
                    else
                    {
                        AddTimer(Config.PlayersSettings.ProtectionTime, () =>
                        {
                            playerData[player].spawnProtection = false;
                        }, TimerFlags.STOP_ON_MAPCHANGE);
                    }
                }

                int slot = IsHaveWeaponFromSlot(player, 0);
                if (slot == 1 || slot == 2)
                {
                    return;
                }
                bool SetupWeaponsMsg = false;
                if (AllowedPrimaryWeaponsList.Count != 0)
                {
                    if (ModeData.RandomWeapons)
                    {
                        int weapon = GetRandomWeaponFromList(AllowedPrimaryWeaponsList);
                        player.GiveNamedItem(AllowedPrimaryWeaponsList[weapon]);
                    }
                    else if (!string.IsNullOrEmpty(playerData[player].primaryWeapon) && AllowedPrimaryWeaponsList.Count > 1)
                    {
                        if (AllowedPrimaryWeaponsList.Contains(playerData[player].primaryWeapon))
                        {
                            if (CheckIsWeaponRestricted(playerData[player].primaryWeapon, AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag), player.TeamNum))
                            {
                                playerData[player].primaryWeapon = "";
                                string localizerWeaponName = Localizer[playerData[player].primaryWeapon];
                                RestrictedWeaponsInfo restrict = RestrictedWeapons[playerData[player].primaryWeapon];
                                player.PrintToChat($"{Localizer["Prefix"]} {Localizer["Weapon_Is_Restricted", localizerWeaponName, restrict.nonVIPRestrict, restrict.VIPRestrict]}");
                                if (Config.DefaultModeWeapons == 2)
                                {
                                    int weapon = GetRandomWeaponFromList(AllowedPrimaryWeaponsList);
                                    player.GiveNamedItem(AllowedPrimaryWeaponsList[weapon]);
                                }
                                else if (Config.DefaultModeWeapons == 1)
                                {
                                    player.GiveNamedItem(AllowedPrimaryWeaponsList[0]);
                                }
                                else
                                {
                                    player.PrintToChat($"{Localizer["Prefix"]} {Localizer["Setup_Weapons_By_Command"]}");
                                    SetupWeaponsMsg = true;
                                }
                            }
                            else
                            {
                                player.GiveNamedItem(playerData[player].primaryWeapon);
                            }
                        }
                        else
                        {
                            string replacedweaponName = Localizer[playerData[player].primaryWeapon];
                            player.PrintToChat($"{Localizer["Prefix"]} {Localizer["PrimaryWeapon_Disabled", replacedweaponName]}");

                            if (Config.DefaultModeWeapons == 2)
                            {
                                int weapon = GetRandomWeaponFromList(AllowedPrimaryWeaponsList);
                                player.GiveNamedItem(AllowedPrimaryWeaponsList[weapon]);
                            }
                            else if (Config.DefaultModeWeapons == 1)
                            {
                                player.GiveNamedItem(AllowedPrimaryWeaponsList[0]);
                            }
                            else
                            {
                                player.PrintToChat($"{Localizer["Prefix"]} {Localizer["Setup_Weapons_By_Command"]}");
                                SetupWeaponsMsg = true;
                            }
                        }
                    }
                    else
                    {
                        if (AllowedPrimaryWeaponsList.Count == 1)
                        {
                            player.GiveNamedItem(AllowedPrimaryWeaponsList[0]);
                        }
                        else
                        {
                            if (Config.DefaultModeWeapons == 2)
                            {
                                int weapon = GetRandomWeaponFromList(AllowedPrimaryWeaponsList);
                                player.GiveNamedItem(AllowedPrimaryWeaponsList[weapon]);
                            }
                            else if (Config.DefaultModeWeapons == 1)
                            {
                                player.GiveNamedItem(AllowedPrimaryWeaponsList[0]);
                            }
                            else
                            {
                                player.PrintToChat($"{Localizer["Prefix"]} {Localizer["Setup_Weapons_By_Command"]}");
                                SetupWeaponsMsg = true;
                            }
                        }
                    }
                }
                if (AllowedSecondaryWeaponsList.Count != 0)
                {
                    if (ModeData.RandomWeapons)
                    {
                        int weapon = GetRandomWeaponFromList(AllowedSecondaryWeaponsList);
                        player.GiveNamedItem(AllowedSecondaryWeaponsList[weapon]);
                    }
                    else if (!string.IsNullOrEmpty(playerData[player].secondaryWeapon) && AllowedSecondaryWeaponsList.Count > 1)
                    {
                        if (AllowedSecondaryWeaponsList.Contains(playerData[player].secondaryWeapon))
                        {
                            if (CheckIsWeaponRestricted(playerData[player].secondaryWeapon, AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag), player.TeamNum))
                            {
                                playerData[player].secondaryWeapon = "";
                                string localizerWeaponName = Localizer[playerData[player].secondaryWeapon];
                                RestrictedWeaponsInfo restrict = RestrictedWeapons[playerData[player].secondaryWeapon];
                                player.PrintToChat($"{Localizer["Prefix"]} {Localizer["Weapon_Is_Restricted", localizerWeaponName, restrict.nonVIPRestrict, restrict.VIPRestrict]}");
                                if (Config.DefaultModeWeapons == 2)
                                {
                                    int weapon = GetRandomWeaponFromList(AllowedSecondaryWeaponsList);
                                    player.GiveNamedItem(AllowedSecondaryWeaponsList[weapon]);
                                }
                                else if (Config.DefaultModeWeapons == 1)
                                {
                                    player.GiveNamedItem(AllowedSecondaryWeaponsList[0]);
                                }
                                else
                                {
                                    if (!SetupWeaponsMsg)
                                        player.PrintToChat($"{Localizer["Prefix"]} {Localizer["Setup_Weapons_By_Command"]}");
                                }
                            }
                            else
                            {
                                player.GiveNamedItem(playerData[player].secondaryWeapon);
                            }
                        }
                        else
                        {
                            string replacedweaponName = Localizer[playerData[player].secondaryWeapon];
                            player.PrintToChat($"{Localizer["Prefix"]} {Localizer["SecondaryWeapon_Disabled", replacedweaponName]}");

                            if (Config.DefaultModeWeapons == 2)
                            {
                                int weapon = GetRandomWeaponFromList(AllowedSecondaryWeaponsList);
                                player.GiveNamedItem(AllowedSecondaryWeaponsList[weapon]);
                            }
                            else if (Config.DefaultModeWeapons == 1)
                            {
                                player.GiveNamedItem(AllowedSecondaryWeaponsList[0]);
                            }
                            else
                            {
                                if (!SetupWeaponsMsg)
                                    player.PrintToChat($"{Localizer["Prefix"]} {Localizer["Setup_Weapons_By_Command"]}");
                            }
                        }
                    }
                    else
                    {
                        if (AllowedSecondaryWeaponsList.Count == 1)
                        {
                            player.GiveNamedItem(AllowedSecondaryWeaponsList[0]);
                        }
                        else
                        {
                            if (Config.DefaultModeWeapons == 2)
                            {
                                int weapon = GetRandomWeaponFromList(AllowedSecondaryWeaponsList);
                                player.GiveNamedItem(AllowedSecondaryWeaponsList[weapon]);
                            }
                            else if (Config.DefaultModeWeapons == 1)
                            {
                                player.GiveNamedItem(AllowedSecondaryWeaponsList[0]);
                            }
                            else
                            {
                                if (!SetupWeaponsMsg)
                                    player.PrintToChat($"{Localizer["Prefix"]} {Localizer["Setup_Weapons_By_Command"]}");
                            }
                        }
                    }
                }
            }
            else if (player != null && player.IsValid && !player.IsHLTV)
            {
                player.InGameMoneyServices!.Account = 0;
                AddTimer(0.2f, () =>
                {
                    int slot = IsHaveWeaponFromSlot(player, slot: 0);
                    if (slot == 1 || slot == 2)
                    {
                        return;
                    }
                    if (AllowedPrimaryWeaponsList.Count != 0)
                    {
                        int weapon = GetRandomWeaponFromList(AllowedPrimaryWeaponsList);
                        player.GiveNamedItem(AllowedPrimaryWeaponsList[weapon]);
                    }
                    if (AllowedSecondaryWeaponsList.Count != 0)
                    {
                        int weapon = GetRandomWeaponFromList(AllowedSecondaryWeaponsList);
                        player.GiveNamedItem(AllowedSecondaryWeaponsList[weapon]);
                    }
                }, TimerFlags.STOP_ON_MAPCHANGE);
            }
        }
        public void DMRespawnPlayer(CCSPlayerController player, string[] spawn, bool teleport = true)
        {
            if (!player.IsValid || !player.PlayerPawn.IsValid || player.PawnIsAlive)
                return;

            if (player.TeamNum == 2 || player.TeamNum == 3)
            {
                MemoryFunctionVoid<CCSPlayerController, CCSPlayerPawn, bool, bool> RespawnFunc = new(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? respawnLinuxSig : respawnWindowsSig);
                RespawnFunc.Invoke(player, player.PlayerPawn.Value!, true, false);
                VirtualFunction.CreateVoid<CCSPlayerController>(player.Handle, GameData.GetOffset("CCSPlayerController_Respawn"))(player);

                if (teleport)
                {
                    var position = ParseVector(spawn[0]);
                    var angle = ParseQAngle(spawn[1]);
                    player.PlayerPawn.Value!.Teleport(position, angle, new Vector(0, 0, 0));
                }
            }
        }
        public bool IsPlayerValid(CCSPlayerController player, bool alive = false)
        {
            if (player is null || !player.IsValid || !player.PlayerPawn.IsValid || player.IsBot || player.IsHLTV)
                return false;
            if (alive && player.PawnIsAlive)
            {
                return true;
            }
            else if (alive && !player.PawnIsAlive)
            {
                return false;
            }
            return true;
        }
        public class PlayerCache<T> : Dictionary<int, T>
        {
            public T this[CCSPlayerController controller]
            {
                get
                {
                    if (controller is null || !controller.IsValid)
                    {
                        throw new ArgumentException("Invalid player controller");
                    }

                    if (controller.IsBot || controller.IsHLTV)
                    {
                        throw new ArgumentException("Player controller is BOT or HLTV");
                    }

                    if (!base.ContainsKey(controller.Slot))
                    {
                        throw new KeyNotFoundException($"Player with ID {controller.Slot} not found in cache");
                    }

                    if (base.TryGetValue(controller.Slot, out T? value))
                    {
                        return value;
                    }

                    return default(T)!;
                }
                set
                {
                    if (controller is null || !controller.IsValid || !controller.PlayerPawn.IsValid)
                    {
                        throw new ArgumentException("Invalid player controller");
                    }

                    if (controller.IsBot || controller.IsHLTV)
                    {
                        throw new ArgumentException("Player controller is BOT or HLTV");
                    }

                    this[controller.Slot] = value;
                }
            }

            public bool ContainsPlayer(CCSPlayerController player)
            {
                if (player is null || !player.IsValid || !player.PlayerPawn.IsValid)
                {
                    //throw new ArgumentException("Invalid player controller");
                    return false;
                }

                if (player.IsBot || player.IsHLTV)
                {
                    //throw new ArgumentException("Player controller is BOT or HLTV");
                    return false;
                }

                return base.ContainsKey(player.Slot);
            }

            public bool RemovePlayer(CCSPlayerController player)
            {
                if (player is null || !player.IsValid || !player.PlayerPawn.IsValid)
                {
                    throw new ArgumentException("Invalid player controller");
                }

                if (player.IsBot || player.IsHLTV)
                {
                    throw new ArgumentException("Player controller is BOT or HLTV");
                }

                return base.Remove(player.Slot);
            }
        }
    }
}