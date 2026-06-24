using CollageGenerator.Models;
using CollageGenerator.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI;
using System.Collections.ObjectModel;

namespace CollageGenerator.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ImageLibraryService _images = new();
    private readonly CollageLayoutService _layout = new();
    private readonly FolderSettingsService _settings = new();

    public ObservableCollection<ImageItem> SelectedImages { get; } = new();
    public ObservableCollection<string> FavoriteFolders { get; } = new();
    public ObservableCollection<string> RandomExcludedSubfolders { get; } = new();
    public IReadOnlyList<ResolutionPreset> Presets { get; } = ResolutionPreset.Defaults;

    [ObservableProperty] private ResolutionPreset selectedPreset = ResolutionPreset.Defaults[0];
    [ObservableProperty] private int collageWidth = 2000;
    [ObservableProperty] private int collageHeight = 2000;
    [ObservableProperty] private double spacing = 12;
    [ObservableProperty] private double margin = 24;
    [ObservableProperty] private ImageOrderMode orderMode = ImageOrderMode.Manual;
    [ObservableProperty] private string backgroundColorName = "White";
    [ObservableProperty] private bool includeSubfolders;
    [ObservableProperty] private int randomLimit = 10;
    [ObservableProperty] private string? randomFolder;
    [ObservableProperty] private string status = "Choose images to begin.";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private CollageLayout? currentLayout;

    public async Task InitializeAsync()
    {
        foreach (var folder in await _settings.LoadFavoritesAsync()) FavoriteFolders.Add(folder);
    }

    partial void OnSelectedPresetChanged(ResolutionPreset value)
    {
        if (value.Name != "Custom") { CollageWidth = value.Width; CollageHeight = value.Height; }
        RegenerateLayout();
    }
    partial void OnCollageWidthChanged(int value) => RegenerateLayout();
    partial void OnCollageHeightChanged(int value) => RegenerateLayout();
    partial void OnSpacingChanged(double value) => RegenerateLayout();
    partial void OnMarginChanged(double value) => RegenerateLayout();
    partial void OnOrderModeChanged(ImageOrderMode value) => RegenerateLayout();
    partial void OnBackgroundColorNameChanged(string value) => RegenerateLayout();

    public async Task AddImagesAsync(IEnumerable<string> paths, ImageSourceKind kind)
    {
        IsBusy = true;
        try
        {
            var unsupported = paths.Count(p => !_images.IsSupported(p));
            var items = await _images.CreateImageItemsAsync(paths, kind);
            foreach (var item in items.Where(i => SelectedImages.All(e => !StringComparer.OrdinalIgnoreCase.Equals(e.FilePath, i.FilePath)))) SelectedImages.Add(item);
            Status = unsupported > 0 ? $"Loaded {items.Count} images. Skipped {unsupported} unsupported files." : $"Loaded {items.Count} images.";
            RegenerateLayout();
        }
        finally { IsBusy = false; }
    }

    public async Task AddFolderAsync(string folder, bool recursive, ImageSourceKind kind)
    {
        IsBusy = true;
        try
        {
            var paths = await _images.ScanFolderAsync(folder, recursive);
            if (paths.Count == 0) { Status = "The folder contains no supported images."; return; }
            await AddImagesAsync(paths, kind);
            RandomFolder = folder;
        }
        catch (Exception ex) { Status = $"Could not load folder: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    public async Task RandomizeImagesAsync(string folder, bool recursive)
    {
        IsBusy = true;
        try
        {
            var paths = await _images.ScanFolderAsync(folder, recursive, excludedFolders: RandomExcludedSubfolders);
            if (paths.Count == 0) { Status = "Random selection found no supported images after applying subfolder exclusions."; return; }
            var chosen = paths.OrderBy(_ => Random.Shared.Next()).Take(Math.Max(1, RandomLimit)).ToList();
            ClearRandomSelection();
            await AddImagesAsync(chosen, ImageSourceKind.Random);
            RandomFolder = folder;
            if (paths.Count < RandomLimit) Status = $"Only {paths.Count} images were available for the requested limit of {RandomLimit} after applying subfolder exclusions.";
        }
        finally { IsBusy = false; }
    }

    public void AddRandomExcludedSubfolder(string folder)
    {
        if (!Directory.Exists(folder)) { Status = "Excluded subfolder is missing or inaccessible."; return; }
        if (!RandomExcludedSubfolders.Contains(folder, StringComparer.OrdinalIgnoreCase)) RandomExcludedSubfolders.Add(folder);
        Status = $"Excluded {RandomExcludedSubfolders.Count} subfolder(s) from random selection.";
    }

    public void RemoveRandomExcludedSubfolder(string folder)
    {
        RandomExcludedSubfolders.Remove(folder);
        Status = $"Excluded {RandomExcludedSubfolders.Count} subfolder(s) from random selection.";
    }

    public void ClearRandomExcludedSubfolders()
    {
        RandomExcludedSubfolders.Clear();
        Status = "Cleared random subfolder exclusions.";
    }

    public void ClearRandomSelection()
    {
        foreach (var item in SelectedImages.Where(i => i.SourceKind == ImageSourceKind.Random && !i.IsLocked).ToList()) SelectedImages.Remove(item);
        RegenerateLayout();
    }

    public async Task AddFavoriteAsync(string folder)
    {
        if (!Directory.Exists(folder)) { Status = "Favorite folder is missing or inaccessible."; return; }
        if (!FavoriteFolders.Contains(folder, StringComparer.OrdinalIgnoreCase)) FavoriteFolders.Add(folder);
        await _settings.SaveFavoritesAsync(FavoriteFolders);
    }

    public async Task RemoveFavoriteAsync(string folder)
    {
        FavoriteFolders.Remove(folder);
        await _settings.SaveFavoritesAsync(FavoriteFolders);
    }

    public void RegenerateLayout()
    {
        CurrentLayout = _layout.Generate(SelectedImages.ToList(), new LayoutOptions(Math.Max(1, CollageWidth), Math.Max(1, CollageHeight), Spacing, Margin, GetBackgroundColor(), OrderMode, Environment.TickCount));
        Status = SelectedImages.Count == 0 ? "No images selected." : $"{SelectedImages.Count} images selected. Empty space estimate: {CurrentLayout.EmptyAreaRatio:P1}.";
    }

    public Microsoft.UI.Color GetBackgroundColor() => BackgroundColorName switch
    {
        "Black" => Colors.Black,
        "Transparent" => Colors.Transparent,
        "Light gray" => Colors.LightGray,
        "Dark gray" => Colors.DarkGray,
        _ => Colors.White
    };

    public void Shuffle()
    {
        var shuffled = SelectedImages.OrderBy(_ => Random.Shared.Next()).ToList();
        SelectedImages.Clear();
        foreach (var item in shuffled) SelectedImages.Add(item);
        RegenerateLayout();
    }
}
