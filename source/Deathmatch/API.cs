using CounterStrikeSharp.API;
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

    public void SetNextMode(int modeId)
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

    public void SetCheckEnemiesSpawnDistance(int distance)
    {
        CheckedEnemiesDistance = distance;
    }

    public void SetHudMessageVisibility(bool visible)
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

    public int GetDefaultCheckDistance()
    {
        return Config.Gameplay.DistanceRespawn;
    }

    public void SetupCustomSpawns(List<SpawnData> spawns, bool clearSpawnsDictionary)
    {
        if (clearSpawnsDictionary)
        {
            spawnPositionsCT.Clear();
            spawnPositionsT.Clear();
        }

        foreach (var data in spawns)
        {
            switch (data.Team)
            {
                case CsTeam.CounterTerrorist:
                    spawnPositionsCT[data.Position] = data.Angle;
                    break;
                case CsTeam.Terrorist:
                    spawnPositionsT[data.Position] = data.Angle;
                    break;
            }
        }
    }
}