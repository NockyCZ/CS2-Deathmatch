using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using CounterStrikeSharp.API;

namespace Deathmatch
{
    public partial class Deathmatch
    {
        public void SetupPlayerWeapons(CCSPlayerController player, string weaponName, CommandInfo info)
        {
            if (string.IsNullOrEmpty(weaponName))
            {
                string primaryWeapons = "";
                string secondaryWeapons = "";
                if (ActiveMode.PrimaryWeapons.Any())
                {
                    foreach (string weapon in ActiveMode.PrimaryWeapons)
                    {
                        if (string.IsNullOrEmpty(primaryWeapons))
                            primaryWeapons = $"{ChatColors.Green} {Localizer[weapon]}";
                        else
                            primaryWeapons = $"{primaryWeapons}{ChatColors.Default}, {ChatColors.Green}{Localizer[weapon]}";
                    }
                    info.ReplyToCommand($"{Localizer["Chat.ListOfAllowedWeapons"]}");
                    info.ReplyToCommand($"{ChatColors.DarkRed}• {primaryWeapons.ToUpper()}");
                }
                if (ActiveMode.SecondaryWeapons.Any())
                {
                    foreach (string weapon in ActiveMode.SecondaryWeapons)
                    {
                        if (string.IsNullOrEmpty(secondaryWeapons))
                            secondaryWeapons = $"{ChatColors.Green} {Localizer[weapon]}";
                        else
                            secondaryWeapons = $"{secondaryWeapons}{ChatColors.Default}, {ChatColors.Green}{Localizer[weapon]}";
                    }
                    info.ReplyToCommand($"{Localizer["Chat.ListOfAllowedSecondaryWeapons"]}");
                    info.ReplyToCommand($"{ChatColors.DarkRed}• {secondaryWeapons.ToUpper()}");
                }
                info.ReplyToCommand($"{Localizer["Chat.Prefix"]} /gun <weapon name>");
                return;
            }

            string replacedweaponName = "";
            string matchingValues = "";
            int matchingCount = 0;
            if (weaponSelectMapping.TryGetValue(weaponName, out var name))
            {
                weaponName = name;
                matchingCount = 1;
            }
            else
            {
                foreach (string weapon in PrimaryWeaponsList.Concat(SecondaryWeaponsList))
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

                if (matchingCount != 0)
                {
                    weaponName = replacedweaponName;
                }
            }

            if (matchingCount > 1)
            {
                info.ReplyToCommand($"{Localizer["Chat.Prefix"]} {Localizer["Chat.MultipleWeaponsSelected"]} {ChatColors.Default}( {matchingValues} {ChatColors.Default})");
                return;
            }
            else if (matchingCount == 0)
            {
                if (!string.IsNullOrEmpty(Config.SoundSettings.CantEquipSound))
                    player.ExecuteClientCommand("play " + Config.SoundSettings.CantEquipSound);
                info.ReplyToCommand($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponNotFound", weaponName]}");
                return;
            }


            bool IsVIP = AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag);
            if (ActiveMode.PrimaryWeapons.Contains(weaponName))
            {
                string localizerWeaponName = Localizer[weaponName];
                if (playerData[player].PrimaryWeapon.TryGetValue(ActiveCustomMode, out var weapon) && weaponName == weapon)
                {
                    if (!string.IsNullOrEmpty(Config.SoundSettings.CantEquipSound))
                        player.ExecuteClientCommand("play " + Config.SoundSettings.CantEquipSound);
                    info.ReplyToCommand($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponsIsAlreadySet", localizerWeaponName]}");
                    return;
                }
                if (CheckIsWeaponRestricted(weaponName, IsVIP, player.Team, true))
                {
                    if (!string.IsNullOrEmpty(Config.SoundSettings.CantEquipSound))
                        player.ExecuteClientCommand("play " + Config.SoundSettings.CantEquipSound);

                    (int NonVIP, int VIP) restrictInfo = GetRestrictData(weaponName, player.Team);
                    info.ReplyToCommand($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponIsRestricted", localizerWeaponName, GetWeaponRestrictLozalizer(restrictInfo.NonVIP), GetWeaponRestrictLozalizer(restrictInfo.VIP)]}");
                    return;
                }

                playerData[player].PrimaryWeapon[ActiveCustomMode] = weaponName;
                info.ReplyToCommand($"{Localizer["Chat.Prefix"]} {Localizer["Chat.PrimaryWeaponSet", localizerWeaponName]}");

                var primaryWeapon = GetWeaponFromSlot(player, gear_slot_t.GEAR_SLOT_RIFLE);
                if (player.PawnIsAlive)
                {
                    if (primaryWeapon == null)
                    {
                        player.GiveNamedItem(weaponName);
                    }
                    else if (Config.Gameplay.SwitchWeapons)
                    {
                        var secondaryWeapon = GetWeaponFromSlot(player, gear_slot_t.GEAR_SLOT_PISTOL);
                        player.RemoveWeapons();
                        player.GiveNamedItem("weapon_knife");
                        player.GiveNamedItem(weaponName);
                        if (secondaryWeapon != null)
                            player.GiveNamedItem(secondaryWeapon.DesignerName);

                        if (ActiveMode.Armor != 0)
                        {
                            var armor = ActiveMode.Armor == 1 ? "item_kevlar" : "item_assaultsuit";
                            player.GiveNamedItem(armor);
                        }
                    }
                }
                return;
            }
            else if (ActiveMode.SecondaryWeapons.Contains(weaponName))
            {
                string localizerWeaponName = Localizer[weaponName];
                if (playerData[player].SecondaryWeapon.TryGetValue(ActiveCustomMode, out var weapon) && weaponName == weapon)
                {
                    if (!string.IsNullOrEmpty(Config.SoundSettings.CantEquipSound))
                        player.ExecuteClientCommand("play " + Config.SoundSettings.CantEquipSound);
                    info.ReplyToCommand($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponsIsAlreadySet", localizerWeaponName]}");
                    return;
                }
                if (CheckIsWeaponRestricted(weaponName, IsVIP, player.Team, false))
                {
                    if (!string.IsNullOrEmpty(Config.SoundSettings.CantEquipSound))
                        player.ExecuteClientCommand("play " + Config.SoundSettings.CantEquipSound);

                    (int NonVIP, int VIP) restrictInfo = GetRestrictData(weaponName, player.Team);
                    info.ReplyToCommand($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponIsRestricted", localizerWeaponName, GetWeaponRestrictLozalizer(restrictInfo.NonVIP), GetWeaponRestrictLozalizer(restrictInfo.VIP)]}");
                    return;
                }

                playerData[player].SecondaryWeapon[ActiveCustomMode] = weaponName;
                info.ReplyToCommand($"{Localizer["Chat.Prefix"]} {Localizer["Chat.SecondaryWeaponSet", localizerWeaponName]}");

                var secondaryWeapon = GetWeaponFromSlot(player, gear_slot_t.GEAR_SLOT_PISTOL);
                if (player.PawnIsAlive)
                {
                    if (secondaryWeapon == null)
                    {
                        player.GiveNamedItem(weaponName);
                    }
                    else if (Config.Gameplay.SwitchWeapons)
                    {
                        var primaryWeapon = GetWeaponFromSlot(player, gear_slot_t.GEAR_SLOT_RIFLE);
                        player.RemoveWeapons();
                        player.GiveNamedItem("weapon_knife");
                        player.GiveNamedItem(weaponName);
                        if (primaryWeapon != null)
                            player.GiveNamedItem(primaryWeapon.DesignerName);

                        if (ActiveMode.Armor != 0)
                        {
                            var armor = ActiveMode.Armor == 1 ? "item_kevlar" : "item_assaultsuit";
                            player.GiveNamedItem(armor);
                        }
                    }
                }
                return;
            }
            else
            {
                if (!string.IsNullOrEmpty(Config.SoundSettings.CantEquipSound))
                    player.ExecuteClientCommand("play " + Config.SoundSettings.CantEquipSound);
                string localizerWeaponName = Localizer[weaponName];
                info.ReplyToCommand($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponIsDisabled", localizerWeaponName]}");
                return;
            }
        }

