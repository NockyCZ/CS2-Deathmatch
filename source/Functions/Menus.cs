using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using System.Drawing;
using CounterStrikeSharp.API.Modules.Menu;
using System.Collections.Generic;

namespace Deathmatch
{
    public partial class DeathmatchCore
    {
        List<(string, bool, int)> PrefsMenuSounds = new List<(string, bool, int)>();
        List<(string, bool, int)> PrefsMenuFunctions = new List<(string, bool, int)>();

        delegate void OnSelectSwitchPrefDelegate(CCSPlayerController player, ChatMenuOption option);

        private void OnSelectSubMenu(CCSPlayerController player, ChatMenuOption option)
        {
            playerData[player].OpenedMenu = option.Text.Contains("Sounds") ? 1 : 2;
            OpenSubMenu(player, playerData[player].OpenedMenu);
        }
        private void OnSelectBack(CCSPlayerController player, ChatMenuOption option)
        {
            OpenMainMenu(player);
        }
        public void OpenMainMenu(CCSPlayerController player)
        {
            playerData[player].OpenedMenu = 0;

            var Menu = new CenterHtmlMenu($"Deathmatch Menu<br>");
            Menu.AddMenuOption($"Functions Menu", OnSelectSubMenu);
            Menu.AddMenuOption($"Sounds Menu", OnSelectSubMenu);

            MenuManager.OpenCenterHtmlMenu(DeathmatchCore.Instance, player!, Menu);
        }
        private void OnSelectSwtichPref(CCSPlayerController player, ChatMenuOption option)
        {
            //SwitchPrefsValue(player, GetPrefsIDbyName(option.Text));
            //OpenSubMenu(player, playerData[player].OpenedMenu);
        }

        private void OnSelectSwtichPref(CCSPlayerController player, ChatMenuOption option, int preference)
        {
            SwitchPrefsValue(player, preference);
            OpenSubMenu(player, playerData[player].OpenedMenu);
        }

        public void OpenSubMenu(CCSPlayerController player, int menu)
        {
            var PrefsMenu = menu == 1 ? PrefsMenuSounds : PrefsMenuFunctions;
            var title = menu == 1 ? Localizer["Menu.SoundsTitle"] : Localizer["Menu.FunctionsTitle"];

            bool IsVIP = AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag);
            var Menu = new CenterHtmlMenu($"{title}<br>");
            string Value;

            foreach (var options in PrefsMenu)
            {
                Value = GetPrefsValue(player!, options.Item3) ? "ON" : "OFF";

                if (options.Item2 && IsVIP || !options.Item2)
                {
                    Menu.AddMenuOption($"{Localizer[options.Item1]} [{Value}]", (p, opt) => OnSelectSwtichPref(player, opt, options.Item3));
                }
            }
            Menu.AddMenuOption($"{Localizer["Menu.Back"]}", OnSelectBack);
            MenuManager.OpenCenterHtmlMenu(DeathmatchCore.Instance, player!, Menu);
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