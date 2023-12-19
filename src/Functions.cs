using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
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
            if (!File.Exists(filepath)){
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
            foreach (var ctSpawn in spawnPositionsCT)
            {
                Vector3 playerPos = new Vector3(X, Y, Z);
                Vector3 ctSpawnPosition = ParseVector3(ctSpawn.Item1);
                float distance = Vector3.Distance(playerPos, ctSpawnPosition);

                if (distance < lowestDistance)
                {
                    lowestDistance = distance;
                    nearestSpawn = ctSpawn.Item1;
                }
            }
            foreach (var tSpawn in spawnPositionsT)
            {
                Vector3 playerPos = new Vector3(X, Y, Z);
                Vector3 tSpawnPosition = ParseVector3(tSpawn.Item1);
                float distance = Vector3.Distance(playerPos, tSpawnPosition);

                if (distance < lowestDistance)
                {
                    lowestDistance = distance;
                    nearestSpawn = tSpawn.Item1;
                }
            }
            bool isDeleted = RemoveSpawnPoint(ModuleDirectory + $"/spawns/{Server.MapName}.json", nearestSpawn);
            if(isDeleted){
                LoadMapSpawns(ModuleDirectory + $"/spawns/{Server.MapName}.json", false);
                RemoveBeams();
                ShowAllSpawnPoints();
                return $"The nearest Spawn point has been successfully deleted! {nearestSpawn}";
            }
            else{
                return "Spawn point cannot be deleted!";
            }
        }
        public void ShowAllSpawnPoints()
        {
            foreach (var ctTeam in spawnPositionsCT)
            {
                CBeam beam = Utilities.CreateEntityByName<CBeam>("beam")!;
                if (beam == null){
                    SendConsoleMessage($"[Deathmatch] Failed to create beam for CT", ConsoleColor.DarkYellow);
                    return;
                }

                Vector position = ParseVector(ctTeam.Item1);
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
            foreach (var tTeam in spawnPositionsT)
            {
                CBeam beam = Utilities.CreateEntityByName<CBeam>("beam")!;
                if (beam == null){
                    SendConsoleMessage($"[Deathmatch] Failed to create beam for T", ConsoleColor.DarkYellow);
                    return;
                }
                Vector position = ParseVector(tTeam.Item1);
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

        /*public void TeleportPlayer(CCSPlayerController player)
        {
            Random random = new Random();
            int iSpawn = random.Next(0, g_iTotalSpawns);

            Vector position = ParseVector(spawnPositions[iSpawn].Item1);
            QAngle angle = ParseQAngle(spawnPositions[iSpawn].Item2);
            //string[] angle = spawnPositions[iSpawn].Item2.Split(' ');

            /*float angleX = float.Parse(angle[0]);
            float angleY = float.Parse(angle[1]);
            float angleZ = float.Parse(angle[2]);
            Server.PrintToChatAll($"{iSpawn} | {position}  {angle}");
            player.Teleport(position, angle, new Vector(0, 0, 0));
        }*/

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
        public bool IsPlayerValid(CCSPlayerController player, bool alive = false)
        {
            if (player is null || !player.IsValid || !player.PlayerPawn.IsValid || player.IsBot || player.IsHLTV)
                return false;
            if(alive && player.PawnIsAlive){
                return true;
            }
            else if(alive && !player.PawnIsAlive){
                return false;
            }
            return true;
        }
        public int IsHaveWeapon(CCSPlayerController player)
        {
            if (player.PlayerPawn.Value == null || player.PlayerPawn.Value.WeaponServices == null) 
                return 3;

            foreach(var weapon in player.PlayerPawn.Value.WeaponServices.MyWeapons)
            {
                if (weapon != null && weapon.IsValid){
                    if(SecondaryWeaponsList.Contains(weapon.Value!.DesignerName)){
                        return 2;
                    }
                    else if(PrimaryWeaponsList.Contains(weapon.Value!.DesignerName)){
                        return 1;
                    }
                }
            }
            return 3;
        }
        public void RemoveEntities()
        {
            var bombSites = Utilities.FindAllEntitiesByDesignerName<CEntityInstance>("func_bomb_target");
            foreach(var site in bombSites) {
                if(site.IsValid) {
                    site.Remove();
                }
            }
            var buyZones = Utilities.FindAllEntitiesByDesignerName<CEntityInstance>("func_buyzone");
            foreach(var zone in buyZones) {
                if(zone.IsValid) {
                    zone.Remove();
                }
            }
        }
        public static void RemoveMapDefaulSpawns()
        {
            g_iDefaultCTSpawnsTeleported = 0;
            g_iDefaultTSpawnsTeleported = 0;
            bool bNewCT = true;
            bool bNewT = true;
            var ctSpawns = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_counterterrorist");
            foreach(var entity in ctSpawns) {
                if(entity.IsValid){
                    if(g_iDefaultCTSpawnsTeleported <= g_iTotalCTSpawns - 1){
                        Vector position = ParseVector(spawnPositionsCT[g_iDefaultCTSpawnsTeleported].Item1);
                        QAngle angle = ParseQAngle(spawnPositionsCT[g_iDefaultCTSpawnsTeleported].Item2);
                        entity.Teleport(position, angle, new Vector(0, 0, 0));
                    }
                    else{
                        bNewCT = false;
                    }
                    g_iDefaultCTSpawnsTeleported++;
                }
            }
            var tSpawns = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_terrorist");
            foreach(var entity in tSpawns) {
                if(entity.IsValid){
                    if(g_iDefaultTSpawnsTeleported <= g_iTotalTSpawns - 1){
                        Vector position = ParseVector(spawnPositionsT[g_iDefaultTSpawnsTeleported].Item1);
                        QAngle angle = ParseQAngle(spawnPositionsT[g_iDefaultTSpawnsTeleported].Item2);
                        entity.Teleport(position, angle, new Vector(0, 0, 0));
                    }
                    else{
                        bNewT = false;
                    }
                    g_iDefaultTSpawnsTeleported++;
                }
            }
            CreateRemainingMapSpawns(bNewCT, bNewT);
        }
        public static void CreateRemainingMapSpawns(bool addCT, bool addT)
        {
            if(addCT){
                for (int i = g_iDefaultCTSpawnsTeleported; i < g_iTotalCTSpawns; i++)
                {
                    Vector position = ParseVector(spawnPositionsCT[i].Item1);
                    QAngle angle = ParseQAngle(spawnPositionsCT[i].Item2);
                    var entity = Utilities.CreateEntityByName<CInfoPlayerTerrorist>("info_player_counterterrorist");
                    if (entity == null){
                        SendConsoleMessage($"[Deathmatch] Failed to create spawn point for CT", ConsoleColor.DarkYellow);
                        return;
                    }
                    entity.Teleport(position, angle, new Vector(0, 0, 0));
                    entity.DispatchSpawn();
                }
            }
            else{
                SendConsoleMessage($"[Deathmatch] Not enough spawn points for CT! Add more! ({g_iTotalCTSpawns} / {g_iDefaultCTSpawnsTeleported})", ConsoleColor.Yellow);
            }
            if(addT){
                for (int i = g_iDefaultTSpawnsTeleported; i < g_iTotalTSpawns; i++)
                {
                    Console.WriteLine($" full new spawn t - {i}");
                    Vector position = ParseVector(spawnPositionsT[i].Item1);
                    QAngle angle = ParseQAngle(spawnPositionsT[i].Item2);
                    var entity = Utilities.CreateEntityByName<CInfoPlayerTerrorist>("info_player_terrorist");
                    if (entity == null){
                        SendConsoleMessage($"[Deathmatch] Failed to create spawn point for T", ConsoleColor.DarkYellow);
                        return;
                    }
                    entity.Teleport(position, angle, new Vector(0, 0, 0));
                    entity.DispatchSpawn();
                }
            }
            else{
                SendConsoleMessage($"[Deathmatch] Not enough spawn points for T! Add more! ({g_iTotalTSpawns} / {g_iDefaultTSpawnsTeleported})", ConsoleColor.Yellow);
            }
        }
        public void RemoveBeams()
        {
            var beams = Utilities.FindAllEntitiesByDesignerName<CEntityInstance>("beam");
            foreach(var beam in beams) {
                if(beam.IsValid) {
                    beam.Remove();
                }
            }
        }
        public int GetRandomModeType()
        {
            if(Config.g_bCustomModes)
            {
                Random random = new Random();
                int iRandomMode;
                do{
                    iRandomMode = random.Next(0, g_iTotalModes);
                } while (iRandomMode == g_iActiveMode);
                return iRandomMode;
            }
            return 0;
        }
        public static void SendConsoleMessage(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }
        internal static CCSGameRules GameRules()
        {
            return Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;
        }
    }
}