using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Timers;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;

namespace Deathmatch
{
    public partial class DeathmatchCore
    {
        public class DeathmatchPlayerData
        {
            public required string PrimaryWeapon { get; set; }
            public required string SecondaryWeapon { get; set; }
            public required bool SpawnProtection { get; set; }
            public required int KillStreak { get; set; }
            public required bool KillSound { get; set; }
            public required bool HSKillSound { get; set; }
            public required bool KnifeKillSound { get; set; }
            public required bool HitSound { get; set; }
            public required bool OnlyHS { get; set; }
            public required bool HudMessages { get; set; }
            public required string LastSpawn { get; set; }
            public required int OpenedMenu { get; set; }
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
                    info.ReplyToCommand($"{ChatColors.DarkRed}• {primaryWeapons.ToUpper()}");
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
                    info.ReplyToCommand($"{ChatColors.DarkRed}• {secondaryWeapons.ToUpper()}");
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
                            matchingValues = $"{ChatColors.Green}{localizerWeaponName}";
                        }
                        else if (matchingCount > 1)
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
                                matchingValues = $"{ChatColors.Green}{localizerWeaponName}";
                            }
                            else if (matchingCount > 1)
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
                if (!string.IsNullOrEmpty(Config.SoundSettings.CantEquipSound))
                    player.ExecuteClientCommand("play " + Config.SoundSettings.CantEquipSound);
                info.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["Weapon_Name_Not_Found", weaponName]}");
                return;
            }

            if (AllowedPrimaryWeaponsList.Contains(weaponName))
            {
                string localizerWeaponName = Localizer[weaponName];
                if (weaponName == playerData[player].PrimaryWeapon)
                {
                    if (!string.IsNullOrEmpty(Config.SoundSettings.CantEquipSound))
                        player.ExecuteClientCommand("play " + Config.SoundSettings.CantEquipSound);
                    info.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["Weapon_Is_Already_Set", localizerWeaponName]}");
                    return;
                }
                if (CheckIsWeaponRestricted(weaponName, AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag), player.TeamNum))
                {
                    if (!string.IsNullOrEmpty(Config.SoundSettings.CantEquipSound))
                        player.ExecuteClientCommand("play " + Config.SoundSettings.CantEquipSound);
                    RestrictedWeaponsInfo restrict = RestrictedWeapons[weaponName];
                    info.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["Weapon_Is_Restricted", localizerWeaponName, restrict.nonVIPRestrict, restrict.VIPRestrict]}");
                    return;
                }
                playerData[player].PrimaryWeapon = weaponName;
                info.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["PrimaryWeapon_Set", localizerWeaponName]}");
                if (player.PawnIsAlive && IsHaveWeaponFromSlot(player, 1) != 1)
                {
                    player.GiveNamedItem(weaponName);
                }
                else if (Config.Gameplay.SwitchWeapons && player.PawnIsAlive)
                {
                    string[] weapon = new string[3];
                    weapon[0] = weaponName;
                    weapon[1] = GetWeaponFromSlot(player, 2);

                    player.RemoveWeapons();
                    player.GiveNamedItem("weapon_knife");
                    player.GiveNamedItem(weapon[0]);
                    if (!string.IsNullOrEmpty(weapon[1]))
                        player.GiveNamedItem(weapon[1]);
                }
                return;
            }
            else if (AllowedSecondaryWeaponsList.Contains(weaponName))
            {
                string localizerWeaponName = Localizer[weaponName];
                if (weaponName == playerData[player].SecondaryWeapon)
                {
                    if (!string.IsNullOrEmpty(Config.SoundSettings.CantEquipSound))
                        player.ExecuteClientCommand("play " + Config.SoundSettings.CantEquipSound);
                    info.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["Weapon_Is_Already_Set", localizerWeaponName]}");
                    return;
                }
                if (CheckIsWeaponRestricted(weaponName, AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag), player.TeamNum))
                {
                    if (!string.IsNullOrEmpty(Config.SoundSettings.CantEquipSound))
                        player.ExecuteClientCommand("play " + Config.SoundSettings.CantEquipSound);
                    RestrictedWeaponsInfo restrict = RestrictedWeapons[weaponName];
                    info.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["Weapon_Is_Restricted", localizerWeaponName, restrict.nonVIPRestrict, restrict.VIPRestrict]}");
                    return;
                }
                playerData[player].SecondaryWeapon = weaponName;
                info.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["SecondaryWeapon_Set", localizerWeaponName]}");
                if (player.PawnIsAlive && IsHaveWeaponFromSlot(player, 2) != 2)
                {
                    player.GiveNamedItem(weaponName);
                }
                else if (Config.Gameplay.SwitchWeapons && player.PawnIsAlive)
                {
                    string[] weapon = new string[3];
                    weapon[0] = weaponName;
                    weapon[1] = GetWeaponFromSlot(player, 1);

                    player.RemoveWeapons();
                    player.GiveNamedItem("weapon_knife");
                    player.GiveNamedItem(weapon[0]);
                    if (!string.IsNullOrEmpty(weapon[1]))
                        player.GiveNamedItem(weapon[1]);
                }
                return;
            }
            else
            {
                if (!string.IsNullOrEmpty(Config.SoundSettings.CantEquipSound))
                    player.ExecuteClientCommand("play " + Config.SoundSettings.CantEquipSound);
                string localizerWeaponName = Localizer[weaponName];
                info.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["Weapon_Disabled", localizerWeaponName]}");
                return;
            }
        }
        public void GivePlayerWeapons(CCSPlayerController player, bool bNewMode)
        {
            if (playerData.ContainsPlayer(player))
            {
                player.InGameMoneyServices!.Account = 16000;
                if (!bNewMode)
                {
                    playerData[player].SpawnProtection = true;
                    if (AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag))
                    {
                        AddTimer(Config.PlayersSettings.ProtectionTimeVIP, () =>
                        {
                            playerData[player].SpawnProtection = false;
                        }, TimerFlags.STOP_ON_MAPCHANGE);
                    }
                    else
                    {
                        AddTimer(Config.PlayersSettings.ProtectionTime, () =>
                        {
                            playerData[player].SpawnProtection = false;
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
                    else if (!string.IsNullOrEmpty(playerData[player].PrimaryWeapon) && AllowedPrimaryWeaponsList.Count > 1)
                    {
                        if (AllowedPrimaryWeaponsList.Contains(playerData[player].PrimaryWeapon))
                        {
                            if (CheckIsWeaponRestricted(playerData[player].PrimaryWeapon, AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag), player.TeamNum))
                            {
                                playerData[player].PrimaryWeapon = "";
                                string localizerWeaponName = Localizer[playerData[player].PrimaryWeapon];
                                RestrictedWeaponsInfo restrict = RestrictedWeapons[playerData[player].PrimaryWeapon];
                                player.PrintToChat($"{Localizer["Prefix"]} {Localizer["Weapon_Is_Restricted", localizerWeaponName, restrict.nonVIPRestrict, restrict.VIPRestrict]}");
                                if (Config.Gameplay.DefaultModeWeapons == 2)
                                {
                                    int weapon = GetRandomWeaponFromList(AllowedPrimaryWeaponsList);
                                    player.GiveNamedItem(AllowedPrimaryWeaponsList[weapon]);
                                }
                                else if (Config.Gameplay.DefaultModeWeapons == 1)
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
                                player.GiveNamedItem(playerData[player].PrimaryWeapon);
                            }
                        }
                        else
                        {
                            string replacedweaponName = Localizer[playerData[player].PrimaryWeapon];
                            player.PrintToChat($"{Localizer["Prefix"]} {Localizer["PrimaryWeapon_Disabled", replacedweaponName]}");

                            if (Config.Gameplay.DefaultModeWeapons == 2)
                            {
                                int weapon = GetRandomWeaponFromList(AllowedPrimaryWeaponsList);
                                player.GiveNamedItem(AllowedPrimaryWeaponsList[weapon]);
                            }
                            else if (Config.Gameplay.DefaultModeWeapons == 1)
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
                            if (Config.Gameplay.DefaultModeWeapons == 2)
                            {
                                int weapon = GetRandomWeaponFromList(AllowedPrimaryWeaponsList);
                                player.GiveNamedItem(AllowedPrimaryWeaponsList[weapon]);
                            }
                            else if (Config.Gameplay.DefaultModeWeapons == 1)
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
                    else if (!string.IsNullOrEmpty(playerData[player].SecondaryWeapon) && AllowedSecondaryWeaponsList.Count > 1)
                    {
                        if (AllowedSecondaryWeaponsList.Contains(playerData[player].SecondaryWeapon))
                        {
                            if (CheckIsWeaponRestricted(playerData[player].SecondaryWeapon, AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag), player.TeamNum))
                            {
                                playerData[player].SecondaryWeapon = "";
                                string localizerWeaponName = Localizer[playerData[player].SecondaryWeapon];
                                RestrictedWeaponsInfo restrict = RestrictedWeapons[playerData[player].SecondaryWeapon];
                                player.PrintToChat($"{Localizer["Prefix"]} {Localizer["Weapon_Is_Restricted", localizerWeaponName, restrict.nonVIPRestrict, restrict.VIPRestrict]}");
                                if (Config.Gameplay.DefaultModeWeapons == 2)
                                {
                                    int weapon = GetRandomWeaponFromList(AllowedSecondaryWeaponsList);
                                    player.GiveNamedItem(AllowedSecondaryWeaponsList[weapon]);
                                }
                                else if (Config.Gameplay.DefaultModeWeapons == 1)
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
                                player.GiveNamedItem(playerData[player].SecondaryWeapon);
                            }
                        }
                        else
                        {
                            string replacedweaponName = Localizer[playerData[player].SecondaryWeapon];
                            player.PrintToChat($"{Localizer["Prefix"]} {Localizer["SecondaryWeapon_Disabled", replacedweaponName]}");

                            if (Config.Gameplay.DefaultModeWeapons == 2)
                            {
                                int weapon = GetRandomWeaponFromList(AllowedSecondaryWeaponsList);
                                player.GiveNamedItem(AllowedSecondaryWeaponsList[weapon]);
                            }
                            else if (Config.Gameplay.DefaultModeWeapons == 1)
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
                            if (Config.Gameplay.DefaultModeWeapons == 2)
                            {
                                int weapon = GetRandomWeaponFromList(AllowedSecondaryWeaponsList);
                                player.GiveNamedItem(AllowedSecondaryWeaponsList[weapon]);
                            }
                            else if (Config.Gameplay.DefaultModeWeapons == 1)
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
        public static void RespawnPlayer(CCSPlayerController player, string[] spawn, bool teleport = true)
        {
            if (!player.IsValid || !player.PlayerPawn.IsValid || player.PawnIsAlive)
                return;

            if (player.TeamNum == 2 || player.TeamNum == 3)
            {
                player.Respawn();

                if (teleport)
                {
                    var position = ParseVector(spawn[0]);
                    var angle = ParseQAngle(spawn[1]);
                    player.PlayerPawn.Value!.Teleport(position, angle, new Vector(0, 0, 0));
                }
            }
        }
        public static bool IsPlayerValid(CCSPlayerController player)
        {
            if (player is null || !player.IsValid || !player.PlayerPawn.IsValid || player.IsBot || player.IsHLTV || player.SteamID.ToString().Length != 17)
                return false;
            return true;
        }

        private static bool GetPrefsValue(CCSPlayerController player, int preference)
        {
            switch (preference)
            {
                case 1:
                    return playerData[player].KillSound;
                case 2:
                    return playerData[player].HSKillSound;
                case 3:
                    return playerData[player].KnifeKillSound;
                case 4:
                    return playerData[player].HitSound;
                case 5:
                    return playerData[player].OnlyHS;
                default:
                    return playerData[player].HudMessages;
            }
        }
        private void SwitchPrefsValue(CCSPlayerController player, int preference)
        {
            string changedValue;
            string Preference;
            switch (preference)
            {
                case 1:
                    playerData[player].KillSound = !playerData[player].KillSound;
                    changedValue = playerData[player].KillSound ? Localizer["Menu.Enabled"] : Localizer["Menu.Disabled"];
                    Preference = Localizer["Prefs.KillSound"];
                    break;
                case 2:
                    playerData[player].HSKillSound = !playerData[player].HSKillSound;
                    changedValue = playerData[player].HSKillSound ? Localizer["Menu.Enabled"] : Localizer["Menu.Disabled"];
                    Preference = Localizer["Prefs.HeadshotKillSound"];
                    break;
                case 3:
                    playerData[player].KnifeKillSound = !playerData[player].KnifeKillSound;
                    changedValue = playerData[player].KnifeKillSound ? Localizer["Menu.Enabled"] : Localizer["Menu.Disabled"];
                    Preference = Localizer["Prefs.KnifeKillSound"];
                    break;
                case 4:
                    playerData[player].HitSound = !playerData[player].HitSound;
                    changedValue = playerData[player].HitSound ? Localizer["Menu.Enabled"] : Localizer["Menu.Disabled"];
                    Preference = Localizer["Prefs.HitSound"];
                    break;
                case 5:
                    playerData[player].OnlyHS = !playerData[player].OnlyHS;
                    changedValue = playerData[player].OnlyHS ? Localizer["Menu.Enabled"] : Localizer["Menu.Disabled"];
                    Preference = Localizer["Prefs.OnlyHS"];
                    break;
                default:
                    playerData[player].HudMessages = !playerData[player].HudMessages;
                    changedValue = playerData[player].HudMessages ? Localizer["Menu.Enabled"] : Localizer["Menu.Disabled"];
                    Preference = Localizer["Prefs.HudMessages"];
                    break;
            }
            player.PrintToChat($"{Localizer["Prefix"]} {Localizer["Prefs.ValueChanged", Preference, changedValue]}");
        }

        //https://github.com/K4ryuu/K4-System/blob/dev/src/Plugin/PluginCache.cs
        public class PlayerCache<T> : Dictionary<int, T>
        {
            public T this[CCSPlayerController controller]
            {
                get
                {
                    if (controller is null || !controller.IsValid || controller.SteamID.ToString().Length != 17)
                    {
                        throw new ArgumentException("Invalid player controller");
                    }

                    if (controller.IsBot || controller.IsHLTV)
                    {
                        throw new ArgumentException("Player controller is BOT or HLTV");
                    }

                    if (!ContainsKey(controller.Slot))
                    {
                        throw new KeyNotFoundException($"Player with ID {controller.Slot} not found in cache");
                    }

                    if (TryGetValue(controller.Slot, out T? value))
                    {
                        return value;
                    }

                    return default(T)!;
                }
                set
                {
                    if (controller is null || !controller.IsValid || !controller.PlayerPawn.IsValid || controller.SteamID.ToString().Length != 17)
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
                if (player == null || !player.IsValid || !player.PlayerPawn.IsValid || player.SteamID.ToString().Length != 17)
                    return false;

                if (player.IsBot || player.IsHLTV)
                    return false;

                return ContainsKey(player.Slot);
            }

            public bool RemovePlayer(CCSPlayerController player)
            {
                if (player == null || !player.IsValid || !player.PlayerPawn.IsValid || player.SteamID.ToString().Length != 17)
                {
                    throw new ArgumentException("Invalid player controller");
                }

                if (player.IsBot || player.IsHLTV)
                {
                    throw new ArgumentException("Player controller is BOT or HLTV");
                }

                return Remove(player.Slot);
            }
        }
    }
}