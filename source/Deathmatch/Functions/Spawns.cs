using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;
using System.Drawing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CounterStrikeSharp.API.Modules.Utils;
using System.Globalization;
using static DeathmatchAPI.Events.IDeathmatchEventsAPI;
using DeathmatchAPI.Helpers;

namespace Deathmatch
{
    public partial class Deathmatch
    {
        public void PerformRespawn(CCSPlayerController player, CsTeam team)
        {
            if (team == CsTeam.None || team == CsTeam.Spectator)
                return;

            //IEnumerable<KeyValuePair<Vector, QAngle>> spawnsDictionary = Config.SpawnSystem.TeamSpawnsSeparation
            //    ? (team == CsTeam.Terrorist ? spawnPositionsT : spawnPositionsCT)
            //    : spawnPositionsT.Concat(spawnPositionsCT);

            IEnumerable<KeyValuePair<Vector, QAngle>> spawnsDictionary = Config.SpawnSystem.TeamSpawnsSeparation
                ? spawnPoints.Where(spawn => spawn.Team == team)
                            .Select(spawn => new KeyValuePair<Vector, QAngle>(spawn.Position, spawn.Angle))
                : spawnPoints.Select(spawn => new KeyValuePair<Vector, QAngle>(spawn.Position, spawn.Angle));

            if (blockedSpawns.TryGetValue(player.Slot, out var lastSpawn))
            {
                spawnsDictionary = spawnsDictionary.Where(spawn => spawn.Key != lastSpawn);
            }

            if (!spawnsDictionary.Any())
            {
                //player.Respawn();
                SendConsoleMessage("[Deathmatch] Spawns list is empty, you got something wrong!", ConsoleColor.Red);
                return;
            }

            var selectedSpawn = GetAvailableSpawn(player, spawnsDictionary);
            if (selectedSpawn.HasValue)
            {
                blockedSpawns[player.Slot] = selectedSpawn.Value.Key;
                player.Pawn.Value?.Teleport(selectedSpawn.Value.Key, selectedSpawn.Value.Value, null);
            }
        }

        public KeyValuePair<Vector, QAngle>? GetAvailableSpawn(CCSPlayerController player, IEnumerable<KeyValuePair<Vector, QAngle>> spawnsList)
        {
            var playerPawns = Utilities.GetPlayers()
                .Where(p => !p.IsHLTV && p.LifeState == (byte)LifeState_t.LIFE_ALIVE && p != player)
                .Select(p => p.PlayerPawn.Value);

            var shuffledSpawns = spawnsList.ToList();
            for (int i = shuffledSpawns.Count - 1; i > 0; i--)
            {
                int j = Random.Shared.Next(i + 1);
                var temp = shuffledSpawns[i];
                shuffledSpawns[i] = shuffledSpawns[j];
                shuffledSpawns[j] = temp;
            }

            foreach (var spawn in shuffledSpawns)
            {
                bool isValidSpawn = true;
                foreach (var pawn in playerPawns)
                {
                    var distance = GetDistance(pawn?.AbsOrigin, spawn.Key);
                    if (distance < CheckedEnemiesDistance)
                    {
                        isValidSpawn = false;
                        break;
                    }

                    /*if (CheckedEnemiesDistance >= 100)
                    {
                        if (distance < CheckedEnemiesDistance)
                        {
                            isValidSpawn = false;
                            break;
                        }

                        if (CheckSpawnVisibility && distance <= 3000 && CanSeeSpawn(pawn, spawn.Key))
                        {
                            isValidSpawn = false;
                            break;
                        }
                    }
                    else
                    {
                        if (distance < 67 && CanSeeSpawn(pawn, spawn.Key))
                        {
                            isValidSpawn = false;
                            break;
                        }
                    }*/
                }

                if (isValidSpawn)
                    return spawn;
            }

            SendConsoleMessage($"[Deathmatch] Player {player.PlayerName} was respawned, but no available spawn point was found!", ConsoleColor.DarkYellow);
            return null;
        }

