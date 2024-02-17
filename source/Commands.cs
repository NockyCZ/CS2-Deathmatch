using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using System.Drawing;

namespace Deathmatch
{
    public partial class DeathmatchCore
    {
        Dictionary<string, string> customShortcuts = new Dictionary<string, string>();
        private void AddCustomCommands(string command, string weapon_name, int type)
        {
            string cmdName = $"css_{command}";
            switch (type)
            {
                case 1:
                    AddCommand(cmdName, weapon_name, (player, info) =>
                    {
                        if (!playerData.ContainsPlayer(player!))
                            return;

                        if (ModeData.RandomWeapons)
                        {
                            info.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["Weapon_Select_Is_Disabled"]}");
                            return;
                        }
                        string weaponName = info.GetArg(0).ToLower();
                        weaponName = weaponName.Replace("css_", "");
                        if (customShortcuts.ContainsKey(weaponName))
                        {
                            string weaponID = customShortcuts.FirstOrDefault(x => x.Key == weaponName).Value;
                            SetupPlayerWeapons(player!, weaponID, info);
                        }
                    });
                    break;
                case 2:
                    AddCommand(cmdName, "Select a weapon by command", (player, info) =>
                    {
                        string weaponName = info.GetArg(1).ToLower();
                        if (!playerData.ContainsPlayer(player!))
                            return;

                        if (ModeData.RandomWeapons)
                        {
                            info.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["Weapon_Select_Is_Disabled"]}");
                            return;
                        }
                        SetupPlayerWeapons(player!, weaponName, info);
                    });
                    break;
                case 3:
                    AddCommand(cmdName, "Opens a Deathmatch menu", (player, info) =>
                    {
                        if (!playerData.ContainsPlayer(player!))
                            return;

                        if (PrefsMenuSounds.Count() == 0 && PrefsMenuFunctions.Count() == 0)
                            return;

                        if (PrefsMenuSounds.Count() > 0 && PrefsMenuFunctions.Count() == 0)
                        {
                            OpenSubMenu(player!, 1, true);
                            return;
                        }
                        if (PrefsMenuSounds.Count() == 0 && PrefsMenuFunctions.Count() > 0)
                        {
                            OpenSubMenu(player!, 2, true);
                            return;
                        }
                        OpenMainMenu(player!);

                    });
                    break;
            }
        }

        [ConsoleCommand("css_dm_checkdistance", "Determine the distance for spawn blocking")]
        [CommandHelper(1, "<distance>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [RequiresPermissions("@css/root")]
        public void OnCheckDistance_CMD(CCSPlayerController player, CommandInfo info)
        {
            if (player.IsValid && !player.PawnIsAlive)
            {
                info.ReplyToCommand($"{Localizer["Prefix"]} You have to be alive to add a new spawn!");
                return;
            }
            var distance = info.GetArg(1);
            if (!int.TryParse(distance, out int radius))
            {
                info.ReplyToCommand($"{Localizer["Prefix"]} The distance must be a number!");
                return;
            }
            var position = player.PlayerPawn.Value!.AbsOrigin!;

            int segments = 72;
            for (int i = 0; i < segments; i++)
            {
                float angle = 2.0f * (float)Math.PI * i / segments;

                float x = position.X + radius * (float)Math.Cos(angle);
                float y = position.Y + radius * (float)Math.Sin(angle);

                CBeam beam = Utilities.CreateEntityByName<CBeam>("beam")!;
                beam.Render = Color.Red;
                beam.Width = 10.5f;
                beam.Teleport(position, new QAngle(0, 0, 0), new Vector(0, 0, 0));
                beam.EndPos.X = x;
                beam.EndPos.Y = y;
                beam.EndPos.Z = position.Z;

                beam.DispatchSpawn();
            }
            AddTimer(10.0f, () => { RemoveBeams(); }, TimerFlags.STOP_ON_MAPCHANGE);
        }

        [ConsoleCommand("css_dm_startmode", "Start Custom Mode")]
        [CommandHelper(1, "<mode id>")]
        [RequiresPermissions("@css/root")]
        public void OnStartMode_CMD(CCSPlayerController player, CommandInfo info)
        {
            string modeid = info.GetArg(1);
            SetupCustomMode(modeid);
        }

        [ConsoleCommand("css_dm_editor", "Enable or Disable spawn points editor")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [RequiresPermissions("@css/root")]
        public void OnEditor_CMD(CCSPlayerController player, CommandInfo info)
        {
            g_bIsActiveEditor = !g_bIsActiveEditor;
            info.ReplyToCommand($"{Localizer["Prefix"]} Spawn Editor has been {ChatColors.Green}{(g_bIsActiveEditor ? "Enabled" : "Disabled")}");
            if (g_bIsActiveEditor)
            {
                ShowAllSpawnPoints();
            }
            else
            {
                RemoveBeams();
            }
            LoadMapSpawns(ModuleDirectory + $"/spawns/{Server.MapName}.json", false);
        }

        [ConsoleCommand("css_dm_addspawn_ct", "Add the new CT spawn point")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [RequiresPermissions("@css/root")]
        public void OnAddSpawnCT_CMD(CCSPlayerController player, CommandInfo info)
        {
            if (!g_bIsActiveEditor)
            {
                info.ReplyToCommand($"{Localizer["Prefix"]} Spawn Editor is disabled!");
                return;
            }
            if (player.IsValid && !player.PawnIsAlive)
            {
                info.ReplyToCommand($"{Localizer["Prefix"]} You have to be alive to add a new spawn!");
                return;
            }
            var position = player.PlayerPawn.Value!.AbsOrigin;
            var angle = player.PlayerPawn.Value.AbsRotation;
            AddNewSpawnPoint(ModuleDirectory + $"/spawns/{Server.MapName}.json", $"{position}", $"{angle}", "ct");
            info.ReplyToCommand($"{Localizer["Prefix"]} Spawn for the CT team has been added. (Total: {ChatColors.Green}{g_iTotalCTSpawns}{ChatColors.Default})");
        }
        [ConsoleCommand("css_dm_addspawn_t", "Add the new T spawn point")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [RequiresPermissions("@css/root")]
        public void OnAddSpawnT_CMD(CCSPlayerController player, CommandInfo info)
        {
            if (!g_bIsActiveEditor)
            {
                info.ReplyToCommand($"{Localizer["Prefix"]} Spawn Editor is disabled!");
                return;
            }
            if (player.IsValid && !player.PawnIsAlive)
            {
                info.ReplyToCommand($"{Localizer["Prefix"]} You have to be alive to add a new spawn!");
                return;
            }
            var position = player.PlayerPawn.Value!.AbsOrigin;
            var angle = player.PlayerPawn.Value.AbsRotation;
            AddNewSpawnPoint(ModuleDirectory + $"/spawns/{Server.MapName}.json", $"{position}", $"{angle}", "t");
            info.ReplyToCommand($"{Localizer["Prefix"]} Spawn for the T team has been added. (Total: {ChatColors.Green}{g_iTotalTSpawns}{ChatColors.Default})");
        }
        [ConsoleCommand("css_dm_removespawn", "Remove the nearest spawn point")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [RequiresPermissions("@css/root")]
        public void OnRemoveSpawn_CMD(CCSPlayerController player, CommandInfo info)
        {
            if (!g_bIsActiveEditor)
            {
                info.ReplyToCommand($"{Localizer["Prefix"]} Spawn Editor is disabled!");
                return;
            }
            if (player.IsValid && !player.PawnIsAlive)
            {
                info.ReplyToCommand($"{Localizer["Prefix"]} You have to be alive to remove a spawn!");
                return;
            }
            if (g_iTotalCTSpawns < 1 && g_iTotalTSpawns < 1)
            {
                info.ReplyToCommand($"{Localizer["Prefix"]} No spawns found!");
                return;
            }
            var position = player.PlayerPawn.Value!.AbsOrigin!;

            string deleted = GetNearestSpawnPoint(position[0], position[1], position[2]);
            player.PrintToChat($"{Localizer["Prefix"]} {ChatColors.Default}{deleted}");
        }
    }
}