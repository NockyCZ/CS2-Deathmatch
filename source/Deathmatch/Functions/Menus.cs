using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using DeathmatchAPI;
using DeathmatchAPI.Helpers;
using static DeathmatchAPI.Preferences;

namespace Deathmatch
{
    public partial class Deathmatch
    {
        public void OpenEditorMenu(CCSPlayerController? player)
        {
            RemoveSpawnModels();
            ShowAllSpawnPoints();

            if (player == null || !player.IsValid || player.PlayerPawn.Value == null)
                return;

            if (!player.PawnIsAlive)
            {
                player.PrintToChat($"{Localizer["Chat.Prefix"]} You have to be alive to use Spawns Editor!");
                return;
            }

            var Menu = new CenterHtmlMenu("<font class='fontSize-l' color='red'>Spawns Editor</font><br> ", this);

            var ctSpawns = DefaultMapSpawnDisabled ? spawnPoints.Count(x => x.Team == CsTeam.CounterTerrorist) : 0;
            var tSpawns = DefaultMapSpawnDisabled ? spawnPoints.Count(x => x.Team == CsTeam.Terrorist) : 0;

            Menu.AddMenuOption($"Add CT Spawn ({ctSpawns})", (player, opt) =>
            {
                AddNewSpawnPoint(player.PlayerPawn.Value?.AbsOrigin!, player.PlayerPawn.Value?.AbsRotation!, CsTeam.CounterTerrorist);
                player.PrintToChat($"{Localizer["Chat.Prefix"]} Spawn for the {ChatColors.DarkBlue}CT team{ChatColors.Default} has been added. (Total: {ChatColors.Green}{spawnPoints.Count(x => x.Team == CsTeam.CounterTerrorist)}{ChatColors.Default})");
                OpenEditorMenu(player);
            });
            Menu.AddMenuOption($"Add T Spawn ({tSpawns})", (player, opt) =>
            {
                AddNewSpawnPoint(player.PlayerPawn.Value?.AbsOrigin!, player.PlayerPawn.Value?.AbsRotation!, CsTeam.Terrorist);
                player.PrintToChat($"{Localizer["Chat.Prefix"]} Spawn for the {ChatColors.Orange}T team{ChatColors.Default} has been added. (Total: {ChatColors.Green}{spawnPoints.Count(x => x.Team == CsTeam.Terrorist)}{ChatColors.Default})");
                OpenEditorMenu(player);
            });

            Menu.AddMenuOption("Remove the Nearest Spawn", (player, opt) =>
            {
                RemoveNearestSpawnPoint(player.PlayerPawn.Value!.AbsOrigin);
                player.PrintToChat($"{Localizer["Chat.Prefix"]} The nearest spawn point has been removed!");
                OpenEditorMenu(player);
            });

            Menu.AddMenuOption("<font class='fontSize-m' color='cyan'>Save Spawns</font>", (player, opt) =>
            {
                SaveSpawnsFile();
                LoadMapSpawns(ModuleDirectory + $"/spawns/{Server.MapName}.json");
                player.PrintToChat($"{Localizer["Chat.Prefix"]} Spawns have been successfully saved!");
                RemoveSpawnModels();
                MenuManager.CloseActiveMenu(player);
            });

            Menu.ExitButton = false;
            Menu.Open(player);
        }

        public void OpenMainMenu(CCSPlayerController player)
        {
            var Menu = new CenterHtmlMenu($"{Localizer["Menu.Title"]}", this);
            foreach (var category in Categorie.GetAllCategories().Where(c => Preferences.Menu.GetOptionsByCategory(c).Any()))
            {
                Menu.AddMenuOption($"{Localizer[category.MenuOption]}", (player, opt) => OpenCategoryMenu(player, category));
            }

            foreach (var option in Preferences.Menu.GetAllOptions().Where(x => !string.IsNullOrEmpty(x.Name) && x.Category == null && x.OnChoose != null))
            {
                if (option.Permission != null && !AdminManager.PlayerHasPermissions(player, option.Permission))
                    continue;

                Menu.AddMenuOption($"{Localizer[option.Name!]}", (player, opt) =>
                {
                    //if (option.OnChoose != null)
                    option.OnChoose!(player, option);
                });
            }

            Menu.ExitButton = true;
            Menu.Open(player);
        }

