using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;
using QAngle = CounterStrikeSharp.API.Modules.Utils.QAngle;
using System.Drawing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Numerics;
using CounterStrikeSharp.API.Modules.Utils;

namespace Deathmatch
{
    public partial class DeathmatchCore
    {
        public static Dictionary<string, string> spawnPositionsCT = new Dictionary<string, string>();
        public static Dictionary<string, string> spawnPositionsT = new Dictionary<string, string>();

        public void PerformRespawn(CCSPlayerController player, int team, bool IsBot)
        {
            if (player == null || !player.IsValid || !player.PlayerPawn.IsValid || player.PawnIsAlive || team == 1 || team == 0)
                return;

            var spawnsDictionary = team == (byte)CsTeam.Terrorist ? spawnPositionsT : spawnPositionsCT;

            var spawnsList = spawnsDictionary.ToList();
            if (!IsBot)
                spawnsList.RemoveAll(x => x.Key == playerData[player].LastSpawn);

            if (spawnsList.Count == 0)
            {
                player.Respawn();
                SendConsoleMessage("[Deathmatch] Spawns list is empty, you got something wrong!", ConsoleColor.Red);
                return;
            }

            Random random = new Random();
            int selectRadnomSpawn = random.Next(0, spawnsList.Count);
            var randomSpawn = spawnsList.ElementAt(selectRadnomSpawn);
            Vector position;
            QAngle angle;

            if (GameRules().WarmupPeriod || !Config.Gameplay.CheckDistance || !g_bDefaultMapSpawnDisabled || Config.Gameplay.DefaultSpawns)
            {
                if (!IsBot)
                    playerData[player].LastSpawn = randomSpawn.Key;

                player.Respawn();
                position = ParseVector(randomSpawn.Key);
                angle = ParseQAngle(randomSpawn.Value);
                player.PlayerPawn.Value!.Teleport(position, angle, new Vector(0, 0, 0));
                return;
            }

            spawnsList = spawnsList.OrderBy(x => random.Next()).ToList();
            var closestDistances = CalculateClosestDistances(player, spawnsList);

            var Spawn = spawnsList.FirstOrDefault(spawn =>
            {
                var spawnKey = spawn.Key;
                var closestDistance = closestDistances[spawnKey];
                return closestDistance > Config.Gameplay.DistanceRespawn;
            });

            if (Spawn.Key != null)
            {
                if (!IsBot)
                    playerData[player].LastSpawn = Spawn.Key;

                player.Respawn();
                position = ParseVector(Spawn.Key);
                angle = ParseQAngle(Spawn.Value);
                player.PlayerPawn.Value!.Teleport(position, angle, new Vector(0, 0, 0));
                return;
            }

            if (!IsBot)
                playerData[player].LastSpawn = randomSpawn.Key;

            player.Respawn();
            position = ParseVector(randomSpawn.Key);
            angle = ParseQAngle(randomSpawn.Value);
            player.PlayerPawn.Value!.Teleport(position, angle, new Vector(0, 0, 0));
            SendConsoleMessage($"[Deathmatch] Player {player.PlayerName} was respawned, but no available spawn point was found! Therefore, a random spawn was selected.", ConsoleColor.DarkYellow);
        }

        private Dictionary<string, double> CalculateClosestDistances(CCSPlayerController player, List<KeyValuePair<string, string>> spawnsList)
        {
            var allPlayers = Utilities.GetPlayers();
            var playerPositions = allPlayers
                .Where(p => p != null && p.IsValid && !p.IsHLTV && p.Connected == PlayerConnectedState.PlayerConnected && p.PlayerPawn.IsValid && p.PawnIsAlive && p != player)
                .Select(p => p.PlayerPawn.Value!.AbsOrigin)
                .ToList();

            var closestDistances = new Dictionary<string, double>();
            foreach (var spawn in spawnsList)
            {
                var spawnKey = spawn.Key;
                var spawnAbsOrigin = ParseVector(spawnKey);
                double closestDistance = double.MaxValue;

                foreach (var playerPos in playerPositions)
                {
                    double distance = GetDistance(playerPos!, spawnAbsOrigin);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                    }
                }
                closestDistances.Add(spawnKey, closestDistance);
            }

