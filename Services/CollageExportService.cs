using CollageGenerator.Models;
using Microsoft.Graphics.Canvas;


namespace CollageGenerator.Services;

public sealed class CollageExportService
{
    public async Task ExportAsync(CollageLayout layout, LayoutOptions options, string outputPath, IProgress<double>? progress = null)
    {
        if (layout.Rects.Count == 0) throw new InvalidOperationException("No images are selected.");
        using var device = CanvasDevice.GetSharedDevice();
        using var target = new CanvasRenderTarget(device, options.Width, options.Height, 96);
        using (var ds = target.CreateDrawingSession())
        {
            ds.Clear(options.Background);
            for (var i = 0; i < layout.Rects.Count; i++)
            {
                var rect = layout.Rects[i];
                using var bitmap = await CanvasBitmap.LoadAsync(device, rect.Image.FilePath);
                ds.DrawImage(bitmap, new Windows.Foundation.Rect(rect.X, rect.Y, rect.Width, rect.Height));
                progress?.Report((i + 1d) / layout.Rects.Count);
            }
        }
        var format = Path.GetExtension(outputPath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => CanvasBitmapFileFormat.Jpeg,
            ".bmp" => CanvasBitmapFileFormat.Bmp,
            ".webp" => CanvasBitmapFileFormat.Png,
            _ => CanvasBitmapFileFormat.Png
        };
        await target.SaveAsync(outputPath, format, 1f);
    }
}