        public void OpenCategoryMenu(CCSPlayerController player, Categorie category)
        {
            if (!playerData.TryGetValue(player.Slot, out var data))
                return;

            var title = category.MenuTitle;
            if (category.UseLocalizer)
                title = Localizer[category.MenuTitle];

            bool IsVIP = AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag);
            var Menu = new CenterHtmlMenu(title, this);

            foreach (var option in Preferences.Menu.GetOptionsByCategory(category))
            {
                bool canSee = option.Preference.VipOnly ? IsVIP : true;
                if (!canSee)
                    continue;

                if (option.Preference.BooleanData != null)
                {

                    var boolValue = GetPrefsValue(data, option.Preference.Name, option.Preference.BooleanData.DefaultValue) ? "ON" : "OFF";
                    Menu.AddMenuOption($"{Localizer[$"Prefs.{option.Preference.Name}"]} [{boolValue}]", (player, opt) =>
                    {
                        SwitchBooleanPrefsValue(player, option.Preference.Name);
                        OpenCategoryMenu(player, category);
                    });
                }
                else if (option.Preference.Data != null)
                {
                    var currentValue = GetPrefsValue(data, option.Preference.Name, option.Preference.Data.DefaultValue) as string;
                    Menu.AddMenuOption($"{Localizer[$"Prefs.{option.Name}"]} [{currentValue}]", (player, opt) =>
                    {
                        SwitchStringPrefsValue(player, option.Preference.Name, option.Preference.Data.Options, currentValue);
                        OpenCategoryMenu(player, category);
                    });
                }
                else if (option.OnChoose != null && !string.IsNullOrEmpty(option.Name))
                {
                    Menu.AddMenuOption(option.Name, (player, opt) =>
                    {
                        option.OnChoose(player, option);
                    });
                }
            }
            if (Categorie.GetAllCategories().Count > 1)
                Menu.AddMenuOption($"{Localizer["Menu.Back"]}", (player, opt) => OpenMainMenu(player));

            Menu.Open(player);
        }


        private void SetupDeathmatchMenus()
        {
            SoundsMenu();
            FunctionsMenu();
            if (Config.General.DisplayZonesEditorInMenu)
            {
                Menu.AddOption("Spawns Editor", null, (player, option) =>
                {
                    if (Config.SpawnSystem.SpawnsMethod != 0)
                    {
                        player.PrintToChat($"{Localizer["Chat.Prefix"]} The Spawn Editor cannot be used if you are using the default spawns!");
                        return;
                    }
                    if (player.IsValid && !player.PawnIsAlive)
                    {
                        player.PrintToChat($"{Localizer["Chat.Prefix"]} You have to be alive to use Spawns Editor!");
                        return;
                    }
                    OpenEditorMenu(player);
                }, "@css/root");
            }
        }

