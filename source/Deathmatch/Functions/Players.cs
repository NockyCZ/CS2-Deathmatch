using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using CounterStrikeSharp.API;
using DeathmatchAPI;
using static DeathmatchAPI.Preferences;

namespace Deathmatch
{
    public partial class Deathmatch
    {
        public void SetupPlayerWeapons(CCSPlayerController player, string weaponName, CommandInfo info)
        {
            if (string.IsNullOrEmpty(weaponName))
            {
                if (ActiveMode.PrimaryWeapons.Any())
                {
                    var weapons = string.Join($"{ChatColors.Default}, {ChatColors.Green}", ActiveMode.PrimaryWeapons.Select(w => Localizer[w]));
                    info.ReplyToCommand($"{Localizer["Chat.ListOfAllowedWeapons"]}");
                    info.ReplyToCommand($"{ChatColors.DarkRed}• {weapons.ToUpper()}");
                }
                if (ActiveMode.SecondaryWeapons.Any())
                {
                    var weapons = string.Join($"{ChatColors.Default}, {ChatColors.Green}", ActiveMode.SecondaryWeapons.Select(w => Localizer[w]));
                    info.ReplyToCommand($"{Localizer["Chat.ListOfAllowedSecondaryWeapons"]}");
                    info.ReplyToCommand($"{ChatColors.DarkRed}• {weapons.ToUpper()}");
                }
                info.ReplyToCommand($"{Localizer["Chat.Prefix"]} /gun <weapon name>");
                return;
            }

            if (weaponSelectMapping.TryGetValue(weaponName, out var name))
            {
                weaponName = name;
            }
            else
            {
                var matches = PrimaryWeaponsList.Concat(SecondaryWeaponsList)
                    .Where(w => w.Contains(weaponName))
                    .ToList();

                if (matches.Count > 1)
                {
                    var matchingValues = string.Join($"{ChatColors.Default}, {ChatColors.Green}", matches.Select(w => Localizer[w]));
                    info.ReplyToCommand($"{Localizer["Chat.Prefix"]} {Localizer["Chat.MultipleWeaponsSelected"]} {ChatColors.Default}( {matchingValues} {ChatColors.Default})");
                    return;
                }
                else if (!matches.Any())
                {
                    if (!string.IsNullOrEmpty(Config.SoundSettings.CantEquipSound))
                        player.ExecuteClientCommand("play " + Config.SoundSettings.CantEquipSound);
                    info.ReplyToCommand($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponNotFound", weaponName]}");
                    return;
                }
                weaponName = matches[0];
            }

            if (!playerData.TryGetValue(player.Slot, out var data))
                return;

            bool IsVIP = AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag);
            var pawn = player.PlayerPawn.Value;
            if (ActiveMode.PrimaryWeapons.Contains(weaponName))
            {
                string localizerWeaponName = Localizer[weaponName];
                if (data.PrimaryWeapon.TryGetValue(ActiveCustomMode, out var weapon) && weaponName == weapon)
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

                data.PrimaryWeapon[ActiveCustomMode] = weaponName;
                info.ReplyToCommand($"{Localizer["Chat.Prefix"]} {Localizer["Chat.PrimaryWeaponSet", localizerWeaponName]}");

                var primaryWeapon = pawn?.GetWeaponFromSlot(gear_slot_t.GEAR_SLOT_RIFLE);
                if (player.PawnIsAlive)
                {
                    if (primaryWeapon == null)
                    {
                        player.GiveNamedItem(weaponName);
                    }
                    else if (Config.Gameplay.SwitchWeapons)
                    {
                        var secondaryWeapon = pawn?.GetWeaponFromSlot(gear_slot_t.GEAR_SLOT_PISTOL);
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
                if (data.SecondaryWeapon.TryGetValue(ActiveCustomMode, out var weapon) && weaponName == weapon)
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

                data.SecondaryWeapon[ActiveCustomMode] = weaponName;
                info.ReplyToCommand($"{Localizer["Chat.Prefix"]} {Localizer["Chat.SecondaryWeaponSet", localizerWeaponName]}");

                var secondaryWeapon = pawn?.GetWeaponFromSlot(gear_slot_t.GEAR_SLOT_PISTOL);
                if (player.PawnIsAlive)
                {
                    if (secondaryWeapon == null)
                    {
                        player.GiveNamedItem(weaponName);
                    }
                    else if (Config.Gameplay.SwitchWeapons)
                    {
                        var primaryWeapon = pawn?.GetWeaponFromSlot(gear_slot_t.GEAR_SLOT_RIFLE);
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
            var pawn = player.PlayerPawn.Value;
            if (playerData.TryGetValue(player.Slot, out var data) && pawn != null)
            {
                if (player.InGameMoneyServices != null && giveUtilities)
                    player.InGameMoneyServices.Account = Config.Gameplay.AllowBuyMenu ? 16000 : 0;

                bool IsVIP = AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag);
                if (!bNewMode)
                {
                    var timer = IsVIP ? Config.PlayersSettings.VIP.ProtectionTime : Config.PlayersSettings.NonVIP.ProtectionTime;
                    if (timer > 0.1)
                    {
                        if (!string.IsNullOrEmpty(Config.Gameplay.SpawnProtectionColor))
                        {
                            Color transparentColor = ColorTranslator.FromHtml(Config.Gameplay.SpawnProtectionColor);
                            pawn.Render = transparentColor;
                            Utilities.SetStateChanged(player, "CBaseModelEntity", "m_clrRender");
                        }
                        data.SpawnProtection = true;
                        playersWithSpawnProtection[player] = (timer, Server.CurrentTime);
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
                        if (ActiveMode.SecondaryWeapons.Any() && !GetPrefsValue(data, "NoPrimary", Config.PlayersPreferences.NoPrimary.DefaultValue))
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


                Server.NextFrame(() =>
                {
                    if (Config.Gameplay.FastWeaponEquip)
                    {
                        var activeWeapon = pawn.GetActiveWeapon();
                        if (activeWeapon != null && activeWeapon.IsValid)
                        {
                            activeWeapon.NextPrimaryAttackTick = Server.TickCount + 1;
                            Utilities.SetStateChanged(player, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
                        }
                    }
                });
                return;
            }

            if (player.InGameMoneyServices != null)
                player.InGameMoneyServices.Account = 0;

            if (pawn != null)
            {
                if (ActiveMode.PrimaryWeapons.Any())
                {
                    if (!pawn.IsHaveWeaponFromSlot(gear_slot_t.GEAR_SLOT_RIFLE))
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
                    if (!pawn.IsHaveWeaponFromSlot(gear_slot_t.GEAR_SLOT_PISTOL))
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
        }

        public void SetupDefaultPreferences(DeathmatchPlayerData data, bool IsVIP)
        {
            var preferences = data.Preferences;
            foreach (var pref in Preference.GetAllPreferences())
            {
                if (preferences.ContainsKey(pref.Name))
                    continue;

                if (pref.BooleanData != null)
                {
                    preferences[pref.Name] = pref.VipOnly && !IsVIP ? false : pref.BooleanData.DefaultValue;
                }
                else if (pref.Data != null)
                {
                    preferences[pref.Name] = pref.VipOnly && !IsVIP ? "-1" : pref.Data.Options[0];
                }
            }
        }

        public void SetupDefaultWeapons(DeathmatchPlayerData data, CsTeam team, bool IsVIP)
        {
            foreach (var mode in Config.CustomModes)
            {
                if (data.PrimaryWeapon.ContainsKey(mode.Key))
                    continue;

                switch (Config.Gameplay.DefaultModeWeapons)
                {
                    case 1:
                        if (mode.Value.PrimaryWeapons.Any())
                        {
                            var primary = mode.Value.PrimaryWeapons.First(w => !Config.RestrictedWeapons.Restrictions.ContainsKey(w));
                            data.PrimaryWeapon[mode.Key] = string.IsNullOrEmpty(primary) ? "" : primary;
                        }
                        else
                            data.PrimaryWeapon[mode.Key] = "";
                        if (mode.Value.SecondaryWeapons.Any())
                        {
                            var secondary = mode.Value.SecondaryWeapons.First(w => !Config.RestrictedWeapons.Restrictions.ContainsKey(w));
                            data.SecondaryWeapon[mode.Key] = string.IsNullOrEmpty(secondary) ? "" : secondary;
                        }
                        else
                            data.SecondaryWeapon[mode.Key] = "";
                        break;
                    case 2 or 3:
                        if (mode.Value.PrimaryWeapons.Any())
                        {
                            var primary = GetRandomWeaponFromList(mode.Value.PrimaryWeapons, mode.Value, IsVIP, team, true);
                            data.PrimaryWeapon[mode.Key] = string.IsNullOrEmpty(primary) ? "" : primary;
                        }
                        else
                            data.PrimaryWeapon[mode.Key] = "";
                        if (mode.Value.SecondaryWeapons.Any())
                        {
                            var secondary = GetRandomWeaponFromList(mode.Value.SecondaryWeapons, mode.Value, IsVIP, team, false);
                            data.SecondaryWeapon[mode.Key] = string.IsNullOrEmpty(secondary) ? "" : secondary;
                        }
                        else
                            data.SecondaryWeapon[mode.Key] = "";
                        break;
                    default:
                        if (mode.Value.PrimaryWeapons.Any())
                        {
                            var primary = mode.Value.PrimaryWeapons.FirstOrDefault();
                            data.PrimaryWeapon[mode.Key] = string.IsNullOrEmpty(primary) ? "" : primary;
                        }
                        else
                            data.PrimaryWeapon[mode.Key] = "";
                        if (mode.Value.SecondaryWeapons.Any())
                        {
                            var secondary = mode.Value.SecondaryWeapons.FirstOrDefault();
                            data.SecondaryWeapon[mode.Key] = string.IsNullOrEmpty(secondary) ? "" : secondary;
                        }
                        else
                            data.SecondaryWeapon[mode.Key] = "";
                        break;
                }
            }
        }

        public static T GetPrefsValue<T>(DeathmatchPlayerData data, string preference, T defaultValue)
        {
            if (data.Preferences.TryGetValue(preference, out var value) && value is T result)
                return result;

            return defaultValue;
        }

        private void SwitchBooleanPrefsValue(CCSPlayerController player, string preference)
        {
            if (!playerData.TryGetValue(player.Slot, out var data))
                return;

            if (data.Preferences.TryGetValue(preference, out var value) && value is bool currentValue)
            {
                data.Preferences[preference] = !currentValue;
            }

            var changedValue = (bool)data.Preferences[preference] ? Localizer["Menu.Enabled"] : Localizer["Menu.Disabled"];
            var Preference = Localizer[$"Prefs.{preference}"];
            player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Prefs.ValueChanged", Preference, changedValue]}");
        }

        private void SwitchStringPrefsValue(CCSPlayerController player, string preference, List<string> values, string? currentValue)
        {
            if (!playerData.TryGetValue(player.Slot, out var data))
                return;

            if (currentValue == null || !values.Contains(currentValue))
            {
                data.Preferences[preference] = values[0];
            }
            else
            {
                int index = values.IndexOf(currentValue);
                var newValue = (index == -1 || index == values.Count - 1) ? values[0] : values[index + 1];
                data.Preferences[preference] = newValue;
            }

            var Preference = Localizer[$"Prefs.{preference}"];
            player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Prefs.ValueSet", Preference, data.Preferences[preference]]}");
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