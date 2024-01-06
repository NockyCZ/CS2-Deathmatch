using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;
using QAngle = CounterStrikeSharp.API.Modules.Utils.QAngle;
using System.Drawing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Numerics;

namespace Deathmatch
{
    public partial class DeathmatchCore
    {
        public static Dictionary<string, string> spawnPositionsCT = new Dictionary<string, string>();
        public static Dictionary<string, string> spawnPositionsT = new Dictionary<string, string>();

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

            double distance = Math.Sqrt(X * X + Y * Y);
            return distance;
        }
        public string[] CheckAvaibleSpawns(CCSPlayerController player, int team)
        {
            if (GameRules().WarmupPeriod || !Config.CheckDistance || !g_bDefaultMapSpawnDisabled)
            {
                string[] randomSpawn = new string[2];
                randomSpawn[0] = "default";
                return randomSpawn;
            }
            if (team == 1 || team == 0)
            {
                string[] randomSpawn = new string[2];
                randomSpawn[0] = "";
                return randomSpawn;
            }

            Dictionary<string, string> availableSpawns = new Dictionary<string, string>();
            int iAvailableSpawns = 0;
            foreach (var spawn in spawnPositionsT)
            {
                double closestValue = Config.DistanceRespawn + 100;
                Vector? spawnAbsOrigin = ParseVector(spawn.Key);

                foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsHLTV && p.PlayerPawn.IsValid && p.PawnIsAlive))
                {
                    var playerPos = p.PlayerPawn.Value!.AbsOrigin!;
                    var distance = GetDistance(playerPos, spawnAbsOrigin!);
                    if (distance < closestValue)
                    {
                        closestValue = distance;
                    }
                }
                if (closestValue > Config.DistanceRespawn)
                {
                    if (team == 2)
                    {
                        if (playerData.ContainsPlayer(player))
                        {
                            if (playerData[player].lastSpawn != spawn.Key)
                            {
                                availableSpawns.Add(spawn.Key, spawn.Value);
                                iAvailableSpawns++;
                            }
                        }
                        else
                        {
                            availableSpawns.Add(spawn.Key, spawn.Value);
                            iAvailableSpawns++;
                        }
                    }
                }
            }
            foreach (var spawn in spawnPositionsCT)
            {
                double closestValue = Config.DistanceRespawn + 100;
                Vector? spawnAbsOrigin = ParseVector(spawn.Key);

                foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsHLTV && p.PlayerPawn.IsValid && p.PawnIsAlive))
                {
                    var playerPos = p.PlayerPawn.Value!.AbsOrigin!;
                    var distance = GetDistance(playerPos, spawnAbsOrigin!);
                    if (distance < closestValue)
                    {
                        closestValue = distance;
                    }
                }
                if (closestValue > Config.DistanceRespawn)
                {
                    if (team == 3)
                    {
                        if (playerData.ContainsPlayer(player))
                        {
                            if (playerData[player].lastSpawn != spawn.Key)
                            {
                                availableSpawns.Add(spawn.Key, spawn.Value);
                                iAvailableSpawns++;
                            }
                        }
                        else
                        {
                            availableSpawns.Add(spawn.Key, spawn.Value);
                            iAvailableSpawns++;
                        }
                    }
                }
            }
            /*if (team == 3)
                SendConsoleMessage($"CT Available spawns - {iAvailableSpawns}", ConsoleColor.Blue);
            if (team == 2)
                SendConsoleMessage($"T Available spawns - {iAvailableSpawns}", ConsoleColor.DarkYellow);*/

            string[] spawnLocation = new string[2];
            if (iAvailableSpawns == 0)
            {
                if (team == 2)
                {
                    spawnLocation[0] = "not found";
                }
                else if (team == 3)
                {
                    spawnLocation[0] = "not found";
                }
                playerData[player].lastSpawn = "0";
            }
            else
            {
                Random random = new Random();
                int randomIndex = random.Next(0, availableSpawns.Count);
                spawnLocation[0] = availableSpawns.Keys.ElementAt(randomIndex);
                spawnLocation[1] = availableSpawns.Values.ElementAt(randomIndex);
                if (playerData.ContainsPlayer(player))
                    playerData[player].lastSpawn = spawnLocation[0];
            }
            return spawnLocation;
        }
        /*public void ResetDisabledSpawns()
        {
            disabledSpawnPoints.Clear();
            var tSpawns = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_terrorist");
            foreach (var entity in tSpawns)
            {
                if (entity.IsValid)
                {
                    if (!defaultSpawnPoints.Contains(entity.Index))
                        entity.AcceptInput("SetEnabled");
                }
            }
            var ctSpawns = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_counterterrorist");
            foreach (var entity in ctSpawns)
            {
                if (entity.IsValid)
                {
                    if (!defaultSpawnPoints.Contains(entity.Index))
                        entity.AcceptInput("SetEnabled");
                }
            }
        }*/
        /*public int CheckAvaibleSpawns(int team)
        {
            if (GameRules().WarmupPeriod || !Config.CheckDistance || !g_bDefaultMapSpawnDisabled)
                return 1;
            if (team == 1 || team == 0)
                return -1;

            int iAvailableSpawns = 0;
            var tSpawns = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_terrorist");
            foreach (var entity in tSpawns)
            {
                if (entity.IsValid)
                {
                    if (!defaultSpawnPoints.Contains(entity.Index))
                    {
                        double closestValue = Config.DistanceRespawn + 100;
                        Vector? entityPos = entity.AbsOrigin;
                        foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsHLTV && p.PlayerPawn.IsValid && p.PawnIsAlive))
                        {
                            var playerPos = p.PlayerPawn.Value!.AbsOrigin!;
                            var distance = GetDistance(playerPos, entityPos!);
                            if (distance < closestValue)
                            {
                                closestValue = distance;
                            }
                        }
                        if (closestValue < Config.DistanceRespawn)
                        {
                            if (!disabledSpawnPoints.Contains(entity.Index))
                            {
                                entity.AcceptInput("SetDisabled");
                                disabledSpawnPoints.Add(entity.Index);
                            }
                        }
                        else
                        {
                            if (disabledSpawnPoints.Contains(entity.Index))
                            {
                                entity.AcceptInput("SetEnabled");
                                disabledSpawnPoints.Remove(entity.Index);
                            }
                            if (team == 2)
                                iAvailableSpawns++;
                        }
                    }
                }
            }
            var ctSpawns = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_counterterrorist");
            foreach (var entity in ctSpawns)
            {
                if (entity.IsValid)
                {
                    if (!defaultSpawnPoints.Contains(entity.Index))
                    {
                        double closestValue = Config.DistanceRespawn + 100;
                        Vector? entityPos = entity.AbsOrigin;
                        foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsHLTV && p.PlayerPawn.IsValid && p.PawnIsAlive))
                        {
                            var playerPos = p.PlayerPawn.Value!.AbsOrigin!;
                            var distance = GetDistance(playerPos, entityPos!);
                            if (distance < closestValue)
                            {
                                closestValue = distance;
                            }
                        }
                        if (closestValue < Config.DistanceRespawn)
                        {
                            if (!disabledSpawnPoints.Contains(entity.Index))
                            {
                                entity.AcceptInput("SetDisabled");
                                disabledSpawnPoints.Add(entity.Index);
                            }
                        }
                        else
                        {
                            if (disabledSpawnPoints.Contains(entity.Index))
                            {
                                entity.AcceptInput("SetEnabled");
                                disabledSpawnPoints.Remove(entity.Index);
                            }
                            if (team == 3)
                                iAvailableSpawns++;
                        }
                    }
                }
            }
            if (team == 3)
                SendConsoleMessage($"CT Available spawns - {iAvailableSpawns}", ConsoleColor.Blue);
            if (team == 2)
                SendConsoleMessage($"T Available spawns - {iAvailableSpawns}", ConsoleColor.DarkYellow);
            return iAvailableSpawns;
        }*/
        public static void RemoveMapDefaulSpawns()
        {
            if (!g_bDefaultMapSpawnDisabled)
            {
                int iDefaultCTSpawns = 0;
                int iDefaultTSpawns = 0;
                var ctSpawns = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_counterterrorist");
                foreach (var entity in ctSpawns)
                {
                    if (entity.IsValid)
                    {
                        entity.AcceptInput("SetDisabled");
                        iDefaultCTSpawns++;
                    }
                }
                var tSpawns = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_terrorist");
                foreach (var entity in tSpawns)
                {
                    if (entity.IsValid)
                    {
                        entity.AcceptInput("SetDisabled");
                        iDefaultTSpawns++;
                    }
                }
                SendConsoleMessage($"[Deathmatch] Total {iDefaultTSpawns} T and {iDefaultCTSpawns} CT default Spawns disabled!", ConsoleColor.Green);
                g_bDefaultMapSpawnDisabled = true;
                CreateCustomMapSpawns();
            }
        }
        public static void CreateCustomMapSpawns()
        {
            foreach (var spawn in spawnPositionsCT)
            {
                var position = ParseVector(spawn.Key);
                var angle = ParseQAngle(spawn.Value);
                var entity = Utilities.CreateEntityByName<CInfoPlayerTerrorist>("info_player_counterterrorist");
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
                var entity = Utilities.CreateEntityByName<CInfoPlayerTerrorist>("info_player_terrorist");
                if (entity == null)
                {
                    SendConsoleMessage($"[Deathmatch] Failed to create spawn point for T", ConsoleColor.DarkYellow);
                    return;
                }
                entity.Teleport(position, angle, new Vector(0, 0, 0));
                entity.DispatchSpawn();
            }
        }
        public static void LoadMapSpawns(string filepath, bool mapstart)
        {
            spawnPositionsCT.Clear();
            spawnPositionsT.Clear();

            if (!File.Exists(filepath))
            {
                SendConsoleMessage($"[Deathmatch] No spawn points found for this map! (Deathmatch/spawns/{Server.MapName}.json)", ConsoleColor.Red);
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
}