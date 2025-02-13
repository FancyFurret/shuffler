using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;
using Shuffler.Core;
using System.Diagnostics;

namespace Shuffler.UI.Overlay;

public class UnfocusableForm : Form
{
    private const int WsExNoactivate = 0x08000000;
    private const int WsExToolwindow = 0x80;
    private const int WsExTopmost = 0x00000008;

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WsExNoactivate | WsExTopmost | WsExToolwindow;
            return cp;
        }
    }
}

public sealed class OverlayForm : UnfocusableForm
{
    private readonly IServiceProvider _serviceProvider;
    private readonly int _targetFrameTime;  // microseconds
    private readonly Thread _animationThread;
    private readonly CancellationTokenSource _animationCancellation = new();
    private double _opacity = 1.0;
    private DateTime _animationStartTime;
    private Point _startPosition;
    private Point _targetPosition;
    private BlazorWebView? _blazorWebView;
    private const double ANIMATION_DURATION = 1.75; // Total seconds to show
    private const double FADE_IN_DURATION = 0.3;
    private const double FADE_OUT_DURATION = 0.3;
    private const int DRIFT_DISTANCE = 100; // Pixels to drift right
    private volatile bool _isAnimating;

    public OverlayForm(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        // Configure form
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.Black;
        TransparencyKey = Color.Black;
        Size = new Size(2500, 1000);

        // Configure animation timing
        var config = serviceProvider.GetRequiredService<ShufflerConfig>();
        _targetFrameTime = (int)(1000000.0 / config.OverlayRefreshRate); // Convert Hz to microseconds
        _animationThread = new Thread(AnimationLoop) { IsBackground = true };
        _animationThread.Start();

        InitializeBlazorWebView();
    }

    private void InitializeBlazorWebView()
    {
        _blazorWebView = new BlazorWebView
        {
            Dock = DockStyle.Fill,
            HostPage = "wwwroot/index.html",
            Services = _serviceProvider,
            WebView =
            {
                DefaultBackgroundColor = Color.Transparent
            }
        };

        _blazorWebView.RootComponents.Add<Components.GameSwitchOverlay>("#app");
        Controls.Add(_blazorWebView);
    }

    private void AnimationLoop()
    {
        var stopwatch = new Stopwatch();
        var frameStopwatch = new Stopwatch();

        while (!_animationCancellation.Token.IsCancellationRequested)
        {
            if (!_isAnimating)
            {
                Thread.Sleep(1);
                continue;
            }

            frameStopwatch.Restart();
            var elapsed = (DateTime.Now - _animationStartTime).TotalSeconds;

            if (elapsed >= ANIMATION_DURATION)
            {
                Invoke(() =>
                {
                    _isAnimating = false;
                    Hide();
                });
                continue;
            }

            // Calculate opacity
            if (elapsed < FADE_IN_DURATION)
            {
                // Fade in
                _opacity = elapsed / FADE_IN_DURATION;
            }
            else if (elapsed > ANIMATION_DURATION - FADE_OUT_DURATION)
            {
                // Fade out
                _opacity = Math.Max(0, 1.0 - (elapsed - (ANIMATION_DURATION - FADE_OUT_DURATION)) / FADE_OUT_DURATION);
            }
            else
            {
                _opacity = 1.0;
            }

            // Calculate position and size
            Point newLocation;
            if (elapsed < FADE_IN_DURATION)
            {
                // Fly in from left
                var positionProgress = elapsed / FADE_IN_DURATION;
                positionProgress = 1 - Math.Pow(1 - positionProgress, 2); // Ease out
                newLocation = new Point(
                    (int)(_startPosition.X + (_targetPosition.X - _startPosition.X) * positionProgress),
                    _targetPosition.Y
                );
            }
            else if (elapsed > ANIMATION_DURATION - FADE_OUT_DURATION)
            {
                // Fly out to right
                var positionProgress = (elapsed - (ANIMATION_DURATION - FADE_OUT_DURATION)) / FADE_OUT_DURATION;
                positionProgress = Math.Pow(positionProgress, 2); // Ease in
                var baseX = _targetPosition.X + (DRIFT_DISTANCE * (elapsed - FADE_IN_DURATION) / (ANIMATION_DURATION - FADE_IN_DURATION - FADE_OUT_DURATION));
                newLocation = new Point(
                    (int)(baseX + positionProgress * Screen.PrimaryScreen!.Bounds.Width),
                    _targetPosition.Y
                );
            }
            else
            {
                // Drift right
                var driftProgress = (elapsed - FADE_IN_DURATION) / (ANIMATION_DURATION - FADE_IN_DURATION - FADE_OUT_DURATION);
                newLocation = new Point(
                    (int)(_targetPosition.X + (DRIFT_DISTANCE * driftProgress)),
                    _targetPosition.Y
                );
            }

            // Update UI on main thread
            Invoke(() =>
            {
                Location = newLocation;
                Opacity = _opacity;
            });

            // Frame pacing
            var frameTime = frameStopwatch.ElapsedTicks * 1000000L / Stopwatch.Frequency;
            if (frameTime < _targetFrameTime)
            {
                var sleepTime = (_targetFrameTime - frameTime) / 1000;  // Convert to milliseconds
                if (sleepTime > 0)
                    Thread.Sleep((int)sleepTime);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animationCancellation.Cancel();
            _animationCancellation.Dispose();
        }
        base.Dispose(disposing);
    }

    public void ShowGameSwitch(string currentGame, PlayerConfig currentPlayer, string? nextGame = null, PlayerConfig? nextPlayer = null, int? timeLeft = null, int? totalTime = null)
    {
        if (_blazorWebView?.RootComponents.Count > 0)
        {
            _blazorWebView.RootComponents.RemoveAt(0);
            _blazorWebView.RootComponents.Add<Components.GameSwitchOverlay>("#app", new Dictionary<string, object?>
            {
                { "CurrentGame", currentGame },
                { "CurrentPlayer", currentPlayer },
                { "NextGame", nextGame },
                { "NextPlayer", nextPlayer ?? new PlayerConfig() },
                { "TimeLeft", timeLeft ?? 0 },
                { "TotalTime", totalTime ?? 300 },
                { "ShowNextGame", nextGame != null }
            });
        }

        // Calculate positions
        var screen = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
        _targetPosition = new Point(
            (screen.Width - Width) / 2,
            (screen.Height - Height) / 2
        );
        _startPosition = new Point(
            -Width,  // Start off-screen to the left
            _targetPosition.Y
        );

        // Reset animation state
        Location = _startPosition;
        Opacity = 0;
        _opacity = 0;
        _animationStartTime = DateTime.Now;

        // Show and start animation
        Enabled = false;
        Show();
        _isAnimating = true;
    }
}