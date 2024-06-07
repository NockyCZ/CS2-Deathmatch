namespace DeathmatchAPI.Events;
public interface IDeathmatchEventsAPI
{
    public record OnCustomModeStarted(int modeId) : IDeathmatchEventsAPI;
}