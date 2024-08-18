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
            if (ActiveMode == null)
                return;

            if (string.IsNullOrEmpty(weaponName))
            {
                string primaryWeapons = "";
                string secondaryWeapons = "";
                if (ActiveMode.PrimaryWeapons.Count != 0)
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
                if (ActiveMode.SecondaryWeapons.Count != 0)
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
            if (weaponSelectMapping.ContainsKey(weaponName))
            {
                weaponName = weaponSelectMapping[weaponName];
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
                if (weaponName == playerData[player].PrimaryWeapon)
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

                    var restrictInfo = GetRestrictData(weaponName, player.Team);
                    info.ReplyToCommand($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponIsRestricted", localizerWeaponName, restrictInfo.Item1, restrictInfo.Item2]}");
                    return;
                }

                playerData[player].PrimaryWeapon = weaponName;
                info.ReplyToCommand($"{Localizer["Chat.Prefix"]} {Localizer["Chat.PrimaryWeaponSet", localizerWeaponName]}");

                var primaryWeapon = GetWeaponFromSlot(player, gear_slot_t.GEAR_SLOT_RIFLE);
                if (player.PawnIsAlive && primaryWeapon == null)
                {
                    player.GiveNamedItem(weaponName);
                }
                else if (Config.Gameplay.SwitchWeapons && player.PawnIsAlive)
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
                return;
            }
            else if (ActiveMode.SecondaryWeapons.Contains(weaponName))
            {
                string localizerWeaponName = Localizer[weaponName];
                if (weaponName == playerData[player].SecondaryWeapon)
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

                    var restrictInfo = GetRestrictData(weaponName, player.Team);
                    info.ReplyToCommand($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponIsRestricted", localizerWeaponName, restrictInfo.Item1, restrictInfo.Item2]}");
                    return;
                }

                playerData[player].SecondaryWeapon = weaponName;
                info.ReplyToCommand($"{Localizer["Chat.Prefix"]} {Localizer["Chat.SecondaryWeaponSet", localizerWeaponName]}");

                var secondaryWeapon = GetWeaponFromSlot(player, gear_slot_t.GEAR_SLOT_PISTOL);
                if (player.PawnIsAlive && secondaryWeapon == null)
                {
                    player.GiveNamedItem(weaponName);
                }
                else if (Config.Gameplay.SwitchWeapons && player.PawnIsAlive)
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
            string PrimaryWeapon = "";
            string SecondaryWeapon = "";
            if (playerData.ContainsPlayer(player) && player.PlayerPawn.Value != null)
            {
                if (player.InGameMoneyServices != null && giveUtilities)
                    player.InGameMoneyServices.Account = Config.Gameplay.AllowBuyMenu ? 16000 : 0;

                if (!bNewMode)
                {
                    var timer = AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag) ? Config.PlayersSettings.ProtectionTimeVIP : Config.PlayersSettings.ProtectionTime;
                    if (timer > 0.1)
                    {
                        if (!playerData.ContainsPlayer(player))
                            return;

                        if (string.IsNullOrEmpty(Config.Gameplay.SpawnProtectionColor))
                        {
                            Color transparentColor = ColorTranslator.FromHtml(Config.Gameplay.SpawnProtectionColor);
                            player.PlayerPawn.Value.Render = transparentColor;
                            Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");
                        }
                        playerData[player].SpawnProtection = true;
                        AddTimer(timer, () =>
                        {
                            if (playerData.ContainsPlayer(player))
                            {
                                playerData[player].SpawnProtection = false;
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
                        playerData[player].BlockRandomWeaponsIntegeration = Server.CurrentTime;
                    }
                }
                if (ActiveMode == null)
                    return;

                bool IsVIP = AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag);
                playerData[player].LastPrimaryWeapon = "";
                playerData[player].LastSecondaryWeapon = "";

                if (ActiveMode.PrimaryWeapons.Count != 0)
                {
                    PrimaryWeapon = playerData[player].PrimaryWeapon;
                    if (ActiveMode.RandomWeapons)
                    {
                        PrimaryWeapon = GetRandomWeaponFromList(ActiveMode.PrimaryWeapons, IsVIP, player.Team, true);
                        playerData[player].LastPrimaryWeapon = PrimaryWeapon;
                    }
                    else if (string.IsNullOrEmpty(PrimaryWeapon) || !ActiveMode.PrimaryWeapons.Contains(PrimaryWeapon))
                    {
                        PrimaryWeapon = ActiveMode.PrimaryWeapons.Count switch
                        {
                            1 => ActiveMode.PrimaryWeapons[0],
                            _ => Config.Gameplay.DefaultModeWeapons switch
                            {
                                2 => GetRandomWeaponFromList(ActiveMode.PrimaryWeapons, IsVIP, player.Team, true),
                                1 => ActiveMode.PrimaryWeapons[0],
                                _ => ""
                            }
                        };
                    }
                }

                if (ActiveMode.SecondaryWeapons.Count != 0)
                {
                    SecondaryWeapon = playerData[player].SecondaryWeapon;
                    if (ActiveMode.RandomWeapons)
                    {
                        SecondaryWeapon = GetRandomWeaponFromList(ActiveMode.SecondaryWeapons, IsVIP, player.Team, false);
                        playerData[player].LastSecondaryWeapon = SecondaryWeapon;
                    }
                    else if (string.IsNullOrEmpty(SecondaryWeapon) || !ActiveMode.SecondaryWeapons.Contains(SecondaryWeapon))
                    {
                        SecondaryWeapon = ActiveMode.SecondaryWeapons.Count switch
                        {
                            1 => ActiveMode.SecondaryWeapons[0],
                            _ => Config.Gameplay.DefaultModeWeapons switch
                            {
                                2 => GetRandomWeaponFromList(ActiveMode.SecondaryWeapons, IsVIP, player.Team, false),
                                1 => ActiveMode.SecondaryWeapons[0],
                                _ => ""
                            }
                        };
                    }
                }

                if (!string.IsNullOrEmpty(PrimaryWeapon))
                    player.GiveNamedItem(PrimaryWeapon);

                if (!string.IsNullOrEmpty(SecondaryWeapon))
                    player.GiveNamedItem(SecondaryWeapon);

                if (giveKnife)
                    player.GiveNamedItem("weapon_knife");

                if (giveUtilities && ActiveMode.Utilities.Count() > 0)
                {
                    foreach (var item in ActiveMode.Utilities)
                        player.GiveNamedItem(item);

                    if (ActiveMode.Utilities.Contains("weapon_taser"))
                        player.GiveNamedItem("weapon_knife");
                }

                return;
            }

            if (player == null || !player.IsValid || ActiveMode == null)
                return;

            if (player.InGameMoneyServices != null)
                player.InGameMoneyServices.Account = 0;

            if (ActiveMode.PrimaryWeapons.Count != 0)
            {
                if (!IsHaveWeaponFromSlot(player, gear_slot_t.GEAR_SLOT_RIFLE))
                {
                    PrimaryWeapon = ActiveMode.PrimaryWeapons.Count switch
                    {
                        1 => ActiveMode.PrimaryWeapons[0],
                        _ => Config.Gameplay.DefaultModeWeapons switch
                        {
                            2 => GetRandomWeaponFromList(ActiveMode.PrimaryWeapons, false, player.Team, true),
                            1 => ActiveMode.PrimaryWeapons[0],
                            _ => ""
                        }
                    };
                    if (!string.IsNullOrEmpty(PrimaryWeapon))
                        player.GiveNamedItem(PrimaryWeapon);
                }
            }
            if (ActiveMode.SecondaryWeapons.Count != 0)
            {
                if (!IsHaveWeaponFromSlot(player, gear_slot_t.GEAR_SLOT_PISTOL))
                {
                    SecondaryWeapon = ActiveMode.SecondaryWeapons.Count switch
                    {
                        1 => ActiveMode.SecondaryWeapons[0],
                        _ => Config.Gameplay.DefaultModeWeapons switch
                        {
                            2 => GetRandomWeaponFromList(ActiveMode.SecondaryWeapons, false, player.Team, true),
                            1 => ActiveMode.SecondaryWeapons[0],
                            _ => ""
                        }
                    };
                    if (!string.IsNullOrEmpty(SecondaryWeapon))
                        player.GiveNamedItem(SecondaryWeapon);
                }
            }
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

            public bool ContainsPlayer(CCSPlayerController? player)
            {
                if (player == null || !player.IsValid || !player.PlayerPawn.IsValid || player.SteamID.ToString().Length != 17)
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
            if (playerData.ContainsPlayer(player))
            {
                var time = Server.CurrentTime - playerData[player].BlockRandomWeaponsIntegeration;
                return time < 0.2;
            }
            return true;
        }
    }
}