        public void SoundsMenu()
        {
            if (!Config.PlayersPreferences.KillSound.Enabled && !Config.PlayersPreferences.HSKillSound.Enabled && !Config.PlayersPreferences.KnifeKillSound.Enabled && !Config.PlayersPreferences.HitSound.Enabled)
            {
                return;
            }

            var soundsMenu = RegisterMenuCategory("SOUNDS", "Sounds", "Sounds Menu");
            if (soundsMenu == null)
                return;

            if (Config.PlayersPreferences.KillSound.Enabled)
            {
                var data = new PreferencesBooleanData()
                {
                    DefaultValue = Config.PlayersPreferences.KillSound.DefaultValue,
                    CommandShortcuts = Config.PlayersPreferences.KillSound.Shotcuts
                };

                var preference = RegisterPreference("KillSound", data, Config.PlayersPreferences.KillSound.OnlyVIP);
                if (preference != null)
                    Menu.AddPreferenceOption(soundsMenu, preference);
            }

            if (Config.PlayersPreferences.HSKillSound.Enabled)
            {
                var data = new PreferencesBooleanData()
                {
                    DefaultValue = Config.PlayersPreferences.HSKillSound.DefaultValue,
                    CommandShortcuts = Config.PlayersPreferences.HSKillSound.Shotcuts
                };
                var preference = RegisterPreference("HeadshotKillSound", data, Config.PlayersPreferences.HSKillSound.OnlyVIP);
                if (preference != null)
                    Menu.AddPreferenceOption(soundsMenu, preference);
            }

            if (Config.PlayersPreferences.KnifeKillSound.Enabled)
            {
                var data = new PreferencesBooleanData()
                {
                    DefaultValue = Config.PlayersPreferences.KnifeKillSound.DefaultValue,
                    CommandShortcuts = Config.PlayersPreferences.KnifeKillSound.Shotcuts
                };
                var preference = RegisterPreference("KnifeKillSound", data, Config.PlayersPreferences.KnifeKillSound.OnlyVIP);
                if (preference != null)
                    Menu.AddPreferenceOption(soundsMenu, preference);
            }
            if (Config.PlayersPreferences.HitSound.Enabled)
            {
                var data = new PreferencesBooleanData()
                {
                    DefaultValue = Config.PlayersPreferences.HitSound.DefaultValue,
                    CommandShortcuts = Config.PlayersPreferences.HitSound.Shotcuts
                };
                var preference = RegisterPreference("HitSound", data, Config.PlayersPreferences.HitSound.OnlyVIP);
                if (preference != null)
                    Menu.AddPreferenceOption(soundsMenu, preference);
            }
        }

        public void FunctionsMenu()
        {
            if (!Config.PlayersPreferences.NoPrimary.Enabled && !Config.PlayersPreferences.OnlyHS.Enabled && !Config.PlayersPreferences.HudMessages.Enabled && !Config.PlayersPreferences.DamageInfo.Enabled)
            {
                return;
            }

            var functionsMenu = RegisterMenuCategory("FUNCTIONS", "Functions", "Functions Menu");
            if (functionsMenu == null)
                return;

            if (Config.PlayersPreferences.NoPrimary.Enabled)
            {
                var data = new PreferencesBooleanData()
                {
                    DefaultValue = Config.PlayersPreferences.NoPrimary.DefaultValue,
                    CommandShortcuts = Config.PlayersPreferences.NoPrimary.Shotcuts
                };
                var preference = RegisterPreference("NoPrimary", data, Config.PlayersPreferences.NoPrimary.OnlyVIP);
                if (preference != null)
                    Menu.AddPreferenceOption(functionsMenu, preference);
            }

            if (Config.PlayersPreferences.OnlyHS.Enabled)
            {
                var data = new PreferencesBooleanData()
                {
                    DefaultValue = Config.PlayersPreferences.OnlyHS.DefaultValue,
                    CommandShortcuts = Config.PlayersPreferences.OnlyHS.Shotcuts
                };
                var preference = RegisterPreference("OnlyHS", data, Config.PlayersPreferences.OnlyHS.OnlyVIP);
                if (preference != null)
                    Menu.AddPreferenceOption(functionsMenu, preference);
            }
            if (Config.PlayersPreferences.HudMessages.Enabled)
            {
                var data = new PreferencesBooleanData()
                {
                    DefaultValue = Config.PlayersPreferences.HudMessages.DefaultValue,
                    CommandShortcuts = Config.PlayersPreferences.HudMessages.Shotcuts
                };
                var preference = RegisterPreference("HudMessages", data, Config.PlayersPreferences.HudMessages.OnlyVIP);
                if (preference != null)
                    Menu.AddPreferenceOption(functionsMenu, preference);
            }
            if (Config.PlayersPreferences.DamageInfo.Enabled)
            {
                var data = new PreferencesBooleanData()
                {
                    DefaultValue = Config.PlayersPreferences.DamageInfo.DefaultValue,
                    CommandShortcuts = Config.PlayersPreferences.DamageInfo.Shotcuts
                };
                var preference = RegisterPreference("DamageInfo", data, Config.PlayersPreferences.DamageInfo.OnlyVIP);
                if (preference != null)
                    Menu.AddPreferenceOption(functionsMenu, preference);
            }
        }
    }
}