        public void GivePlayerWeapons(CCSPlayerController player, bool bNewMode, bool giveUtilities = true, bool giveKnife = false)
        {
            if (playerData.TryGetValue(player.Slot, out var data) && player.PlayerPawn.Value != null)
            {
                if (player.InGameMoneyServices != null && giveUtilities)
                    player.InGameMoneyServices.Account = Config.Gameplay.AllowBuyMenu ? 16000 : 0;

                bool IsVIP = AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag);
                if (!bNewMode)
                {
                    var timer = IsVIP ? Config.PlayersSettings.VIP.ProtectionTime : Config.PlayersSettings.NonVIP.ProtectionTime;
                    if (timer > 0.1)
                    {
                        if (string.IsNullOrEmpty(Config.Gameplay.SpawnProtectionColor))
                        {
                            Color transparentColor = ColorTranslator.FromHtml(Config.Gameplay.SpawnProtectionColor);
                            player.PlayerPawn.Value.Render = transparentColor;
                            Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");
                        }
                        data.SpawnProtection = true;
                        AddTimer(timer, () =>
                        {
                            if (player != null && player.IsValid && playerData.TryGetValue(player.Slot, out data))
                            {
                                data.SpawnProtection = false;
                                if (string.IsNullOrEmpty(Config.Gameplay.SpawnProtectionColor))
                                {
                                    player.PlayerPawn.Value.Render = Color.White;
                                    Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");
                                }
                            }
                        });
                    }

                    if (!IsCasualGamemode)
                    {
                        data.BlockRandomWeaponsIntegeration = Server.CurrentTime;
                    }
                }

                if (ActiveMode.PrimaryWeapons.Any())
                {
                    if (ActiveMode.RandomWeapons)
                    {
                        var weapon = GetRandomWeaponFromList(ActiveMode.PrimaryWeapons, ActiveMode, IsVIP, player.Team, true);
                        if (!string.IsNullOrEmpty(weapon))
                            player.GiveNamedItem(weapon);
                    }
                    else if (data.PrimaryWeapon.TryGetValue(ActiveCustomMode, out var weapon) && !string.IsNullOrEmpty(weapon))
                    {
                        player.GiveNamedItem(weapon);
                    }
                }

                if (ActiveMode.SecondaryWeapons.Any())
                {
                    if (ActiveMode.RandomWeapons)
                    {
                        var weapon = GetRandomWeaponFromList(ActiveMode.SecondaryWeapons, ActiveMode, IsVIP, player.Team, false);
                        if (!string.IsNullOrEmpty(weapon))
                            player.GiveNamedItem(weapon);
                    }
                    else if (data.SecondaryWeapon.TryGetValue(ActiveCustomMode, out var weapon) && !string.IsNullOrEmpty(weapon))
                    {
                        player.GiveNamedItem(weapon);
                    }
                }

                if (giveKnife)
                    player.GiveNamedItem("weapon_knife");

                if (giveUtilities && ActiveMode.Utilities.Any())
                {
                    foreach (var item in ActiveMode.Utilities)
                        player.GiveNamedItem(item);

                    if (ActiveMode.Utilities.Contains("weapon_taser"))
                        player.GiveNamedItem("weapon_knife");
                }

                return;
            }

