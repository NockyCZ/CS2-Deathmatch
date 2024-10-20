using CounterStrikeSharp.API.Modules.Utils;
using DeathmatchAPI;
using DeathmatchAPI.Events;
using DeathmatchAPI.Helpers;

namespace Deathmatch;

public partial class Deathmatch : IDeathmatchAPI
{
    public event EventHandler<IDeathmatchEventsAPI>? DeathmatchEventHandlers;
    public void TriggerEvent(IDeathmatchEventsAPI @event)
    {
        DeathmatchEventHandlers?.Invoke(this, @event);
    }

    public void RegisterNewPreference(PreferencesData preferencesData)
    {
        Preferences.Add(preferencesData);
    }

    public void StartCustomMode(int modeId)
    {
        if (!Config.CustomModes.ContainsKey(modeId.ToString()))
            throw new Exception($"A Custom mode with ID '{modeId}' cannot be started, because this mode does not exist!");

        SetupCustomMode(modeId.ToString());
    }

    public void ChangeNextMode(int modeId)
    {
        if (!Config.CustomModes.ContainsKey(modeId.ToString()))
            throw new Exception($"A Custom mode with ID '{modeId}' cannot be set as next mode, because this mode does not exist!");

        NextMode = modeId;
    }

    public void AddCustomMode(int modeId, ModeData mode)
    {
        if (Config.CustomModes.ContainsKey(modeId.ToString()))
            throw new Exception($"A Custom mode with ID '{modeId}' cannot be added, because this mode already exists!");

        Config.CustomModes.Add(modeId.ToString(), mode);
    }

    public void ChangeCheckDistance(int distance)
    {
        CheckedEnemiesDistance = distance;
    }

    public void SetupCustomSpawns(string team, Dictionary<Vector, QAngle> spawns)
    {
        if (team.Equals("ct"))
        {
            spawnPositionsCT.Clear();
            foreach (var spawn in spawns)
            {
                spawnPositionsCT.Add(spawn.Key, spawn.Value);
            }
        }
        else if (team.Equals("t"))
        {
            spawnPositionsT.Clear();
            foreach (var spawn in spawns)
            {
                spawnPositionsT.Add(spawn.Key, spawn.Value);
            }
        }
        else
        {
            throw new Exception($"Invalid team name '{team}'! Allowed options: ct , t");
        }
    }

    public void SwapHudMessageVisibility(bool visible)
    {
        VisibleHud = visible;
    }

    public int GetActiveModeId()
    {
        return int.Parse(ActiveCustomMode);
    }

    public int GetActiveModeRemainingTime()
    {
        return RemainingTime;
    }

    public Dictionary<string, ModeData> GetCustomModes()
    {
        return Config.CustomModes;
    }
}