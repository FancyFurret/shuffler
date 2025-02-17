namespace Shuffler.Core.Models;

public class ShufflerSession
{
    public ShufflerPreset? Preset { get; internal set; }
    public List<SessionPlayer> Players { get; internal set; } = [];
    public List<SessionGame> Games { get; internal set; } = [];

    public SessionGame? CurrentGame { get; internal set; }
    public SessionPlayer? CurrentPlayer { get; internal set; }
    public SessionGame? NextGame { get; internal set; }
    public SessionPlayer? NextPlayer { get; internal set; }

    public DateTime? NextSwapTime { get; internal set; }
    public DateTime SessionStartTime { get; private set; }
    public TimeSpan TotalSessionTime => DateTime.Now - SessionStartTime;

    public Queue<SessionGame>? BaggedGames { get; set; }
    public Queue<SessionPlayer>? BaggedPlayers { get; set; }

    public ShufflerSession()
    {
        SessionStartTime = DateTime.Now;
    }
}