        public void SaveSpawnsFile()
        {
            string FormatValue(float value)
            {
                return value.ToString("N2", CultureInfo.InvariantCulture);
            }

            var spawnpointsWrapper = new
            {
                spawnpoints = new List<object>()
            };

            foreach (var spawnData in spawnPoints)
            {
                var data = new
                {
                    team = spawnData.Team == CsTeam.Terrorist ? "t" : "ct",
                    pos = $"{FormatValue(spawnData.Position.X)} {FormatValue(spawnData.Position.Y)} {FormatValue(spawnData.Position.Z)}",
                    angle = $"{FormatValue(spawnData.Angle.X)} {FormatValue(spawnData.Angle.Y)} {FormatValue(spawnData.Angle.Z)}"
                };
                spawnpointsWrapper.spawnpoints.Add(data);
            }

            var filePath = ModuleDirectory + $"/spawns/{Server.MapName}.json";
            var jsonContent = JsonConvert.SerializeObject(spawnpointsWrapper, Formatting.Indented);
            File.WriteAllText(filePath, jsonContent);
        }

        public void AddNewSpawnPoint(Vector posValue, QAngle angleValue, CsTeam team)
        {
            if (!DefaultMapSpawnDisabled)
            {
                DefaultMapSpawnDisabled = true;
                spawnPoints.Clear();
            }

            var newPosition = new Vector(posValue.X, posValue.Y, posValue.Z);
            var newAngle = new QAngle(angleValue.X, angleValue.Y, angleValue.Z);

            var data = new SpawnData()
            {
                Team = team,
                Position = newPosition,
                Angle = newAngle
            };

            spawnPoints.Add(data);
        }

        public void RemoveNearestSpawnPoint(Vector? playerPos)
        {
            if (playerPos == null)
                return;

            double lowestDistance = float.MaxValue;
            SpawnData? nearestSpawn = null;
            foreach (var spawn in spawnPoints)
            {
                double distance = GetDistance(playerPos, spawn.Position);
                if (distance < lowestDistance)
                {
                    lowestDistance = distance;
                    nearestSpawn = spawn;
                }
            }

            if (nearestSpawn == null)
                return;

            spawnPoints.Remove(nearestSpawn);
        }

        public void ShowAllSpawnPoints()
        {
            savedSpawnsModel.Clear();
            foreach (var spawn in spawnPoints)
            {
                var textVector = Utilities.CreateEntityByName<CPointWorldText>("point_worldtext");
                var model = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
                if (model == null || textVector == null)
                    continue;

                //model.SetModel("characters/models/shared/animsets/animset_uiplayer.vmdl");
                //model.SetModel("characters/models/ctm_fbi/ctm_fbi_variantf.vmdl");
                model.SetModel(spawn.Team == CsTeam.CounterTerrorist ? "characters/models/ctm_sas/ctm_sas.vmdl" : "characters/models/tm_leet/tm_leet_variantb.vmdl");
                //
                model.UseAnimGraph = false;
                model.AcceptInput("SetAnimation", value: "tools_preview");
                //
                model.DispatchSpawn();
                model.Render = Color.FromArgb(255, 0, 102, 255);
                model.Glow.GlowColorOverride = Color.Blue;
                model.Glow.GlowRange = 2000;
                model.Glow.GlowTeam = -1;
                model.Glow.GlowType = 3;
                model.Glow.GlowRangeMin = 25;
                model.Teleport(spawn.Position, spawn.Angle, new Vector(0, 0, 0));
                savedSpawnsModel.Add(model);

                textVector.MessageText = $"{spawn.Position.X}  {spawn.Position.Y}  {spawn.Position.Z}";
                textVector.Enabled = true;
                textVector.FontSize = 40f;
                textVector.Color = Color.Black;
                textVector.Fullbright = true;
                textVector.WorldUnitsPerPx = 0.1f;
                textVector.DepthOffset = 0.0f;
                textVector.JustifyHorizontal = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_CENTER;
                textVector.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_CENTER;
                textVector.ReorientMode = PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_NONE;

                var textPos = new Vector(spawn.Position.X, spawn.Position.Y, spawn.Position.Z);
                var textAngle = new QAngle(spawn.Angle.X, spawn.Angle.Y, spawn.Angle.Z);
                textPos.Z += 80f;
                textAngle.Z += 90f;
                textAngle.Y += 90f;
                textVector.Teleport(textPos, textAngle);
                textVector.DispatchSpawn();
                savedSpawnsModel.Add(textVector);
            }
        }