            if (player.InGameMoneyServices != null)
                player.InGameMoneyServices.Account = 0;

            if (ActiveMode.PrimaryWeapons.Any())
            {
                if (!IsHaveWeaponFromSlot(player, gear_slot_t.GEAR_SLOT_RIFLE))
                {
                    var PrimaryWeapon = ActiveMode.PrimaryWeapons.Count switch
                    {
                        1 => ActiveMode.PrimaryWeapons[0],
                        _ => Config.Gameplay.DefaultModeWeapons switch
                        {
                            2 or 3 => GetRandomWeaponFromList(ActiveMode.PrimaryWeapons, ActiveMode, false, player.Team, true),
                            _ or 1 => ActiveMode.PrimaryWeapons[0]
                        }
                    };
                    if (!string.IsNullOrEmpty(PrimaryWeapon))
                        player.GiveNamedItem(PrimaryWeapon);
                }
            }
            if (ActiveMode.SecondaryWeapons.Any())
            {
                if (!IsHaveWeaponFromSlot(player, gear_slot_t.GEAR_SLOT_PISTOL))
                {
                    var SecondaryWeapon = ActiveMode.SecondaryWeapons.Count switch
                    {
                        1 => ActiveMode.SecondaryWeapons[0],
                        _ => Config.Gameplay.DefaultModeWeapons switch
                        {
                            2 or 3 => GetRandomWeaponFromList(ActiveMode.SecondaryWeapons, ActiveMode, false, player.Team, false),
                            _ or 1 => ActiveMode.SecondaryWeapons[0]
                        }
                    };

                    if (!string.IsNullOrEmpty(SecondaryWeapon))
                        player.GiveNamedItem(SecondaryWeapon);
                }
            }
        }

        public void SetupDefaultPreferences(CCSPlayerController player)
        {
            bool IsVIP = AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag);
            foreach (var data in Preferences)
            {
                if (playerData[player].Preferences.ContainsKey(data.Name))
                    continue;

                if (data.vipOnly)
                {
                    if (IsVIP)
                        playerData[player].Preferences[data.Name] = data.defaultValue;
                    else
                        playerData[player].Preferences[data.Name] = false;
                }
                else
                    playerData[player].Preferences[data.Name] = data.defaultValue;
            }
        }

        public void SetupDefaultWeapons(CCSPlayerController player)
        {
            bool IsVIP = AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag);
            foreach (var mode in Config.CustomModes)
            {
                if (playerData[player].PrimaryWeapon.ContainsKey(mode.Key))
                    continue;

                switch (Config.Gameplay.DefaultModeWeapons)
                {
                    case 1:
                        if (mode.Value.PrimaryWeapons.Any())
                        {
                            var primary = mode.Value.PrimaryWeapons.Where(w => !Config.RestrictedWeapons.Restrictions.ContainsKey(w)).FirstOrDefault();
                            playerData[player].PrimaryWeapon[mode.Key] = string.IsNullOrEmpty(primary) ? "" : primary;
                        }
                        else
                            playerData[player].PrimaryWeapon[mode.Key] = "";
                        if (mode.Value.SecondaryWeapons.Any())
                        {
                            var secondary = mode.Value.SecondaryWeapons.Where(w => !Config.RestrictedWeapons.Restrictions.ContainsKey(w)).FirstOrDefault();
                            playerData[player].SecondaryWeapon[mode.Key] = string.IsNullOrEmpty(secondary) ? "" : secondary;
                        }
                        else
                            playerData[player].SecondaryWeapon[mode.Key] = "";
                        break;
                    case 2 or 3:
                        if (mode.Value.PrimaryWeapons.Any())
                        {
                            var primary = GetRandomWeaponFromList(mode.Value.PrimaryWeapons, mode.Value, IsVIP, player.Team, true);
                            playerData[player].PrimaryWeapon[mode.Key] = string.IsNullOrEmpty(primary) ? "" : primary;
                        }
                        else
                            playerData[player].PrimaryWeapon[mode.Key] = "";
                        if (mode.Value.SecondaryWeapons.Any())
                        {
                            var secondary = GetRandomWeaponFromList(mode.Value.SecondaryWeapons, mode.Value, IsVIP, player.Team, false);
                            playerData[player].SecondaryWeapon[mode.Key] = string.IsNullOrEmpty(secondary) ? "" : secondary;
                        }
                        else
                            playerData[player].SecondaryWeapon[mode.Key] = "";
                        break;
                    default:
                        if (mode.Value.PrimaryWeapons.Any())
                        {
                            var primary = mode.Value.PrimaryWeapons.FirstOrDefault();
                            playerData[player].PrimaryWeapon[mode.Key] = string.IsNullOrEmpty(primary) ? "" : primary;
                        }
                        else
                            playerData[player].PrimaryWeapon[mode.Key] = "";
                        if (mode.Value.SecondaryWeapons.Any())
                        {
                            var secondary = mode.Value.SecondaryWeapons.FirstOrDefault();
                            playerData[player].SecondaryWeapon[mode.Key] = string.IsNullOrEmpty(secondary) ? "" : secondary;
                        }
                        else
                            playerData[player].SecondaryWeapon[mode.Key] = "";
                        break;
                }
            }
        }

        public static bool GetPrefsValue(int slot, string preference)
        {
            if (playerData.TryGetValue(slot, out var data) && data.Preferences.TryGetValue(preference, out var value))
                return value;

            return false;
        }

        private void SwitchPrefsValue(CCSPlayerController player, string preference)
        {
            playerData[player].Preferences[preference] = !playerData[player].Preferences[preference];
            var changedValue = playerData[player].Preferences[preference] ? Localizer["Menu.Enabled"] : Localizer["Menu.Disabled"];
            var Preference = Localizer[$"Prefs.{preference}"];
            player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Prefs.ValueChanged", Preference, changedValue]}");
        }

        //https://github.com/K4ryuu/K4-System/blob/dev/src/Plugin/PluginCache.cs
        public class PlayerCache<T> : Dictionary<int, T>
        {
            public T this[CCSPlayerController? controller]
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
                if (player.SteamID.ToString().Length != 17)
                    return false;

                if (player.IsBot || player.IsHLTV)
                    return false;

                return ContainsKey(player.Slot);
            }

            public bool RemovePlayer(CCSPlayerController? player)
            {
                if (player == null || !player.IsValid || !player.PlayerPawn.IsValid || player.SteamID.ToString().Length != 17)
                    return false;

                if (player.IsBot || player.IsHLTV)
                    return false;

                return Remove(player.Slot);
            }
        }

        public bool IsHaveBlockedRandomWeaponsIntegration(CCSPlayerController player)
        {
            if (playerData.TryGetValue(player.Slot, out var data))
            {
                var time = Server.CurrentTime - data.BlockRandomWeaponsIntegeration;
                return time < 0.5;
            }
            return true;
        }
    }
}