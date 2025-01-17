
using DeathmatchAPI.Helpers;

namespace DeathmatchAPI.Events;
public interface IDeathmatchEventsAPI
{
    public record OnCustomModeStarted(int modeId, ModeData data) : IDeathmatchEventsAPI;
    public record OnSpawnPointsLoaded(List<SpawnData> spawns) : IDeathmatchEventsAPI;
}