        public static void RemoveUnusedSpawns(bool defaultSpawns = false)
        {
            var spawns = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_counterterrorist").Concat(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_terrorist")).Concat(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_deathmatch_spawn"));
            int DMSpawns = 0;
            foreach (var entity in spawns)
            {
                if (entity == null || !entity.IsValid)
                    continue;

                if (spawnPoints.Any(x => x.Entity == entity))
                    entity.AcceptInput("SetEnabled");
                else
                {
                    entity.AcceptInput("SetDisabled");
                    DMSpawns++;
                }
            }

            if (defaultSpawns)
            {
                SendConsoleMessage($"[Deathmatch] Total {DMSpawns} Default Spawns disabled!", ConsoleColor.Green);
                DefaultMapSpawnDisabled = true;
            }
        }

        /*public static void CreateCustomMapSpawns()
        {
            string infoPlayerCT = IsCasualGamemode ? "info_player_counterterrorist" : "info_deathmatch_spawn";
            string infoPlayerT = IsCasualGamemode ? "info_player_terrorist" : "info_deathmatch_spawn";

            foreach (var spawn in spawnPositionsCT)
            {
                var entity = Utilities.CreateEntityByName<SpawnPoint>(infoPlayerCT);
                if (entity == null)
                {
                    SendConsoleMessage($"[Deathmatch] Failed to create spawn point for CT", ConsoleColor.DarkYellow);
                    continue;
                }
                entity.Teleport(spawn.Key, spawn.Value, new Vector(0, 0, 0));
                entity.DispatchSpawn();
                spawnPointsEntities.Add(entity);
            }

            foreach (var spawn in spawnPositionsT)
            {
                var entity = Utilities.CreateEntityByName<SpawnPoint>(infoPlayerT);
                if (entity == null)
                {
                    SendConsoleMessage($"[Deathmatch] Failed to create spawn point for T", ConsoleColor.DarkYellow);
                    continue;
                }
                entity.Teleport(spawn.Key, spawn.Value, new Vector(0, 0, 0));
                entity.DispatchSpawn();
                spawnPointsEntities.Add(entity);
            }
        }*/

        public SpawnPoint? CreateSpawnEntity(Vector Postion, QAngle Angle, CsTeam team)
        {
            var entityName = "info_deathmatch_spawn";
            if (IsCasualGamemode)
                entityName = team == CsTeam.CounterTerrorist ? "info_player_counterterrorist" : "info_player_terrorist";

            var entity = Utilities.CreateEntityByName<SpawnPoint>(entityName);
            if (entity == null)
            {
                SendConsoleMessage($"[Deathmatch] Failed to create spawn point!", ConsoleColor.DarkYellow);
                return null;
            }
            entity.Teleport(Postion, Angle, new Vector(0, 0, 0));
            entity.DispatchSpawn();
            return entity;
        }

        public void LoadMapSpawns(string filePath)
        {
            spawnPoints.Clear();
            if (Config.SpawnSystem.SpawnsMethod >= 1)
            {
                addDefaultSpawnsToList();
            }
            else
            {
                if (!File.Exists(filePath))
                {
                    SendConsoleMessage($"[Deathmatch] No spawn points found for this map! (Deathmatch/spawns/{Server.MapName}.json)", ConsoleColor.Red);
                    addDefaultSpawnsToList();
                }
                else
                {
                    SendConsoleMessage($"[Deathmatch] Loading Custom Map Spawns..", ConsoleColor.DarkYellow);

                    var jsonContent = File.ReadAllText(filePath);
                    JObject jsonData = JsonConvert.DeserializeObject<JObject>(jsonContent)!;

                    foreach (var teamData in jsonData["spawnpoints"]!)
                    {
                        var team = teamData["team"]!.ToString() == "ct" ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
                        var pos = ParseVector(teamData["pos"]!.ToString());
                        var angle = ParseQAngle(teamData["angle"]!.ToString());

                        var spawn = new SpawnData()
                        {
                            Team = team,
                            Position = pos,
                            Angle = angle,
                            Entity = CreateSpawnEntity(pos, angle, team)
                        };
                        spawnPoints.Add(spawn);
                    }

                    SendConsoleMessage($"[Deathmatch] Total Loaded Custom Spawns: CT {spawnPoints.Count(x => x.Team == CsTeam.CounterTerrorist)} | T {spawnPoints.Count(x => x.Team == CsTeam.Terrorist)}", ConsoleColor.Green);
                    RemoveUnusedSpawns(true);
                }
            }

            Server.NextFrame(() =>
            {
                DeathmatchAPI.Get()?.TriggerEvent(new OnSpawnPointsLoaded(spawnPoints));
            });
        }

