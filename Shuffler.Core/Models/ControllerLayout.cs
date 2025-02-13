using System.ComponentModel;

namespace Shuffler.Core.Models;

public enum ControllerButton
{
    A,
    B,
    X,
    Y,
    Start,
    Select,
    LeftThumb,
    RightThumb,
    LeftShoulder,
    RightShoulder,
    LeftTrigger,
    RightTrigger,
}

public static class ControllerButtonExtensions
{
    public static string GetDisplayName(this ControllerButton button) =>
        button switch
        {
            ControllerButton.A => "A",
            ControllerButton.B => "B",
            ControllerButton.X => "X",
            ControllerButton.Y => "Y",
            ControllerButton.Start => "Start",
            ControllerButton.Select => "Select",
            ControllerButton.LeftThumb => "Left Stick",
            ControllerButton.RightThumb => "Right Stick",
            ControllerButton.LeftShoulder => "Left Bumper",
            ControllerButton.RightShoulder => "Right Bumper",
            ControllerButton.LeftTrigger => "Left Trigger",
            ControllerButton.RightTrigger => "Right Trigger",
            _ => throw new InvalidEnumArgumentException(nameof(button), (int)button, typeof(ControllerButton))
        };
}

public record ControllerPressResult(ShufflerController Controller, ControllerButton Button);

public class ButtonRemap
{
    public ControllerButton Source { get; set; }
    public ControllerButton Target { get; set; }
}

public class ControllerLayout
{
    public string PlayerId { get; set; } = "";
    public string GameId { get; set; } = "";
    public List<ButtonRemap> Remaps { get; set; } = new();
}