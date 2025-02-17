using System.Runtime.InteropServices;
using Shuffler.Core.Models;

namespace Shuffler.Core;

public class GamepadManager : IDisposable
{
    private const int MaxControllers = 4;
    private readonly Dictionary<string, DateTime> _lastVibrationTime = new();
    private bool _isDisposed;

    [DllImport("xinput1_4.dll")]
    private static extern int XInputGetState(int dwUserIndex, ref XInputState state);

    [DllImport("xinput1_4.dll")]
    private static extern int XInputSetState(int dwUserIndex, ref XInputVibration vibration);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private const int VK_ESCAPE = 0x1B;

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputState
    {
        public uint dwPacketNumber;
        public XInputGamepad Gamepad;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputGamepad
    {
        public ushort wButtons;
        public byte bLeftTrigger;
        public byte bRightTrigger;
        public short sThumbLX;
        public short sThumbLY;
        public short sThumbRX;
        public short sThumbRY;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputVibration
    {
        public ushort wLeftMotorSpeed;
        public ushort wRightMotorSpeed;
    }

    public IEnumerable<ShufflerController> GetAvailableControllers(IEnumerable<SessionPlayer> allPlayers)
    {
        var allControllers = ListControllers();
        var assignedControllerIds = allPlayers
            .Where(p => p.AssignedController != null)
            .Select(p => p.AssignedController!.Id)
            .ToHashSet();

        return allControllers.Where(c => !assignedControllerIds.Contains(c.Id));
    }

    private IEnumerable<ShufflerController> ListControllers()
    {
        var controllers = new List<ShufflerController>();

        for (int i = 0; i < MaxControllers; i++)
        {
            var state = new XInputState();
            if (XInputGetState(i, ref state) == 0) // 0 = SUCCESS
            {
                controllers.Add(new ShufflerController
                {
                    Id = i.ToString(),
                    Name = $"XInput Controller {i + 1}",
                });
            }
        }

        return controllers;
    }

    public void Vibrate(ShufflerController controllerInfo, float intensity = 1.0f, int durationMs = 500)
    {
        if (!int.TryParse(controllerInfo.Id, out int controllerId))
            return;

        // Prevent rapid repeated vibrations
        if (_lastVibrationTime.TryGetValue(controllerInfo.Id, out var lastTime))
        {
            if ((DateTime.Now - lastTime).TotalMilliseconds < 100)
                return;
        }

        _lastVibrationTime[controllerInfo.Id] = DateTime.Now;

        // Convert intensity (0.0-1.0) to motor speed (0-65535)
        var motorSpeed = (ushort)(intensity * 65535);

        var vibration = new XInputVibration
        {
            wLeftMotorSpeed = motorSpeed,
            wRightMotorSpeed = motorSpeed
        };

        XInputSetState(controllerId, ref vibration);

        // Schedule vibration stop
        Task.Delay(durationMs).ContinueWith(_ =>
        {
            var stopVibration = new XInputVibration
            {
                wLeftMotorSpeed = 0,
                wRightMotorSpeed = 0
            };
            XInputSetState(controllerId, ref stopVibration);
        });
    }

    public async Task VibrateSuccess(ShufflerController controllerInfo)
    {
        if (!int.TryParse(controllerInfo.Id, out int controllerId))
            return;

        // Two quick pulses followed by a longer one
        var pattern = new[]
        {
            (intensity: 1.0f, duration: 100),
            (intensity: 0.0f, duration: 100),
            (intensity: 1.0f, duration: 100),
        };

        foreach (var (intensity, duration) in pattern)
        {
            var motorSpeed = (ushort)(intensity * 65535);
            var vibration = new XInputVibration
            {
                wLeftMotorSpeed = motorSpeed,
                wRightMotorSpeed = motorSpeed
            };

            XInputSetState(controllerId, ref vibration);
            await Task.Delay(duration);
        }

        // Ensure vibration is stopped
        var stopVibration = new XInputVibration
        {
            wLeftMotorSpeed = 0,
            wRightMotorSpeed = 0
        };
        XInputSetState(controllerId, ref stopVibration);
    }

    public async Task<ControllerPressResult?> WaitForButtonPress(CancellationToken cancellationToken)
    {
        var startTime = DateTime.Now;
        var timeout = TimeSpan.FromSeconds(5);

        while (!cancellationToken.IsCancellationRequested && DateTime.Now - startTime < timeout)
        {
            // Check for escape key
            if ((GetAsyncKeyState(VK_ESCAPE) & 0x8000) != 0)
                return null;

            for (int i = 0; i < MaxControllers; i++)
            {
                var state = new XInputState();
                if (XInputGetState(i, ref state) == 0) // 0 = SUCCESS
                {
                    var controller = new ShufflerController
                    {
                        Id = i.ToString(),
                        Name = $"XInput Controller {i + 1}"
                    };

                    // Check buttons in order of ControllerButton enum
                    if ((state.Gamepad.wButtons & 0x1000) != 0) return new(controller, ControllerButton.A);
                    if ((state.Gamepad.wButtons & 0x2000) != 0) return new(controller, ControllerButton.B);
                    if ((state.Gamepad.wButtons & 0x4000) != 0) return new(controller, ControllerButton.X);
                    if ((state.Gamepad.wButtons & 0x8000) != 0) return new(controller, ControllerButton.Y);
                    if ((state.Gamepad.wButtons & 0x0010) != 0) return new(controller, ControllerButton.Start);
                    if ((state.Gamepad.wButtons & 0x0020) != 0) return new(controller, ControllerButton.Select);
                    if ((state.Gamepad.wButtons & 0x0040) != 0) return new(controller, ControllerButton.LeftThumb);
                    if ((state.Gamepad.wButtons & 0x0080) != 0) return new(controller, ControllerButton.RightThumb);
                    if ((state.Gamepad.wButtons & 0x0100) != 0) return new(controller, ControllerButton.LeftShoulder);
                    if ((state.Gamepad.wButtons & 0x0200) != 0) return new(controller, ControllerButton.RightShoulder);
                    if (state.Gamepad.bLeftTrigger > 0) return new(controller, ControllerButton.LeftTrigger);
                    if (state.Gamepad.bRightTrigger > 0) return new(controller, ControllerButton.RightTrigger);
                }
            }

            await Task.Delay(50, cancellationToken); // Poll at 20Hz
        }

        return null;
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        // Stop any ongoing vibrations
        for (int i = 0; i < MaxControllers; i++)
        {
            try
            {
                var stopVibration = new XInputVibration
                {
                    wLeftMotorSpeed = 0,
                    wRightMotorSpeed = 0
                };
                XInputSetState(i, ref stopVibration);
            }
            catch
            {
                // ignored
            }
        }
    }
}