        private static Vector ParseVector(string pos)
        {
            pos = pos.Replace(",", "");
            var values = pos.Split(' ');
            if (values.Length == 3 &&
                float.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) &&
                float.TryParse(values[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
            {
                return new Vector(x, y, z);
            }
            return new Vector(0, 0, 0);
        }

        private static QAngle ParseQAngle(string angle)
        {
            angle = angle.Replace(",", "");
            var values = angle.Split(' ');
            if (values.Length == 3 &&
                float.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) &&
                float.TryParse(values[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
            {
                return new QAngle(x, y, z);
            }

            return new QAngle(0, 0, 0);
        }

        private static double GetDistance(Vector? v1, Vector? v2)
        {
            if (v1 == null || v2 == null)
                return 100;

            double X = v1.X - v2.X;
            double Y = v1.Y - v2.Y;

            return Math.Sqrt(X * X + Y * Y);
        }

        public void addDefaultSpawnsToList()
        {
            if (IsCasualGamemode)
            {
                SendConsoleMessage($"[Deathmatch] Loading Default Map Spawns..", ConsoleColor.DarkYellow);
                foreach (var spawn in Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_counterterrorist"))
                {
                    if (spawn == null || spawn.AbsOrigin == null || spawn.AbsRotation == null)
                        continue;

                    var data = new SpawnData()
                    {
                        Position = spawn.AbsOrigin,
                        Angle = spawn.AbsRotation,
                        Team = CsTeam.CounterTerrorist,
                        Entity = spawn
                    };
                    spawnPoints.Add(data);
                }
                foreach (var spawn in Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_terrorist"))
                {
                    if (spawn == null || spawn.AbsOrigin == null || spawn.AbsRotation == null)
                        continue;

                    var data = new SpawnData()
                    {
                        Position = spawn.AbsOrigin,
                        Angle = spawn.AbsRotation,
                        Team = CsTeam.Terrorist,
                        Entity = spawn
                    };
                    spawnPoints.Add(data);
                }
            }
            else
            {
                SendConsoleMessage($"[Deathmatch] Loading Default Deathmatch Map Spawns..", ConsoleColor.DarkYellow);
                int randomizer = 0;
                foreach (var spawn in Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_deathmatch_spawn"))
                {
                    randomizer++;
                    if (randomizer % 2 == 0)
                    {
                        if (spawn == null || spawn.AbsOrigin == null || spawn.AbsRotation == null)
                            continue;
                        var data = new SpawnData()
                        {
                            Position = spawn.AbsOrigin,
                            Angle = spawn.AbsRotation,
                            Team = CsTeam.Terrorist,
                            Entity = spawn
                        };
                        spawnPoints.Add(data);
                    }
                    else
                    {
                        if (spawn == null || spawn.AbsOrigin == null || spawn.AbsRotation == null)
                            continue;
                        var data = new SpawnData()
                        {
                            Position = spawn.AbsOrigin,
                            Angle = spawn.AbsRotation,
                            Team = CsTeam.CounterTerrorist,
                            Entity = spawn
                        };
                        spawnPoints.Add(data);
                    }
                }
            }
            SendConsoleMessage($"[Deathmatch] Total Loaded Spawns: CT {spawnPoints.Count(x => x.Team == CsTeam.CounterTerrorist)} | T {spawnPoints.Count(x => x.Team == CsTeam.Terrorist)}", ConsoleColor.Green);
        }
    }
}