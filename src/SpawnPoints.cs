using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CounterStrikeSharp.API;

namespace Deathmatch
{
    public partial class DeathmatchCore
    {
        public static List<Tuple<string, string>> spawnPositionsCT = new List<Tuple<string, string>>();
        public static List<Tuple<string, string>> spawnPositionsT = new List<Tuple<string, string>>();

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

                    Tuple<string, string> teamTuple = Tuple.Create(pos, angle);

                    if (teamType == "ct")
                    {
                        spawnPositionsCT.Add(teamTuple);
                    }
                    else if (teamType == "t")
                    {
                        spawnPositionsT.Add(teamTuple);
                    }
                }

                g_iTotalCTSpawns = spawnPositionsCT.Count;
                g_iTotalTSpawns = spawnPositionsT.Count;
                RemoveMapDefaulSpawns();
            }
        }
    }
}