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

                var primaryWeapon = pawn != null ? GetWeaponFromSlot(pawn, gear_slot_t.GEAR_SLOT_RIFLE) : null;
                if (player.PawnIsAlive)
                {
                    if (primaryWeapon == null)
                    {
                        player.GiveNamedItem(weaponName);
                    }
                    else if (Config.Gameplay.SwitchWeapons)
                    {
                        var secondaryWeapon = pawn != null ? GetWeaponFromSlot(pawn, gear_slot_t.GEAR_SLOT_PISTOL) : null;
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

                var secondaryWeapon = pawn != null ? GetWeaponFromSlot(pawn, gear_slot_t.GEAR_SLOT_PISTOL) : null;
                if (player.PawnIsAlive)
                {
                    if (secondaryWeapon == null)
                    {
                        player.GiveNamedItem(weaponName);
                    }
                    else if (Config.Gameplay.SwitchWeapons)
                    {
                        var primaryWeapon = pawn != null ? GetWeaponFromSlot(pawn, gear_slot_t.GEAR_SLOT_RIFLE) : null;
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
                        AddTimer(timer, () =>
                        {
                            if (player != null && player.IsValid && playerData.TryGetValue(player.Slot, out data))
                            {
                                data.SpawnProtection = false;
                                if (!string.IsNullOrEmpty(Config.Gameplay.SpawnProtectionColor))
                                {
                                    pawn.Render = Color.White;
                                    Utilities.SetStateChanged(player, "CBaseModelEntity", "m_clrRender");
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


                Server.NextFrame(() =>
                {
                    if (Config.PlayersPreferences.EquipSlot.Enabled)
                    {
                        var equipSlot = GetPrefsValue(data, "EquipSlot", "-1");
                        if (equipSlot == "2")
                        {
                            var weapon = player.PlayerPawn.Value?.SetActiveWeapon(player, gear_slot_t.GEAR_SLOT_PISTOL);
                            if (Config.Gameplay.FastWeaponEquip && weapon != null && weapon.IsValid)
                            {
                                weapon.NextPrimaryAttackTick = Server.TickCount + 1;
                                Utilities.SetStateChanged(player, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
                            }
                        }
                        else if (Config.Gameplay.FastWeaponEquip)
                        {
                            var activeWeapon = player.PlayerPawn.Value?.GetActiveWeapon();
                            if (activeWeapon != null && activeWeapon.IsValid)
                            {
                                activeWeapon.NextPrimaryAttackTick = Server.TickCount + 1;
                                Utilities.SetStateChanged(player, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
                            }
                        }
                    }
                    else if (Config.Gameplay.FastWeaponEquip)
                    {
                        var activeWeapon = player.PlayerPawn.Value?.GetActiveWeapon();
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
                    if (!IsHaveWeaponFromSlot(pawn, gear_slot_t.GEAR_SLOT_RIFLE))
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
                    if (!IsHaveWeaponFromSlot(pawn, gear_slot_t.GEAR_SLOT_PISTOL))
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
            foreach (var pref in Preferences)
            {
                if (data.Preferences.ContainsKey(pref.Name))
                    continue;

                if (pref.vipOnly)
                {
                    if (IsVIP)
                        data.Preferences[pref.Name] = pref.defaultValue;
                    else
                    {
                        if (pref.defaultValue is bool)
                            data.Preferences[pref.Name] = false;
                        else if (pref.defaultValue is int)
                            data.Preferences[pref.Name] = -1;
                    }
                }
                else
                    data.Preferences[pref.Name] = pref.defaultValue;
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
                            var primary = mode.Value.PrimaryWeapons.Where(w => !Config.RestrictedWeapons.Restrictions.ContainsKey(w)).FirstOrDefault();
                            data.PrimaryWeapon[mode.Key] = string.IsNullOrEmpty(primary) ? "" : primary;
                        }
                        else
                            data.PrimaryWeapon[mode.Key] = "";
                        if (mode.Value.SecondaryWeapons.Any())
                        {
                            var secondary = mode.Value.SecondaryWeapons.Where(w => !Config.RestrictedWeapons.Restrictions.ContainsKey(w)).FirstOrDefault();
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

        private void SwitchStringPrefsValue(CCSPlayerController player, string preference, string value, string valueName)
        {
            if (!playerData.TryGetValue(player.Slot, out var data))
                return;

            data.Preferences[preference] = value;

            var Preference = Localizer[$"Prefs.{preference}"];
            player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Prefs.ValueSet", Preference, valueName]}");
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