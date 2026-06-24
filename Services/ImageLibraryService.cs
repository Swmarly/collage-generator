using CollageGenerator.Models;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;
using System.Runtime.InteropServices.WindowsRuntime;

namespace CollageGenerator.Services;

public sealed class ImageLibraryService
{
    public static readonly string[] SupportedExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp" };

    public bool IsSupported(string path) => SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<string>> ScanFolderAsync(string folder, bool recursive, IProgress<int>? progress = null, CancellationToken token = default, IEnumerable<string>? excludedFolders = null)
    {
        if (!Directory.Exists(folder)) throw new DirectoryNotFoundException(folder);
        return await Task.Run(() =>
        {
            var files = new List<string>();
            var excluded = NormalizeExcludedFolders(excludedFolders);
            ScanDirectory(folder, recursive, excluded, files, progress, token);
            progress?.Report(files.Count);
            return (IReadOnlyList<string>)files;
        }, token);
    }

    private void ScanDirectory(string folder, bool recursive, IReadOnlySet<string> excludedFolders, List<string> files, IProgress<int>? progress, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (IsExcluded(folder, excludedFolders)) return;

        IEnumerable<string> localFiles;
        try
        {
            localFiles = Directory.EnumerateFiles(folder, "*.*").ToList();
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }
        catch (IOException)
        {
            return;
        }

        foreach (var file in localFiles)
        {
            token.ThrowIfCancellationRequested();
            if (IsSupported(file)) files.Add(file);
            if (files.Count % 25 == 0) progress?.Report(files.Count);
        }

        if (!recursive) return;

        IEnumerable<string> children;
        try
        {
            children = Directory.EnumerateDirectories(folder).ToList();
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }
        catch (IOException)
        {
            return;
        }

        foreach (var child in children)
        {
            ScanDirectory(child, true, excludedFolders, files, progress, token);
        }
    }

    private static IReadOnlySet<string> NormalizeExcludedFolders(IEnumerable<string>? excludedFolders)
    {
        return (excludedFolders ?? Array.Empty<string>())
            .Where(Directory.Exists)
            .Select(path => Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsExcluded(string folder, IReadOnlySet<string> excludedFolders)
    {
        var current = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return excludedFolders.Any(excluded => current.Equals(excluded, StringComparison.OrdinalIgnoreCase) || current.StartsWith(excluded + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<ImageItem>> CreateImageItemsAsync(IEnumerable<string> paths, ImageSourceKind kind, IProgress<int>? progress = null, CancellationToken token = default)
    {
        var results = new List<ImageItem>();
        foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            token.ThrowIfCancellationRequested();
            if (!IsSupported(path) || !File.Exists(path)) continue;
            var (width, height) = await GetDimensionsAsync(path);
            results.Add(new ImageItem { FilePath = path, FileName = Path.GetFileName(path), SourceKind = kind, PixelWidth = width, PixelHeight = height });
            progress?.Report(results.Count);
        }
        return results;
    }

    public async Task<(int Width, int Height)> GetDimensionsAsync(string path)
    {
        try
        {
            var file = await StorageFile.GetFileFromPathAsync(path);
            var props = await file.Properties.GetImagePropertiesAsync();
            if (props.Width > 0 && props.Height > 0) return ((int)props.Width, (int)props.Height);
        }
        catch { }
        return (1, 1);
    }

    public async Task<BitmapImage> LoadPreviewAsync(string path)
    {
        var bitmap = new BitmapImage { DecodePixelWidth = 240 };
        await using var stream = File.OpenRead(path);
        await bitmap.SetSourceAsync(stream.AsRandomAccessStream());
        return bitmap;
    }
}
