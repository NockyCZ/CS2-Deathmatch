using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;
using System.Drawing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using System.Globalization;

namespace Deathmatch
{
    public partial class Deathmatch
    {
        public void PerformRespawn(CCSPlayerController player, CsTeam team, bool IsBot)
        {
            if (player == null || !player.IsValid || player.PlayerPawn.Value == null || player.PawnIsAlive || team == CsTeam.None || team == CsTeam.Spectator)
                return;

            var spawnsDictionary = team == CsTeam.Terrorist ? spawnPositionsT : spawnPositionsCT;
            var spawnsList = spawnsDictionary.ToList();
            if (!IsBot)
                spawnsList.RemoveAll(x => x.Key == playerData[player].LastSpawn);

            if (spawnsList.Count == 0)
            {
                player.Respawn();
                SendConsoleMessage("[Deathmatch] Spawns list is empty, you got something wrong!", ConsoleColor.Red);
                return;
            }

            if (GameRules().WarmupPeriod || !Config.Gameplay.CheckDistance)
            {
                Random random = new Random();
                var randomSpawn = spawnsDictionary.ElementAt(random.Next(spawnsDictionary.Count));
                if (!IsBot)
                    playerData[player].LastSpawn = randomSpawn.Key;

                player.Respawn();
                player.PlayerPawn.Value.Teleport(randomSpawn.Key, randomSpawn.Value, new Vector(0, 0, 0));
                spawnsDictionary.Remove(randomSpawn.Key);

                AddTimer(5.0f, () =>
                {
                    if (!spawnsDictionary.ContainsKey(randomSpawn.Key))
                        spawnsDictionary.Add(randomSpawn.Key, randomSpawn.Value);
                }, TimerFlags.STOP_ON_MAPCHANGE);
                return;
            }

            var Spawn = GetAvailableSpawn(player, spawnsList);
            if (!IsBot)
                playerData[player].LastSpawn = Spawn.Key;

            player.Respawn();
            player.PlayerPawn.Value.Teleport(Spawn.Key, Spawn.Value, new Vector(0, 0, 0));
            AddTimer(5.0f, () =>
            {
                if (!spawnsDictionary.ContainsKey(Spawn.Key))
                    spawnsDictionary.Add(Spawn.Key, Spawn.Value);
            }, TimerFlags.STOP_ON_MAPCHANGE);
        }

        private KeyValuePair<Vector, QAngle> GetAvailableSpawn(CCSPlayerController player, List<KeyValuePair<Vector, QAngle>> spawnsList)
        {
            var allPlayers = Utilities.GetPlayers();
            var playerPositions = allPlayers
                .Where(p => !p.IsHLTV && p.Connected == PlayerConnectedState.PlayerConnected && p.PlayerPawn.IsValid && p.PawnIsAlive && p != player)
                .Select(p => p.PlayerPawn.Value!.AbsOrigin)
                .ToList();

            var availableSpawns = new Dictionary<Vector, QAngle>();
            int spawnCount = 0;
            foreach (KeyValuePair<Vector, QAngle> spawn in spawnsList)
            {

                spawnCount++;
                double closestDistance = 4000;
                foreach (var playerPos in playerPositions)
                {
                    if (playerPos != null)
                    {
                        double distance = GetDistance(playerPos, spawn.Key);
                        //Console.WriteLine($"Distance {distance} | {closestDistance}");
                        if (distance < closestDistance)
                        {
                            //Console.WriteLine($"ClosestDistance Distance {distance}");
                            closestDistance = distance;
                        }
                    }
                }
                if (closestDistance > Config.Gameplay.DistanceRespawn)
                {
                    //Console.WriteLine($"closestDistance {closestDistance} > DistanceRespawn {Config.Gameplay.DistanceRespawn}");
                    availableSpawns.Add(spawn.Key, spawn.Value);
                }
            }

            Random random = new Random();
            if (availableSpawns.Count > 0)
            {
                //SendConsoleMessage($"[Deathmatch] Player {player.PlayerName} was respawned, available spawns found: {availableSpawns.Count})", ConsoleColor.DarkYellow);
                var randomAvailableSpawn = availableSpawns.ElementAt(random.Next(availableSpawns.Count));
                return randomAvailableSpawn;
            }
            SendConsoleMessage($"[Deathmatch] Player {player.PlayerName} was respawned, but no available spawn point was found! Therefore, a random spawn was selected. (T {spawnPositionsT.Count()} : CT {spawnPositionsCT.Count()})", ConsoleColor.DarkYellow);
            var randomSpawn = spawnsList.ElementAt(random.Next(spawnsList.Count));
            return randomSpawn;
        }

