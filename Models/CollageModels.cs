using Microsoft.UI;
using Windows.UI;

namespace CollageGenerator.Models;

public enum ImageSourceKind { Manual, Random }
public enum ImageOrderMode { Manual, Automatic, Random }

public sealed record ResolutionPreset(string Name, int Width, int Height)
{
    public static IReadOnlyList<ResolutionPreset> Defaults { get; } = new[]
    {
        new ResolutionPreset("1:1 square (2000 × 2000)", 2000, 2000),
        new ResolutionPreset("4:3 standard (2400 × 1800)", 2400, 1800),
        new ResolutionPreset("3:2 photo (3000 × 2000)", 3000, 2000),
        new ResolutionPreset("16:9 widescreen (3840 × 2160)", 3840, 2160),
        new ResolutionPreset("9:16 portrait (2160 × 3840)", 2160, 3840),
        new ResolutionPreset("A4 portrait 300 DPI (2480 × 3508)", 2480, 3508),
        new ResolutionPreset("A4 landscape 300 DPI (3508 × 2480)", 3508, 2480),
        new ResolutionPreset("Custom", 2000, 2000)
    };
}

public sealed class ImageItem
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public required ImageSourceKind SourceKind { get; set; }
    public int PixelWidth { get; set; }
    public int PixelHeight { get; set; }
    public bool IsLocked { get; set; }
    public double AspectRatio => PixelHeight <= 0 ? 1 : (double)PixelWidth / PixelHeight;
}

public sealed record LayoutOptions(int Width, int Height, double Spacing, double Margin, Color Background, ImageOrderMode OrderMode, int Seed);
public sealed record LayoutRect(ImageItem Image, double X, double Y, double Width, double Height);
public sealed record CollageLayout(int Width, int Height, IReadOnlyList<LayoutRect> Rects, double EmptyAreaRatio);