            return closestDistances;
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
        public string GetNearestSpawnPoint(float X, float Y, float Z)
        {
            float lowestDistance = float.MaxValue;
            string nearestSpawn = "";
            foreach (var ctSpawn in spawnPositionsCT.Keys)
            {
                Vector3 playerPos = new Vector3(X, Y, Z);
                Vector3 ctSpawnPosition = ParseVector3(ctSpawn);
                float distance = Vector3.Distance(playerPos, ctSpawnPosition);

                if (distance < lowestDistance)
                {
                    lowestDistance = distance;
                    nearestSpawn = ctSpawn;
                }
            }
            foreach (var tSpawn in spawnPositionsT.Keys)
            {
                Vector3 playerPos = new Vector3(X, Y, Z);
                Vector3 tSpawnPosition = ParseVector3(tSpawn);
                float distance = Vector3.Distance(playerPos, tSpawnPosition);

                if (distance < lowestDistance)
                {
                    lowestDistance = distance;
                    nearestSpawn = tSpawn;
                }
            }
            bool isDeleted = RemoveSpawnPoint(ModuleDirectory + $"/spawns/{Server.MapName}.json", nearestSpawn);
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

                Vector position = ParseVector(ctTeam);
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
                Vector position = ParseVector(tTeam);
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
                var position = ParseVector(spawn.Key);
                var angle = ParseQAngle(spawn.Value);

                CBaseEntity entity;
                if (IsCasualGamemode)
                    entity = Utilities.CreateEntityByName<CInfoPlayerTerrorist>(infoPlayerCT)!;
                else
                    entity = Utilities.CreateEntityByName<CInfoDeathmatchSpawn>(infoPlayerCT)!;

                if (entity == null)
                {
                    SendConsoleMessage($"[Deathmatch] Failed to create spawn point for CT", ConsoleColor.DarkYellow);
                    return;
                }
                entity.Teleport(position, angle, new Vector(0, 0, 0));
                entity.DispatchSpawn();
            }
            foreach (var spawn in spawnPositionsT)
            {
                var position = ParseVector(spawn.Key);
                var angle = ParseQAngle(spawn.Value);
                CBaseEntity entity;
                if (IsCasualGamemode)
                    entity = Utilities.CreateEntityByName<CInfoPlayerTerrorist>(infoPlayerT)!;
                else
                    entity = Utilities.CreateEntityByName<CInfoDeathmatchSpawn>(infoPlayerT)!;
                if (entity == null)
                {
                    SendConsoleMessage($"[Deathmatch] Failed to create spawn point for T", ConsoleColor.DarkYellow);
                    return;
                }
                entity.Teleport(position, angle, new Vector(0, 0, 0));
                entity.DispatchSpawn();
            }
        }
        public void LoadMapSpawns(string filepath, bool mapstart)
        {
            spawnPositionsCT.Clear();
            spawnPositionsT.Clear();
            if (Config.Gameplay.DefaultSpawns)
            {
                if (IsCasualGamemode)
                {
                    foreach (var spawn in Utilities.FindAllEntitiesByDesignerName<CInfoPlayerTerrorist>("info_player_counterterrorist"))
                    {
                        if (spawn == null)
                            return;
                        spawnPositionsCT.Add($"{spawn.AbsOrigin}", $"{spawn.AbsRotation}");
                    }
                    foreach (var spawn in Utilities.FindAllEntitiesByDesignerName<CInfoPlayerTerrorist>("info_player_terrorist"))
                    {
                        if (spawn == null)
                            return;

                        spawnPositionsT.Add($"{spawn.AbsOrigin}", $"{spawn.AbsRotation}");
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
                            if (spawn == null)
                                return;
                            spawnPositionsT.Add($"{spawn.AbsOrigin}", $"{spawn.AbsRotation}");
                        }
                        else
                        {
                            if (spawn == null)
                                return;
                            spawnPositionsCT.Add($"{spawn.AbsOrigin}", $"{spawn.AbsRotation}");
                        }
                    }
                }

                g_iTotalCTSpawns = spawnPositionsCT.Count;
                g_iTotalTSpawns = spawnPositionsT.Count;
            }
            else
            {
                if (!File.Exists(filepath))
                {
                    SendConsoleMessage($"[Deathmatch] No spawn points found for this map! (Deathmatch/spawns/{Server.MapName}.json)", ConsoleColor.Red);

                    foreach (var spawn in Utilities.FindAllEntitiesByDesignerName<CInfoPlayerTerrorist>("info_player_counterterrorist"))
                    {
                        if (spawn == null)
                            return;
                        spawnPositionsCT.Add($"{spawn.AbsOrigin}", $"{spawn.AbsRotation}");
                    }
                    foreach (var spawn in Utilities.FindAllEntitiesByDesignerName<CInfoPlayerTerrorist>("info_player_terrorist"))
                    {
                        if (spawn == null)
                            return;

                        spawnPositionsT.Add($"{spawn.AbsOrigin}", $"{spawn.AbsRotation}");
                    }

                    g_iTotalCTSpawns = spawnPositionsCT.Count;
                    g_iTotalTSpawns = spawnPositionsT.Count;
                }
                else
                {
                    var jsonContent = File.ReadAllText(filepath);
                    JObject jsonData = JsonConvert.DeserializeObject<JObject>(jsonContent)!;

                    foreach (var teamData in jsonData["spawnpoints"]!)
                    {
                        string teamType = teamData["team"]!.ToString();
                        string pos = teamData["pos"]!.ToString();
                        string angle = teamData["angle"]!.ToString();

                        if (teamType == "ct")
                        {
                            spawnPositionsCT.Add(pos, angle);
                        }
                        else if (teamType == "t")
                        {
                            spawnPositionsT.Add(pos, angle);
                        }
                    }

                    g_iTotalCTSpawns = spawnPositionsCT.Count;
                    g_iTotalTSpawns = spawnPositionsT.Count;
                    if (mapstart)
                        RemoveMapDefaulSpawns();
                }
            }
        }
        private static Vector ParseVector(string pos)
        {
            var values = pos.Split(' ');
            if (values.Length == 3 &&
                float.TryParse(values[0], out float x) &&
                float.TryParse(values[1], out float y) &&
                float.TryParse(values[2], out float z))
            {
                return new Vector(x, y, z);
            }

            return new Vector(0, 0, 0);
        }
        private static QAngle ParseQAngle(string angle)
        {
            var values = angle.Split(' ');
            if (values.Length == 3 &&
                float.TryParse(values[0], out float x) &&
                float.TryParse(values[1], out float y) &&
                float.TryParse(values[2], out float z))
            {
                return new QAngle(x, y, z);
            }

            return new QAngle(0, 0, 0);
        }
        private static Vector3 ParseVector3(string pos)
        {
            var values = pos.Split(' ');
            if (values.Length == 3 &&
                float.TryParse(values[0], out float x) &&
                float.TryParse(values[1], out float y) &&
                float.TryParse(values[2], out float z))
            {
                return new Vector3(x, y, z);
            }

            return new Vector3(0, 0, 0);
        }
        static double GetDistance(Vector v1, Vector v2)
        {
            double X = v1.X - v2.X;
            double Y = v1.Y - v2.Y;

            return Math.Sqrt(X * X + Y * Y);
        }
    }
}