using System.Drawing;

namespace Shuffler.Core.Services;

public class ExecutableInfo
{
    public string? Base64Icon { get; set; }
}

public class ExecutableInfoService
{
    private readonly ILogger _logger;

    public ExecutableInfoService()
    {
        _logger = LogManager.Create(nameof(ExecutableInfoService));
    }

    public async Task<ExecutableInfo> GetExecutableInfoAsync(string exePath)
    {
        var info = new ExecutableInfo();

        try
        {
            if (!File.Exists(exePath))
            {
                return info;
            }

            await Task.Run(() =>
            {
                // Extract the largest icon from the exe (256x256)
                using var icon = Icon.ExtractIcon(exePath, 0, 256);
                if (icon != null)
                {
                    using var ms = new MemoryStream();
                    using var bitmap = icon.ToBitmap();
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    var bytes = ms.ToArray();
                    info.Base64Icon = $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
                }
                else
                {
                    // Fallback to associated icon if no icons found in resources
                    using var fallbackIcon = Icon.ExtractAssociatedIcon(exePath);
                    if (fallbackIcon != null)
                    {
                        using var ms = new MemoryStream();
                        using var bitmap = fallbackIcon.ToBitmap();
                        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        var bytes = ms.ToArray();
                        info.Base64Icon = $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error extracting icon from exe {exePath}: {ex.Message}");
        }

        return info;
    }
}