        public void AddNewSpawnPoint(string filepath, string posValue, string angleValue, string team)
        {
            if (!File.Exists(filepath))
            {
                JObject newRow = new JObject
                {
                    { "team", team },
                    { "pos", posValue },
                    { "angle", angleValue }
                };

                JObject jsonData = new JObject
                {
                    { "spawnpoints", new JArray(newRow) }
                };
                File.WriteAllText(filepath, jsonData.ToString());
            }
            else
            {
                string jsonContent = File.ReadAllText(filepath);
                JObject jsonData = JsonConvert.DeserializeObject<JObject>(jsonContent)!;

                JObject newRow = new JObject
                {
                    { "team", team },
                    { "pos", posValue },
                    { "angle", angleValue }
                };

                JArray spawnpointsArray = (JArray)jsonData["spawnpoints"]!;
                spawnpointsArray.Add(newRow);

                string updatedJsonContent = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
                File.WriteAllText(filepath, updatedJsonContent);
            }
            LoadMapSpawns(ModuleDirectory + $"/spawns/{Server.MapName}.json", false);
            RemoveBeams();
            ShowAllSpawnPoints();
        }

        public bool RemoveSpawnPoint(string filepath, string posValue)
        {
            if (!File.Exists(filepath))
            {
                return false;
            }
            string jsonContent = File.ReadAllText(filepath);
            JObject jsonData = JObject.Parse(jsonContent);

            JArray spawnpointsArray = (JArray)jsonData["spawnpoints"]!;
            RemoveSpawnpointByPos(spawnpointsArray, posValue);
            File.WriteAllText(filepath, jsonData.ToString());
            return true;
        }

