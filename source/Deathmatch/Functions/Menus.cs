using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;

namespace Deathmatch
{
    public partial class Deathmatch
    {
        private void OnSelectSubMenu(CCSPlayerController player, ChatMenuOption option, int menutype)
        {
            playerData[player].OpenedMenu = menutype;
            OpenSubMenu(player, menutype);
        }
        private void OnSelectBack(CCSPlayerController player, ChatMenuOption option)
        {
            OpenMainMenu(player);
        }
        public void OpenMainMenu(CCSPlayerController player)
        {
            playerData[player].OpenedMenu = 0;

            var Menu = new CenterHtmlMenu($"{Localizer["Menu.Title"]}<br>", this);
            Menu.AddMenuOption($"{Localizer["Menu.Functions"]}", (player, opt) => OnSelectSubMenu(player, opt, 2));
            Menu.AddMenuOption($"{Localizer["Menu.Sounds"]}", (player, opt) => OnSelectSubMenu(player, opt, 1));

            Menu.Open(player);
        }
        private void OnSelectSwitchPref(CCSPlayerController player, ChatMenuOption option, int preference, bool solo = false)
        {
            SwitchPrefsValue(player, preference);
            OpenSubMenu(player, playerData[player].OpenedMenu, solo);
        }

        public void OpenSubMenu(CCSPlayerController player, int menu, bool solo = false)
        {
            var PrefsMenu = menu == 1 ? PrefsMenuSounds : PrefsMenuFunctions;
            var title = menu == 1 ? Localizer["Menu.SoundsTitle"] : Localizer["Menu.FunctionsTitle"];

            bool IsVIP = AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag);
            var Menu = new CenterHtmlMenu($"{title}<br>", this);
            string Value;

            foreach (var options in PrefsMenu)
            {
                Value = GetPrefsValue(player!, options.Item3) ? "ON" : "OFF";

                if (options.Item2 && IsVIP || !options.Item2)
                {
                    Menu.AddMenuOption($"{Localizer[options.Item1]} [{Value}]", (player, opt) => OnSelectSwitchPref(player, opt, options.Item3, solo));
                }
            }
            if (!solo)
                Menu.AddMenuOption($"{Localizer["Menu.Back"]}", OnSelectBack);

            Menu.Open(player);
        }

        private void SetupDeathmatchMenus()
        {
            PrefsMenuSounds.Clear();
            PrefsMenuFunctions.Clear();

            if (Config.PlayersPreferences.KillSound.Enabled)
                PrefsMenuSounds.Add(("Prefs.KillSound", Config.PlayersPreferences.KillSound.OnlyVIP, 1));
            if (Config.PlayersPreferences.HSKillSound.Enabled)
                PrefsMenuSounds.Add(("Prefs.HeadshotKillSound", Config.PlayersPreferences.HSKillSound.OnlyVIP, 2));
            if (Config.PlayersPreferences.KnifeKillSound.Enabled)
                PrefsMenuSounds.Add(("Prefs.KnifeKillSound", Config.PlayersPreferences.KnifeKillSound.OnlyVIP, 3));
            if (Config.PlayersPreferences.HitSound.Enabled)
                PrefsMenuSounds.Add(("Prefs.HitSound", Config.PlayersPreferences.HitSound.OnlyVIP, 4));
            if (Config.PlayersPreferences.OnlyHS.Enabled)
                PrefsMenuFunctions.Add(("Prefs.OnlyHS", Config.PlayersPreferences.OnlyHS.OnlyVIP, 5));
            if (Config.PlayersPreferences.HudMessages.Enabled)
                PrefsMenuFunctions.Add(("Prefs.HudMessages", Config.PlayersPreferences.HudMessages.OnlyVIP, 6));
        }
    }
}