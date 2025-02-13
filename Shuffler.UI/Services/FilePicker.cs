using System;
using System.Linq;
using System.Threading.Tasks;

namespace Shuffler.UI.Services;

public class FileType
{
    public string Name { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
}

public class FilePickerOptions
{
    public FileType[]? FileTypes { get; set; }
    public string? Title { get; set; }
    public string? InitialDirectory { get; set; }
}

public static class FilePicker
{
    public static Task<string?> PickFileAsync(FilePickerOptions options)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = options.Title ?? "Select File",
            InitialDirectory = options.InitialDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            CheckFileExists = true,
            CheckPathExists = true,
        };

        if (options.FileTypes?.Length > 0)
        {
            var filter = string.Join("|", options.FileTypes.Select(type =>
                $"{type.Name}|*.{type.Extension.TrimStart('.')}"));
            dialog.Filter = filter;
        }
        else
        {
            throw new ArgumentException("At least one file type must be specified");
        }

        var result = dialog.ShowDialog();
        return Task.FromResult(result == true ? dialog.FileName : null);
    }
}