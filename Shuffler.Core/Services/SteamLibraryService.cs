using System.Text.RegularExpressions;

namespace Shuffler.Core.Services;

public class SteamLibraryService
{
    private readonly ILogger _logger;
    private readonly string[] _steamLibraryPaths;
    private readonly string? _steamPath;

    public SteamLibraryService(ShufflerConfig config)
    {
        _logger = LogManager.Create(nameof(SteamLibraryService));
        _steamPath = config.SteamPath;
        _steamLibraryPaths = GetSteamLibraryPaths();
    }

    private string[] GetSteamLibraryPaths()
    {
        try
        {
            var steamPath = _steamPath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Steam"
            );

            var libraryFoldersPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");

            if (!File.Exists(libraryFoldersPath))
                return new[] { Path.Combine(steamPath, "steamapps", "common") };

            var content = File.ReadAllText(libraryFoldersPath);
            var paths = new List<string> { Path.Combine(steamPath, "steamapps", "common") };

            // Simple regex to extract paths from VDF file
            var matches = Regex.Matches(content, @"""path""\s+""([^""]+)""");
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    var path = Path.Combine(match.Groups[1].Value.Replace("\\\\", "\\"), "steamapps", "common");
                    if (Directory.Exists(path))
                        paths.Add(path);
                }
            }

            return paths.ToArray();
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting Steam library paths: {ex.Message}");
            return Array.Empty<string>();
        }
    }

    public int? GetSteamAppIdFromExePath(string exePath)
    {
        try
        {
            var normalizedExePath = Path.GetFullPath(exePath);
            foreach (var libraryPath in _steamLibraryPaths)
            {
                if (!normalizedExePath.StartsWith(libraryPath, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Go up to the common folder's parent (steamapps)
                var steamappsPath = Path.GetDirectoryName(libraryPath)!;
                var manifestFiles = Directory.GetFiles(steamappsPath, "appmanifest_*.acf");

                foreach (var manifestFile in manifestFiles)
                {
                    var manifestContent = File.ReadAllText(manifestFile);
                    var installDirMatch = Regex.Match(manifestContent, @"""installdir""\s+""([^""]+)""");
                    if (!installDirMatch.Success) continue;

                    var gameFolder = installDirMatch.Groups[1].Value;
                    var gamePath = Path.Combine(libraryPath, gameFolder);

                    // Make sure we match the exact game folder, not a substring
                    var relativeExePath =
                        normalizedExePath[libraryPath.Length..].TrimStart(Path.DirectorySeparatorChar);
                    var gameRelativePath = gameFolder + Path.DirectorySeparatorChar;
                    if (relativeExePath.StartsWith(gameRelativePath, StringComparison.OrdinalIgnoreCase))
                    {
                        var appIdMatch = Regex.Match(Path.GetFileName(manifestFile), @"appmanifest_(\d+)\.acf");
                        return appIdMatch.Success ? int.Parse(appIdMatch.Groups[1].Value) : null;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting Steam AppId from exe path {exePath}: {ex.Message}");
        }

        return null;
    }
}