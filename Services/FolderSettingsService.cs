using System.Text.Json;

namespace CollageGenerator.Services;

public sealed class FolderSettingsService
{
    private readonly string _settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CollageGenerator", "settings.json");

    public async Task<IList<string>> LoadFavoritesAsync()
    {
        if (!File.Exists(_settingsPath)) return new List<string>();
        await using var stream = File.OpenRead(_settingsPath);
        var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream);
        return settings?.FavoriteFolders?.Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>();
    }

    public async Task SaveFavoritesAsync(IEnumerable<string> folders)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        await using var stream = File.Create(_settingsPath);
        await JsonSerializer.SerializeAsync(stream, new AppSettings { FavoriteFolders = folders.Distinct(StringComparer.OrdinalIgnoreCase).ToList() }, new JsonSerializerOptions { WriteIndented = true });
    }

    private sealed class AppSettings
    {
        public List<string> FavoriteFolders { get; set; } = new();
    }
}
