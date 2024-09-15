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
            var title = menuType == CategoryType.SOUNDS ? Localizer["Menu.SoundsTitle"] : Localizer["Menu.FunctionsTitle"];

            bool IsVIP = AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag);
            var Menu = new CenterHtmlMenu(title, this);
            string Value;

            foreach (var option in Preferences.Where(x => x.Category == menuType))
            {
                Value = GetPrefsValue(player, option.Name) ? "ON" : "OFF";
                if (option.vipOnly)
                {
                    if (IsVIP)
                        Menu.AddMenuOption($"{Localizer[$"Prefs.{option.Name}"]} [{Value}]", (player, opt) => OnSelectSwitchPref(player, menuType, option.Name, solo));
                }
                else
                    Menu.AddMenuOption($"{Localizer[$"Prefs.{option.Name}"]} [{Value}]", (player, opt) => OnSelectSwitchPref(player, menuType, option.Name, solo));

            }
            if (!solo)
                Menu.AddMenuOption($"{Localizer["Menu.Back"]}", OnSelectBack);

            Menu.Open(player);
        }

        public void OnSelectSwitchPref(CCSPlayerController player, CategoryType menuType, string preference, bool solo = false)
        {
            SwitchPrefsValue(player, preference);
            OpenSubMenu(player, menuType, solo);
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
                    vipOnly = Config.PlayersPreferences.KillSound.OnlyVIP
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
                    vipOnly = Config.PlayersPreferences.HSKillSound.OnlyVIP
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
                    vipOnly = Config.PlayersPreferences.KnifeKillSound.OnlyVIP
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
                    vipOnly = Config.PlayersPreferences.HitSound.OnlyVIP
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
                    vipOnly = Config.PlayersPreferences.OnlyHS.OnlyVIP
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
                    vipOnly = Config.PlayersPreferences.HudMessages.OnlyVIP
                };
                Preferences.Add(data);
            }
        }
    }
}