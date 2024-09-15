using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using System.Drawing;
using DeathmatchAPI.Helpers;

namespace Deathmatch
{
    public partial class Deathmatch
    {
        private void AddCustomCommands(string command, string weapon_name, int type)
        {
            string cmdName = $"css_{command}";
            switch (type)
            {
                case 1:
                    AddCommand(cmdName, $"Weapon Shortcut: {weapon_name}", (player, info) =>
                    {
                        if (!playerData.ContainsPlayer(player))
                            return;

                        if (ActiveMode.RandomWeapons)
                        {
                            info.ReplyToCommand($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponsSelectIsDisabled"]}");
                            return;
                        }
                        SetupPlayerWeapons(player!, weapon_name, info);
                    });
                    break;
                case 2:
                    AddCommand(cmdName, "Select a weapon by command", (player, info) =>
                    {
                        if (!playerData.ContainsPlayer(player))
                            return;
                        if (ActiveMode.RandomWeapons)
                        {
                            info.ReplyToCommand($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponsSelectIsDisabled"]}");
                            return;
                        }

                        string weaponName = info.GetArg(1).ToLower();
                        SetupPlayerWeapons(player!, weaponName, info);
                    });
                    break;
                case 3:
                    AddCommand(cmdName, "Opens a Deathmatch menu", (player, info) =>
                    {
                        if (!playerData.ContainsPlayer(player))
                            return;

                        if (Preferences.Count == 0)
                            return;

                        if (Preferences.Where(x => x.Category == CategoryType.SOUNDS).Count() > 0 && Preferences.Where(x => x.Category == CategoryType.FUNCTIONS).Count() == 0)
                        {
                            OpenSubMenu(player!, CategoryType.SOUNDS, true);
                            return;
                        }
                        if (Preferences.Where(x => x.Category == CategoryType.FUNCTIONS).Count() > 0 && Preferences.Where(x => x.Category == CategoryType.SOUNDS).Count() == 0)
                        {
                            OpenSubMenu(player!, CategoryType.FUNCTIONS, true);
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
                info.ReplyToCommand($"{Localizer["Chat.Prefix"]} You have to be alive to see the distance!");
                return;
            }
            var distance = info.GetArg(1);
            if (!int.TryParse(distance, out int radius))
            {
                info.ReplyToCommand($"{Localizer["Chat.Prefix"]} The distance must be a number!");
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
            if (int.TryParse(modeid, out _))
            {
                if (!CustomModes.ContainsKey(modeid))
                {
                    info.ReplyToCommand($"{Localizer["Chat.Prefix"]} A mod with a number doesn't exist!");
                    return;
                }
            }
            else
            {
                info.ReplyToCommand($"{Localizer["Chat.Prefix"]} Mode ID must be a number!");
                return;
            }
            SetupCustomMode(modeid);
        }

        [ConsoleCommand("css_dm_editor", "Enable or Disable spawn points editor")]
        [ConsoleCommand("css_dm_spawns", "Enable or Disable spawn points editor")]
        [ConsoleCommand("css_dm_spawnseditor", "Enable or Disable spawn points editor")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [RequiresPermissions("@css/root")]
        public void OnEditor_CMD(CCSPlayerController player, CommandInfo info)
        {
            if (Config.Gameplay.DefaultSpawns)
            {
                info.ReplyToCommand($"{Localizer["Chat.Prefix"]} The Spawn Editor cannot be used if you are using the default spawns!");
                return;
            }
            if (player.IsValid && !player.PawnIsAlive)
            {
                info.ReplyToCommand($"{Localizer["Chat.Prefix"]} You have to be alive to use Spawns Editor!");
                return;
            }
            ShowAllSpawnPoints();
            ActiveEditor = player;
        }
    }
}