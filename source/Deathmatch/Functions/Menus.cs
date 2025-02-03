using System.Net.Sockets;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using DeathmatchAPI.Helpers;

namespace Deathmatch
{
    public partial class Deathmatch
    {
        public void OpenMainMenu(CCSPlayerController player)
        {
            var Menu = new CenterHtmlMenu($"{Localizer["Menu.Title"]}", this);
            if (Preferences.Where(x => x.Category == CategoryType.FUNCTIONS).Count() > 0)
                Menu.AddMenuOption($"{Localizer["Menu.Functions"]}", (player, opt) => OnSelectSubMenu(player, CategoryType.FUNCTIONS));

            if (Preferences.Where(x => x.Category == CategoryType.SOUNDS).Count() > 0)
                Menu.AddMenuOption($"{Localizer["Menu.Sounds"]}", (player, opt) => OnSelectSubMenu(player, CategoryType.SOUNDS));

            Menu.Open(player);
        }

        public void OpenSubMenu(CCSPlayerController player, CategoryType menuType, bool solo = false)
        {
            if (!playerData.TryGetValue(player.Slot, out var data))
                return;

            var title = menuType == CategoryType.SOUNDS ? Localizer["Menu.SoundsTitle"] : Localizer["Menu.FunctionsTitle"];

            bool IsVIP = AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag);
            var Menu = new CenterHtmlMenu(title, this);
            //string Value;

            foreach (var option in Preferences.Where(x => x.Category == menuType))
            {
                bool canSee = option.vipOnly ? IsVIP : true;
                if (!canSee)
                    continue;

                switch (option.defaultValue)
                {
                    case bool value:
                        var boolValue = GetPrefsValue(data, option.Name, value) ? "ON" : "OFF";

                        Menu.AddMenuOption($"{Localizer[$"Prefs.{option.Name}"]} [{boolValue}]", (player, opt) =>
                        {
                            SwitchBooleanPrefsValue(player, option.Name);
                            OpenSubMenu(player, menuType, solo);
                        });
                        break;
                    case string value:
                        if (!int.TryParse(GetPrefsValue(data, option.Name, value), out var intValue))
                            break;

                        var Value = intValue == 2 ? Localizer["Prefs.Secondary"] : Localizer["Prefs.Primary"];
                        Menu.AddMenuOption($"{Localizer[$"Prefs.{option.Name}"]} [{Value}]", (player, opt) =>
                        {
                            if (intValue == 2)
                                intValue = 1;
                            else
                                intValue = 2;
                            var Value = intValue == 2 ? Localizer["Prefs.Secondary"] : Localizer["Prefs.Primary"];

                            SwitchStringPrefsValue(player, option.Name, intValue.ToString(), Value);
                            OpenSubMenu(player, menuType, solo);
                        });
                        break;
                }
            }
            if (!solo)
                Menu.AddMenuOption($"{Localizer["Menu.Back"]}", OnSelectBack);

            Menu.Open(player);
        }

        public void OnSelectSubMenu(CCSPlayerController player, CategoryType menuType)
        {
            OpenSubMenu(player, menuType);
        }

        public void OnSelectBack(CCSPlayerController player, ChatMenuOption option)
        {
            OpenMainMenu(player);
        }

        private void SetupDeathmatchMenus()
        {
            Preferences.Clear();
            if (Config.PlayersPreferences.KillSound.Enabled)
            {
                var data = new PreferencesData()
                {
                    Name = "KillSound",
                    Category = CategoryType.SOUNDS,
                    defaultValue = Config.PlayersPreferences.KillSound.DefaultValue,
                    vipOnly = Config.PlayersPreferences.KillSound.OnlyVIP,
                    CommandShortcuts = Config.PlayersPreferences.KillSound.Shotcuts
                };
                Preferences.Add(data);
            }

            if (Config.PlayersPreferences.HSKillSound.Enabled)
            {
                var data = new PreferencesData()
                {
                    Name = "HeadshotKillSound",
                    Category = CategoryType.SOUNDS,
                    defaultValue = Config.PlayersPreferences.HSKillSound.DefaultValue,
                    vipOnly = Config.PlayersPreferences.HSKillSound.OnlyVIP,
                    CommandShortcuts = Config.PlayersPreferences.HSKillSound.Shotcuts
                };
                Preferences.Add(data);
            }

            if (Config.PlayersPreferences.KnifeKillSound.Enabled)
            {
                var data = new PreferencesData()
                {
                    Name = "KnifeKillSound",
                    Category = CategoryType.SOUNDS,
                    defaultValue = Config.PlayersPreferences.KnifeKillSound.DefaultValue,
                    vipOnly = Config.PlayersPreferences.KnifeKillSound.OnlyVIP,
                    CommandShortcuts = Config.PlayersPreferences.KnifeKillSound.Shotcuts
                };
                Preferences.Add(data);
            }
            if (Config.PlayersPreferences.HitSound.Enabled)
            {
                var data = new PreferencesData()
                {
                    Name = "HitSound",
                    Category = CategoryType.SOUNDS,
                    defaultValue = Config.PlayersPreferences.HitSound.DefaultValue,
                    vipOnly = Config.PlayersPreferences.HitSound.OnlyVIP,
                    CommandShortcuts = Config.PlayersPreferences.HitSound.Shotcuts
                };
                Preferences.Add(data);
            }

            if (Config.PlayersPreferences.EquipSlot.Enabled)
            {
                var data = new PreferencesData()
                {
                    Name = "EquipSlot",
                    Category = CategoryType.FUNCTIONS,
                    defaultValue = Config.PlayersPreferences.EquipSlot.DefaultValue,
                    vipOnly = Config.PlayersPreferences.EquipSlot.OnlyVIP,
                    CommandShortcuts = Config.PlayersPreferences.EquipSlot.Shotcuts
                };
                Preferences.Add(data);
            }

            if (Config.PlayersPreferences.OnlyHS.Enabled)
            {
                var data = new PreferencesData()
                {
                    Name = "OnlyHS",
                    Category = CategoryType.FUNCTIONS,
                    defaultValue = Config.PlayersPreferences.OnlyHS.DefaultValue,
                    vipOnly = Config.PlayersPreferences.OnlyHS.OnlyVIP,
                    CommandShortcuts = Config.PlayersPreferences.OnlyHS.Shotcuts
                };
                Preferences.Add(data);
            }
            if (Config.PlayersPreferences.HudMessages.Enabled)
            {
                var data = new PreferencesData()
                {
                    Name = "HudMessages",
                    Category = CategoryType.FUNCTIONS,
                    defaultValue = Config.PlayersPreferences.HudMessages.DefaultValue,
                    vipOnly = Config.PlayersPreferences.HudMessages.OnlyVIP,
                    CommandShortcuts = Config.PlayersPreferences.HudMessages.Shotcuts
                };
                Preferences.Add(data);
            }
            if (Config.PlayersPreferences.DamageInfo.Enabled)
            {
                var data = new PreferencesData()
                {
                    Name = "DamageInfo",
                    Category = CategoryType.FUNCTIONS,
                    defaultValue = Config.PlayersPreferences.DamageInfo.DefaultValue,
                    vipOnly = Config.PlayersPreferences.DamageInfo.OnlyVIP,
                    CommandShortcuts = Config.PlayersPreferences.DamageInfo.Shotcuts
                };
                Preferences.Add(data);
            }
        }
    }
}