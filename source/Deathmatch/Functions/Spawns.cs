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

            IEnumerable<KeyValuePair<Vector, QAngle>> spawnsDictionary = Config.SpawnSystem.TeamSpawnsSeparation
                ? (team == CsTeam.Terrorist ? spawnPositionsT : spawnPositionsCT)
                : spawnPositionsT.Concat(spawnPositionsCT);

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
                savedSpawnsModel.Add(textVector);
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
                savedSpawnsModel.Add(textVector);
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
                //spawnPositionsCTEntity.Add(entity);
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
                //spawnPositionsTEntity.Add(entity);
            }
        }

        public void LoadMapSpawns(string filePath, bool mapstart)
        {
            spawnPositionsCT.Clear();
            spawnPositionsT.Clear();
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

                    spawnPositionsCT.Add(spawn.AbsOrigin, spawn.AbsRotation);
                    //spawnPositionsCTEntity.Add(spawn);
                }
                foreach (var spawn in Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_terrorist"))
                {
                    if (spawn == null || spawn.AbsOrigin == null || spawn.AbsRotation == null)
                        continue;

                    spawnPositionsT.Add(spawn.AbsOrigin, spawn.AbsRotation);
                    //spawnPositionsTEntity.Add(spawn);
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
                        //spawnPositionsTEntity.Add(spawn);
                    }
                    else
                    {
                        if (spawn == null || spawn.AbsOrigin == null || spawn.AbsRotation == null)
                            continue;
                        spawnPositionsCT.Add(spawn.AbsOrigin, spawn.AbsRotation);
                        //spawnPositionsCTEntity.Add(spawn);
                    }
                }
            }
            SendConsoleMessage($"[Deathmatch] Total Loaded Spawns: CT {spawnPositionsCT.Count} | T {spawnPositionsT.Count}", ConsoleColor.Green);
        }
    }
}