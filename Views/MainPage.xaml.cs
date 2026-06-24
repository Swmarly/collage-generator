using CollageGenerator.Models;
using CollageGenerator.Services;
using CollageGenerator.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace CollageGenerator.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; } = new();
    private readonly CollageExportService _export = new();

    public MainPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => { await ViewModel.InitializeAsync(); DrawPreview(); };
        ViewModel.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(ViewModel.CurrentLayout)) _ = DispatcherQueue.TryEnqueue(DrawPreview); };
    }

    private nint WindowHandle => WindowNative.GetWindowHandle(App.MainAppWindow);

    private async void SelectFiles_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker { ViewMode = PickerViewMode.Thumbnail, SuggestedStartLocation = PickerLocationId.PicturesLibrary };
        foreach (var ext in ImageLibraryService.SupportedExtensions) picker.FileTypeFilter.Add(ext);
        InitializeWithWindow.Initialize(picker, WindowHandle);
        var files = await picker.PickMultipleFilesAsync();
        await ViewModel.AddImagesAsync(files.Select(f => f.Path), ImageSourceKind.Manual);
    }

    private async Task<string?> PickFolderAsync()
    {
        var picker = new FolderPicker { SuggestedStartLocation = PickerLocationId.PicturesLibrary };
        picker.FileTypeFilter.Add("*");
        InitializeWithWindow.Initialize(picker, WindowHandle);
        return (await picker.PickSingleFolderAsync())?.Path;
    }

    private async void LoadFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = await PickFolderAsync();
        if (folder is not null) await ViewModel.AddFolderAsync(folder, ViewModel.IncludeSubfolders, ImageSourceKind.Manual);
    }

    private async void RandomFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = await PickFolderAsync();
        if (folder is not null) await ViewModel.RandomizeImagesAsync(folder, ViewModel.IncludeSubfolders);
    }

    private async void AddRandomExcludedSubfolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = await PickFolderAsync();
        if (folder is not null) ViewModel.AddRandomExcludedSubfolder(folder);
    }

    private void RemoveRandomExcludedSubfolder_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is string folder) ViewModel.RemoveRandomExcludedSubfolder(folder);
    }

    private void ClearRandomExcludedSubfolders_Click(object sender, RoutedEventArgs e) => ViewModel.ClearRandomExcludedSubfolders();
    private void ClearRandom_Click(object sender, RoutedEventArgs e) => ViewModel.ClearRandomSelection();
    private void ClearAll_Click(object sender, RoutedEventArgs e) { ViewModel.SelectedImages.Clear(); ViewModel.RegenerateLayout(); }
    private void Shuffle_Click(object sender, RoutedEventArgs e) => ViewModel.Shuffle();
    private void Regenerate_Click(object sender, RoutedEventArgs e) => ViewModel.RegenerateLayout();

    private void RemoveImage_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is ImageItem item) { ViewModel.SelectedImages.Remove(item); ViewModel.RegenerateLayout(); }
    }

    private async void AddFavorite_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.RandomFolder is null) ViewModel.Status = "Select or randomize a folder before adding a favorite.";
        else await ViewModel.AddFavoriteAsync(ViewModel.RandomFolder);
    }

    private async void UseFavorite_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is string folder) await ViewModel.AddFolderAsync(folder, ViewModel.IncludeSubfolders, ImageSourceKind.Manual);
    }

    private async void RemoveFavorite_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is string folder) await ViewModel.RemoveFavoriteAsync(folder);
    }

    private void BackgroundBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if ((BackgroundBox.SelectedItem as ComboBoxItem)?.Content is string name) ViewModel.BackgroundColorName = name;
    }

    private void OrderModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if ((OrderModeBox.SelectedItem as ComboBoxItem)?.Tag is string tag && Enum.TryParse<ImageOrderMode>(tag, out var mode)) ViewModel.OrderMode = mode;
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentLayout is null || ViewModel.SelectedImages.Count == 0) { ViewModel.Status = "Select at least one image before exporting."; return; }
        var picker = new FileSavePicker { SuggestedStartLocation = PickerLocationId.PicturesLibrary, SuggestedFileName = "collage" };
        picker.FileTypeChoices.Add("PNG image", new List<string> { ".png" });
        picker.FileTypeChoices.Add("JPEG image", new List<string> { ".jpg" });
        picker.FileTypeChoices.Add("Bitmap image", new List<string> { ".bmp" });
        InitializeWithWindow.Initialize(picker, WindowHandle);
        var file = await picker.PickSaveFileAsync();
        if (file is null) return;
        await _export.ExportAsync(ViewModel.CurrentLayout, new LayoutOptions(ViewModel.CollageWidth, ViewModel.CollageHeight, ViewModel.Spacing, ViewModel.Margin, ViewModel.GetBackgroundColor(), ViewModel.OrderMode, Environment.TickCount), file.Path);
        ViewModel.Status = $"Exported {file.Path}";
    }

    private void PreviewCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => DrawPreview();

    private async void DrawPreview()
    {
        PreviewCanvas.Children.Clear();
        PreviewCanvas.Background = new SolidColorBrush(ViewModel.GetBackgroundColor());
        var layout = ViewModel.CurrentLayout;
        if (layout is null) return;
        var scale = Math.Min(Math.Max(1, PreviewCanvas.ActualWidth) / layout.Width, Math.Max(1, PreviewCanvas.ActualHeight) / layout.Height);
        foreach (var rect in layout.Rects)
        {
            var image = new Image { Width = rect.Width * scale, Height = rect.Height * scale, Stretch = Stretch.Uniform, Source = new BitmapImage(new Uri(rect.Image.FilePath)) };
            Canvas.SetLeft(image, rect.X * scale);
            Canvas.SetTop(image, rect.Y * scale);
            PreviewCanvas.Children.Add(image);
        }
        await Task.CompletedTask;
    }
}
