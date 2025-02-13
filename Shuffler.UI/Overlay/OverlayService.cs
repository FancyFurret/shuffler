using Microsoft.Extensions.Logging;
using Shuffler.Core;

namespace Shuffler.UI.Overlay;

public class OverlayService : IDisposable
{
    private readonly ILogger<OverlayService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private OverlayForm? _overlayForm;

    public OverlayService(ILogger<OverlayService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public void ShowGameSwitch(string currentGame, PlayerConfig currentPlayer, string? nextGame = null, PlayerConfig? nextPlayer = null, int? timeLeft = null, int? totalTime = null)
    {
        try
        {
            if (_overlayForm?.IsDisposed == true || _overlayForm == null)
            {
                _overlayForm = new OverlayForm(_serviceProvider);
            }

            _overlayForm.ShowGameSwitch(currentGame, currentPlayer, nextGame, nextPlayer, timeLeft, totalTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing game switch overlay");
        }
    }

    public void Dispose()
    {
        _overlayForm?.Dispose();
    }
}