        static void RemoveSpawnpointByPos(JArray spawnpointsArray, string posToRemove)
        {
            for (int i = spawnpointsArray.Count - 1; i >= 0; i--)
            {
                JObject spawnpoint = (JObject)spawnpointsArray[i];
                if (spawnpoint["pos"] != null && spawnpoint["pos"]!.ToString() == posToRemove)
                {
                    spawnpointsArray.RemoveAt(i);
                }
            }
        }
        public string GetNearestSpawnPoint(Vector? playerPos)
        {
            if (playerPos == null)
                return "Spawn point cannot be deleted!";

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

            bool isDeleted = false;
            if (nearestSpawn != null)
                isDeleted = RemoveSpawnPoint(ModuleDirectory + $"/spawns/{Server.MapName}.json", $"{nearestSpawn}");

            if (isDeleted)
            {
                LoadMapSpawns(ModuleDirectory + $"/spawns/{Server.MapName}.json", false);
                RemoveBeams();
                ShowAllSpawnPoints();
                return $"The nearest Spawn point has been successfully deleted! {nearestSpawn}";
            }
            else
            {
                return "Spawn point cannot be deleted!";
            }
        }
        public void ShowAllSpawnPoints()
        {
            foreach (var ctTeam in spawnPositionsCT.Keys)
            {
                CBeam beam = Utilities.CreateEntityByName<CBeam>("beam")!;
                if (beam == null)
                {
                    SendConsoleMessage($"[Deathmatch] Failed to create beam for CT", ConsoleColor.DarkYellow);
                    return;
                }

                var position = ctTeam;
                beam.Render = Color.Blue;
                beam.Width = 5.5f;
                position[2] += 50.00f;
                beam.Teleport(position, new QAngle(0, 0, 0), new Vector(0, 0, 0));
                position[2] -= 50.00f;
                beam.EndPos.X = position[0];
                beam.EndPos.Y = position[1];
                beam.EndPos.Z = position[2];

                beam.DispatchSpawn();
            }
            foreach (var tTeam in spawnPositionsT.Keys)
            {
                CBeam beam = Utilities.CreateEntityByName<CBeam>("beam")!;
                if (beam == null)
                {
                    SendConsoleMessage($"[Deathmatch] Failed to create beam for T", ConsoleColor.DarkYellow);
                    return;
                }
                var position = tTeam;
                beam.Render = Color.Orange;
                beam.Width = 5.5f;
                position[2] += 50.00f;
                beam.Teleport(position, new QAngle(0, 0, 0), new Vector(0, 0, 0));
                position[2] -= 50.00f;
                beam.EndPos.X = position[0];
                beam.EndPos.Y = position[1];
                beam.EndPos.Z = position[2];

                beam.DispatchSpawn();
            }
        }
        public static void RemoveMapDefaulSpawns()
        {
            if (!g_bDefaultMapSpawnDisabled)
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
                g_bDefaultMapSpawnDisabled = true;
                CreateCustomMapSpawns();
            }
        }
        public static void CreateCustomMapSpawns()
        {
            string infoPlayerCT = IsCasualGamemode ? "info_player_counterterrorist" : "info_deathmatch_spawn";
            string infoPlayerT = IsCasualGamemode ? "info_player_terrorist" : "info_deathmatch_spawn";

            foreach (var spawn in spawnPositionsCT)
            {
                CBaseEntity entity;
                if (IsCasualGamemode)
                    entity = Utilities.CreateEntityByName<CInfoPlayerTerrorist>(infoPlayerCT)!;
                else
                    entity = Utilities.CreateEntityByName<CInfoDeathmatchSpawn>(infoPlayerCT)!;

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
                CBaseEntity entity;
                if (IsCasualGamemode)
                    entity = Utilities.CreateEntityByName<CInfoPlayerTerrorist>(infoPlayerT)!;
                else
                    entity = Utilities.CreateEntityByName<CInfoDeathmatchSpawn>(infoPlayerT)!;
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

                    SendConsoleMessage($"[Deathmatch] Total Loaded Spawns: CT {spawnPositionsCT.Count} | T {spawnPositionsT.Count}", ConsoleColor.Green);
                    if (mapstart)
                        RemoveMapDefaulSpawns();
                }
            }
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
                foreach (var spawn in Utilities.FindAllEntitiesByDesignerName<CInfoPlayerTerrorist>("info_player_counterterrorist"))
                {
                    if (spawn == null || spawn.AbsOrigin == null || spawn.AbsRotation == null)
                        return;
                    spawnPositionsCT.Add(spawn.AbsOrigin, spawn.AbsRotation);
                }
                foreach (var spawn in Utilities.FindAllEntitiesByDesignerName<CInfoPlayerTerrorist>("info_player_terrorist"))
                {
                    if (spawn == null || spawn.AbsOrigin == null || spawn.AbsRotation == null)
                        return;
                    spawnPositionsT.Add(spawn.AbsOrigin, spawn.AbsRotation);
                }
            }
            else
            {
                int randomizer = 0;
                foreach (var spawn in Utilities.FindAllEntitiesByDesignerName<CInfoDeathmatchSpawn>("info_deathmatch_spawn"))
                {
                    randomizer++;
                    if (randomizer % 2 == 0)
                    {
                        if (spawn == null || spawn.AbsOrigin == null || spawn.AbsRotation == null)
                            return;
                        spawnPositionsT.Add(spawn.AbsOrigin, spawn.AbsRotation);
                    }
                    else
                    {
                        if (spawn == null || spawn.AbsOrigin == null || spawn.AbsRotation == null)
                            return;
                        spawnPositionsCT.Add(spawn.AbsOrigin, spawn.AbsRotation);
                    }
                }
            }
        }
    }
}