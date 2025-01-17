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

            var spawnsDictionary = team == CsTeam.Terrorist ? new Dictionary<Vector, QAngle>(spawnPositionsT) : new Dictionary<Vector, QAngle>(spawnPositionsCT);
            if (blockedSpawns.TryGetValue(player.Slot, out var lastSpawn))
                spawnsDictionary.Remove(lastSpawn);

            if (!spawnsDictionary.Any())
            {
                //player.Respawn();
                SendConsoleMessage("[Deathmatch] Spawns list is empty, you got something wrong!", ConsoleColor.Red);
                return;
            }

            (Vector position, QAngle angle) selectedSpawn;
            if (GameRules().WarmupPeriod || !Config.Gameplay.CheckDistance)
            {
                var randomSpawn = spawnsDictionary.ElementAt(Random.Next(spawnsDictionary.Count));
                selectedSpawn = (randomSpawn.Key, randomSpawn.Value);
            }
            else
            {
                selectedSpawn = GetAvailableSpawn(player, new List<KeyValuePair<Vector, QAngle>>(spawnsDictionary));
            }

            blockedSpawns[player.Slot] = selectedSpawn.position;
            //player.Respawn();
            player.Pawn.Value?.Teleport(selectedSpawn.position, selectedSpawn.angle, new Vector());
        }

        public (Vector, QAngle) GetAvailableSpawn(CCSPlayerController player, List<KeyValuePair<Vector, QAngle>> spawnsList)
        {
            var playerPositions = Utilities.GetPlayers()
                .Where(p => !p.IsHLTV && p.Connected == PlayerConnectedState.PlayerConnected && p.PlayerPawn.IsValid && p.PawnIsAlive && p != player)
                .Select(p => p.PlayerPawn.Value!.AbsOrigin)
                .ToList();

            var availableSpawns = new Dictionary<Vector, QAngle>();
            foreach (var spawn in spawnsList)
            {
                double minDistance = 10000;
                foreach (var playerPos in playerPositions)
                {
                    if (playerPos == null)
                        continue;

                    double distance = GetDistance(playerPos, spawn.Key);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                    }
                }
                if (minDistance > CheckedEnemiesDistance)
                    availableSpawns[spawn.Key] = spawn.Value;
            }

            if (availableSpawns.Any())
            {
                var spawn = availableSpawns.ElementAt(Random.Next(availableSpawns.Count));
                return (spawn.Key, spawn.Value);
            }
            else
            {
                SendConsoleMessage($"[Deathmatch] Player {player.PlayerName} was respawned, but no available spawn point was found!", ConsoleColor.DarkYellow);
                var spawn = spawnsList.ElementAt(Random.Next(spawnsList.Count));
                return (spawn.Key, spawn.Value);
            }
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

            foreach (var spawn in spawnPositionsCT)
            {
                var data = new
                {
                    team = "ct",
                    pos = $"{FormatValue(spawn.Key.X)} {FormatValue(spawn.Key.Y)} {FormatValue(spawn.Key.Z)}",
                    angle = $"{FormatValue(spawn.Value.X)} {FormatValue(spawn.Value.Y)} {FormatValue(spawn.Value.Z)}"
                };
                spawnpointsWrapper.spawnpoints.Add(data);
            }

            foreach (var spawn in spawnPositionsT)
            {
                var data = new
                {
                    team = "t",
                    pos = $"{FormatValue(spawn.Key.X)} {FormatValue(spawn.Key.Y)} {FormatValue(spawn.Key.Z)}",
                    angle = $"{FormatValue(spawn.Value.X)} {FormatValue(spawn.Value.Y)} {FormatValue(spawn.Value.Z)}"
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
                spawnPositionsT.Clear();
                spawnPositionsCT.Clear();
            }

            var newPosition = new Vector(posValue.X, posValue.Y, posValue.Z);
            var newAngle = new QAngle(angleValue.X, angleValue.Y, angleValue.Z);

            switch (team)
            {
                case CsTeam.Terrorist:
                    spawnPositionsT[newPosition] = newAngle;
                    break;
                case CsTeam.CounterTerrorist:
                    spawnPositionsCT[newPosition] = newAngle;
                    break;
            }

            RemoveSpawnModels();
            ShowAllSpawnPoints();
        }

        public void RemoveNearestSpawnPoint(Vector? playerPos)
        {
            if (playerPos == null)
                return;

            double lowestDistance = float.MaxValue;
            Vector? nearestSpawn = null;
            foreach (var ctSpawn in spawnPositionsCT.Keys)
            {
                double distance = GetDistance(playerPos, ctSpawn);
                if (distance < lowestDistance)
                {
                    lowestDistance = distance;
                    nearestSpawn = ctSpawn;
                }
            }
            foreach (var tSpawn in spawnPositionsT.Keys)
            {
                double distance = GetDistance(playerPos, tSpawn);
                if (distance < lowestDistance)
                {
                    lowestDistance = distance;
                    nearestSpawn = tSpawn;
                }
            }

            if (nearestSpawn == null)
                return;

            spawnPositionsCT.Remove(nearestSpawn);
            spawnPositionsT.Remove(nearestSpawn);

            RemoveSpawnModels();
            ShowAllSpawnPoints();
        }

        public void ShowAllSpawnPoints()
        {
            savedSpawnsModel.Clear();
            foreach (var spawn in spawnPositionsCT)
            {
                var textVector = Utilities.CreateEntityByName<CPointWorldText>("point_worldtext");
                var model = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
                if (model == null || textVector == null)
                    continue;

                //model.SetModel("characters/models/shared/animsets/animset_uiplayer.vmdl");
                //model.SetModel("characters/models/ctm_fbi/ctm_fbi_variantf.vmdl");
                model.SetModel("characters/models/ctm_sas/ctm_sas.vmdl");
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
                model.Teleport(spawn.Key, spawn.Value, new Vector(0, 0, 0));
                savedSpawnsModel.Add(model);

                textVector.MessageText = $"{spawn.Key.X}  {spawn.Key.Y}  {spawn.Key.Z}";
                textVector.Enabled = true;
                textVector.FontSize = 40f;
                textVector.Color = Color.Black;
                textVector.Fullbright = true;
                textVector.WorldUnitsPerPx = 0.1f;
                textVector.DepthOffset = 0.0f;
                textVector.JustifyHorizontal = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_CENTER;
                textVector.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_CENTER;
                textVector.ReorientMode = PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_NONE;

                var textPos = new Vector(spawn.Key.X, spawn.Key.Y, spawn.Key.Z);
                var textAngle = new QAngle(spawn.Value.X, spawn.Value.Y, spawn.Value.Z);
                textPos.Z += 80f;
                textAngle.Z += 90f;
                textAngle.Y += 90f;
                textVector.Teleport(textPos, textAngle);
                textVector.DispatchSpawn();
                savedSpawnsVectorText.Add(textVector);
            }

            foreach (var spawn in spawnPositionsT)
            {
                var textVector = Utilities.CreateEntityByName<CPointWorldText>("point_worldtext");
                var model = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
                if (model == null || textVector == null)
                    continue;

                //model.SetModel("characters/models/shared/animsets/animset_uiplayer.vmdl");
                model.SetModel("characters/models/tm_leet/tm_leet_variantb.vmdl");
                //
                model.UseAnimGraph = false;
                model.AcceptInput("SetAnimation", value: "tools_preview");
                //
                model.DispatchSpawn();
                model.Render = Color.FromArgb(255, 255, 0, 0);
                model.Glow.GlowColorOverride = Color.Red;
                model.Glow.GlowRange = 2000;
                model.Glow.GlowTeam = -1;
                model.Glow.GlowType = 3;
                model.Glow.GlowRangeMin = 25;
                model.Teleport(spawn.Key, spawn.Value, new Vector(0, 0, 0));
                savedSpawnsModel.Add(model);

                textVector.MessageText = $"{spawn.Key.X}  {spawn.Key.Y}  {spawn.Key.Z}";
                textVector.Enabled = true;
                textVector.FontSize = 40f;
                textVector.Color = Color.Black;
                textVector.Fullbright = true;
                textVector.WorldUnitsPerPx = 0.1f;
                textVector.DepthOffset = 0.0f;
                textVector.JustifyHorizontal = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_CENTER;
                textVector.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_CENTER;
                textVector.ReorientMode = PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_NONE;

                var textPos = new Vector(spawn.Key.X, spawn.Key.Y, spawn.Key.Z);
                var textAngle = new QAngle(spawn.Value.X, spawn.Value.Y, spawn.Value.Z);
                textPos.Z += 80f;
                textAngle.Z += 90f;
                textAngle.Y += 90f;
                textVector.Teleport(textPos, textAngle);
                textVector.DispatchSpawn();
                savedSpawnsVectorText.Add(textVector);
            }
        }
        public static void RemoveMapDefaulSpawns()
        {
            if (!DefaultMapSpawnDisabled)
            {
                if (IsCasualGamemode)
                {
                    int iDefaultCTSpawns = 0;
                    int iDefaultTSpawns = 0;
                    var ctSpawns = Utilities.FindAllEntitiesByDesignerName<CInfoPlayerTerrorist>("info_player_counterterrorist");
                    foreach (var entity in ctSpawns)
                    {
                        if (entity.IsValid)
                        {
                            entity.AcceptInput("SetDisabled");
                            iDefaultCTSpawns++;
                        }
                    }
                    var tSpawns = Utilities.FindAllEntitiesByDesignerName<CInfoPlayerTerrorist>("info_player_terrorist");
                    foreach (var entity in tSpawns)
                    {
                        if (entity.IsValid)
                        {
                            entity.AcceptInput("SetDisabled");
                            iDefaultTSpawns++;
                        }
                    }
                    SendConsoleMessage($"[Deathmatch] Total {iDefaultTSpawns} T and {iDefaultCTSpawns} CT default Spawns disabled!", ConsoleColor.Green);
                }
                else
                {
                    int DMSpawns = 0;
                    var dmSpawns = Utilities.FindAllEntitiesByDesignerName<CInfoDeathmatchSpawn>("info_deathmatch_spawn");
                    foreach (var entity in dmSpawns)
                    {
                        if (entity.IsValid)
                        {
                            entity.AcceptInput("SetDisabled");
                            DMSpawns++;
                        }
                    }
                    SendConsoleMessage($"[Deathmatch] Total {DMSpawns} default Spawns disabled!", ConsoleColor.Green);

                }
                DefaultMapSpawnDisabled = true;
                CreateCustomMapSpawns();
            }
        }

        public static void CreateCustomMapSpawns()
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
            }
        }

        public void LoadMapSpawns(string filePath, bool mapstart)
        {
            spawnPositionsCT.Clear();
            spawnPositionsT.Clear();
            if (Config.Gameplay.DefaultSpawns)
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
                        string teamType = teamData["team"]!.ToString();
                        string pos = teamData["pos"]!.ToString();
                        string angle = teamData["angle"]!.ToString();

                        if (teamType == "ct")
                        {
                            spawnPositionsCT.Add(ParseVector(pos), ParseQAngle(angle));
                        }
                        else if (teamType == "t")
                        {
                            spawnPositionsT.Add(ParseVector(pos), ParseQAngle(angle));
                        }
                    }

                    SendConsoleMessage($"[Deathmatch] Total Loaded Custom Spawns: CT {spawnPositionsCT.Count} | T {spawnPositionsT.Count}", ConsoleColor.Green);
                    if (mapstart)
                        RemoveMapDefaulSpawns();
                }
            }

            Server.NextFrame(() =>
            {
                var spawns = new List<SpawnData>();
                foreach (var spawn in spawnPositionsCT)
                {
                    spawns.Add(new()
                    {
                        Team = CsTeam.CounterTerrorist,
                        Position = spawn.Key,
                        Angle = spawn.Value
                    });
                }
                foreach (var spawn in spawnPositionsT)
                {
                    spawns.Add(new()
                    {
                        Team = CsTeam.Terrorist,
                        Position = spawn.Key,
                        Angle = spawn.Value
                    });
                }
                DeathmatchAPI.Get()?.TriggerEvent(new OnSpawnPointsLoaded(spawns));
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

        private static double GetDistance(Vector v1, Vector v2)
        {
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

                    spawnPositionsCT.Add(spawn.AbsOrigin, spawn.AbsRotation);
                }
                foreach (var spawn in Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_terrorist"))
                {
                    if (spawn == null || spawn.AbsOrigin == null || spawn.AbsRotation == null)
                        continue;
                    spawnPositionsT.Add(spawn.AbsOrigin, spawn.AbsRotation);
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
                        spawnPositionsT.Add(spawn.AbsOrigin, spawn.AbsRotation);
                    }
                    else
                    {
                        if (spawn == null || spawn.AbsOrigin == null || spawn.AbsRotation == null)
                            continue;
                        spawnPositionsCT.Add(spawn.AbsOrigin, spawn.AbsRotation);
                    }
                }
            }
            SendConsoleMessage($"[Deathmatch] Total Loaded Spawns: CT {spawnPositionsCT.Count} | T {spawnPositionsT.Count}", ConsoleColor.Green);
        }
    }
}