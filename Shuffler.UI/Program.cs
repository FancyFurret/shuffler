using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shuffler.Core;
using Shuffler.Core.Models;
using Shuffler.Core.Services;
using Shuffler.UI.Overlay;
using Shuffler.UI.Services;

namespace Shuffler.UI;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var services = new ServiceCollection();

        // Add Blazor services
        services.AddWindowsFormsBlazorWebView();

#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif

        // Register configuration service
        services.AddSingleton<ConfigurationService>();

        // Load configuration
        var form = new MainForm();
        var configService = new ConfigurationService();
        var config = configService.Load();
        services.AddSingleton(config);

        // Register other Shuffler services
        services.AddLogging(builder => builder.AddDebug());
        services.AddSingleton<ILogger<OverlayService>>(sp =>
            sp.GetRequiredService<ILoggerFactory>().CreateLogger<OverlayService>());
        services.AddSingleton<OverlayService>();
        services.AddSingleton<AvatarService>();
        services.AddSingleton<GameInfoService>();
        services.AddSingleton<GamepadManager>();
        services.AddSingleton<ShufflerCore>();
        services.AddScoped<ConfirmService>();
        services.AddSingleton<EditPresetService>();

        var serviceProvider = services.BuildServiceProvider();

        // Look up Steam App IDs for games that don't have them set
        var gameInfoService = serviceProvider.GetRequiredService<GameInfoService>();

        // Preload game info for all games
        Task.Run(() => gameInfoService.PreloadGamesAsync(config.Games)).ConfigureAwait(false);

        form.Init(serviceProvider);
        Application.Run(form);
    }
}