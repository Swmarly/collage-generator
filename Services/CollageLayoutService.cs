using CollageGenerator.Models;

namespace CollageGenerator.Services;

public sealed class CollageLayoutService
{
    public CollageLayout Generate(IReadOnlyList<ImageItem> source, LayoutOptions options)
    {
        if (source.Count == 0) return new CollageLayout(options.Width, options.Height, Array.Empty<LayoutRect>(), 1);
        var images = Order(source, options).ToList();
        var usableWidth = Math.Max(1, options.Width - 2 * options.Margin);
        var best = BuildRows(images, options, 1);
        var maxRows = Math.Min(images.Count, Math.Max(1, (int)Math.Sqrt(images.Count) * 3 + 4));
        for (var rows = 1; rows <= maxRows; rows++)
        {
            var candidate = BuildRows(images, options, rows);
            if (candidate.EmptyAreaRatio < best.EmptyAreaRatio) best = candidate;
        }
        return best;
    }

    private static IEnumerable<ImageItem> Order(IReadOnlyList<ImageItem> images, LayoutOptions options)
    {
        var list = images.ToList();
        if (options.OrderMode == ImageOrderMode.Automatic) return list.OrderByDescending(i => i.AspectRatio);
        if (options.OrderMode == ImageOrderMode.Random) return list.OrderBy(_ => Random.Shared.Next());
        return list;
    }

    private static CollageLayout BuildRows(IReadOnlyList<ImageItem> images, LayoutOptions options, int targetRows)
    {
        var groups = Enumerable.Range(0, targetRows).Select(_ => new List<ImageItem>()).ToList();
        for (var i = 0; i < images.Count; i++) groups[Math.Min(targetRows - 1, i * targetRows / images.Count)].Add(images[i]);
        groups.RemoveAll(g => g.Count == 0);

        var usableWidth = Math.Max(1, options.Width - 2 * options.Margin);
        var usableHeight = Math.Max(1, options.Height - 2 * options.Margin);
        var rowHeights = groups.Select(g => (usableWidth - options.Spacing * Math.Max(0, g.Count - 1)) / g.Sum(i => i.AspectRatio)).ToList();
        var scale = usableHeight / Math.Max(1, rowHeights.Sum() + options.Spacing * Math.Max(0, groups.Count - 1));
        rowHeights = rowHeights.Select(h => h * scale).ToList();

        var rects = new List<LayoutRect>();
        var y = options.Margin;
        var usedArea = 0d;
        for (var r = 0; r < groups.Count; r++)
        {
            var row = groups[r];
            var h = rowHeights[r];
            var rowWidth = row.Sum(i => i.AspectRatio * h) + options.Spacing * Math.Max(0, row.Count - 1);
            var x = options.Margin + Math.Max(0, (usableWidth - rowWidth) / 2);
            foreach (var image in row)
            {
                var w = image.AspectRatio * h;
                rects.Add(new LayoutRect(image, x, y, w, h));
                usedArea += w * h;
                x += w + options.Spacing;
            }
            y += h + options.Spacing;
        }
        var empty = Math.Clamp(1 - usedArea / Math.Max(1, usableWidth * usableHeight), 0, 1);
        return new CollageLayout(options.Width, options.Height, rects, empty);
    }
}
