using System;

namespace Shuffler.Core.Models;

public class SessionPlayer
{
    public required PlayerConfig Config { get; set; }
    public ShufflerController? AssignedController { get; internal set; }
    public TimeSpan TotalPlayTime { get; internal set; }
    public bool IsActive { get; internal set; } = true;
}