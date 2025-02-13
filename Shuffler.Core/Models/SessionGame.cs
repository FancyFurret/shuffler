using System;

namespace Shuffler.Core.Models;

public class SessionGame
{
    public required GameProcess Process { get; set; }
    public required GameConfig GameConfig { get; set; }
    public TimeSpan TotalPlayTime { get; internal set; }

    public SessionGame()
    {
        TotalPlayTime = TimeSpan.Zero;
    }
}